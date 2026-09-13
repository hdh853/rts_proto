using System;
using System.Collections.Generic;
using System.Linq;

namespace BackpackRTS.Meta {
    public sealed partial class MetaRules {
        public readonly MetaDefinition Data;
        public MetaRules(MetaDefinition data) { Data = data; ValidateDefinition(); }
        static void Need(bool value, string error) { if (!value) throw new MetaError(error); }
        public GrowthCard Card(string id) { var c = Data.cards.FirstOrDefault(x => x.id == id); if(c == null) throw new MetaError("unknown_card"); return c; }
        public static void Race(string race) { Need(race == "Human" || race == "Orc", "invalid_race"); }
        public string[] DefaultDeck(string race) { Race(race); return Enumerable.Range(1,4).Select(n => (race == "Human" ? "H" : "O") + n.ToString("00")).Concat(new[]{"mine","tower","fence","fire"}).ToArray(); }
        public string[] Pool(string race, int stage) { Race(race); return stage <= 12 ? DefaultDeck(race) : Data.cards.Where(c=>c.race==race || c.race=="Shared").Select(c=>c.id).ToArray(); }
        public GrowthCost Cost(CardState card) { Card(card.id); return Data.levels.FirstOrDefault(c=>c.rank==card.rank && c.fromLevel==card.level); }
        public void ValidateDefinition() {
            Need(Data != null && !string.IsNullOrEmpty(Data.version), "invalid_definition");
            Need(Data.levelStatIncrement>=0 && !double.IsInfinity(Data.levelStatIncrement),"invalid_growth_increment");
            Need(Data.cards != null && Data.cards.Length==20 && Data.cards.Select(x=>x.id).Distinct().Count()==20, "invalid_cards");
            Need(Data.chests != null && Data.chests.Length==5 && Data.evolutionCosts != null && Data.evolutionCosts.SequenceEqual(new[]{1,3,8,20}), "invalid_economy");
            foreach(var weights in new[]{Data.normalWeights,Data.eliteWeights}) Need(weights!=null && weights.Length==5 && weights.All(w=>w>=0) && Math.Abs(weights.Sum()-1)<1e-9, "invalid_weights");
            Need(Data.levels!=null && Data.levels.Length==40 && Data.levels.Select(x=>x.fromLevel).Distinct().Count()==40,"invalid_levels");
            int[] start={1,4,9,16,26}, end={4,9,16,26,41};
            for(int rank=0;rank<5;rank++) {
                var costs=Data.levels.Where(x=>x.rank==rank).OrderBy(x=>x.fromLevel).ToArray();
                Need(costs.Select(x=>x.fromLevel).SequenceEqual(Enumerable.Range(start[rank],end[rank]-start[rank])),"missing_level_cost");
                Need(costs.All(x=>x.copies>0 && x.stones>0),"invalid_cost");
            }
            foreach(var card in Data.cards) Need((card.race=="Human" || card.race=="Orc" || card.race=="Shared") && card.maxRank>=0 && card.maxRank<5 && card.maxLevel>start[card.maxRank] && card.maxLevel<=end[card.maxRank],"invalid_cap");
            Need(Data.cards.All(c=>new[]{"health","damage","duration","healthAndDamage"}.Contains(c.growthStatPolicy)),"invalid_growth_policy");
            foreach(var chest in Data.chests) Need(chest.minutes>0 && chest.bundles>0 && chest.bundles<=5 && chest.copies>=chest.bundles && chest.stones>=0 && new[]{chest.raceChance,chest.shardChance,chest.heroChance}.All(p=>p>=0 && p<=1) && chest.raceMin>=0 && chest.raceMax>=chest.raceMin && chest.shardMin>=0 && chest.shardMax>=chest.shardMin,"invalid_chest");
            Need(Data.heroUnlockShards>0 && Data.heroDuplicateShards>0 && Data.ownedHeroShardStoneConversion>0 && Data.maxedCardStoneConversion>0,"invalid_conversion");
        }
        public MetaState Create(LegacySave old = null) {
            if(old!=null) Need(old.schema==1 && old.stones>=0 && old.upgrades!=null && old.cleared!=null,"invalid_legacy_save");
            var s = new MetaState { stones=old==null?0:old.stones, migrated=old!=null };
            var starters = DefaultDeck("Human").Concat(DefaultDeck("Orc")).ToArray();
            foreach(var def in Data.cards) {
                var upgrade=old==null?null:old.upgrades.FirstOrDefault(x=>x.id==def.id);
                if(upgrade!=null) Need(upgrade.level>=1 && upgrade.level<=5,"invalid_legacy_level");
                int level=upgrade==null?1:1+(int)Math.Ceiling(1.5*(upgrade.level-1));
                s.cards.Add(new CardState { id=def.id, unlocked=old!=null || starters.Contains(def.id), level=level, rank=level>4?1:0 });
            }
            foreach(var race in new[]{"Human","Orc"}) {
                s.raceStones.Add(new RaceBalance { race=race });
                s.heroes.Add(new HeroState { id=race+"Hero01" });
                string[] previous=old==null?null:race=="Human"?old.humanDeck:old.orcDeck;
                var allowed=Pool(race,13);
                var deck=(previous??new string[0]).Where(allowed.Contains).Concat(DefaultDeck(race)).Distinct().Take(8).ToArray();
                s.factions.Add(new FactionState { race=race, deck=deck });
            }
            if(old!=null) s.legacyCleared=old.cleared.Distinct().ToList();
            Validate(s); return s;
        }
        public void Validate(MetaState s) {
            Need(s!=null && s.schema==2 && s.revision>=0 && s.stones>=0 && s.cards!=null && s.chests!=null && s.receipts!=null && s.factions!=null && s.raceStones!=null && s.heroes!=null && s.legacyCleared!=null,"invalid_save");
            Need(s.cards.All(x=>x!=null) && s.chests.All(x=>x!=null) && s.receipts.All(x=>x!=null) && s.factions.All(x=>x!=null) && s.raceStones.All(x=>x!=null) && s.heroes.All(x=>x!=null),"invalid_save_entries");
            Need(s.cards.Count==20 && s.cards.Select(c=>c.id).Distinct().Count()==20,"invalid_inventory");
            int[] start={1,4,9,16,26}, end={4,9,16,26,41};
            foreach(var c in s.cards) { var d=Card(c.id); Need(c.rank>=0 && c.rank<=d.maxRank && c.level>=start[c.rank] && c.level<=Math.Min(end[c.rank],d.maxLevel) && c.copies>=0,"invalid_growth"); }
            Need(s.raceStones.Count==2 && s.raceStones.Select(x=>x.race).Distinct().Count()==2 && s.raceStones.All(x=>(x.race=="Human" || x.race=="Orc") && x.amount>=0),"invalid_balances");
            Need(s.factions.Count==2 && s.factions.Select(x=>x.race).Distinct().Count()==2,"invalid_factions");
            foreach(var f in s.factions) { Race(f.race); Need(f.cleared!=null && f.deck!=null && f.deck.Length==8 && f.deck.Distinct().Count()==8 && f.deck.All(id=>s.cards.Any(c=>c.id==id && c.unlocked) && Pool(f.race,13).Contains(id)),"invalid_deck"); }
            Need(s.heroes.Count==2 && s.heroes.Select(x=>x.id).Distinct().Count()==2 && s.heroes.All(h=>(h.id=="HumanHero01" || h.id=="OrcHero01") && h.shards>=0 && h.shards<Data.heroUnlockShards && (!h.unlocked || h.shards==0)),"invalid_heroes");
            Need(s.chests.Count<=5 && s.chests.Select(c=>c.id).Distinct().Count()==s.chests.Count && s.receipts.Count<=128,"invalid_chests");
            Need(s.receipts.All(x=>!string.IsNullOrEmpty(x.id) && x.payload!=null && x.revision>0 && x.revision<=s.revision) && s.receipts.Select(x=>x.id).Distinct().Count()==s.receipts.Count,"invalid_receipts");
            foreach(var c in s.chests) {
                Race(c.race);
                Need(!string.IsNullOrEmpty(c.id) && c.rarity>=0 && c.rarity<5 && c.loot!=null && !string.IsNullOrEmpty(c.economyVersion) && !string.IsNullOrEmpty(c.poolVersion) && (!c.started || c.readyAt>=c.awardedAt),"invalid_chest_state");
                ValidateLoot(c.loot); Need(c.race==c.loot.race,"loot_race_mismatch");
            }
            if(s.battle!=null) {
                Race(s.battle.race); Need(!string.IsNullOrEmpty(s.battle.id) && s.battle.stage>0 && s.battle.pool!=null && s.battle.pool.Length>0 && s.battle.pool.All(id=>Pool(s.battle.race,13).Contains(id)),"invalid_battle_ticket");
            }
        }
        public int RollRarity(bool elite, IMetaRandom rng) {
            double value=rng.Next(), sum=0; var weights=elite?Data.eliteWeights:Data.normalWeights;
            for(int i=0;i<weights.Length;i++) { sum+=weights[i]; if(value<sum)return i; }
            throw new MetaError("invalid_random");
        }
        public Loot RollLoot(int rarity, string race, string[] pool, IMetaRandom rng) {
            var def=Data.chests[rarity]; var loot=new Loot { stones=def.stones, race=race, hero=race+"Hero01" };
            var available=pool.Distinct().ToList(); Need(available.Count>0,"empty_pool");
            for(int b=0;b<def.bundles;b++) {
                if(available.Count==0) available=pool.Distinct().ToList();
                int index=rng.Range(0,available.Count); string id=available[index]; available.RemoveAt(index);
                loot.cards.Add(new LootCard { id=id, copies=def.copies/def.bundles+(b<def.copies%def.bundles?1:0) });
            }
            if(rng.Next()<def.raceChance) loot.raceStones=rng.Range(def.raceMin,def.raceMax+1);
            if(rng.Next()<def.shardChance) loot.shards=rng.Range(def.shardMin,def.shardMax+1);
            loot.fullHero=rng.Next()<def.heroChance; return loot;
        }
        public void ValidateLoot(Loot loot) {
            Need(loot!=null && loot.stones>=0 && loot.raceStones>=0 && loot.shards>=0 && loot.cards!=null,"invalid_loot");
            Race(loot.race); Need(loot.hero==loot.race+"Hero01","invalid_loot_hero");
            foreach(var c in loot.cards) Need(c!=null && c.copies>0 && Pool(loot.race,13).Contains(c.id),"invalid_loot_card");
        }
        public void Grant(MetaState state, Loot loot) {
            ValidateLoot(loot);
            checked {
                state.stones+=loot.stones;
                state.raceStones.Single(x=>x.race==loot.race).amount+=loot.raceStones;
                foreach(var reward in loot.cards) {
                    var card=state.cards.Single(x=>x.id==reward.id); int quantity=reward.copies;
                    if(!card.unlocked) { card.unlocked=true; quantity--; }
                    if(card.level>=Card(card.id).maxLevel)state.stones+=(long)quantity*Data.maxedCardStoneConversion;
                    else card.copies+=quantity;
                }
                var hero=state.heroes.Single(x=>x.id==loot.hero); int shards=loot.shards;
                if(loot.fullHero) {
                    if(hero.unlocked)shards+=Data.heroDuplicateShards;
                    else { hero.unlocked=true; state.stones+=(long)hero.shards*Data.ownedHeroShardStoneConversion; hero.shards=0; }
                }
                if(hero.unlocked) state.stones+=(long)shards*Data.ownedHeroShardStoneConversion;
                else {
                    hero.shards+=shards;
                    if(hero.shards>=Data.heroUnlockShards) { hero.unlocked=true; state.stones+=(long)(hero.shards-Data.heroUnlockShards)*Data.ownedHeroShardStoneConversion; hero.shards=0; }
                }
            }
        }
    }
}
