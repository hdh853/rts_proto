using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace BackpackRTS {
public partial class PrototypeGame {
 bool galleryActive;string galleryTitle;string[] galleryNames;
 void GalleryLabels(){Box(new Rect(0,0,540,94),panel);Text(new Rect(20,16,500,36),galleryTitle,23,gold,true);Text(new Rect(20,58,500,28),"T1        T2        T3        T4 · A       T4 · B",17,ink);for(int i=0;i<galleryNames.Length;i++)Text(new Rect(15,110+i*126,160,24),galleryNames[i],13,gold);}
 IEnumerator GalleryCaptures(string dir){CancelPlacement();selected=null;selectedUnit=null;mergeSource=null;galleryActive=true;ClearVisuals();cam.rect=new Rect(0,0,1,1);cam.transform.position=new Vector3(0,20,-16);cam.transform.LookAt(Vector3.zero);cam.orthographicSize=8.5f;
  foreach(string faction in new[]{"Human","Orc"})foreach(bool buildings in new[]{false,true}){race=faction;sim.players[0].race=faction;galleryTitle=(faction=="Human"?"휴먼":"오크")+(buildings?" · 건물 성장":" · 유닛 성장");var list=catalog.Race(faction);galleryNames=list.Select(u=>u.name).ToArray();var root=new GameObject("Tier gallery");Part(root,PrimitiveType.Cube,new Vector3(0,-.25f,0),new Vector3(12,.1f,20),new Color(.09f,.14f,.18f));for(int row=0;row<list.Count;row++)for(int col=0;col<5;col++){int tier=Mathf.Min(4,col+1),branch=col==4?1:0;GameObject model;if(buildings){var shape=sim.Shape(list[row].id);model=BuildingModel(new Building{kind=list[row].id,team=0,tier=tier,branch=branch,shape=shape,pos=new Vector2((float)shape.Average(q=>q.x)+.5f,(float)shape.Average(q=>q.y)+.5f)});model.transform.localScale=Vector3.one*.65f;}else model=UnitModel(new Fighter{kind=list[row].id,team=0,tier=tier,branch=branch,stats=list[row].Tier(tier,branch)});model.transform.SetParent(root.transform,false);model.transform.position=new Vector3(-3.3f+col*1.65f,0,7-row*2.8f);if(!buildings)model.transform.rotation=Quaternion.Euler(0,180,0);}
   yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(dir,faction+(buildings?"-building-tiers":"-unit-tiers")+".png"));yield return new WaitForSeconds(1);Destroy(root);}
 }
}
}
