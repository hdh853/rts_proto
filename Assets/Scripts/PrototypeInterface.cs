using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackRTS {
public partial class PrototypeGame {
 readonly PlacementGesture gesture=new PlacementGesture();Vector2 cardScroll,pointer;GameObject preview;
 Material ghostMaterial,cellMaterial;string previewKey="",previewReason="";bool previewValid;
 void TabbedMenu(){
  Text(new Rect(25,23,340,22),"BACKPACK / RTS",13,gold,true);Text(new Rect(25,59,440,50),screen=="deck"?"전투 덱 편성":screen=="growth"?"유닛 성장":"국경의 전장",29,ink,true);
  Text(new Rect(386,25,130,28),"스톤 "+store.Data.stones,15,gold);Text(new Rect(25,115,480,30),"세로형 RTS · 0.5.0",13,muted);
  if(screen=="growth"){GrowthMenu();BottomTabs();return;}
  if(Button(new Rect(25,159,235,44),"휴먼 · 균형",race=="Human"?blue:panel)){ChooseRace("Human");cardScroll=Vector2.zero;}
  if(Button(new Rect(280,159,235,44),"오크 · 정예",race=="Orc"?red:panel)){ChooseRace("Orc");cardScroll=Vector2.zero;}
  if(screen=="deck"){DeckPage();BottomTabs();return;}
  Box(new Rect(25,233,490,240),panel);Text(new Rect(45,257,440,30),"CHAPTER 01",15,gold,true);Text(new Rect(45,300,440,42),"국경 원정 · 검증 전장",26,ink,true);
  Text(new Rect(45,355,440,40),"클리어 "+store.Data.cleared.Count+" / 3   ·   "+(store.Data.cleared.Count>=3?"전체 전장 완료":"다음 전장에 도전하세요"),17,muted);
  Text(new Rect(45,414,440,40),"대기 보상 없음 · 승리 보상은 자동 지급",14,gold);
  Text(new Rect(25,505,490,30),"전투 덱  "+deck.Count+" / 8",20,ink,true);
  Text(new Rect(25,547,490,76),string.Join(" · ",deck.Select(catalog.Name)),15,muted);
  string[] maps={"01 평야","02 두 다리","03 던전"};for(int i=0;i<3;i++)if(Button(new Rect(25+i*166,653,158,48),maps[i]+(store.Data.cleared.Contains(i)?" ✓":""),map==i?blue:panel))map=i;
  if(Button(new Rect(25,731,318,62),"게임 플레이 →",blue,catalog.ValidDeck(race,deck.ToArray())))StartBattle(false);
  if(Button(new Rect(355,731,160,62),"연습",panel,catalog.ValidDeck(race,deck.ToArray())))StartBattle(true);
  if(Button(new Rect(25,820,490,42),"플레이 방법"))help=true;BottomTabs();
 }
 void BottomTabs(){Box(new Rect(0,895,540,65),panel);string[] names={"메인","덱","성장"},ids={"home","deck","growth"};for(int i=0;i<3;i++)if(Button(new Rect(8+i*177,906,169,45),names[i],screen==ids[i]?blue:panel)){screen=ids[i];notice="";}}
 void DeckPage(){
  Text(new Rect(25,222,350,30),"선택 카드  "+deck.Count+" / 8",20,ink,true);Text(new Rect(25,262,490,48),"생산 · 기능 · 마법 / 같은 카드는 한 번만 편성",14,muted);
  for(int i=0;i<8;i++){Rect cell=new Rect(25+(i%4)*124,312+(i/4)*51,116,44);if(i<deck.Count){string id=deck[i];if(Button(cell,catalog.Name(id),blue)){deck.RemoveAt(i);SaveDeck();}}else{Box(cell,panel);Text(new Rect(cell.x+35,cell.y+10,70,24),"빈 슬롯",13,muted);}}
  Text(new Rect(25,430,490,27),"보유 카드 · 선택한 카드를 누르면 제외",15,muted);
  var ids=catalog.Available(race).ToList();cardScroll=GUI.BeginScrollView(new Rect(20,469,504,368),cardScroll,new Rect(0,0,477,ids.Count*77));
  for(int i=0;i<ids.Count;i++){string id=ids[i];var unit=catalog.Find(id);var card=catalog.Card(id);bool chosen=deck.Contains(id);Rect row=new Rect(0,i*77,474,70);Box(row,chosen?new Color(.13f,.25f,.31f):panel);
   Text(new Rect(12,row.y+8,310,26),catalog.Name(id),17,ink,true);string category=unit!=null?"생산":card.category=="spell"?"마법":"기능";int cost=unit?.cost??card.cost;
   Text(new Rect(12,row.y+39,325,24),category+" · "+cost+"G · "+(unit!=null?Profile(unit.profile):card.category=="spell"?"즉시 사용 · 합성 불가":id=="mine"?"일꾼 고용 · 합성 불가":"T4 합성 가능"),12,muted);
   if(Button(new Rect(375,row.y+16,85,38),chosen?"제외":"추가",chosen?blue:panel,chosen||deck.Count<8)){if(chosen)deck.Remove(id);else deck.Add(id);SaveDeck();}}
  GUI.EndScrollView();Text(new Rect(25,850,490,32),deck.Count==8?"8장 편성 완료 · 메인 탭에서 출전하세요":"정확히 8장을 선택해야 출전할 수 있습니다",14,gold);
 }
 void SaveDeck(){if(race=="Human")store.Data.humanDeck=deck.ToArray();else store.Data.orcDeck=deck.ToArray();store.Write();}
 void CancelPreviewOnly(){gesture.Cancel();if(preview!=null)Destroy(preview);preview=null;previewKey="";previewReason="";}
 void CancelPlacement(){CancelMergeDrag();CancelPreviewOnly();selectedCard=-1;}
 bool PointerWorld(Vector2 ui,out Vector2 at){at=Vector2.zero;if(sim==null||uiScale<=0||!arena.Contains(ui))return false;
  Vector2 real=new Vector2(uiOffset.x+ui.x*uiScale,Screen.height-uiOffset.y-ui.y*uiScale);Ray ray=cam.ScreenPointToRay(real);var plane=new Plane(Vector3.up,Vector3.zero);if(!plane.Raycast(ray,out float enter))return false;Vector3 point=ray.GetPoint(enter);at=new Vector2(point.x+sim.Width(field)/2f,point.z+sim.Height(field)/2f);return sim.Inside(field,Mathf.FloorToInt(at.x),Mathf.FloorToInt(at.y));
 }
 bool PointerCell(Vector2 ui,out int x,out int y){bool ok=PointerWorld(ui,out var at);x=Mathf.FloorToInt(at.x);y=Mathf.FloorToInt(at.y);return ok;}
 void HandlePlacementInput(){
  if(sim==null||paused||help||sim.result!=-2){CancelPlacement();return;}var e=Event.current;
  if(Input.touchCount>1){CancelPlacement();return;}
  if(e.type==EventType.MouseDown||e.type==EventType.MouseDrag||e.type==EventType.MouseMove||e.type==EventType.MouseUp)pointer=e.mousePosition;
  if(gesture.active){bool inside=PointerCell(pointer,out int x,out int y);gesture.Move(inside,x,y);if(e.type==EventType.MouseUp){PointerWorld(pointer,out var at);bool placed=gesture.ReleaseAt(sim,inside,at);if(!placed)sim.message="사용 취소 · 카드와 골드 유지";CancelPlacement();e.Use();}else if(e.type==EventType.MouseDrag||e.type==EventType.MouseDown)e.Use();return;}
  if(selectedCard>=0&&e.type==EventType.MouseDown&&e.button==0){if(PointerCell(pointer,out int x,out int y)){gesture.Begin(selectedCard,field,x,y);e.Use();}else if(arena.Contains(pointer)){CancelPlacement();e.Use();}}
 }
 void UpdatePreview(){
  if(selectedCard<0||sim==null||paused||help||sim.result!=-2){if(preview!=null)preview.SetActive(false);return;}
  if(!gesture.active&&Input.touchCount==0)pointer=new Vector2((Input.mousePosition.x-uiOffset.x)/Mathf.Max(.01f,uiScale),(Screen.height-Input.mousePosition.y-uiOffset.y)/Mathf.Max(.01f,uiScale));
  bool inside=PointerCell(pointer,out int x,out int y);if(gesture.active)gesture.Move(inside,x,y);
  if(!inside||gesture.cancelled&&gesture.active){if(preview!=null)preview.SetActive(false);return;}
  string id=sim.players[0].hand[selectedCard];if(id==null){CancelPlacement();return;}
  PointerWorld(pointer,out var location);previewValid=sim.CanUseAt(0,selectedCard,field,location,out previewReason);string key=id+":"+field;
  if(ghostMaterial==null){ghostMaterial=new Material(Resources.Load<Shader>("PlacementGhost"));cellMaterial=new Material(Resources.Load<Shader>("PlacementGhost"));}
  if(preview==null||previewKey!=key){if(preview!=null)Destroy(preview);preview=new GameObject("Placement preview");previewKey=key;
   if(!catalog.IsSpell(id)){var shape=sim.Shape(id);var mock=new Building{kind=id,team=0,field=field,x=0,y=0,shape=shape,pos=new Vector2((float)shape.Average(c=>c.x)+.5f,(float)shape.Average(c=>c.y)+.5f)};var model=BuildingModel(mock);model.transform.SetParent(preview.transform,false);model.transform.localPosition=new Vector3(mock.pos.x,0,mock.pos.y);foreach(var renderer in model.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=ghostMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}}
   if(!catalog.IsSpell(id))foreach(var c in sim.Shape(id)){var tile=Part(preview,PrimitiveType.Cube,new Vector3(c.x+.5f,.08f,c.y+.5f),new Vector3(.95f,.035f,.95f),Color.white);tile.GetComponent<Renderer>().sharedMaterial=cellMaterial;tile.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}}
  var color=previewValid?new Color(.2f,1,.45f,.45f):new Color(1,.2f,.18f,.45f);ghostMaterial.color=color;cellMaterial.color=new Color(color.r,color.g,color.b,.3f);preview.transform.position=World(new Vector2(x,y));preview.SetActive(!catalog.IsSpell(id)&&sim.At(field,x,y)==null);
 }
 void DrawPlacementNotice(){if(selectedCard>=0&&PointerWorld(pointer,out var noticeAt)&&!(gesture.active&&gesture.cancelled))Text(new Rect(25,139,490,27),previewValid?(catalog.IsSpell(sim.players[0].hand[selectedCard])?"원형 범위 적용 · 손을 떼면 사용":sim.At(field,Mathf.FloorToInt(noticeAt.x),Mathf.FloorToInt(noticeAt.y))!=null?"↑ 직접 합성 · 손을 떼면 T2":"건설 가능 · 손을 떼면 사용"):previewReason,14,previewValid?new Color(.4f,1,.6f):new Color(1,.45f,.4f));
  foreach(var e in sim.effects.Where(e=>e.field==field)){var p=Project(World(e.to,.45f));float size=6+(sim.time-e.time)*28;Box(new Rect(p.x-size/2,p.y-size/2,size,size),new Color(1,.7f,.2f,Mathf.Clamp01(1-(sim.time-e.time)/.35f)));}}
 void DrawWorkers(){foreach(var b in sim.buildings.Where(b=>b.alive&&b.field==field&&b.kind=="mine")){
  var p=Project(World(b.pos+new Vector2(0,-.35f),.03f));for(int i=0;i<6;i++)Box(new Rect(p.x-18+i*6,p.y,4,5),i<b.workers?gold:new Color(.15f,.18f,.19f));}}
 void ZoneLines(){float[] rows=field==0?new[]{10f,14f}:new[]{6f};foreach(float row in rows)for(float x=.15f;x<sim.Width(field);x+=.7f)Part(terrainRoot,PrimitiveType.Cube,World(new Vector2(x,row),.035f),new Vector3(.32f,.018f,.035f),new Color(.58f,.31f,.29f));}
 GameObject CompactBuilding(Building b)=>TierBuilding(b);
 void RenderProjectiles(HashSet<int> alive){foreach(var p in sim.projectiles){if(p.field!=field)continue;alive.Add(p.id);if(!views.TryGetValue(p.id,out var go)){go=new GameObject("Projectile "+p.mode);Part(go,p.mode=="blast"?PrimitiveType.Sphere:PrimitiveType.Cube,Vector3.zero,p.mode=="blast"?new Vector3(.18f,.18f,.18f):new Vector3(.06f,.06f,.36f),p.mode=="blast"?new Color(.85f,.42f,1):gold);views[p.id]=go;viewKeys[p.id]="projectile";}
   float t=Vector2.Distance(p.from,p.pos)/Mathf.Max(.01f,Vector2.Distance(p.from,p.end));go.transform.position=World(p.pos,.45f+(p.mode=="blast"?Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI)*.7f:0));go.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(p.direction.x,p.direction.y)*Mathf.Rad2Deg,0);}}
}
}
