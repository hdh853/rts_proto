using System;
using System.Collections.Generic;

namespace BackpackRTS.Meta {
    [Serializable] public class MetaState {
        public int schema = 2;
        public long revision;
        public long stones;
        public List<RaceBalance> raceStones = new List<RaceBalance>();
        public List<CardState> cards = new List<CardState>();
        public List<HeroState> heroes = new List<HeroState>();
        public List<ChestState> chests = new List<ChestState>();
        public List<Receipt> receipts = new List<Receipt>();
        public List<FactionState> factions = new List<FactionState>();
        public List<int> legacyCleared = new List<int>();
        public bool migrated;
        public BattleTicket battle;
    }
    [Serializable] public class RaceBalance { public string race; public long amount; }
    [Serializable] public class CardState { public string id; public int level = 1, rank; public long copies; public bool unlocked; }
    [Serializable] public class HeroState { public string id; public bool unlocked; public int shards; }
    [Serializable] public class FactionState { public string race; public string[] deck; public List<int> cleared = new List<int>(); }
    [Serializable] public class LootCard { public string id; public int copies; }
    [Serializable] public class Loot {
        public long stones; public string race, hero;
        public int raceStones, shards; public bool fullHero;
        public List<LootCard> cards = new List<LootCard>();
    }
    [Serializable] public class ChestState {
        public string id, race, economyVersion, poolVersion;
        public int stage, rarity;
        public long awardedAt, readyAt;
        public bool started;
        public Loot loot;
    }
    [Serializable] public class BattleTicket {
        public string id, race, economyVersion, poolVersion;
        public int stage; public bool eligible, elite;
        public long expiresAt; public string[] pool;
    }
    [Serializable] public class Receipt { public string id, payload, chestId; public long revision, at; public Loot loot; }
    [Serializable] public class LegacyUpgrade { public string id; public int level = 1; }
    [Serializable] public class LegacySave {
        public int schema = 1, stones;
        public List<LegacyUpgrade> upgrades = new List<LegacyUpgrade>();
        public List<int> cleared = new List<int>();
        public string[] humanDeck, orcDeck;
    }
    [Serializable] public class GrowthCard { public string id, race, growthStatPolicy; public int maxRank, maxLevel; }
    [Serializable] public class GrowthCost { public int rank, fromLevel, copies, stones; }
    [Serializable] public class ChestDefinition {
        public string id; public int minutes, copies, bundles, stones, raceMin, raceMax, shardMin, shardMax;
        public double raceChance, shardChance, heroChance;
    }
    [Serializable] public class MetaDefinition {
        public string version;
        public double levelStatIncrement;
        public GrowthCard[] cards;
        public GrowthCost[] levels;
        public int[] evolutionCosts;
        public ChestDefinition[] chests;
        public double[] normalWeights, eliteWeights;
        public int heroUnlockShards, heroDuplicateShards, ownedHeroShardStoneConversion, maxedCardStoneConversion;
    }
    public interface IMetaCodec { string Encode<T>(T value); T Decode<T>(string json); }
    public interface IMetaClock { long UtcSeconds { get; } }
    public interface IMetaRandom { double Next(); int Range(int minimum, int exclusiveMaximum); }
    public interface IMetaRepository {
        MetaState Load();
        // expectedRevision=-1 means create only if no valid save exists.
        void Save(MetaState state, long expectedRevision);
    }
    public sealed class MetaError : Exception { public MetaError(string code) : base(code) {} }
    public sealed class MetaResult {
        public bool success, replayed; public string error, chestId;
        public long revision; public Loot loot;
    }
}
