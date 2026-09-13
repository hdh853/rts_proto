using System;
using System.Linq;
using UnityEngine;

namespace BackpackRTS {
// One definition drives the hand icon, model outline, collision and selection.
public static class Footprint {
 public const float Margin=.22f;
 public static Cell[] Icon(Cell[] cells){int top=cells.Max(c=>c.y);return cells.Select(c=>new Cell(c.x,top-c.y)).ToArray();}
 public static Rect[] Body(Cell[] cells){
  bool Has(int x,int y)=>cells.Any(c=>c.x==x&&c.y==y);
  return cells.Select(c=>{float x0=c.x+(Has(c.x-1,c.y)?0:Margin),x1=c.x+1-(Has(c.x+1,c.y)?0:Margin),y0=c.y+(Has(c.x,c.y-1)?0:Margin),y1=c.y+1-(Has(c.x,c.y+1)?0:Margin);return Rect.MinMaxRect(x0,y0,x1,y1);}).ToArray();
 }
 public static bool Contains(Rect[] body,Vector2 p,float padding=0)=>body.Any(r=>p.x>=r.xMin-padding&&p.x<=r.xMax+padding&&p.y>=r.yMin-padding&&p.y<=r.yMax+padding);
 public static float Distance(Rect[] body,Vector2 p){float best=float.MaxValue;foreach(var r in body)best=Mathf.Min(best,Vector2.Distance(p,new Vector2(Mathf.Clamp(p.x,r.xMin,r.xMax),Mathf.Clamp(p.y,r.yMin,r.yMax))));return best;}
}
}
