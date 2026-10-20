using System;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEngine.InputSystem.UI;using UnityEngine.Rendering.Universal;using UIFramework;using VisualAlgo.StartMenu;
public static class BuildUnifiedStartMenu {
 const string Root="Assets/StartMenu";static Font font;static Color Ink=new Color(.1f,.16f,.2f),Muted=new Color(.4f,.47f,.5f),Paper=new Color(.96f,.965f,.95f),Teal=new Color(.04f,.49f,.46f);
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;if(parent)r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
 static MenuGraphic Surface(string name,Transform parent,float x,float y,float w,float h,Color color,float radius=16){var r=Rect(name,parent,x,y,w,h);var g=r.gameObject.AddComponent<MenuGraphic>();g.color=color;g.radius=radius;g.raycastTarget=false;return g;}
 static Text Label(string name,Transform parent,string text,float x,float y,float w,float h,int size,Color color,FontStyle style=FontStyle.Normal){var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.fontStyle=style;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
 static Button Button(string name,Transform parent,string text,float x,float y,float w,float h,Color background,Color foreground){var bg=Surface(name,parent,x,y,w,h,background,10);bg.raycastTarget=true;var b=bg.gameObject.AddComponent<Button>();b.targetGraphic=bg;var c=b.colors;c.highlightedColor=new Color(.9f,.96f,.94f);c.pressedColor=new Color(.8f,.88f,.86f);c.selectedColor=c.highlightedColor;b.colors=c;var label=Label("Label",bg.transform,text,14,0,w-28,h,18,foreground);label.alignment=TextAnchor.MiddleCenter;return b;}
 static void Field(UnityEngine.Object obj,string name,UnityEngine.Object value){var so=new SerializedObject(obj);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static Button Card(Transform parent,string name,string index,string title,string description,string tags,MenuGraphic.Shape shape,float x,float y,float w,float h,Color accent,bool wide){
  Surface(name+" Shadow",parent,x,y+5,w,h,new Color(.09f,.17f,.2f,.05f),18);
  var card=Surface(name,parent,x,y,w,h,Color.white,18);card.raycastTarget=true;var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card;var colors=button.colors;colors.highlightedColor=new Color(.95f,.985f,.98f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.88f,.95f,.93f);button.colors=colors;card.gameObject.AddComponent<MenuHover>();
  Label("Index",card.transform,index,24,18,75,26,15,Muted);
  if(wide){Label("Title",card.transform,title,24,75,230,40,27,Ink,FontStyle.Bold);Label("Description",card.transform,description,24,122,230,64,16,Muted);var art=Surface("Preview",card.transform,w-185,25,160,130,accent);art.shape=shape;art.faint=new Color(.68f,.76f,.78f);}
  else {var art=Surface("Preview",card.transform,150,19,w-172,106,accent);art.shape=shape;art.faint=new Color(.78f,.85f,.86f);Label("Title",card.transform,title,24,130,w-48,40,26,Ink,FontStyle.Bold);Label("Description",card.transform,description,24,179,w-48,48,16,Muted);}
  Surface("Rule",card.transform,24,h-48,w-48,1,new Color(.9f,.93f,.92f),0);Label("Tags",card.transform,tags,24,h-35,w-66,28,13,accent);Label("Arrow",card.transform,"→",w-48,h-38,28,30,23,accent);
  return button;
 }
 public static string Run(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");foreach(var s in EditorSceneManager.GetSceneManagerSetup())if(s.isLoaded&&UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty)throw new Exception("Unsaved scene: "+s.path);
  EditorSceneManager.OpenScene("Assets/Scenes/00-StartMenu.unity");
  var original=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x.name=="Text_Introduce");var previous=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/StartMenuDialog.prefab");string bio=original?"VisualAlgo  /  HK416-C\n\n"+original.text.Trim():previous?previous.GetComponent<StartMenuDialog>().creditsText:"VisualAlgo  /  HK416-C";
  font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ConvexHullDemo/Fonts/GenSenRounded2-M.ttc");if(!font)throw new Exception("Font missing");
  if(!AssetDatabase.IsValidFolder(Root+"/Prefabs"))AssetDatabase.CreateFolder(Root,"Prefabs");
  string framePath=Root+"/Prefabs/StartMenuFrame.prefab";if(!AssetDatabase.LoadAssetAtPath<GameObject>(framePath))AssetDatabase.CopyAsset("Assets/ConvexHullDemo/Prefabs/HullUIFrame.prefab",framePath);
  var frame=PrefabUtility.LoadPrefabContents(framePath);try{frame.name="StartMenuFrame";var scaler=frame.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1440,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;foreach(var c in frame.GetComponentsInChildren<Camera>(true))c.GetUniversalAdditionalCameraData().SetRenderer(1);PrefabUtility.SaveAsPrefabAsset(frame,framePath);}finally{PrefabUtility.UnloadPrefabContents(frame);}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=Rect("StartMenuPanel",null,0,0,1440,900);Stretch(root);var panel=root.gameObject.AddComponent<StartMenuPanel>();root.gameObject.AddComponent<CanvasGroup>();
  var backdrop=Surface("Background",root,0,0,1440,900,Paper,0);Stretch((RectTransform)backdrop.transform);
  var bleed=Surface("Sidebar extension",root,0,0,252,900,Ink,0);var br=(RectTransform)bleed.transform;br.anchorMin=Vector2.zero;br.anchorMax=new Vector2(.5f,1);br.offsetMin=Vector2.zero;br.offsetMax=new Vector2(-468,0);
  var artboard=Rect("Layout",root,0,0,1440,900);artboard.anchorMin=artboard.anchorMax=artboard.pivot=new Vector2(.5f,.5f);artboard.anchoredPosition=Vector2.zero;
  Surface("Sidebar",artboard,0,0,252,900,Ink,0);Surface("Brand mark",artboard,35,57,34,5,new Color(.27f,.8f,.68f),2);
  Label("Brand",artboard,"VISUAL\nALGO",34,84,200,130,43,Color.white,FontStyle.Bold);Label("Brand subtitle",artboard,"交互式算法实验室",36,220,190,30,16,new Color(.6f,.72f,.75f));
  Surface("Sidebar divider",artboard,35,279,182,1,new Color(.3f,.39f,.42f),0);Surface("Active navigation",artboard,23,314,206,54,new Color(.18f,.27f,.3f),10);Surface("Active dot",artboard,39,337,7,7,new Color(.27f,.8f,.68f),4);Label("Navigation",artboard,"探索算法",61,326,160,30,19,Color.white);Label("Count",artboard,"05  /  可交互实验",37,393,180,30,15,new Color(.6f,.72f,.75f));
  Label("Sidebar note",artboard,"观察每一步，\n理解每一种选择。",36,530,185,80,20,new Color(.78f,.85f,.86f));
  panel.credits=Button("Btn_Producer",artboard,"关于 / 制作人",32,735,188,46,new Color(.18f,.27f,.3f),Color.white);panel.quit=Button("Btn_Exit",artboard,"退出应用",32,791,188,46,Ink,new Color(.7f,.77f,.8f));Label("Version",artboard,"VISUALALGO   /   V0.2",36,862,200,24,11,new Color(.5f,.62f,.65f));
  Label("Eyebrow",artboard,"EXPLORE  /  算法探索",308,60,700,28,15,Teal);Label("Heading",artboard,"让算法，变得看得见。",306,105,1000,74,43,Ink,FontStyle.Bold);Label("Intro",artboard,"从排序与搜索，到空间中的几何边界。选择一个实验，开始探索。",310,194,1050,36,18,Muted);Label("Section",artboard,"基础与结构",310,257,500,30,17,Ink);Label("Section count",artboard,"01 — 03",1220,259,120,25,13,Muted);
  panel.entries=new Button[5];
  panel.entries[0]=Card(artboard,"Btn_SortScene","01","排序算法","比较与交换，让无序变得有序。","比较 · 交换 · 归并",MenuGraphic.Shape.Sorting,310,300,330,276,new Color(.08f,.5f,.59f),false);
  panel.entries[1]=Card(artboard,"Btn_GridMapNavScene","02","路径探索","穿过障碍，寻找抵达终点的路。","搜索 · 网格 · 最短路",MenuGraphic.Shape.Grid,660,300,330,276,Teal,false);
  panel.entries[2]=Card(artboard,"Btn_BinaryTreeScene","03","二叉树","从节点连接中，理解树的结构。","节点 · 插入 · 平衡",MenuGraphic.Shape.Tree,1010,300,330,276,new Color(.47f,.4f,.7f),false);
  Label("Geometry section",artboard,"计算几何",310,609,500,30,17,Ink);Label("Geometry count",artboard,"04 — 05",1220,611,120,25,13,Muted);
  panel.entries[3]=Card(artboard,"Btn_ConvexHull2DScene","04","二维凸包","在平面点集中，\n勾勒最外侧的边界。","平面 · 点集 · 算法演示",MenuGraphic.Shape.Hull2D,310,650,505,216,new Color(.17f,.48f,.69f),true);
  panel.entries[4]=Card(artboard,"Btn_ConvexHull3DScene","05","三维凸包","走进三维空间，\n观察凸包的表面与结构。","空间 · 面片 · 算法演示",MenuGraphic.Shape.Hull3D,835,650,505,216,new Color(.78f,.43f,.25f),true);
  root.gameObject.AddComponent<StartMenuResponsive>();
  var mainAsset=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/StartMenuPanel.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);
  var dialogRoot=Rect("StartMenuDialog",null,0,0,1440,900);Stretch(dialogRoot);var dialog=dialogRoot.gameObject.AddComponent<StartMenuDialog>();var props=new SerializedObject(dialog);props.FindProperty("properties").FindPropertyRelative("priority").enumValueIndex=3;props.ApplyModifiedPropertiesWithoutUndo();
  var dim=Surface("Dim",dialogRoot,0,0,1440,900,new Color(.035f,.08f,.1f,.65f),0);Stretch((RectTransform)dim.transform);dim.raycastTarget=true;
  var card=Surface("Dialog card",dialogRoot,0,0,680,510,Paper,22);var cr=(RectTransform)card.transform;cr.anchorMin=cr.anchorMax=cr.pivot=new Vector2(.5f,.5f);cr.anchoredPosition=Vector2.zero;card.raycastTarget=true;
  dialog.heading=Label("Heading",card.transform,"关于这个项目",40,38,570,52,32,Ink,FontStyle.Bold);dialog.body=Label("Body",card.transform,"",40,118,600,270,19,Muted);dialog.body.lineSpacing=1.4f;dialog.creditsText=bio;
  dialog.close=Button("Close",card.transform,"×",600,24,48,48,Paper,Muted);dialog.cancel=Button("Cancel",card.transform,"返回实验室",40,420,280,54,Color.white,Ink);dialog.confirm=Button("Confirm quit",card.transform,"确认退出",340,420,300,54,Ink,Color.white);
  var dialogAsset=PrefabUtility.SaveAsPrefabAsset(dialogRoot.gameObject,Root+"/Prefabs/StartMenuDialog.prefab");UnityEngine.Object.DestroyImmediate(dialogRoot.gameObject);
  var setting=AssetDatabase.LoadAssetAtPath<UISetting>(Root+"/StartMenuSetting.asset");if(!setting){setting=ScriptableObject.CreateInstance<UISetting>();typeof(UISetting).GetField("screensToRegister",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(setting,new System.Collections.Generic.List<GameObject>());AssetDatabase.CreateAsset(setting,Root+"/StartMenuSetting.asset");}
  var config=new SerializedObject(setting);config.FindProperty("templateUIPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(framePath).GetComponent<UIFrame>();var screens=config.FindProperty("screensToRegister");screens.arraySize=2;screens.GetArrayElementAtIndex(0).objectReferenceValue=mainAsset;screens.GetArrayElementAtIndex(1).objectReferenceValue=dialogAsset;config.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(setting);
  var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Paper;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=0;camera.GetUniversalAdditionalCameraData().SetRenderer(1);
  // StartMenuFrame contains the sole EventSystem and its Input System module.
  var boot=new GameObject("Start Menu",typeof(StartMenuBootstrap),typeof(GameManager));boot.GetComponent<StartMenuBootstrap>().setting=setting;
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/00-StartMenu.unity");return "Saved new UIFramework start scene, three prefabs and UISetting.";
 }
}
