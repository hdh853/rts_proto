using System;
using System.IO;
using UnityEngine;

namespace BackpackRTS.Meta {
    public sealed class UnityMetaCodec : IMetaCodec {
        public string Encode<T>(T value) { return JsonUtility.ToJson(value,true); }
        public T Decode<T>(string json) { return JsonUtility.FromJson<T>(json); }
    }
    public static class UnityMetaAdapter {
        // Explicit opt-in for editor validation. Production UI migration is a subsequent PR.
        // Do not run concurrently with the legacy ProgressStore economy.
        public static MetaService Open(string directory) {
            var asset=Resources.Load<TextAsset>("meta-economy");
            if(asset==null)throw new InvalidOperationException("Missing meta-economy.json");
            var codec=new UnityMetaCodec(); var rules=new MetaRules(codec.Decode<MetaDefinition>(asset.text));
            var repository=new MetaFileRepository(Path.Combine(directory,"meta-progress-v2.json"),codec,rules);
            repository.InitializeFromLegacy(Path.Combine(directory,"progress.json"));
            return new MetaService(rules,repository,codec,new SystemMetaClock(),new SystemMetaRandom());
        }
    }
}
