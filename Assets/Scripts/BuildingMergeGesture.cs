using UnityEngine;
namespace BackpackRTS {
public sealed class BuildingMergeGesture {
 public Building source;public bool active,dragging,cancelled;Vector2 start;
 public bool Begin(BattleSimulation sim,Building b,Vector2 pointer){Cancel();if(!sim.CanStartMerge(b))return false;source=b;start=pointer;cancelled=false;active=true;return true;}
 public void Move(bool inside,Vector2 pointer){if(!active)return;if(!inside)cancelled=true;if(Vector2.Distance(pointer,start)>=10)dragging=true;}
 public bool Release(BattleSimulation sim,bool inside,Building target){bool use=active&&dragging&&!cancelled&&inside;active=false;return use&&sim.CanStartMerge(source)&&sim.Merge(source,target);}
 public void Cancel(){active=false;dragging=false;cancelled=true;source=null;}
}
}

