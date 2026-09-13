using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackRTS {
public class Projectile {
 public int id,team,field,remaining;public Entity source;public Vector2 from,pos,end,direction;
 public float damage,speed,travel,maxTravel,splash,poison;public int targetLimit;public string mode;
 public HashSet<int> struck=new HashSet<int>();
}

// Shared by touch and mouse. Leaving the board cancels the entire gesture.
public class PlacementGesture {
 public bool active,cancelled;public int slot=-1,field,x,y;
 public void Begin(int selected,int f,int cx,int cy){active=true;cancelled=false;slot=selected;field=f;x=cx;y=cy;}
 public void Move(bool inside,int cx,int cy){if(!active)return;if(!inside)cancelled=true;x=cx;y=cy;}
 public bool Release(BattleSimulation sim,bool inside,int cx,int cy)=>ReleaseAt(sim,inside,new Vector2(cx+.5f,cy+.5f));
 public bool ReleaseAt(BattleSimulation sim,bool inside,Vector2 pos){Move(inside,Mathf.FloorToInt(pos.x),Mathf.FloorToInt(pos.y));bool commit=active&&!cancelled&&inside;active=false;if(!commit)return false;string id=sim.players[0].hand[slot];return sim.catalog.IsSpell(id)?sim.CastCard(0,slot,field,pos):sim.PlaceCard(0,slot,field,x,y);}
 public void Cancel(){active=false;cancelled=true;slot=-1;}
}

public partial class BattleSimulation {
 public List<Projectile> projectiles=new List<Projectile>();
 public bool Navigable(int field,Vector2 p){
  const float radius=.08f;
  for(int k=0;k<4;k++){float x=p.x+(k%2==0?-radius:radius),y=p.y+(k<2?-radius:radius);if(Terrain(field,Mathf.FloorToInt(x),Mathf.FloorToInt(y))>=2)return false;}
  foreach(var b in buildings){if(b.alive&&b.field==field&&Footprint.Contains(b.body??Footprint.Body(b.shape),p-new Vector2(b.x,b.y),radius))return false;}
  return true;
 }
 public bool SegmentClear(int field,Vector2 a,Vector2 b){int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.05f));for(int i=1;i<=n;i++)if(!Navigable(field,Vector2.Lerp(a,b,(float)i/n)))return false;return true;}
 readonly Dictionary<int,bool[]> navigation=new Dictionary<int,bool[]>();readonly Dictionary<int,long> navKeys=new Dictionary<int,long>();
 bool[] Grid(int f){long key=removed.Count;foreach(var b in buildings)if(b.alive&&b.field==f)key=unchecked(key*31+b.id);if(navKeys.TryGetValue(f,out long old)&&old==key)return navigation[f];
  int w=Width(f)*4,h=Height(f)*4;var grid=new bool[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)grid[x+y*w]=Navigable(f,new Vector2((x+.5f)/4,(y+.5f)/4));navigation[f]=grid;navKeys[f]=key;return grid;
 }
 List<Vector2> Path(Fighter f,Entity target){
  int w=Width(f.field)*4,h=Height(f.field)*4,sx=Mathf.Clamp((int)(f.pos.x*4),0,w-1),sy=Mathf.Clamp((int)(f.pos.y*4),0,h-1),start=sx+sy*w;var grid=Grid(f.field);
  var q=new Queue<int>();var prev=Enumerable.Repeat(-2,w*h).ToArray();int end=-1;start=-1;
  // Connect the actual position to a visible nearby node; never skip this connector.
  var seeds=new List<int>();for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){int x=sx+dx,y=sy+dy;if(x>=0&&y>=0&&x<w&&y<h&&grid[x+y*w])seeds.Add(x+y*w);}
  foreach(int k in seeds.OrderBy(k=>Vector2.SqrMagnitude(new Vector2((k%w+.5f)/4,(k/w+.5f)/4)-f.pos))){var point=new Vector2((k%w+.5f)/4,(k/w+.5f)/4);if(SegmentClear(f.field,f.pos,point)){start=k;break;}}
  if(start<0)return new List<Vector2>();q.Enqueue(start);prev[start]=-1;
  while(q.Count>0){int k=q.Dequeue();var center=new Vector2((k%w+.5f)/4,(k/w+.5f)/4);
   if(Distance(center,target)<=Mathf.Max(.05f,f.stats.range-.05f)&&Sight(f.field,center,target.pos)){end=k;break;}
   foreach(var d in dirs){int x=k%w+d.x,y=k/w+d.y;if(x<0||y<0||x>=w||y>=h)continue;int j=x+y*w;if(prev[j]!=-2||!grid[j])continue;prev[j]=k;q.Enqueue(j);}}
  var path=new List<Vector2>();if(end<0)return path;for(int k=end;k>=0;k=prev[k])path.Add(new Vector2((k%w+.5f)/4,(k/w+.5f)/4));path.Reverse();if(path.Count>0&&Vector2.Distance(f.pos,path[0])<.0001f)path.RemoveAt(0);return path;
 }

 public bool CanUse(int team,int slot,int f,int x,int y,out string reason)=>CanUseAt(team,slot,f,new Vector2(x+.5f,y+.5f),out reason);
 public bool CanUseAt(int team,int slot,int f,Vector2 pos,out string reason){reason="";if(slot<0||slot>=4||players[team].hand[slot]==null){reason="카드를 선택하세요";return false;}string id=players[team].hand[slot];if(players[team].gold<Cost(id)){reason="골드가 부족합니다";return false;}if(catalog.IsSpell(id))return CanCast(id,team,f,pos,out reason);int x=Mathf.FloorToInt(pos.x),y=Mathf.FloorToInt(pos.y);var target=At(f,x,y);if(target!=null){bool valid=CanMergeCard(team,id,target);reason=valid?"직접 합성 · T2":"같은 종류의 완성된 T1 건물만 합성합니다";return valid;}return CanPlace(id,team,f,x,y,out reason);}
 public IEnumerable<Entity> SpellTargets(string id,int team,int field,Vector2 pos){var c=catalog.Card(id);if(c==null)yield break;
  foreach(var b in buildings)if(b.alive&&b.field==field&&((c.behavior=="mining"&&b.kind=="mine"&&b.team==team&&b.built<=time&&Vector2.Distance(pos,b.pos)<=c.radius)||(c.behavior=="fire"&&b.team!=team&&Distance(pos,b)<=c.radius)))yield return b;
  if(c.behavior=="mining")yield break;foreach(var u in fighters)if(u.alive&&u.field==field&&(c.behavior=="rally"?u.team==team:u.team!=team)&&Vector2.Distance(u.pos,pos)<=c.radius)yield return u;
 }
 public bool CanCast(string id,int team,int field,Vector2 pos,out string reason){reason="";if(pos.x<0||pos.y<0||!Inside(field,(int)pos.x,(int)pos.y)||(field==1&&(map!=2||dungeonCleared))){reason="필드 안을 선택하세요";return false;}if(!catalog.IsSpell(id)){reason="마법이 아닙니다";return false;}if(!SpellTargets(id,team,field,pos).Any()){reason="범위 안에 효과를 받을 대상이 없습니다";return false;}return true;}
 public bool CastCard(int team,int slot,int field,Vector2 pos){
  if(result!=-2||slot<0||slot>=4)return false;var p=players[team];var c=catalog.Card(p.hand[slot]);if(c==null||p.gold<c.cost)return false;
  if(!CanCast(c.id,team,field,pos,out var reason)){message=reason;return false;}
  foreach(var e in SpellTargets(c.id,team,field,pos).ToArray()){if(c.behavior=="mining")((Building)e).boostUntil=time+c.duration;else if(c.behavior=="rally")((Fighter)e).hasteUntil=time+c.duration;else hits.Add(new DamageHit{target=e,team=team,damage=c.damage,sourceId=-1});}
  effects.Add(new AttackEvent{from=pos,to=pos,field=field,time=time,spell=true});p.gold-=c.cost;p.hand[slot]=null;p.refill[slot]=time+.8f;message=c.name+" 사용";return true;
 }
 public void Launch(Entity source,Entity target,float damage,TierDefinition stats){
  var dir=(target.pos-source.pos).normalized;if(dir.sqrMagnitude<.001f)dir=Vector2.up;
  string mode=string.IsNullOrEmpty(stats.projectile)||stats.projectile=="none"?"line":stats.projectile;
  projectiles.Add(new Projectile{id=nextId++,source=source,team=source.team,field=source.field,from=source.pos,pos=source.pos,end=target.pos,direction=dir,damage=damage,speed=Mathf.Max(1,stats.projectileSpeed),remaining=mode=="pierce"?Mathf.Max(2,stats.pierce):1,maxTravel=Mathf.Max(stats.range+3,Vector2.Distance(source.pos,target.pos)+2),splash=stats.splash,targetLimit=Mathf.Max(1,stats.targetLimit),mode=mode,poison=stats.effect=="poison"?stats.effectPower:0});
 }
 void ProjectileHit(Projectile p,Entity e,float damage){hits.Add(new DamageHit{target=e,damage=damage,team=p.team,sourceId=p.source.id});effects.Add(new AttackEvent{from=p.pos,to=e.pos,field=p.field,time=time,spell=true});
  if(p.poison>0&&e is Fighter f){f.poisonUntil=time+3;f.poisonTeam=p.team;f.poisonSource=p.source.id;f.poisonDamage=Mathf.Max(f.poisonDamage,damage*p.poison);f.poisonTick=time+1;}}
 void TickProjectiles(float dt){foreach(var p in projectiles.ToArray()){
   if(p.mode=="blast"){p.pos=Vector2.MoveTowards(p.pos,p.end,p.speed*dt);if(Vector2.Distance(p.pos,p.end)>.01f)continue;
    foreach(var e in Enemies(p.source).Where(e=>Distance(p.end,e)<=Mathf.Max(.35f,p.splash)).OrderBy(e=>Distance(p.end,e)).Take(p.targetLimit))ProjectileHit(p,e,p.damage);
    effects.Add(new AttackEvent{from=p.end,to=p.end,field=p.field,time=time,spell=true});projectiles.Remove(p);continue;}
   float distance=p.speed*dt;int steps=Mathf.Max(1,Mathf.CeilToInt(distance/.08f));for(int i=0;i<steps&&p.remaining>0;i++){
    p.pos+=p.direction*(distance/steps);p.travel+=distance/steps;
    var e=Enemies(p.source).Where(e=>!p.struck.Contains(e.id)&&Distance(p.pos,e)<=.10f).OrderBy(e=>Distance(p.pos,e)).ThenBy(e=>e.id).FirstOrDefault();
    if(e!=null){p.struck.Add(e.id);ProjectileHit(p,e,p.damage);p.remaining--;}
    if(p.travel>=p.maxTravel||!Inside(p.field,Mathf.FloorToInt(p.pos.x),Mathf.FloorToInt(p.pos.y)))p.remaining=0;
   }if(p.remaining==0)projectiles.Remove(p);
  }
 }
}
}
