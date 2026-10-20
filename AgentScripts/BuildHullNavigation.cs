using System;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEditor;using UnityEditor.Events;using UnityEditor.SceneManagement;using TMPro;using VisualAlgo.Managers.UI;using VisualAlgo.Managers.UI.StartMenu;using VisualAlgo.ConvexHulls;using ConvexHullDemo;using ConvexHullDemo3D;
public static class BuildHullNavigation {
 static void Set(UnityEngine.Object obj,string field,UnityEngine.Object value){var s=new SerializedObject(obj);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static Image Image(string name,Transform parent,Vector2 position,Vector2 size,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
 static void Edge(Transform parent,Vector2 a,Vector2 b){var im=Image("Hull edge",parent,(a+b)*.5f,new Vector2(Vector2.Distance(a,b),3),new Color(.15f,.45f,.8f));im.transform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);}
 static void Card(Button source,string name,string title,UnityEngine.Events.UnityAction action,bool spatial){
  if(GameObject.Find(name)!=null)return;
  var go=UnityEngine.Object.Instantiate(source.gameObject,source.transform.parent);go.name=name;
  var visual=go.GetComponent<StartMenuBinaryTreeButtonVisualizer>();if(visual)UnityEngine.Object.DestroyImmediate(visual);
  var preview=go.transform.Find("BinaryTreePreview");if(preview)UnityEngine.Object.DestroyImmediate(preview.gameObject);
  go.GetComponentInChildren<TMP_Text>(true).text=title;
  var btn=go.GetComponent<Button>();btn.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(btn.onClick,action);
  var root=new GameObject("Hull preview",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(go.transform,false);root.anchoredPosition=new Vector2(0,45);root.sizeDelta=new Vector2(200,180);
  Vector2[] points=spatial?new[]{new Vector2(-75,-40),new Vector2(25,-65),new Vector2(80,-10),new Vector2(-20,15),new Vector2(-75,55),new Vector2(25,30),new Vector2(80,85),new Vector2(-20,110)}:new[]{new Vector2(-85,-35),new Vector2(-65,70),new Vector2(20,100),new Vector2(85,40),new Vector2(55,-65),new Vector2(-20,-75)};
  if(spatial){for(int i=0;i<4;i++){Edge(root,points[i],points[(i+1)%4]);Edge(root,points[i+4],points[(i+1)%4+4]);Edge(root,points[i],points[i+4]);}}
  else for(int i=0;i<points.Length;i++)Edge(root,points[i],points[(i+1)%points.Length]);
  foreach(var p in points)Image("Vertex",root,p,Vector2.one*10,new Color(.15f,.45f,.8f));
 }
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");var setup=EditorSceneManager.GetSceneManagerSetup();foreach(var x in setup)if(x.isLoaded&&UnityEngine.SceneManagement.SceneManager.GetSceneByPath(x.path).isDirty)throw new Exception("Unsaved scene: "+x.path);
  try{
   var menuScene=EditorSceneManager.OpenScene("Assets/Scenes/00-StartMenu.unity");var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();var source=GameObject.Find("Btn_BinaryTreeScene").GetComponent<Button>();
   Card(source,"Btn_ConvexHull2DScene","二维凸包",manager.Btn_ConvexHull2DScene,false);Card(source,"Btn_ConvexHull3DScene","三维凸包",manager.Btn_ConvexHull3DScene,true);
   string[] names={"Btn_SortScene","Btn_GridMapNavScene","Btn_BinaryTreeScene","Btn_ConvexHull2DScene","Btn_ConvexHull3DScene","Btn_Producer","Btn_Exit"};
   for(int i=0;i<names.Length;i++){var r=(RectTransform)GameObject.Find(names[i]).transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=i<4?new Vector2((i-1.5f)*340,75):new Vector2((i-5)*340,-245);r.localScale=Vector3.one*.7f;}
   EditorSceneManager.MarkSceneDirty(menuScene);EditorSceneManager.SaveScene(menuScene);
   foreach(var framePath in new[]{"Assets/ConvexHullDemo/Prefabs/HullUIFrame.prefab","Assets/ConvexHull3DDemo/Prefabs/Hull3DUIFrame.prefab"}){var root=PrefabUtility.LoadPrefabContents(framePath);try{if(root.GetComponent<CanvasGroup>()==null)root.AddComponent<CanvasGroup>();PrefabUtility.SaveAsPrefabAsset(root,framePath);}finally{PrefabUtility.UnloadPrefabContents(root);}}
   foreach(var path in new[]{"Assets/Scenes/ConvexHullDemo.unity","Assets/Scenes/ConvexHull3DDemo.unity"}){
    var scene=EditorSceneManager.OpenScene(path);if(UnityEngine.Object.FindFirstObjectByType<HullPauseIntegration>()!=null)continue;
    var host=new GameObject("Convex Hull Pause UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
    var panel=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Perfabs/ESC Pause Panel.prefab"),scene);panel.transform.SetParent(host.transform,false);var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;panel.SetActive(false);
    var controller=host.AddComponent<PauseMenuController>();Set(controller,"pausePanel",panel);
    foreach(var b in panel.GetComponentsInChildren<Button>(true)){if(b.name.StartsWith("返回游戏"))Set(controller,"resumeButton",b);else if(b.name.StartsWith("返回主菜单"))Set(controller,"mainMenuButton",b);else if(b.name.StartsWith("退出游戏"))Set(controller,"quitButton",b);}
    var slider=panel.GetComponentInChildren<Slider>(true);Set(controller,"zoomSensitivitySlider",slider);foreach(var d in panel.GetComponentsInChildren<TMP_Dropdown>(true))Set(controller,d.name.Contains("Screen Mode")?"screenModeDropdown":"resolutionDropdown",d);
    var bridge=host.AddComponent<HullPauseIntegration>();bridge.menu=controller;bridge.planar=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();bridge.spatial=UnityEngine.Object.FindFirstObjectByType<ConvexHull3DController>();bridge.sensitivity=slider;
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   var builds=EditorBuildSettings.scenes.ToList();foreach(var path in new[]{"Assets/Scenes/ConvexHullDemo.unity","Assets/Scenes/ConvexHull3DDemo.unity"}){var old=builds.Find(x=>x.path==path);if(old!=null)old.enabled=true;else builds.Add(new EditorBuildSettingsScene(path,true));}EditorBuildSettings.scenes=builds.ToArray();EditorBuildSettings.globalScenes=builds.ToArray();AssetDatabase.SaveAssets();
   return "Two menu cards, two pause prefab instances and build scene entries saved.";
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
}
