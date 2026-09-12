using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackRTS {
public class Entity {
 public int id,team,field,tier=1; public Vector2 pos; public float hp,maxHp,shield,shieldUntil; public bool alive=true;
}
public class Building:Entity {
 public string kind;public int x,y,branch=-1,workers,invested;public float progress,built,attackReady;public Cell[] shape;
 public bool Pending=>tier==4&&branch<0;
}
public class Fighter:Entity {
 public string kind;public TierDefinition stats;public bool hero,boss,stationary;public float cooldown,supportReady,poisonUntil,poisonTick,poisonDamage,hasteUntil,nextPath,walked,chargeReady,flash;
 public int poisonTeam,target=-1;public List<Vector2> path=new List<Vector2>();public Vector2 home;
}
public class PlayerState {
 public float gold=20;public string race;public string[] deck,hand=new string[4];public float[] refill=new float[4];public List<string> bag=new List<string>();
 public int swaps=1,bought,soul;public float swapReady;public bool heroUnlocked;public int heroLevel=1,heroXp;public float reviveAt=-1,skillAt;
}
public struct AttackEvent {public Vector2 from,to;public int field;public float time;public bool spell;}
public class BattleSimulation {
 public Catalog catalog;public ProgressStore progress;public PlayerState[] players=new PlayerState[2];public List<Building> buildings=new List<Building>();public List<Fighter> fighters=new List<Fighter>();
 public List<AttackEvent> effects=new List<AttackEvent>();public List<string> events=new List<string>();
 public float time;public int map,result=-2;public string message="";public readonly string battleId=Guid.NewGuid().ToString("N");public bool practice,dungeonCleared,bossPaid;public bool enableAI=true;
 public HashSet<int> removed=new HashSet<int>();int nextId=1;System.Random rng;float nextAI;List<DamageHit> hits=new List<DamageHit>();
 struct DamageHit{public Entity target;public float damage;public int team;}
 public BattleSimulation(Catalog c,string race,string[] deck,int map,bool practice,ProgressStore p=null,int seed=7251){
  if(!c.ValidDeck(race,deck))throw new ArgumentException("Deck must contain exactly four unique units from the selected race.");
  catalog=c;progress=p;this.map=map;this.practice=practice;rng=new System.Random(seed);string enemy=race=="Human"?"Orc":"Human";
  players[0]=new PlayerState{race=race,deck=(string[])deck.Clone(),gold=practice?200:20};players[1]=new PlayerState{race=enemy,deck=c.Race(enemy).Take(4).Select(u=>u.id).ToArray()};
  for(int i=0;i<2;i++){players[i].hand=new[]{players[i].deck[0],players[i].deck[1],players[i].deck[2],"mine"};AddBuilding("base",i,0,3,i==0?0:16,false);var mine=AddBuilding("mine",i,0,i==0?0:6,i==0?1:15,false);mine.workers=1;}
  if(map==2){for(int k=0;k<4;k++){var def=c.Find(k%2==0?"O01":"O02");var f=Spawn(def,1,0,2,1,new Vector2(k%2==0?1.5f:4.5f,k<2?5.5f:7.2f));f.stationary=true;}
   var boss=Spawn(c.Find("O04"),1,0,2,1,new Vector2(3,8.6f));boss.boss=true;boss.stationary=true;boss.kind="boss";boss.hp=boss.maxHp=1400;boss.stats=new TierDefinition{name="돌심장 수호자",hp=1400,damage=36,attackInterval=2,range=.7f,speed=.45f,effect="splash",splash=1.2f,targetLimit=4};boss.supportReady=8;}
  nextAI=map==0?6:2;
 }
 public int Width(int field)=>field==0?8:6;public int Height(int field)=>field==0?18:10;
 public bool Inside(int f,int x,int y)=>x>=0&&y>=0&&x<Width(f)&&y<Height(f);
 public int Terrain(int f,int x,int y){if(!Inside(f,x,y))return 9;if(f==1)return 0;
  if(map>=1&&y>=8&&y<=9&&!((x>=1&&x<=2)||(x>=5&&x<=6)))return 2;
  if(map==2&&((x==3&&y==7)||(x==4&&y==10)))return 3;
  if(((x<=1||x>=6)&&(y>=1&&y<=2||y>=15&&y<=16)))return 1;
  if(((x==0||x==7)&&(y==5||y==12))&&!removed.Contains(x+y*8))return 3;
  return 0;
 }
 public bool Removable(int x,int y)=>((x==0||x==7)&&(y==5||y==12));
 public static Cell[] Rect(int w,int h){var l=new List<Cell>();for(int y=0;y<h;y++)for(int x=0;x<w;x++)l.Add(new Cell(x,y));return l.ToArray();}
 public Cell[] Shape(string kind)=>kind=="base"||kind=="mine"?Rect(2,2):kind=="tower"?Rect(1,1):catalog.Find(kind).shape;
 public int Cost(string kind)=>kind=="mine"?4:kind=="tower"?8:catalog.Find(kind).cost;
 public Building At(int f,int x,int y){return buildings.Find(b=>b.alive&&b.field==f&&b.shape.Any(c=>b.x+c.x==x&&b.y+c.y==y));}
 public Building Base(int team)=>buildings.Find(b=>b.team==team&&b.kind=="base"&&b.alive);
 public Fighter Hero(int team)=>fighters.Find(f=>f.alive&&f.hero&&f.team==team);
 public bool Walkable(int f,int x,int y,HashSet<int> extra=null){if(!Inside(f,x,y)||Terrain(f,x,y)>=2||At(f,x,y)!=null)return false;return extra==null||!extra.Contains(x+y*Width(f));}
 public bool IsHome(int team,int f,int y)=>f==1?team==0&&y<4:team==0?y<=6:y>=11;
 bool Protected(int f,int x,int y)=>f==0?(x==3||x==4)&&(y==2||y==15):(x==2||x==3)&&y==0;
 public bool CanPlace(string kind,int team,int f,int x,int y,out string reason){
  reason="";if(f==1&&(map!=2||team!=0||kind=="mine"||dungeonCleared)){reason="이 필드에는 배치할 수 없습니다";return false;}
  var cells=Shape(kind);var extra=new HashSet<int>();
  if(kind=="mine"&&!(x==0||x==6)){reason="금광은 자원 영역에 맞춰 배치합니다";return false;}
  foreach(var c in cells){int a=x+c.x,b=y+c.y;if(!Inside(f,a,b)||!IsHome(team,f,b)||Protected(f,a,b)){reason="건설 구역이나 출구를 확인하세요";return false;}
   if(At(f,a,b)!=null||Terrain(f,a,b)!=(kind=="mine"?1:0)){reason="겹침 또는 잘못된 지형입니다";return false;}
   if(fighters.Any(u=>u.alive&&u.field==f&&Mathf.FloorToInt(u.pos.x)==a&&Mathf.FloorToInt(u.pos.y)==b)){reason="유닛이 지나간 뒤 배치하세요";return false;}extra.Add(a+b*Width(f));}
  if(!HasRoute(f,team,extra,null)){reason="기지와 전선 사이 통로가 필요합니다";return false;}
  foreach(var b in buildings.Where(b=>b.alive&&b.team==team&&b.field==f&&b.kind!="mine"&&b.kind!="base")){if(!HasRoute(f,team,extra,b)){reason="다른 병영의 출구가 막힙니다";return false;}}
  var ghost=new Building{x=x,y=y,shape=cells};if(!HasRoute(f,team,extra,ghost)){reason="이 건물의 출구가 막힙니다";return false;}return true;
 }
 bool HasRoute(int f,int team,HashSet<int> extra,Building building){
  int w=Width(f),h=Height(f);var q=new Queue<Cell>();var seen=new HashSet<int>();
  if(building==null){int sy=f==1?0:team==0?2:15;for(int x=w/2-1;x<=w/2;x++)if(Walkable(f,x,sy,extra)){q.Enqueue(new Cell(x,sy));seen.Add(x+sy*w);}}
  else foreach(var c in building.shape)foreach(var d in dirs){int x=building.x+c.x+d.x,y=building.y+c.y+d.y,k=x+y*w;if(Walkable(f,x,y,extra)&&seen.Add(k))q.Enqueue(new Cell(x,y));}
  while(q.Count>0){var c=q.Dequeue();if(f==1?c.y>=4:team==0?c.y>=7:c.y<=10)return true;foreach(var d in dirs){int x=c.x+d.x,y=c.y+d.y,k=x+y*w;if(Walkable(f,x,y,extra)&&seen.Add(k))q.Enqueue(new Cell(x,y));}}return false;
 }
 static Cell[] dirs={new Cell(0,1),new Cell(1,0),new Cell(-1,0),new Cell(0,-1)};
 public Building AddBuilding(string kind,int team,int f,int x,int y,bool paid=true){
  float hp=kind=="base"?1800:kind=="mine"?240:kind=="tower"?360:catalog.Find(kind).buildingHp;
  var shape=Shape(kind);var b=new Building{id=nextId++,kind=kind,team=team,field=f,x=x,y=y,shape=shape,hp=hp,maxHp=hp,built=paid?time+1:time,invested=paid?Cost(kind):0};
  b.pos=new Vector2(x+(float)shape.Average(c=>c.x)+.5f,y+(float)shape.Average(c=>c.y)+.5f);buildings.Add(b);return b;
 }
 public bool PlaceCard(int team,int slot,int field,int x,int y){var p=players[team];if(slot<0||slot>3||p.hand[slot]==null||result!=-2)return false;string k=p.hand[slot];if(p.gold<Cost(k)){message="골드가 부족합니다";return false;}
  if(!CanPlace(k,team,field,x,y,out var reason)){message=reason;return false;}p.gold-=Cost(k);AddBuilding(k,team,field,x,y);p.hand[slot]=null;p.refill[slot]=time+.8f;message="건설 완료 후 생산을 시작합니다";Log("build "+k+" f"+field+" "+x+","+y);return true;}
 public bool Swap(int team,int slot){var p=players[team];if(result!=-2||p.swaps==0||slot<0||slot>3||p.hand[slot]==null)return false;p.hand[slot]=null;p.refill[slot]=time+.8f;p.swaps=0;p.swapReady=time+catalog.refreshSeconds;Log("swap "+team);return true;}
 string Draw(PlayerState p){if(p.bag.Count==0){foreach(var d in p.deck){p.bag.Add(d);p.bag.Add(d);}p.bag.AddRange(new[]{"mine","mine","tower","tower"});for(int i=p.bag.Count-1;i>0;i--){int j=rng.Next(i+1);var t=p.bag[i];p.bag[i]=p.bag[j];p.bag[j]=t;}}var v=p.bag[p.bag.Count-1];p.bag.RemoveAt(p.bag.Count-1);return v;}
 public TierDefinition BuildingTier(Building b){var u=catalog.Find(b.kind);return u==null?null:u.Tier(b.Pending?3:b.tier,b.branch);}
 public bool Merge(Building from,Building into){if(result!=-2||from==null||into==null||from==into||!from.alive||!into.alive||from.kind=="base"||from.kind!=into.kind||from.team!=into.team||from.field!=into.field||from.tier!=into.tier||from.tier>=4||from.built>time||into.built>time){message="동일 건물과 동일 티어만 합성합니다";return false;}
  float ratio=(from.hp+into.hp)/(from.maxHp+into.maxHp);float prod=BuildingTier(into)?.spawnInterval??1;float percent=Mathf.Min(from.progress/prod,into.progress/prod);
  into.tier++;into.branch=-1;float[] m={1,1.6f,2.56f,3.84f};float basehp=into.kind=="mine"?240:into.kind=="tower"?360:catalog.Find(into.kind).buildingHp;
  into.maxHp=basehp*m[into.tier-1];into.hp=into.maxHp*ratio;into.invested+=from.invested;into.workers+=from.workers;into.progress=percent*(BuildingTier(into)?.spawnInterval??1);into.attackReady=Mathf.Max(from.attackReady,into.attackReady);from.alive=false;message=into.Pending?"T4 진급을 선택하세요":"합성 성공 · T"+into.tier;Log("merge "+into.kind+" T"+into.tier);return true;}
 public bool Choose(Building b,int branch){if(b==null||!b.alive||!b.Pending||branch<0||branch>1)return false;float old=BuildingTier(b)?.spawnInterval??1;b.branch=branch;b.progress=b.progress/old*(BuildingTier(b)?.spawnInterval??1);if(b.kind=="mine"&&branch==0){float ratio=b.hp/b.maxHp;b.maxHp*=1.3f;b.hp=b.maxHp*ratio;}message="진급 선택 완료";return true;}
 public int WorkerCost(int team)=>3+players[team].bought/4;
 public bool BuyWorker(Building b){if(b==null||!b.alive||b.kind!="mine"||b.built>time||b.workers>=2*(1<<(b.tier-1)))return false;var p=players[b.team];int c=WorkerCost(b.team);if(p.gold<c){message="골드가 부족합니다";return false;}p.gold-=c;p.bought++;b.workers++;message="일꾼 추가";return true;}
 public float Income(int team){float sum=.35f;foreach(var b in buildings)if(b.alive&&b.team==team&&b.kind=="mine"&&b.built<=time)sum+=b.workers*.1f*(1+.05f*(b.tier-1))*(b.tier==4&&b.branch==1?1.1f:1);return sum;}
 public int Refund(Building b)=>Mathf.FloorToInt(b.invested*.5f*b.hp/b.maxHp);
 public bool Sell(Building b){if(b==null||!b.alive||b.kind=="base")return false;players[b.team].gold+=Refund(b);b.alive=false;message="건물 판매";return true;}
 public bool ClearRock(int team,int x,int y){if(!IsHome(team,0,y)||!Removable(x,y)||removed.Contains(x+y*8)||players[team].gold<3)return false;players[team].gold-=3;removed.Add(x+y*8);return true;}
 public Fighter Spawn(UnitDefinition def,int tier,int branch,int team,int field,Vector2 pos){var t=def.Tier(tier,branch);float m=team==0&&progress!=null?1+.03f*(progress.Level(def.id)-1):1;var u=new Fighter{id=nextId++,kind=def.id,team=team,field=field,pos=pos,home=pos,tier=tier,stats=t,maxHp=Mathf.Round(t.hp*m),hp=Mathf.Round(t.hp*m),cooldown=time+t.attackInterval*.35f,supportReady=time+6};fighters.Add(u);return u;}
 public int Count(int team)=>fighters.Count(f=>f.alive&&f.team==team);
 public bool TryExit(Building b,out Vector2 pos){int direction=b.team==0?1:-1;var all=new List<Cell>();foreach(var c in b.shape)foreach(var d in dirs){var n=new Cell(b.x+c.x+d.x,b.y+c.y+d.y);if(!all.Any(p=>p.x==n.x&&p.y==n.y))all.Add(n);}
  foreach(var c in all.OrderByDescending(c=>c.y*direction)){Vector2 v=new Vector2(c.x+.5f,c.y+.5f);if(Walkable(b.field,c.x,c.y)&&!fighters.Any(f=>f.alive&&f.field==b.field&&Vector2.Distance(f.pos,v)<.52f)){pos=v;return true;}}pos=Vector2.zero;return false;}
 public bool Summon(int team){var p=players[team];if(map!=2||p.heroUnlocked||p.soul<10||Base(team)==null||result!=-2)return false;p.soul-=10;p.heroUnlocked=true;p.reviveAt=time;TryRevive(team);Log("hero unlocked");return true;}
 void TryRevive(int team){var p=players[team];if(result!=-2||!p.heroUnlocked||Hero(team)!=null||p.reviveAt<0||time<p.reviveAt||Count(team)>=60)return;var b=Base(team);if(b==null||!TryExit(b,out var pos))return;
  float mul=1+.12f*(p.heroLevel-1);var t=new TierDefinition{name="국경대장",hp=900*mul,damage=65*mul,attackInterval=1.2f,range=.75f,speed=.95f,effect="hero",targetLimit=1};var h=new Fighter{id=nextId++,team=team,field=0,hero=true,kind="hero",pos=pos,home=pos,hp=t.hp,maxHp=t.hp,stats=t,cooldown=time+.42f};fighters.Add(h);p.reviveAt=-1;message="영웅 출전 · Lv"+p.heroLevel;}
 public bool Skill(int team){var p=players[team];var h=Hero(team);if(h==null||time<p.skillAt||result!=-2)return false;foreach(var u in fighters.Where(f=>f.alive&&f.team==team&&f.field==0&&Vector2.Distance(f.pos,h.pos)<=2).OrderBy(f=>Vector2.Distance(f.pos,h.pos)).Take(5)){u.shield=Mathf.Max(u.shield,u.maxHp*.15f);u.shieldUntil=time+4;}p.skillAt=time+18;message="수호의 함성!";return true;}
 void Xp(int team,int amount){var h=Hero(team);if(h==null)return;var p=players[team];p.heroXp+=amount;int[] need={40,70,110,160};while(p.heroLevel<5&&p.heroXp>=need[p.heroLevel-1]){p.heroXp-=need[p.heroLevel-1];p.heroLevel++;float ratio=h.hp/h.maxHp,m=1+.12f*(p.heroLevel-1);h.maxHp=900*m;h.hp=h.maxHp*ratio;h.stats.damage=65*m;Log("hero level "+p.heroLevel);}}
 public IEnumerable<Entity> Enemies(Entity e){foreach(var b in buildings)if(b.alive&&b.field==e.field&&b.team!=e.team)yield return b;foreach(var f in fighters)if(f.alive&&f.field==e.field&&f.team!=e.team)yield return f;}
 public float Distance(Vector2 p,Entity e){var b=e as Building;if(b==null)return Mathf.Max(0,Vector2.Distance(p,e.pos)-.23f);float best=100;foreach(var c in b.shape){float x=Mathf.Clamp(p.x,b.x+c.x,b.x+c.x+1),y=Mathf.Clamp(p.y,b.y+c.y,b.y+c.y+1);best=Mathf.Min(best,Vector2.Distance(p,new Vector2(x,y)));}return best;}
 bool Sight(int f,Vector2 a,Vector2 b){int n=Mathf.CeilToInt(Vector2.Distance(a,b)*4);for(int k=1;k<n;k++){Vector2 v=Vector2.Lerp(a,b,(float)k/n);if(Terrain(f,Mathf.FloorToInt(v.x),Mathf.FloorToInt(v.y))==3)return false;}return true;}
 Entity Target(Fighter f){float radius=Mathf.Max(4,f.stats.range+.75f);var candidates=Enemies(f).Where(e=>Distance(f.pos,e)<radius).ToList();if(candidates.Count==0)return f.stationary?null:Base(1-f.team);
  Entity current=candidates.Find(e=>e.id==f.target);if(current!=null)return current;
  var front=candidates.Where(e=>(e.pos.y-f.pos.y)*(f.team==0?1:-1)>=-.2f||Distance(f.pos,e)<1).ToList();if(front.Count>0)candidates=front;
  return candidates.OrderBy(e=>Distance(f.pos,e)+(f.stats.effect=="siege"&&e is Building?-.7f:0)).ThenBy(e=>e.id).First();}
 List<Vector2> Path(Fighter f,Entity target){int w=Width(f.field),h=Height(f.field),sx=Mathf.Clamp((int)f.pos.x,0,w-1),sy=Mathf.Clamp((int)f.pos.y,0,h-1),start=sx+sy*w;var q=new Queue<int>();int[] prev=Enumerable.Repeat(-2,w*h).ToArray();q.Enqueue(start);prev[start]=-1;int end=-1;
  while(q.Count>0){int k=q.Dequeue();var center=new Vector2(k%w+.5f,k/w+.5f);if(Distance(center,target)<=f.stats.range+.12f&&Sight(f.field,center,target.pos)){end=k;break;}foreach(var d in dirs){int x=k%w+d.x,y=k/w+d.y,j=x+y*w;if(Inside(f.field,x,y)&&prev[j]==-2&&Walkable(f.field,x,y)){prev[j]=k;q.Enqueue(j);}}}
  var list=new List<Vector2>();if(end<0)return list;for(int k=end;k!=start&&k>=0;k=prev[k])list.Add(new Vector2(k%w+.5f,k/w+.5f));list.Reverse();return list;}
 void Hit(Entity source,Entity target,float damage){hits.Add(new DamageHit{target=target,damage=damage,team=source.team});effects.Add(new AttackEvent{from=source.pos,to=target.pos,field=source.field,time=time,spell=source is Fighter f&&f.stats.range>1});}
 void Attack(Fighter f,Entity target){float dmg=f.stats.damage;var def=catalog.Find(f.kind);if(f.team==0&&progress!=null&&def!=null)dmg*=1+.03f*(progress.Level(f.kind)-1);
  if(f.stats.effect=="siege"&&target is Building)dmg*=f.stats.effectPower;
  if(f.stats.effect=="antiCavalry"&&target is Fighter g&&catalog.Find(g.kind)?.role=="cavalry")dmg*=f.stats.effectPower;
  if(f.stats.effect=="charge"&&f.walked>=2&&time>=f.chargeReady){dmg*=f.stats.effectPower;f.chargeReady=time+6;}f.walked=0;
  if(f.stats.effect=="shieldbreak"&&target.shield>0)dmg+=Mathf.Min(target.shield,dmg);
  Hit(f,target,dmg);
  if(f.stats.splash>0){foreach(var other in Enemies(f).Where(e=>e.id!=target.id&&Distance(target.pos,e)<=f.stats.splash).OrderBy(e=>Distance(target.pos,e)).Take(Mathf.Max(0,f.stats.targetLimit-1)))Hit(f,other,dmg*.6f);}
  if(f.stats.effect=="poison"&&target is Fighter poison){poison.poisonUntil=time+3;poison.poisonDamage=Mathf.Max(poison.poisonDamage,dmg*f.stats.effectPower);poison.poisonTeam=f.team;if(poison.poisonTick<=time)poison.poisonTick=time+1;}
 }
 void Support(Fighter f){string e=f.stats.effect;if(time<f.supportReady)return;
  var friends=fighters.Where(u=>u.alive&&u.id!=f.id&&u.team==f.team&&u.field==f.field&&Vector2.Distance(u.pos,f.pos)<2.2f).OrderBy(u=>u.hp/u.maxHp).ToList();
  if(e=="heal"){var u=friends.Find(u=>u.hp<u.maxHp);if(u!=null){u.hp=Mathf.Min(u.maxHp,u.hp+u.maxHp*f.stats.effectPower);f.supportReady=time+6;}}
  if(e=="shield"&&friends.Count>0){var u=friends[0];u.shield=Mathf.Max(u.shield,u.maxHp*f.stats.effectPower);u.shieldUntil=time+4;f.supportReady=time+6;}
  if(e=="haste"&&friends.Count>0){foreach(var u in friends.Take(3))u.hasteUntil=time+4;f.supportReady=time+6;}
 }
 public void Tick(float dt){if(result!=-2)return;time+=dt;effects.RemoveAll(e=>time-e.time>.35f);
  for(int t=0;t<2;t++){var p=players[t];p.gold=Mathf.Min(999,p.gold+Income(t)*dt);if(p.swaps==0&&time>=p.swapReady)p.swaps=1;for(int i=0;i<4;i++)if(p.hand[i]==null&&time>=p.refill[i])p.hand[i]=Draw(p);TryRevive(t);}
  foreach(var b in buildings.ToArray()){if(!b.alive||b.built>time)continue;if(b.kind=="tower"){if(time>=b.attackReady){float range=b.tier==4&&b.branch==1?4.3f:3.2f;var target=Enemies(b).Where(e=>Distance(b.pos,e)<=range&&Sight(b.field,b.pos,e.pos)).OrderBy(e=>Distance(b.pos,e)).FirstOrDefault();if(target!=null){Hit(b,target,24*Mathf.Pow(1.6f,b.tier-1));b.attackReady=time+(b.tier==4&&b.branch==0? .9f:1.3f);}}continue;}
   var s=BuildingTier(b);if(s==null||(b.field==1&&dungeonCleared))continue;b.progress=Mathf.Min(s.spawnInterval,b.progress+dt);if(b.progress>=s.spawnInterval&&Count(b.team)<60&&TryExit(b,out var pos)){Spawn(catalog.Find(b.kind),b.Pending?3:b.tier,b.branch,b.team,b.field,pos);b.progress=0;}}
  foreach(var f in fighters.ToArray()){if(!f.alive)continue;if(f.shieldUntil<=time)f.shield=0;if(f.poisonUntil>=time&&f.poisonTick<=time){hits.Add(new DamageHit{target=f,damage=f.poisonDamage,team=f.poisonTeam});f.poisonTick+=1;}Support(f);
   var target=Target(f);if(target==null)continue;float distance=Distance(f.pos,target);bool sight=Sight(f.field,f.pos,target.pos);
   if(distance<=f.stats.range&&sight){f.path.Clear();if(time>=f.cooldown){Attack(f,target);float rate=f.stats.attackInterval*(f.hasteUntil>time? .85f:1)*(f.stats.effect=="berserk"&&f.hp<=f.maxHp*.5f? .75f:1);f.cooldown=time+rate;f.flash=time+.12f;} }
   else{if(time>=f.nextPath||target.id!=f.target){f.path=Path(f,target);f.nextPath=time+.6f;}if(f.path.Count>0){Vector2 next=f.path[0];if(!Walkable(f.field,(int)next.x,(int)next.y)){f.path.Clear();f.nextPath=0;}else{Vector2 move=Vector2.MoveTowards(f.pos,next,f.stats.speed*dt);var blocker=fighters.Find(u=>u.alive&&u.id!=f.id&&u.field==f.field&&Vector2.Distance(move,u.pos)<.32f&&Vector2.Distance(f.pos,u.pos)>=.32f);if(blocker==null){f.walked+=Vector2.Distance(move,f.pos);f.pos=move;}if(Vector2.Distance(f.pos,next)<.03f)f.path.RemoveAt(0);}}}
   f.target=target.id;
  }
  var killed=new Dictionary<int,int>();foreach(var hit in hits){if(!hit.target.alive)continue;float damage=hit.damage;bool guarded=fighters.Any(g=>g.alive&&g.team==hit.target.team&&g.field==hit.target.field&&g.id!=hit.target.id&&g.stats.effect=="guard"&&Vector2.Distance(g.pos,hit.target.pos)<1.2f);if(guarded)damage*=.9f;float shield=Mathf.Min(hit.target.shield,damage);hit.target.shield-=shield;hit.target.hp-=damage-shield;if(hit.target.hp<=0)killed[hit.target.id]=hit.team;}hits.Clear();
  foreach(var b in buildings)if(b.alive&&b.hp<=0)b.alive=false;
  foreach(var f in fighters){if(!f.alive||f.hp>0)continue;f.alive=false;if(f.hero&&f.team<2){players[f.team].reviveAt=time+catalog.heroRespawnSeconds;Log("hero down");}else if(f.boss&&!bossPaid){players[0].soul+=10;bossPaid=true;message="보스 처치! 소울 10 획득 · 기지에서 영웅 소환";Log("boss soul 10");}else if(f.field==0&&f.team==1&&killed.TryGetValue(f.id,out int killer)&&killer==0)Xp(0,10*f.tier);}
  if(map==2&&!dungeonCleared&&!fighters.Any(f=>f.alive&&f.field==1&&f.team==2)){dungeonCleared=true;foreach(var f in fighters.Where(f=>f.field==1&&f.team==0))f.alive=false;Log("dungeon cleared");}
  bool ours=Base(0)!=null,theirs=Base(1)!=null;if(!ours&&!theirs)result=2;else if(!ours)result=1;else if(!theirs)result=0;else if(time>=360)result=1;
  if(enableAI&&result==-2&&time>=nextAI){string previousMessage=message;AI();message=previousMessage;nextAI=time+(map==0?3:2);}
  if(fighters.Count>200)fighters.RemoveAll(f=>!f.alive);if(buildings.Count>150)buildings.RemoveAll(b=>!b.alive);
 }
 void AI(){var p=players[1];var pending=buildings.FirstOrDefault(b=>b.alive&&b.team==1&&b.Pending);if(pending!=null){Choose(pending,rng.Next(2));return;}
  var owned=buildings.Where(b=>b.alive&&b.team==1&&b.kind!="base").ToList();foreach(var b in owned){var other=owned.FirstOrDefault(o=>o.id!=b.id&&o.kind==b.kind&&o.tier==b.tier&&o.tier<4);if(other!=null&&time>20&&Merge(b,other))return;}
  if(time<120&&rng.NextDouble()<.3){var mine=owned.Find(b=>b.kind=="mine"&&b.workers<2*(1<<(b.tier-1)));if(mine!=null&&BuyWorker(mine))return;}
  for(int s=0;s<4;s++){string k=p.hand[s];if(k==null||p.gold<Cost(k))continue;for(int y=11;y<18;y++)for(int x=0;x<8;x++)if(CanPlace(k,1,0,x,y,out var reason)){PlaceCard(1,s,0,x,y);return;}}
  if(p.swaps>0)Swap(1,rng.Next(4));
 }
 void Log(string text){events.Add(time.ToString("F2")+" "+text);if(events.Count>500)events.RemoveAt(0);}
 public void PracticeSetup(){if(!practice)return;players[0].gold=300;players[0].soul=10;message="연습 자원 지급 · 보상 없음";}
}
}
