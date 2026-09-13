using System;
using System.Linq;

namespace BackpackRTS.Meta {
    public sealed partial class MetaRules {
        public GrowthMultipliers Multipliers(string cardId,int level) {
            var def=Card(cardId); Need(level>=1 && level<=def.maxLevel,"invalid_growth_level");
            double scale=1+Data.levelStatIncrement*(level-1);
            return new GrowthMultipliers {
                health=def.growthStatPolicy=="health" || def.growthStatPolicy=="healthAndDamage"?scale:1,
                damage=def.growthStatPolicy=="damage" || def.growthStatPolicy=="healthAndDamage"?scale:1,
                duration=def.growthStatPolicy=="duration"?scale:1
            };
        }
        // Returns a detached quote. The command recalculates it against the committed revision.
        public GrowthQuote Quote(MetaState state,string cardId,string raceStone=null) {
            var def=Card(cardId); var card=state.cards.Single(c=>c.id==cardId);
            var quote=new GrowthQuote {
                cardId=cardId,revision=state.revision,level=card.level,rank=card.rank,
                nextLevel=card.level,nextRank=card.rank,maxLevel=def.maxLevel,maxRank=def.maxRank,
                ownedCopies=card.copies,ownedStones=state.stones,growthStatPolicy=def.growthStatPolicy,
                current=Multipliers(cardId,card.level),next=Multipliers(cardId,card.level),
                action="max",reason="max_level",skillEffectsAvailable=false
            };
            if(card.level<def.maxLevel) {
                var cost=Cost(card);
                if(cost!=null) {
                    quote.action="upgrade"; quote.nextLevel=card.level+1;
                    quote.copiesCost=cost.copies; quote.stonesCost=cost.stones;
                    quote.next=Multipliers(cardId,quote.nextLevel);
                    quote.reason=card.copies<cost.copies?"insufficient_copies":state.stones<cost.stones?"insufficient_stones":null;
                } else {
                    Need(card.rank<def.maxRank && card.rank<Data.evolutionCosts.Length,"missing_growth_cost");
                    quote.action="evolve";quote.nextRank=card.rank+1;
                    quote.raceStonesCost=Data.evolutionCosts[card.rank];
                    quote.raceStone=raceStone??(def.race=="Shared"?null:def.race);
                    if(quote.raceStone==null)quote.reason="race_stone_required";
                    else if(quote.raceStone!="Human" && quote.raceStone!="Orc")quote.reason="invalid_race";
                    else if(def.race!="Shared" && def.race!=quote.raceStone)quote.reason="wrong_race_stone";
                    else {
                        quote.ownedRaceStones=state.raceStones.Single(x=>x.race==quote.raceStone).amount;
                        quote.reason=quote.ownedRaceStones<quote.raceStonesCost?"insufficient_race_stones":null;
                    }
                }
            }
            if(!card.unlocked)quote.reason="card_locked";
            quote.canExecute=quote.reason==null;return quote;
        }
    }
    public sealed partial class MetaService {
        public GrowthQuote GetGrowthQuote(string cardId,string raceStone=null) { return rules.Quote(Read(),cardId,raceStone); }
        public MetaResult UpgradeCard(string command,long revision,string cardId) {
            return Execute(command,revision,Key("upgrade",cardId),s=> {
                var quote=rules.Quote(s,cardId);
                Need(quote.action!="max","max_level");
                Need(quote.action=="upgrade","evolution_required");
                Need(quote.canExecute,quote.reason);
                var card=s.cards.Single(c=>c.id==cardId);
                card.copies-=quote.copiesCost; s.stones-=quote.stonesCost; card.level=quote.nextLevel;
                return new Receipt { growth=Change(quote) };
            });
        }
        public MetaResult EvolveCard(string command,long revision,string cardId,string raceStone=null) {
            return Execute(command,revision,Key("evolve",cardId,raceStone),s=> {
                var quote=rules.Quote(s,cardId,raceStone);
                Need(quote.action!="max","max_level");
                Need(quote.action=="evolve","level_upgrades_required");
                Need(quote.canExecute,quote.reason);
                var card=s.cards.Single(c=>c.id==cardId);
                s.raceStones.Single(x=>x.race==quote.raceStone).amount-=quote.raceStonesCost;
                card.rank=quote.nextRank;
                return new Receipt { growth=Change(quote) };
            });
        }
        static GrowthChange Change(GrowthQuote q) {
            return new GrowthChange { cardId=q.cardId,action=q.action,raceStone=q.raceStone,
                oldLevel=q.level,newLevel=q.nextLevel,oldRank=q.rank,newRank=q.nextRank,
                copiesSpent=q.copiesCost,stonesSpent=q.stonesCost,raceStonesSpent=q.raceStonesCost };
        }
    }
}
