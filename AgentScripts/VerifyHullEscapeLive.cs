using System;using UnityEditor;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using VisualAlgo.Managers.UI;
public static class VerifyHullEscapeLive {
 public static string Begin(){var menu=UnityEngine.Object.FindFirstObjectByType<PauseMenuController>();menu.ResumeGame();EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();EditorApplication.isPaused=false;
 var old=Keyboard.current;var key=InputSystem.AddDevice<Keyboard>();key.MakeCurrent();bool background=Application.runInBackground;Application.runInBackground=true;var behavior=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
 int stage=0;double next=EditorApplication.timeSinceStartup+.3;Action pending=null;int frames=Time.frameCount;
 Action input=()=>{if(InputState.currentUpdateType==InputUpdateType.Dynamic&&pending!=null){var action=pending;pending=null;action();}};InputSystem.onBeforeUpdate+=input;
 EditorApplication.CallbackFunction tick=null;Action<string> finish=result=>{EditorApplication.update-=tick;InputSystem.onBeforeUpdate-=input;InputSystem.RemoveDevice(key);if(old!=null)old.MakeCurrent();Application.runInBackground=background;InputSystem.settings.backgroundBehavior=behavior;SessionState.SetString("HullEscapeResult",result);};
 tick=()=>{if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.3;switch(stage++){
 case 0:pending=()=>InputSystem.QueueStateEvent(key,new KeyboardState(Key.Escape));break;
 case 1:if(!menu.IsPaused){finish("FAIL opening frames="+(Time.frameCount-frames));return;}pending=()=>InputSystem.QueueStateEvent(key,new KeyboardState());break;
 case 2:pending=()=>InputSystem.QueueStateEvent(key,new KeyboardState(Key.Escape));break;
 case 3:if(menu.IsPaused){finish("FAIL closing");return;}pending=()=>InputSystem.QueueStateEvent(key,new KeyboardState());break;
 case 4:pending=()=>InputSystem.QueueStateEvent(key,new KeyboardState(Key.Escape));break;
 case 5:finish(menu.IsPaused?"PASS live ESC opens, closes and reopens menu":"FAIL reopening");break;
 }};SessionState.SetString("HullEscapeResult","running");EditorApplication.update+=tick;return "Live ESC test scheduled";
 }
}
