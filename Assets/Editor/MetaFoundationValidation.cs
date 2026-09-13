using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BackpackRTS.Meta;

public static class MetaFoundationValidation {
    [MenuItem("Prototype/Validate meta foundation")]
    public static void Run() {
        string directory=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"artifacts/meta-unity",Guid.NewGuid().ToString("N"));
        var service=UnityMetaAdapter.Open(directory);
        if(service.Read().cards.Count!=20)throw new Exception("Unity serializer lost inventory fields");
        var begin=service.BeginBattle("begin",0,"validation","Human",1,false);
        if(!begin.success)throw new Exception(begin.error);
        var complete=service.CompleteBattle("complete",begin.revision,"validation",true);
        if(!complete.success)throw new Exception(complete.error);
        var reopened=UnityMetaAdapter.Open(directory);
        if(reopened.Read().chests.Count!=1 || !reopened.CompleteBattle("complete",begin.revision,"validation",true).replayed)throw new Exception("Unity serializer lost chest/receipt fields");
        Debug.Log("META_UNITY_SERIALIZATION_PASSED");
    }
}
