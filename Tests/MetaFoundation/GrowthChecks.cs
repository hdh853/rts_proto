using System;
using System.IO;
using System.Linq;
using BackpackRTS.Meta;

static class GrowthChecks {
    sealed class Memory : IMetaRepository {
        readonly Codec codec=new Codec(); string json;
        public Memory(MetaState initial) { json=codec.Encode(initial); }
        public MetaState Load() { return codec.Decode<MetaState>(json); }
        public void Save(MetaState state,long revision) {
            if(Load().revision!=revision)throw new MetaError("revision_conflict");
            json=codec.Encode(state);
        }
    }
    static MetaService Service(MetaRules rules,MetaState state) { return new MetaService(rules,new Memory(state),new Codec(),new Clock(),new Rng()); }
    static MetaState Rich(MetaRules rules,string id="H01",int level=1,int rank=0) {
        var s=rules.Create();s.stones=1000000;
        foreach(var r in s.raceStones)r.amount=1000;
        var c=s.cards.Single(x=>x.id==id);c.unlocked=true;c.copies=10000;c.level=level;c.rank=rank;
        rules.Validate(s);return s;
    }
    public static void Run(string root,MetaRules rules,Action<bool,string> check) {
        var codec=new Codec();var service=Service(rules,Rich(rules));
        var before=codec.Encode(service.Read());var quote=service.GetGrowthQuote("H01");
        check(quote.action=="upgrade" && quote.copiesCost==2 && quote.stonesCost==20 && quote.nextLevel==2 && quote.canExecute,"growth quote provides exact first upgrade cost");
        quote.next.health=999;quote.copiesCost=0;
        check(codec.Encode(service.Read())==before && service.GetGrowthQuote("H01").next.health==1.02,"growth quote is detached and does not mutate inventory");
        var upgraded=service.UpgradeCard("upgrade",0,"H01");var state=service.Read();
        check(upgraded.success && state.cards.Single(c=>c.id=="H01").level==2 && state.stones==999980 && state.cards.Single(c=>c.id=="H01").copies==9998,"upgrade atomically consumes copies and stones");
        check(upgraded.growth.oldLevel==1 && upgraded.growth.newLevel==2 && upgraded.growth.stonesSpent==20 && upgraded.growth.raceStonesSpent==0,"growth result records exact committed change");
        var next=service.UpgradeCard("upgrade2",upgraded.revision,"H01");
        var replay=service.UpgradeCard("upgrade",0,"H01");
        check(replay.replayed && replay.growth.newLevel==2 && service.Read().cards.Single(c=>c.id=="H01").level==3,"retry returns original growth receipt after later upgrades");
        check(service.UpgradeCard("upgrade",next.revision,"H02").error=="command_id_reused","growth command id cannot be reused for another card");
        check(service.UpgradeCard("stale",0,"H01").error=="revision_conflict","outdated quote cannot spend current inventory");
        check(service.EvolveCard("early",next.revision,"H01").error=="level_upgrades_required","evolution rejected before rank boundary");
        var third=service.UpgradeCard("upgrade3",next.revision,"H01");
        check(service.GetGrowthQuote("H01").action=="evolve" && service.UpgradeCard("too-far",third.revision,"H01").error=="evolution_required","rank boundary requires evolution instead of fourth N upgrade");

        state=Rich(rules,"H01",4,0);state.stones=0;state.cards[0].copies=0;
        service=Service(rules,state);quote=service.GetGrowthQuote("H01");
        check(quote.canExecute && quote.copiesCost==0 && quote.stonesCost==0 && quote.raceStonesCost==1,"evolution needs no duplicate cards or normal stones");
        before=codec.Encode(service.Read());
        check(service.EvolveCard("wrong",0,"H01","Orc").error=="wrong_race_stone" && codec.Encode(service.Read())==before,"production evolution rejects other race without mutation");
        var evolved=service.EvolveCard("evolve",0,"H01");
        check(evolved.success && evolved.growth.oldRank==0 && evolved.growth.newRank==1 && evolved.growth.newLevel==4 && service.Read().raceStones.Single(x=>x.race=="Human").amount==999,"evolution retains level and debits own race once");
        check(service.Read().stones==0 && service.Read().cards[0].copies==0 && service.Read().raceStones.Single(x=>x.race=="Orc").amount==1000,"evolution leaves copies normal stones and other race untouched");
        check(service.GetGrowthQuote("H01").copiesCost==8 && service.GetGrowthQuote("H01").stonesCost==100,"R boundary exposes next approved cost");
        check(!service.GetGrowthQuote("H01").skillEffectsAvailable,"unimplemented rank skills are not advertised as available");

        state=Rich(rules,"mine",9,1);state.raceStones[0].amount=1;state.raceStones[1].amount=2;
        service=Service(rules,state);
        check(service.GetGrowthQuote("mine").reason=="race_stone_required","common evolution requires explicit race selection");
        check(service.EvolveCard("mixed",0,"mine","Orc").error=="insufficient_race_stones" && service.Read().raceStones.Sum(x=>x.amount)==3,"common evolution cannot combine two race balances");
        check(service.EvolveCard("invalid",0,"mine","Human+Orc").error=="invalid_race","compound race input is rejected");
        state.raceStones[1].amount=3;service=Service(rules,state);
        evolved=service.EvolveCard("common",0,"mine","Orc");
        check(evolved.success && service.Read().cards.Single(c=>c.id=="mine").rank==2 && service.Read().raceStones[1].amount==0 && service.Read().raceStones[0].amount==1,"common evolution spends selected race and updates shared card");
        quote=service.GetGrowthQuote("mine","Human");
        check(quote.rank==2 && Math.Abs(quote.current.health-1.16)<1e-9 && quote.current.damage==1 && quote.current.duration==1,"mine preview grows health only and is shared across race selection");

        state=Rich(rules);state.cards[0].copies=1;service=Service(rules,state);before=codec.Encode(service.Read());
        check(service.UpgradeCard("poor-copies",0,"H01").error=="insufficient_copies" && codec.Encode(service.Read())==before,"insufficient copies cannot consume stones");
        state.cards[0].copies=2;state.stones=19;service=Service(rules,state);before=codec.Encode(service.Read());
        check(service.UpgradeCard("poor-stones",0,"H01").error=="insufficient_stones" && codec.Encode(service.Read())==before,"insufficient stones cannot consume copies");
        state=rules.Create();state.stones=10000;state.cards.Single(c=>c.id=="H05").copies=10000;service=Service(rules,state);
        check(service.UpgradeCard("locked-growth",0,"H05").error=="card_locked","locked card cannot be upgraded even with materials");
        check(service.UpgradeCard("unknown",0,"missing").error=="unknown_card","unknown card returns domain error");

        string path=Path.Combine(root,"artifacts/growth-checks",Guid.NewGuid().ToString("N"),"meta.json");
        var repository=new MetaFileRepository(path,codec,rules);repository.Save(Rich(rules),-1);
        var failing=new FailingRepository(repository);service=new MetaService(rules,failing,codec,new Clock(),new Rng());
        failing.fail=true;before=codec.Encode(service.Read());
        check(service.UpgradeCard("disk",0,"H01").error=="storage_failure" && codec.Encode(service.Read())==before,"upgrade storage failure rolls back all growth and balances");
        failing.fail=false;upgraded=service.UpgradeCard("disk",0,"H01");
        service=new MetaService(rules,new MetaFileRepository(path,codec,rules),codec,new Clock(),new Rng());
        check(service.UpgradeCard("disk",0,"H01").replayed && service.Read().cards[0].level==2,"upgrade receipt survives disk reload");
        state=Rich(rules,"H01",4,0);var evolutionRepo=new FailingRepository(new Memory(state)) { fail=true };
        service=new MetaService(rules,evolutionRepo,codec,new Clock(),new Rng());before=codec.Encode(service.Read());
        check(service.EvolveCard("failed-evolution",0,"H01").error=="storage_failure" && codec.Encode(service.Read())==before,"evolution storage failure preserves rank and race stones");

        state=Rich(rules,"H04");state.stones=164180;state.cards.Single(c=>c.id=="H04").copies=4996;state.raceStones[0].amount=32;
        service=Service(rules,state);int upgrades=0,evolutions=0;
        while(true) {
            quote=service.GetGrowthQuote("H04");if(quote.action=="max")break;
            var result=quote.action=="upgrade"?service.UpgradeCard("full-u"+upgrades++,quote.revision,"H04"):service.EvolveCard("full-e"+evolutions++,quote.revision,"H04");
            if(!result.success)throw new Exception("Full progression failed: "+result.error);
        }
        state=service.Read();
        check(upgrades==40 && evolutions==4 && state.stones==0 && state.cards.Single(c=>c.id=="H04").copies==0 && state.raceStones[0].amount==0,"full H04 progression consumes approved 4996 copies 164180 stones and 32 race stones");
        check(quote.level==41 && quote.rank==4 && quote.current.health==1.8 && quote.current.damage==1.8,"max level uses linear base-stat growth rather than compounded growth");
        check(!quote.canExecute && quote.nextLevel==41 && quote.copiesCost==0 && service.UpgradeCard("max-up",quote.revision,"H04").error=="max_level" && service.EvolveCard("max-e",quote.revision,"H04").error=="max_level","maximum growth cannot charge any further materials");
        bool caps=true;
        foreach(var def in rules.Data.cards) {
            var maxState=Rich(rules,def.id,def.maxLevel,def.maxRank);var maximum=Service(rules,maxState);var q=maximum.GetGrowthQuote(def.id,"Human");
            caps &= q.action=="max" && !q.canExecute && maximum.UpgradeCard("cap",0,def.id).error=="max_level";
        }
        check(caps,"all twenty card-specific caps block further upgrades");
        var fire=rules.Multipliers("fire",6);var mining=rules.Multipliers("mining",6);var fence=rules.Multipliers("fence",6);
        check(fire.damage==1.1 && fire.health==1 && mining.duration==1.1 && mining.damage==1 && fence.health==1.1 && fence.damage==1,"spell duration damage and fence health follow separate growth policies");
    }
}
