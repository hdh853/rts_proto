using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BackpackRTS.Meta;

public static class MetaGrowthValidation {
    [MenuItem("Prototype/Validate meta growth")]
    public static void Run() {
        string directory=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"artifacts/meta-growth-unity",Guid.NewGuid().ToString("N"));
        var codec=new UnityMetaCodec();
        var rules=new MetaRules(codec.Decode<MetaDefinition>(Resources.Load<TextAsset>("meta-economy").text));
        string path=Path.Combine(directory,"meta.json");
        var repository=new MetaFileRepository(path,codec,rules);
        var state=rules.Create();state.stones=140;
        state.cards.Single(c=>c.id=="H01").copies=12;
        state.raceStones.Single(r=>r.race=="Human").amount=1;
        repository.Save(state,-1);
        var service=new MetaService(rules,repository,codec,new SystemMetaClock(),new SystemMetaRandom());
        for(int i=0;i<3;i++) {
            var q=service.GetGrowthQuote("H01");
            var result=service.UpgradeCard("u"+i,q.revision,"H01");
            if(!result.success)throw new Exception(result.error);
        }
        var evolution=service.EvolveCard("e",3,"H01");
        if(!evolution.success)throw new Exception(evolution.error);
        var reloaded=new MetaService(rules,new MetaFileRepository(path,codec,rules),codec,new SystemMetaClock(),new SystemMetaRandom());
        var replay=reloaded.EvolveCard("e",3,"H01");
        var quote=reloaded.GetGrowthQuote("H01");
        if(!replay.replayed || replay.growth==null || replay.growth.newRank!=1 || quote.level!=4 || quote.rank!=1 || quote.ownedStones!=0 || quote.ownedCopies!=0)
            throw new Exception("Unity growth or receipt round trip failed");
        Debug.Log("META_UNITY_GROWTH_PASSED");
    }
}
