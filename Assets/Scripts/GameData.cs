using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BackpackRTS {
[Serializable] public struct Cell { public int x,y; public Cell(int x,int y){this.x=x;this.y=y;} }
[Serializable] public class TierDefinition {
 public string name,effect; public float hp,damage,attackInterval,range,speed,spawnInterval,effectPower,splash; public int targetLimit=1;
}
[Serializable] public class UnitDefinition {
 public string id,race,name,role,profile; public int cost; public float buildingHp; public Cell[] shape; public TierDefinition[] tiers;
 public TierDefinition Tier(int tier,int branch){return tiers[tier==4?(branch==1?4:3):tier-1];}
}
[Serializable] public class Catalog {
 public string version; public float refreshSeconds=30,heroRespawnSeconds=20; public UnitDefinition[] units;
 public UnitDefinition Find(string id){return Array.Find(units,u=>u.id==id);}
 public List<UnitDefinition> Race(string race){return new List<UnitDefinition>(Array.FindAll(units,u=>u.race==race));}
 public static Catalog Load(){var t=Resources.Load<TextAsset>("balance");if(t==null)throw new Exception("Missing balance.json");return JsonUtility.FromJson<Catalog>(t.text);}
 public bool ValidDeck(string race,string[] ids){if(ids==null||ids.Length!=4)return false;var set=new HashSet<string>();foreach(var id in ids){var u=Find(id);if(u==null||u.race!=race||!set.Add(id))return false;}return true;}
}
[Serializable] public class Upgrade { public string id; public int level=1; }
[Serializable] public class SaveData { public int schema=1,stones;public List<Upgrade> upgrades=new List<Upgrade>();public List<int> cleared=new List<int>();public List<string> paid=new List<string>();public string[] humanDeck,orcDeck; }
public class ProgressStore {
 public SaveData Data; readonly string path; public string LastError="";
 public ProgressStore(string customPath=null){path=customPath??Path.Combine(Application.persistentDataPath,"progress.json");Data=Read(path)??Read(path+".bak")??new SaveData();}
 SaveData Read(string p){try{if(!File.Exists(p))return null;var s=JsonUtility.FromJson<SaveData>(File.ReadAllText(p));if(s==null||s.schema!=1||s.upgrades==null||s.cleared==null||s.paid==null)return null;return s;}catch{return null;}}
 public int Level(string id){var u=Data.upgrades.Find(x=>x.id==id);return u==null?1:u.level;}
 public bool Write(){try{Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+".tmp",JsonUtility.ToJson(Data,true));if(File.Exists(path))File.Copy(path,path+".bak",true);File.Copy(path+".tmp",path,true);File.Delete(path+".tmp");LastError="";return true;}catch(Exception e){LastError=e.Message;return false;}}
 public bool UpgradeUnit(string id){int l=Level(id),cost=l*10;if(l>=5||Data.stones<cost)return false;var before=JsonUtility.ToJson(Data);Data.stones-=cost;var u=Data.upgrades.Find(x=>x.id==id);if(u==null){u=new Upgrade{id=id};Data.upgrades.Add(u);}u.level++;if(Write())return true;Data=JsonUtility.FromJson<SaveData>(before);return false;}
 public int Reward(string battle,int map,bool practice){if(practice||Data.paid.Contains(battle))return 0;var before=JsonUtility.ToJson(Data);int[] first={20,30,50},repeat={4,6,10};int reward=Data.cleared.Contains(map)?repeat[map]:first[map];Data.stones+=reward;Data.paid.Add(battle);if(!Data.cleared.Contains(map))Data.cleared.Add(map);if(Write())return reward;Data=JsonUtility.FromJson<SaveData>(before);return -1;}
}
}
