using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BackpackRTS {
[Serializable] public struct Cell { public int x,y; public Cell(int x,int y){this.x=x;this.y=y;} }
[Serializable] public class TierDefinition {
 public string name,effect,projectile; public float detectionRange=4,projectileSpeed=7; public int pierce=1; public float hp,damage,attackInterval,range,speed,spawnInterval,effectPower,splash; public int targetLimit=1;
}
[Serializable] public class UnitDefinition {
 public string id,race,name,role,profile; public int cost; public float buildingHp; public Cell[] shape; public TierDefinition[] tiers;
 public TierDefinition Tier(int tier,int branch){return tiers[tier==4?(branch==1?4:3):tier-1];}
}
[Serializable] public class CardDefinition {
 public string id,name,category,behavior; public int cost; public float hp,damage,range,interval,radius,duration;public Cell[] shape;
}
[Serializable] public class Catalog {
 public float matchSeconds=480; public int[] workerCosts={3,5,8,12,17,23}; public string version; public float refreshSeconds=30,heroRespawnSeconds=20; public UnitDefinition[] units; public CardDefinition[] cards;
 public UnitDefinition Find(string id){return Array.Find(units,u=>u.id==id);}
 public List<UnitDefinition> Race(string race){return new List<UnitDefinition>(Array.FindAll(units,u=>u.race==race));}
 public static Catalog Load(){var t=Resources.Load<TextAsset>("balance");if(t==null)throw new Exception("Missing balance.json");return JsonUtility.FromJson<Catalog>(t.text);}
 public CardDefinition Card(string id){return cards==null?null:Array.Find(cards,c=>c.id==id);}
 public bool IsSpell(string id)=>Card(id)?.category=="spell";
 public bool IsTower(string id)=>Card(id)?.behavior=="tower";
 public string Name(string id)=>Find(id)?.name??Card(id)?.name??id;
 public IEnumerable<string> Available(string race)=>Race(race).Select(u=>u.id).Concat((cards??new CardDefinition[0]).OrderBy(c=>c.category=="spell"?1:0).Select(c=>c.id));
 public string[] DefaultDeck(string race)=>Race(race).Take(4).Select(u=>u.id).Concat(new[]{"mine","tower","fence","fire"}).ToArray();
 public string[] MigrateDeck(string race,string[] old){var allowed=new HashSet<string>(Available(race));return (old??new string[0]).Where(allowed.Contains).Concat(DefaultDeck(race)).Concat(Available(race)).Distinct().Take(8).ToArray();}
 public bool ValidDeck(string race,string[] ids){if(ids==null||ids.Length!=8)return false;var allowed=new HashSet<string>(Available(race));return ids.All(allowed.Contains)&&ids.Distinct().Count()==8;}

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
