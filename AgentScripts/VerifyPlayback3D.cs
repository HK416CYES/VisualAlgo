using System;using System.Linq;using ConvexHullDemo3D;using UnityEditor;using UnityEngine;using UnityEngine.UI;
public static class VerifyPlayback3D {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 public static object Run(){checks=0;var d=UnityEngine.Object.FindFirstObjectByType<ConvexHull3DController>();var p=d.playback;p.Stop();bool paused=EditorApplication.isPaused;EditorApplication.isPaused=true;
  var camera=d.worldCamera;var origin=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
  try{
   Check(d.Toolbar.algorithmDropdown.options.Count==4,"Four algorithms");var points=d.Points.Select(x=>x.Position).ToArray();var ids=d.Points.Select(x=>x.Id).ToArray();int revision=d.HullRevision;var hull=d.Hull;int loops=0;
   for(int algorithm=0;algorithm<4;algorithm++){
    p.SelectAlgorithm(algorithm);d.Select(d.Points[0].Id);camera.transform.position=origin+Vector3.right*2;var before=camera.transform.position;
    Check(p.Play(out var error),"Play "+error);Check(camera.transform.position==before&&camera.transform.rotation==rotation&&camera.fieldOfView==fov,"Play never moves camera");
    Check(d.PlaybackPanel.IsVisible&&d.PlaybackPanel.gameObject.activeInHierarchy,"Playback panel is visible");
    Check(!d.surface.gameObject.activeSelf&&!d.gizmo.gameObject.activeSelf&&!d.SelectedId.HasValue,"Realtime hull and selection hidden");
    var block=new MaterialPropertyBlock();foreach(var point in d.Points){point.View.dot.GetPropertyBlock(block);Check(block.GetColor("_BaseColor")==d.innerColor,"Original point colors neutral");}
    Check(!d.Toolbar.algorithmDropdown.interactable&&!d.Toolbar.addButton.interactable&&!d.Toolbar.randomButton.interactable&&!d.Toolbar.countInput.interactable,"Editing UI locked");
    d.MovePoint(ids[0],Vector3.one*999);d.RemovePoint(ids[0]);d.AddPoint(Vector3.one*999);d.ClearPoints();Check(!d.Randomize("20",out _),"Random controller lock");Check(d.Points.Count==points.Length&&d.Points[0].Position==points[0],"Controller locks edits");
    p.SelectAlgorithm((algorithm+1)%4);Check((int)p.SelectedAlgorithm==algorithm,"Algorithm locked");p.Tick(0);p.TogglePause();var step=p.Session.VisibleSteps;var work=p.Session.WorkUnits;p.Tick(100);Check(p.Session.VisibleSteps==step&&p.Session.WorkUnits==work,"Pause freezes calculation");
    d.BeginFly(new Vector2(camera.pixelWidth*.1f,camera.pixelHeight*.5f));Check(d.IsFlying,"Camera enabled while paused");d.Fly(new Vector2(2,3),Vector3.forward,.1f);Check(camera.transform.position!=before,"Paused fly moves");d.EndFly();var moved=camera.transform.position;var movedRotation=camera.transform.rotation;
    p.NextStep();p.Tick(0);p.Tick(1);Check(p.Session.VisibleSteps==step+1&&p.IsPaused&&p.CanStep,"One step and stay paused");
    p.SetSpeed(4);Check(p.Speed==4,"Speed selection");p.TogglePause();d.BeginFly(new Vector2(camera.pixelWidth*.1f,camera.pixelHeight*.5f));Check(d.IsFlying,"Camera enabled while running");d.Fly(Vector2.zero,Vector3.right,.1f);d.EndFly();moved=camera.transform.position;
    var session=p.Session;while(p.IsActive){p.Tick(2);if(++loops>30000)throw new Exception("Playback failed to finish: "+p.Message);}
    Check(session.Result!=null&&new System.Collections.Generic.HashSet<Vector3>(session.Result.Vertices).SetEquals(hull.Vertices),"Teaching final result "+algorithm);
    Check(camera.transform.position==moved&&camera.transform.rotation==movedRotation&&camera.fieldOfView==fov,"Animation and completion never move camera");
    Check(d.HullRevision==revision&&ReferenceEquals(d.Hull,hull)&&d.Points.Select(x=>x.Id).SequenceEqual(ids)&&d.Points.Select(x=>x.Position).SequenceEqual(points),"Live hull, IDs, points and statistics unchanged");
    Check(d.surface.gameObject.activeSelf&&!p.overlay.gameObject.activeSelf&&p.overlay.ActiveLineCount==0&&!d.PlaybackPanel.IsVisible,"Cleanup restores realtime view");
    Check(d.Toolbar.algorithmDropdown.interactable&&d.Toolbar.addButton.interactable,"Editing restored");camera.transform.SetPositionAndRotation(origin,rotation);
    Check(p.Play(out error),"Replay");p.Tick(0);p.Stop();Check(!p.IsActive&&p.overlay.ActiveLineCount==0,"Immediate stop cleanup");
   }
   p.SelectAlgorithm(2);p.SetSpeed(2);Check(p.Play(out _),"Preview start");p.TogglePause();for(int i=0;i<3000;i++){p.NextStep();p.Tick(0);p.Tick(1);if(p.Session.Current.Preview!=null&&p.overlay.VisibleFaceCount>0)break;}Repaint();return new{passed=true,checks,loops,preview=p.Message};
  }finally{camera.transform.SetPositionAndRotation(origin,rotation);EditorApplication.isPaused=paused;}
 }
 static void Repaint(){Canvas.ForceUpdateCanvases();EditorApplication.delayCall+=()=>{if(Application.isPlaying){EditorApplication.isPaused=true;EditorApplication.Step();}};}
 public static string PreviewAction(int algorithm,string kind){var d=UnityEngine.Object.FindFirstObjectByType<ConvexHull3DController>();var p=d.playback;p.Stop();p.SelectAlgorithm(algorithm);if(!p.Play(out var error))throw new Exception(error);p.TogglePause();for(int i=0;i<3000&&p.IsActive;i++){p.NextStep();p.Tick(0);p.Tick(1);if(p.Session.Current.Kind.ToString()==kind&&p.Session.VisibleSteps>8){Repaint();return p.Message;}}throw new Exception("Stage not found");}
 public static string Preview(int algorithm,int steps){var d=UnityEngine.Object.FindFirstObjectByType<ConvexHull3DController>();var p=d.playback;p.Stop();p.SelectAlgorithm(algorithm);if(!p.Play(out var error))throw new Exception(error);p.TogglePause();for(int i=0;i<steps&&p.IsActive;i++){p.NextStep();p.Tick(0);p.Tick(1);}return p.Message;}
}
