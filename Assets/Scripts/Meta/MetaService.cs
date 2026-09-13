using System;
using System.Linq;

namespace BackpackRTS.Meta {
    // Local validation adapter: CompleteBattle is NOT an authoritative online victory verifier.
    public sealed class MetaService {
        readonly MetaRules rules; readonly IMetaRepository repository;
        readonly IMetaCodec codec; readonly IMetaClock clock; readonly IMetaRandom random;
        public MetaService(MetaRules rules, IMetaRepository repository, IMetaCodec codec, IMetaClock clock, IMetaRandom random) {
            this.rules=rules; this.repository=repository; this.codec=codec; this.clock=clock; this.random=random;
        }
        public MetaState Read() { var state=repository.Load(); rules.Validate(state); return state; }
        static void Need(bool condition,string error) { if(!condition)throw new MetaError(error); }
        MetaResult Execute(string id,long expectedRevision,string payload,Func<MetaState,Receipt> action) {
            try {
                Need(!string.IsNullOrWhiteSpace(id) && id.Length<=128,"invalid_command_id");
                var state=Read(); var previous=state.receipts.FirstOrDefault(x=>x.id==id);
                if(previous!=null) {
                    Need(previous.payload==payload,"command_id_reused");
                    return new MetaResult { success=true, replayed=true, revision=previous.revision, chestId=previous.chestId, loot=previous.loot };
                }
                Need(state.revision==expectedRevision,"revision_conflict");
                var next=codec.Decode<MetaState>(codec.Encode(state));
                var receipt=action(next); next.revision=checked(state.revision+1);
                receipt.id=id; receipt.payload=payload; receipt.revision=next.revision; receipt.at=clock.UtcSeconds;
                next.receipts.Add(receipt);
                if(next.receipts.Count>128)next.receipts.RemoveAt(0);
                rules.Validate(next); repository.Save(next,state.revision);
                return new MetaResult { success=true, revision=next.revision, chestId=receipt.chestId, loot=receipt.loot };
            } catch(MetaError ex) { return new MetaResult { error=ex.Message }; }
            catch(System.IO.IOException) { return new MetaResult { error="storage_failure" }; }
            catch(UnauthorizedAccessException) { return new MetaResult { error="storage_failure" }; }
            catch(OverflowException) { return new MetaResult { error="quantity_overflow" }; }
        }
        // Length-prefixing prevents ambiguities in user-controlled identifiers.
        static string Key(params string[] values) { return string.Concat(values.Select(v=>(v??"").Length+":"+(v??""))); }
        public MetaResult BeginBattle(string command,long revision,string battleId,string race,int stage,bool acceptFullSlots) {
            return Execute(command,revision,Key("begin",battleId,race,stage.ToString(),acceptFullSlots.ToString()),s=> {
                MetaRules.Race(race); Need(!string.IsNullOrWhiteSpace(battleId) && battleId.Length<=128,"invalid_battle_id");
                // Only chapter 1 is playable in this foundation. Chapter 2 pools are data-ready.
                Need(stage>=1 && stage<=12,"stage_not_available");
                var progress=s.factions.Single(x=>x.race==race);
                Need(stage==1 || progress.cleared.Contains(stage-1),"stage_locked");
                Need(s.battle==null || s.battle.expiresAt<=clock.UtcSeconds,"battle_in_progress");
                bool eligible=s.chests.Count<5; Need(eligible || acceptFullSlots,"confirm_full_slots");
                s.battle=new BattleTicket { id=battleId,race=race,stage=stage,elite=stage%6==0,eligible=eligible,
                    pool=rules.Pool(race,stage),poolVersion="chapter1-v1",economyVersion=rules.Data.version,expiresAt=checked(clock.UtcSeconds+1800) };
                return new Receipt();
            });
        }
        public MetaResult CompleteBattle(string command,long revision,string battleId,bool won) {
            return Execute(command,revision,Key("complete",battleId,won.ToString()),s=> {
                var battle=s.battle; Need(battle!=null && battle.id==battleId,"battle_not_active");
                Need(clock.UtcSeconds<battle.expiresAt,"battle_expired");
                Need(battle.economyVersion==rules.Data.version,"economy_version_unavailable");
                var receipt=new Receipt();
                if(won) {
                    var progress=s.factions.Single(x=>x.race==battle.race);
                    if(!progress.cleared.Contains(battle.stage))progress.cleared.Add(battle.stage);
                    if(battle.eligible) {
                        Need(s.chests.Count<5,"reservation_conflict");
                        int rarity=rules.RollRarity(battle.elite,random);
                        // The loot is persisted with the chest; opening never rolls again.
                        var chest=new ChestState { id=Guid.NewGuid().ToString("N"),stage=battle.stage,race=battle.race,
                            rarity=rarity,awardedAt=clock.UtcSeconds,economyVersion=battle.economyVersion,poolVersion=battle.poolVersion,
                            loot=rules.RollLoot(rarity,battle.race,battle.pool,random) };
                        s.chests.Add(chest); receipt.chestId=chest.id;
                    }
                }
                s.battle=null; return receipt;
            });
        }
        public MetaResult StartChestUnlock(string command,long revision,string chestId) {
            return Execute(command,revision,Key("start",chestId),s=> {
                var chest=s.chests.FirstOrDefault(x=>x.id==chestId); Need(chest!=null,"chest_missing");
                Need(!chest.started,"already_started");
                Need(!s.chests.Any(x=>x.started && x.readyAt>clock.UtcSeconds),"unlock_in_progress");
                // Timing is frozen at start. Previously awarded versions require their definition.
                Need(chest.economyVersion==rules.Data.version,"economy_version_unavailable");
                chest.started=true; chest.readyAt=checked(clock.UtcSeconds+rules.Data.chests[chest.rarity].minutes*60L);
                return new Receipt { chestId=chest.id };
            });
        }
        public MetaResult ClaimChest(string command,long revision,string chestId) {
            return Execute(command,revision,Key("claim",chestId),s=> {
                var chest=s.chests.FirstOrDefault(x=>x.id==chestId); Need(chest!=null,"chest_missing");
                Need(chest.started && clock.UtcSeconds>=chest.readyAt,"chest_not_ready");
                Need(chest.economyVersion==rules.Data.version,"economy_version_unavailable");
                rules.Grant(s,chest.loot); s.chests.Remove(chest);
                return new Receipt { chestId=chest.id,loot=chest.loot };
            });
        }
    }
}
