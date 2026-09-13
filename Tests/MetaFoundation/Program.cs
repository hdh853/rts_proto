using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using BackpackRTS.Meta;

class Codec : IMetaCodec {
    readonly JsonSerializerOptions options=new JsonSerializerOptions { IncludeFields=true };
    public string Encode<T>(T value) { return JsonSerializer.Serialize(value,options); }
    public T Decode<T>(string json) { try { return JsonSerializer.Deserialize<T>(json,options); } catch(JsonException ex) { throw new FormatException("Invalid JSON",ex); } }
}
class Clock : IMetaClock { public long now=1000000; public long UtcSeconds { get { return now; } } }
class Rng : IMetaRandom {
    readonly Random rng=new Random(853); public int calls;
    public double Next() { calls++; return rng.NextDouble(); }
    public int Range(int a,int b) { calls++; return rng.Next(a,b); }
}
class FailingRepository : IMetaRepository {
    readonly IMetaRepository inner; public bool fail;
    public FailingRepository(IMetaRepository inner) { this.inner=inner; }
    public MetaState Load() { return inner.Load(); }
    public void Save(MetaState state,long revision) { if(fail)throw new IOException("injected"); inner.Save(state,revision); }
}
class Program {
    static int count; static Codec codec=new Codec(); static MetaRules rules;
    static void Check(bool value,string name) { if(!value)throw new Exception("FAIL "+name); Console.WriteLine("PASS "+name); count++; }
    static void Error(Action action,string expected,string name) { try { action(); } catch(MetaError e) { Check(e.Message==expected,name); return; } throw new Exception("Expected "+expected); }
    static ChestState Chest(string id,int rarity=0) { return new ChestState { id=id,race="Human",stage=1,rarity=rarity,awardedAt=1000000,economyVersion=rules.Data.version,poolVersion="chapter1-v1",loot=new Loot { stones=40,race="Human",hero="HumanHero01" } }; }
    static int Main(string[] args) {
        try { Run(args); Console.WriteLine("META_CHECKS_PASSED "+count); return 0; }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    static void Run(string[] args) {
        string root=Path.GetFullPath(args[0]);
        rules=new MetaRules(codec.Decode<MetaDefinition>(File.ReadAllText(Path.Combine(root,"Assets/Resources/meta-economy.json"))));
        Check(rules.Data.levels.Sum(x=>x.copies)==4996 && rules.Data.levels.Sum(x=>x.stones)==164180,"approved forty-level costs");
        var fresh=rules.Create();
        Check(fresh.cards.Count(c=>c.unlocked)==12 && fresh.cards.All(c=>c.copies==0) && fresh.stones==0,"starter decks share four common cards and grant no materials");
        Check(rules.Pool("Human",1).Length==8 && rules.Pool("Human",13).Length==14,"versioned chapter pools");
        Check(rules.Data.evolutionCosts.SequenceEqual(new[]{1,3,8,20}),"evolution costs contain only race stone amounts");
        Check(rules.Data.levelStatIncrement==.02 && rules.Card("mine").growthStatPolicy=="health" && rules.Card("mining").growthStatPolicy=="duration","growth policy excludes passive mine income and attack speed");
        string directory=Path.Combine(root,"artifacts/meta-checks",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        string legacyPath=Path.Combine(directory,"progress.json");
        var legacy=new LegacySave { stones=157,humanDeck=rules.DefaultDeck("Human").Reverse().ToArray() };
        for(int i=1;i<=5;i++)legacy.upgrades.Add(new LegacyUpgrade { id="H0"+i,level=i });
        legacy.cleared.Add(2); string raw=codec.Encode(legacy); File.WriteAllText(legacyPath,raw);
        var repository=new MetaFileRepository(Path.Combine(directory,"meta.json"),codec,rules);
        var migrated=repository.InitializeFromLegacy(legacyPath);
        Check(migrated.cards.Where(c=>c.id.StartsWith("H")).Take(5).Select(c=>c.level).SequenceEqual(new[]{1,3,4,6,7}),"legacy levels preserve or improve prior bonus");
        Check(migrated.cards.Single(c=>c.id=="H04").rank==1 && migrated.cards.All(c=>c.unlocked && c.copies==0),"migration grants required rank and preserves twenty unlocked cards");
        Check(migrated.stones==157 && migrated.legacyCleared.SequenceEqual(new[]{2}) && migrated.factions.All(f=>f.cleared.Count==0),"migration preserves currency without inventing chapter clears");
        Check(migrated.factions[0].deck.SequenceEqual(legacy.humanDeck),"migration preserves a valid selected deck");
        Check(File.ReadAllText(legacyPath)==raw && File.ReadAllText(legacyPath+".pre-meta-v2.bak")==raw,"migration keeps exact source and immutable backup");
        File.WriteAllText(legacyPath,"broken"); Check(repository.InitializeFromLegacy(legacyPath).stones==157,"migration retry uses committed new state, not old source");
        Error(()=>rules.Create(new LegacySave { schema=9 }),"invalid_legacy_save","unsupported legacy schema rejected");
        string nullLegacy=Path.Combine(directory,"null-legacy.json");File.WriteAllText(nullLegacy,"null");
        var nullRepo=new MetaFileRepository(Path.Combine(directory,"null-meta.json"),codec,rules);
        Error(()=>nullRepo.InitializeFromLegacy(nullLegacy),"invalid_legacy_save","null legacy document cannot reset an existing account");
        Check(!File.Exists(Path.Combine(directory,"null-meta.json")),"invalid migration never commits new save");
        var bad=rules.Create();bad.cards[0].copies=-1; Error(()=>rules.Validate(bad),"invalid_growth","negative inventory rejected");
        var poisoned=Chest("poison");poisoned.loot.stones=-1;var poisonedState=rules.Create();poisonedState.chests.Add(poisoned);
        Error(()=>rules.Validate(poisonedState),"invalid_loot","corrupt negative reward rejected on load");

        var clock=new Clock();var rng=new Rng();var fail=new FailingRepository(repository);var service=new MetaService(rules,fail,codec,clock,rng);
        Check(!service.BeginBattle("locked",0,"b","Human",2,false).success,"locked stage rejected");
        Check(!service.BeginBattle("future",0,"b","Human",13,false).success,"unfinished chapter cannot award rewards");
        var begin=service.BeginBattle("begin",0,"battle1","Human",1,false);Check(begin.success,"begin reserves reward capacity");
        Check(!service.BeginBattle("other",begin.revision,"battle2","Human",1,false).success,"only one battle active");
        var win=service.CompleteBattle("win",begin.revision,"battle1",true);Check(win.success && service.Read().stones==157,"victory awards chest instead of direct stones");
        int calls=rng.calls;var repeat=service.CompleteBattle("win",begin.revision,"battle1",true);
        Check(repeat.success && repeat.replayed && repeat.chestId==win.chestId && rng.calls==calls,"identical completion retry preserves chest and roll");
        Check(service.CompleteBattle("win",win.revision,"battle1",false).error=="command_id_reused","same command cannot change payload");
        Check(service.CompleteBattle("win-other",win.revision,"battle1",true).error=="battle_not_active","different completion command cannot settle again");
        Check(service.StartChestUnlock("stale",0,win.chestId).error=="revision_conflict","stale revision rejected");
        var start=service.StartChestUnlock("start",win.revision,win.chestId); Check(start.success,"manual unlock starts");
        Check(service.ClaimChest("early",start.revision,win.chestId).error=="chest_not_ready","early claim rejected");
        clock.now=service.Read().chests.Single().readyAt-1;
        Check(!service.ClaimChest("edge",start.revision,win.chestId).success,"one second before finish rejected");
        clock.now++; long oldStones=service.Read().stones;var savedLoot=codec.Encode(service.Read().chests.Single().loot);
        fail.fail=true;Check(service.ClaimChest("claim",start.revision,win.chestId).error=="storage_failure" && service.Read().stones==oldStones && service.Read().chests.Count==1,"failed save rolls back award and removal");
        fail.fail=false;var claim=service.ClaimChest("claim",start.revision,win.chestId);
        Check(claim.success && service.Read().chests.Count==0 && codec.Encode(claim.loot)==savedLoot && rng.calls==calls,"claim applies frozen reward without reroll");
        long balance=service.Read().stones;repeat=service.ClaimChest("claim",start.revision,win.chestId);
        Check(repeat.replayed && service.Read().stones==balance,"claim retry returns receipt without duplicate payment");
        Check(service.ClaimChest("new-claim",claim.revision,win.chestId).error=="chest_missing","consumed chest cannot be claimed with fresh command");
        var reopened=new MetaService(rules,new MetaFileRepository(Path.Combine(directory,"meta.json"),codec,rules),codec,clock,rng);
        Check(reopened.ClaimChest("claim",start.revision,win.chestId).replayed,"receipt survives process reload");

        var state=service.Read();state.chests.Add(Chest("c1"));state.chests.Add(Chest("c2"));state.revision++;repository.Save(state,state.revision-1);
        start=service.StartChestUnlock("s1",state.revision,"c1");
        Check(service.StartChestUnlock("s2-block",start.revision,"c2").error=="unlock_in_progress","second concurrent unlock rejected");
        clock.now=service.Read().chests.First(c=>c.id=="c1").readyAt;
        var second=service.StartChestUnlock("s2",start.revision,"c2");
        Check(second.success && service.Read().chests.Count==2,"ready unclaimed chest permits another unlock but keeps slot");
        state=service.Read();for(int i=3;i<=5;i++)state.chests.Add(Chest("c"+i));state.revision++;repository.Save(state,state.revision-1);
        Check(service.BeginBattle("no-consent",state.revision,"full","Human",1,false).error=="confirm_full_slots","full slots require explicit consent");
        begin=service.BeginBattle("full-consent",state.revision,"full","Human",1,true);
        var freed=service.ClaimChest("free",begin.revision,"c1");win=service.CompleteBattle("full-win",freed.revision,"full",true);
        Check(win.success && win.chestId==null && service.Read().chests.Count==4,"full-start battle remains ineligible after slot frees");
        begin=service.BeginBattle("expire-begin",win.revision,"expired","Human",1,false);clock.now+=1800;
        Check(service.CompleteBattle("expire-end",begin.revision,"expired",true).error=="battle_expired","expired battle cannot award chest");
        Check(service.BeginBattle("replace-expired",begin.revision,"replacement","Human",1,false).success,"expired reservation can be replaced");

        var lootState=rules.Create();var loot=new Loot { race="Human",hero="HumanHero01",fullHero=true,shards=3 };
        lootState.heroes[0].shards=7;rules.Grant(lootState,loot);
        Check(lootState.heroes[0].unlocked && lootState.stones==1000,"full hero first converts stored and simultaneous shards");
        rules.Grant(lootState,new Loot { race="Human",hero="HumanHero01",fullHero=true });
        Check(lootState.stones==4000,"duplicate full hero converts thirty shards to stones");
        var shardState=rules.Create();rules.Grant(shardState,new Loot { race="Human",hero="HumanHero01",shards=33 });
        Check(shardState.heroes[0].unlocked && shardState.heroes[0].shards==0 && shardState.stones==300,"thirty shards unlock and only excess converts");
        var cardLoot=new Loot { race="Human",hero="HumanHero01" };cardLoot.cards.Add(new LootCard { id="H05",copies=10 });rules.Grant(shardState,cardLoot);
        Check(shardState.cards.Single(c=>c.id=="H05").unlocked && shardState.cards.Single(c=>c.id=="H05").copies==9,"first card consumes one copy for unlock");
        var max=shardState.cards.Single(c=>c.id=="H01");max.level=36;max.rank=4;
        cardLoot.cards.Clear();cardLoot.cards.Add(new LootCard { id="H01",copies=6 });rules.Grant(shardState,cardLoot);
        Check(shardState.stones==330 && max.copies==0,"maxed card reward converts at five stones per copy");
        bool totals=true;for(int rarity=0;rarity<5;rarity++)for(int n=0;n<200;n++) {
            var rolled=rules.RollLoot(rarity,"Human",rules.Pool("Human",13),rng);
            totals &= rolled.cards.Sum(c=>c.copies)==rules.Data.chests[rarity].copies && rolled.cards.Select(c=>c.id).Distinct().Count()==rules.Data.chests[rarity].bundles;
        }
        Check(totals,"one thousand loot rolls preserve quantity and distinct bundles");
        var small=rules.RollLoot(4,"Human",new[]{"H01"},rng);Check(small.cards.Sum(c=>c.copies)==500,"tiny pool repeats without losing reward quantity");
        var stale=repository.Load();var updated=repository.Load();updated.revision++;repository.Save(updated,updated.revision-1);stale.revision++;
        Error(()=>repository.Save(stale,stale.revision-1),"revision_conflict","independent repository CAS rejects stale write");
        var lockedState=repository.Load();lockedState.revision++;
        using(var guard=new FileStream(Path.Combine(directory,"meta.json.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
            bool rejected=false;try { repository.Save(lockedState,lockedState.revision-1); } catch(IOException) { rejected=true; }
            Check(rejected && repository.Load().revision==lockedState.revision-1,"exclusive writer lock rejects simultaneous file transaction");
        }
        File.WriteAllText(Path.Combine(directory,"meta.json"),"broken");Check(repository.Load()!=null,"valid backup recovers corrupt primary");
        File.WriteAllText(Path.Combine(directory,"meta.json.bak"),"broken");Error(()=>repository.Load(),"save_corrupt","two corrupt saves fail closed rather than reset account");
    }
}
