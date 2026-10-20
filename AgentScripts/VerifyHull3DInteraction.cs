using System;
using System.Linq;
using System.Threading.Tasks;
using ConvexHullDemo3D;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class VerifyHull3DInteraction {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static Vector2 Screen(Component c){var r=(RectTransform)c.transform;return RectTransformUtility.WorldToScreenPoint(c.GetComponentInParent<Canvas>().rootCanvas.worldCamera,r.TransformPoint(r.rect.center));}
 static void MouseEvent(Mouse mouse,Vector2 at,bool left=false,bool right=false){using(StateEvent.From(mouse,out var e)){mouse.position.WriteValueIntoEvent(at,e);mouse.leftButton.WriteValueIntoEvent(left?1f:0f,e);mouse.rightButton.WriteValueIntoEvent(right?1f:0f,e);InputSystem.QueueEvent(e);}InputSystem.Update();}
 static void Click(Mouse mouse,Component c){var pointer=new PointerEventData(EventSystem.current){position=Screen(c),button=PointerEventData.InputButton.Left};var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0,"UI raycast hit");ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);}

 public static async Task<object> Run(){
  var d=UnityEngine.Object.FindFirstObjectByType<ConvexHull3DController>();Check(d!=null&&d.Toolbar!=null,"Scene and UI initialized");
  EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();await Task.Delay(200);
  var oldMode=InputSystem.settings.updateMode;InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;bool paused=EditorApplication.isPaused;EditorApplication.isPaused=true;Canvas.ForceUpdateCanvases();
  var oldMouse=Mouse.current;var oldKeyboard=Keyboard.current;var mouse=InputSystem.AddDevice<Mouse>("Hull3D test mouse");var keyboard=InputSystem.AddDevice<Keyboard>("Hull3D test keyboard");mouse.MakeCurrent();keyboard.MakeCurrent();
  var camera=d.worldCamera;var originalPosition=camera.transform.position;var originalRotation=camera.transform.rotation;
  try{
   d.ClearPoints();foreach(var value in d.initialPoints)d.AddPoint(value,false);d.FlushChanges();Check(d.Points.Count==12&&d.Hull.Vertices.Length==8&&d.Hull.Triangles.Length==36,"Initial cube and internal points");
   Check(Cursor.visible&&Cursor.lockState==CursorLockMode.None,"Visible unlocked cursor");
   int revision=d.HullRevision;d.FlushChanges();d.Select(d.Points[0].Id);d.FlushChanges();Check(d.HullRevision==revision,"Selection/idle do not recompute");
   Vector2 canvas=new Vector2(camera.pixelWidth*.1f,camera.pixelHeight*.25f);
   d.BeginFly(canvas);Check(d.IsFlying,"RMB activates fly");d.Fly(Vector2.zero,new Vector3(1,1,1),1);Check(Mathf.Abs(Vector3.Distance(originalPosition,camera.transform.position)-6)<.001f,"Normalized local movement");
   d.Fly(new Vector2(0,100000),Vector3.zero,0);Check(Mathf.Abs(Mathf.DeltaAngle(0,camera.transform.eulerAngles.x)+89)<.01f,"Pitch clamp");
   d.SendMessage("OnApplicationFocus",false);Check(!d.IsFlying,"Focus loss stops fly");camera.transform.SetPositionAndRotation(originalPosition,originalRotation);d.FlushChanges();Check(revision==d.HullRevision,"Camera movement does not recompute");
   d.BeginFly(Screen(d.Toolbar.addButton));Check(!d.IsFlying,"UI blocks fly");
   d.Select(d.Points[8].Id);var point=d.Points[8];var start=point.Position;
   for(int axis=0;axis<3;axis++){
    Vector3 unit=HullGizmo3D.Axis(axis);Vector2 press=camera.WorldToScreenPoint(start+unit*.8f);
    d.BeginLeft(press);Check(d.DragAxis==axis,"Pick axis "+axis);d.Drag(press);Check((point.Position-start).sqrMagnitude<1e-8f,"No press jump");
    d.BeginFly(canvas);Check(!d.IsFlying,"Camera frozen while dragging");
    d.Drag(camera.WorldToScreenPoint(start+unit*1.4f));Check((point.Position-(start+unit*.6f)).sqrMagnitude<1e-6f,"Only selected axis changes "+axis);
    d.EndDrag();d.MovePoint(point.Id,start);d.FlushChanges();
   }
   camera.transform.position=start-Vector3.forward*10;camera.transform.LookAt(start);d.Select(point.Id);Check(!d.gizmo.Available[2]&&!d.BeginAxisDrag(2,camera.WorldToScreenPoint(start)),"View-aligned axis disabled");camera.transform.SetPositionAndRotation(originalPosition,originalRotation);
   d.ClearPoints();var p1=d.AddPoint(Vector3.zero,false);var p2=d.AddPoint(Vector3.zero,false);d.FlushChanges();Vector2 center=camera.WorldToScreenPoint(Vector3.zero);
   Check(d.HitTest(center)==p2.Id,"Overlap picks latest");d.Select(p1.Id);Check(d.HitTest(center)==p1.Id,"Selected overlap priority");
   MouseEvent(mouse,center,true);d.SendMessage("Update");MouseEvent(mouse,center);d.SendMessage("Update");Check(d.SelectedId==p1.Id,"Selected overlap remains selected");
   d.RemovePoint(d.SelectedId.Value);d.FlushChanges();Check(d.Points.Count==1&&!d.SelectedId.HasValue,"Delete selection count="+d.Points.Count+" selection="+d.SelectedId+" keyboard="+Keyboard.current.name+" paused="+EditorApplication.isPaused);
   d.Select(p2.Id);d.Toolbar.countInput.ActivateInputField();d.Toolbar.countInput.Select();EditorApplication.Step();await Task.Delay(180);Check(d.Toolbar.countInput.isFocused,"Input field focused");
   d.BeginFly(canvas);Check(!d.IsFlying,"Focused input isolates camera");d.Toolbar.countInput.DeactivateInputField();EventSystem.current.SetSelectedGameObject(null);
   int count=d.Points.Count;Click(mouse,d.Toolbar.addButton);d.FlushChanges();Check(d.Points.Count==count+1,"Add button event dispatch");var added=d.Points.Last();Check(d.SelectedId==added.Id&&(added.Position-(camera.transform.position+camera.transform.forward*8)).sqrMagnitude<1e-6f,"Add position and selection");
   d.BeginLeft(Screen(d.Toolbar.randomButton));Check(d.SelectedId==added.Id,"UI does not select world");
   double worst=0,total=0;foreach(int n in new[]{1,2,20,1000}){Check(d.Randomize(n.ToString(),out var error),"Random "+n+" "+error);d.FlushChanges();Check(d.Points.Count==n&&!d.SelectedId.HasValue,"Random count and selection");Rect area=d.RandomScreenRect();foreach(var p in d.Points){Vector3 s=camera.WorldToScreenPoint(p.Position);Check(area.Contains(s)&&s.z>=5.999f&&s.z<=14.001f,"Random visible area and depth");}worst=Math.Max(worst,d.LastComputeMilliseconds);Check(camera.transform.position==originalPosition,"Random preserves camera");}
   foreach(string invalid in new[]{"","0","-1","1.5","abc","1001"}){Check(!d.Randomize(invalid,out _)&&d.Points.Count==1000,"Invalid preserves points "+invalid);}
   d.Toolbar.countInput.text="20";Click(mouse,d.Toolbar.randomButton);d.FlushChanges();Check(d.Points.Count==20&&d.Toolbar.pointCount.text=="20","Random button event dispatch and statistics");
   d.Randomize("1000",out _);d.FlushChanges();var mesh=d.surface.frontFilter.sharedMesh;int objectCount=d.pointsRoot.childCount;int gc=GC.CollectionCount(0);
   for(int i=0;i<50;i++){revision=d.HullRevision;var p=d.Points[0];d.MovePoint(p.Id,p.Position+Vector3.right*.001f);d.MovePoint(p.Id,p.Position+Vector3.up*.001f);Check(d.HullRevision==revision,"Deferred update");d.FlushChanges();Check(d.HullRevision==revision+1,"One flush for multiple point changes");total+=d.LastComputeMilliseconds;worst=Math.Max(worst,d.LastComputeMilliseconds);Check(d.surface.frontFilter.sharedMesh==mesh&&d.pointsRoot.childCount==objectCount,"Mesh and node reuse");}
   int collections=GC.CollectionCount(0)-gc;
   d.ClearPoints();d.FlushChanges();Check(d.Hull.Dimension==HullDimension.Empty&&d.Toolbar.hullCount.text=="0"&&!d.gizmo.gameObject.activeSelf,"Empty clean state");
   foreach(var p in d.initialPoints)d.AddPoint(p,false);d.FlushChanges();d.Select(d.Points[2].Id);Canvas.ForceUpdateCanvases();
   return new{passed=true,checks,random1000DragAverageMs=total/50,worstMs=worst,generation0CollectionsDuring50Drags=collections,width=camera.pixelWidth,height=camera.pixelHeight};
  }finally{d.CancelGestures();camera.transform.SetPositionAndRotation(originalPosition,originalRotation);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);if(oldMouse!=null)oldMouse.MakeCurrent();if(oldKeyboard!=null)oldKeyboard.MakeCurrent();InputSystem.settings.updateMode=oldMode;EditorApplication.isPaused=paused;}
 }
}
