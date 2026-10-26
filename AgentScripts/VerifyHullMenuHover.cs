using System;using System.Linq;using System.Threading.Tasks;using UnityEngine;using UnityEngine.EventSystems;using UnityEngine.UI;using UnityEditor;using VisualAlgo.StartMenu;
public static class VerifyHullMenuHover {
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 public static async Task<string> Run(){bool bg=Application.runInBackground;Application.runInBackground=true;try{EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();await Task.Delay(400);var panel=UnityEngine.Object.FindFirstObjectByType<StartMenuPanel>();var e=new PointerEventData(EventSystem.current);var a=panel.entries[3].GetComponent<HullMenuHover>();var b=panel.entries[4].GetComponent<HullMenuHover>();
 foreach(var h in new[]{a,b}){h.preview.Progress=0;ExecuteEvents.Execute(h.gameObject,e,ExecuteEvents.pointerExitHandler);}
 Check(!a.preview.PlanarBoundary.Contains(a.preview.MovingPoint2D),"2D point initially internal");Check(!b.preview.SpatialHull.SourceIndices.Contains(8),"3D point initially internal");
 foreach(var h in new[]{a,b})ExecuteEvents.Execute(h.gameObject,e,ExecuteEvents.pointerEnterHandler);await Task.Delay(1800);
 Check(a.preview.Progress>.99f&&b.preview.Progress>.99f,"Both move out");Check(a.preview.PlanarBoundary.Contains(a.preview.MovingPoint2D),"2D point extends hull");Check(b.preview.SpatialHull.SourceIndices.Contains(8),"3D point extends real hull");
 foreach(var h in new[]{a,b}){Check(h.transform.localScale==Vector3.one,"No button scale");ExecuteEvents.Execute(h.gameObject,e,ExecuteEvents.pointerExitHandler);}await Task.Delay(1800);
 Check(a.preview.Progress<.001f&&b.preview.Progress<.001f,"Return to original");Check(!a.preview.PlanarBoundary.Contains(a.preview.MovingPoint2D)&&!b.preview.SpatialHull.SourceIndices.Contains(8),"Both hulls restored");
 foreach(var h in new[]{a,b}){ExecuteEvents.Execute(h.gameObject,e,ExecuteEvents.pointerEnterHandler);}await Task.Delay(200);float before=a.preview.Progress;ExecuteEvents.Execute(a.gameObject,e,ExecuteEvents.pointerExitHandler);Check(a.preview.Progress==before,"No jump on reversal");await Task.Delay(1800);Check(a.preview.Progress<.001f,"Interrupted transition restores");
 foreach(var h in new[]{a,b})ExecuteEvents.Execute(h.gameObject,e,ExecuteEvents.pointerExitHandler);
 return "PASS: both internal points move outside and enter the computed hull, boundaries restore, reversal continuous, no button scaling";
 }finally{Application.runInBackground=bg;}}
 public static async Task<string> Preview(){bool bg=Application.runInBackground;Application.runInBackground=true;try{var p=UnityEngine.Object.FindFirstObjectByType<StartMenuPanel>();foreach(int i in new[]{3,4})ExecuteEvents.Execute(p.entries[i].gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);await Task.Delay(1800);return "Both hulls expanded";}finally{Application.runInBackground=bg;}}
}
