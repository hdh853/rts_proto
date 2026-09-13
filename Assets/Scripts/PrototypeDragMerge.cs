using UnityEngine;
namespace BackpackRTS {
public partial class PrototypeGame {
 readonly BuildingMergeGesture buildingDrag=new BuildingMergeGesture();GameObject mergeGhost;Material mergeGhostMaterial;Vector2 mergePointer;bool swallowMergeRelease;
 void CancelMergeDrag(){if(buildingDrag.active&&buildingDrag.dragging)swallowMergeRelease=true;buildingDrag.Cancel();if(mergeGhost!=null)Destroy(mergeGhost);mergeGhost=null;}
 void HandleMergeDrag(){var e=Event.current;if(e.type==EventType.MouseDown)swallowMergeRelease=false;if(e.type==EventType.MouseUp&&swallowMergeRelease){swallowMergeRelease=false;e.Use();return;}if(sim==null||paused||help||sim.result!=-2||Input.touchCount>1||selectedCard>=0){CancelMergeDrag();if(e.type==EventType.MouseUp&&swallowMergeRelease){swallowMergeRelease=false;e.Use();}return;}if(e.type!=EventType.MouseDown&&e.type!=EventType.MouseDrag&&e.type!=EventType.MouseUp)return;mergePointer=e.mousePosition;
  if(buildingDrag.active){bool inside=PointerCell(mergePointer,out int x,out int y);buildingDrag.Move(inside,mergePointer);if(e.type==EventType.MouseUp){bool dragged=buildingDrag.dragging;var source=buildingDrag.source;var target=inside?sim.At(field,x,y):null;bool merged=buildingDrag.Release(sim,inside,target);if(dragged){selected=merged?target:source!=null&&source.alive?source:null;mergeSource=null;sim.message=merged?"드래그 합성 완료":"합성 취소 · 원래 위치 유지";e.Use();}CancelMergeDrag();}else if(buildingDrag.dragging)e.Use();return;}
  if(e.type==EventType.MouseDown&&e.button==0&&mergeSource==null&&!(selectedUnit!=null&&new Rect(22,428,315,238).Contains(mergePointer))&&PointerCell(mergePointer,out int cx,out int cy)){var b=sim.At(field,cx,cy);if(buildingDrag.Begin(sim,b,mergePointer)){selected=b;selectedUnit=null;}}
 }
 void UpdateMergeGhost(){if(!buildingDrag.active||!buildingDrag.dragging||buildingDrag.cancelled||buildingDrag.source==null||!buildingDrag.source.alive||!PointerWorld(mergePointer,out var at)){if(mergeGhost!=null)mergeGhost.SetActive(false);return;}var b=buildingDrag.source;if(mergeGhost==null){mergeGhost=BuildingModel(b);mergeGhost.name="Drag merge preview";if(mergeGhostMaterial==null)mergeGhostMaterial=new Material(Resources.Load<Shader>("PlacementGhost"));foreach(var renderer in mergeGhost.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=mergeGhostMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}}
  var target=sim.At(field,Mathf.FloorToInt(at.x),Mathf.FloorToInt(at.y));bool valid=sim.CanMerge(b,target);mergeGhostMaterial.color=valid?new Color(.2f,1,1,.5f):new Color(1,.25f,.2f,.4f);mergeGhost.transform.position=World(at,.15f);mergeGhost.SetActive(true);
 }
}
}
