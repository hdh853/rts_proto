using System;
using System.IO;

namespace BackpackRTS.Meta {
    public sealed class MetaFileRepository : IMetaRepository {
        readonly string path; readonly IMetaCodec codec; readonly MetaRules rules;
        public MetaFileRepository(string path,IMetaCodec codec,MetaRules rules) { this.path=Path.GetFullPath(path); this.codec=codec; this.rules=rules; }
        MetaState ReadFile(string file) {
            if(!File.Exists(file))return null;
            try { var state=codec.Decode<MetaState>(File.ReadAllText(file)); rules.Validate(state); return state; }
            catch(Exception ex) when(ex is ArgumentException || ex is MetaError || ex is FormatException) { return null; }
        }
        public MetaState Load() {
            var primary=ReadFile(path); if(primary!=null)return primary;
            var backup=ReadFile(path+".bak"); if(backup!=null)return backup;
            if(File.Exists(path) || File.Exists(path+".bak"))throw new MetaError("save_corrupt");
            return null;
        }
        public void Save(MetaState state,long expectedRevision) {
            rules.Validate(state);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // An exclusive file handle serializes writers, including separate local repository instances.
            using(var guard=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
                var existing=Load();
                if((existing==null?-1:existing.revision)!=expectedRevision)throw new MetaError("revision_conflict");
                if(state.revision!=(expectedRevision<0?0:expectedRevision+1))throw new MetaError("invalid_revision");
                var encoded=codec.Encode(state);
                using(var stream=new FileStream(path+".tmp",FileMode.Create,FileAccess.Write,FileShare.None)) {
                    var bytes=System.Text.Encoding.UTF8.GetBytes(encoded); stream.Write(bytes,0,bytes.Length); stream.Flush(true);
                }
                if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");
                else File.Move(path+".tmp",path);
            }
        }
        public MetaState InitializeFromLegacy(string legacyPath) {
            var existing=Load(); if(existing!=null)return existing;
            LegacySave legacy=null;
            if(File.Exists(legacyPath)) {
                var text=File.ReadAllText(legacyPath);
                legacy=codec.Decode<LegacySave>(text);
                if(legacy==null)throw new MetaError("invalid_legacy_save");
                var initial=rules.Create(legacy); // validate before creating the migration marker
                string backup=legacyPath+".pre-meta-v2.bak";
                // Preserve the exact first source, never replace it on a retry.
                if(!File.Exists(backup))File.Copy(legacyPath,backup,false);
                Save(initial,-1); return initial;
            }
            // A failed legacy primary must not silently turn an existing account into a fresh account.
            if(File.Exists(legacyPath+".bak"))throw new MetaError("legacy_primary_missing_restore_backup_first");
            var fresh=rules.Create(); Save(fresh,-1); return fresh;
        }
    }
    public sealed class SystemMetaClock : IMetaClock { public long UtcSeconds { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } } }
    public sealed class SystemMetaRandom : IMetaRandom {
        readonly Random random=new Random();
        public double Next() { return random.NextDouble(); }
        public int Range(int minimum,int exclusiveMaximum) { return random.Next(minimum,exclusiveMaximum); }
    }
}
