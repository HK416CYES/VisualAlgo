using System;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;using UnityEngine.Rendering.Universal;using UIFramework;using VisualAlgo.StartMenu;using VisualAlgo.Managers.UI.StartMenu;
public static class BuildUnifiedStartMenu {
 const string Root="Assets/StartMenu";static Font font;static Color Ink=new Color(.08f,.09f,.1f);
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;if(parent)r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
 static Text Text(string name,Transform parent,string content,float x,float y,float w,float h,int size,Color color,TextAnchor alignment=TextAnchor.MiddleLeft){var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=content;t.fontSize=size;t.color=color;t.alignment=alignment;t.raycastTarget=false;return t;}
 static Image Image(string name,Transform parent,string path,float x,float y,float w,float h){var r=Rect(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);im.raycastTarget=false;return im;}
 static void Relative(RectTransform r,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi){r.anchorMin=min;r.anchorMax=max;r.offsetMin=lo;r.offsetMax=hi;}
 static Button Button(string name,Transform parent,float x,float y,float w,float h,Color accent){var r=Rect(name,parent,x,y,w,h);var g=r.gameObject.AddComponent<LaunchButtonGraphic>();g.color=new Color(.985f,.985f,.98f);g.accent=accent;g.compact=h<100;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=g;var c=b.colors;c.normalColor=Color.white;c.highlightedColor=Color.Lerp(Color.white,accent,.13f);c.selectedColor=c.highlightedColor;c.pressedColor=Color.Lerp(Color.white,accent,.32f);c.disabledColor=Color.white;c.fadeDuration=.12f;b.colors=c;return b;}
 static Button SmallButton(string name,Transform parent,float x,float y,float w,float h,Color fill) {
 var r=Rect(name,parent,x,y,w,h);var g=r.gameObject.AddComponent<MenuGraphic>();g.radius=4;g.color=fill;g.raycastTarget=true;
 var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.15f,.16f,.17f,.65f);outline.effectDistance=new Vector2(1,-1);
 var b=r.gameObject.AddComponent<Button>();b.targetGraphic=g;var colors=b.colors;colors.highlightedColor=new Color(.82f,.86f,.87f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.65f,.72f,.75f);colors.disabledColor=Color.white;b.colors=colors;return b;
 }
 static void Bind(UnityEngine.Object target,string field,UnityEngine.Object value) {var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static void OriginalAnimation(Button button,RectTransform art,int index) {
 art.gameObject.AddComponent<CanvasRenderer>();
 if(index==0) {
 var inner=Rect("Bars",art,0,0,150,140);inner.anchorMin=inner.anchorMax=new Vector2(.5f,.5f);inner.pivot=new Vector2(.5f,.5f);inner.anchoredPosition=Vector2.zero;
 float[] heights={90,65,120,40};var bars=new RectTransform[4];
 for(int i=0;i<4;i++){var bar=Rect("Bar_"+i,inner,0,0,23,heights[i]);bar.anchorMin=bar.anchorMax=new Vector2(0,0);bar.pivot=new Vector2(.5f,0);bar.anchoredPosition=new Vector2(21+i*36,10);var image=bar.gameObject.AddComponent<Image>();image.color=i==3?new Color(.16f,.62f,.81f):new Color(.12f+i*.1f,.12f+i*.1f,.12f+i*.1f);image.raycastTarget=false;bars[i]=bar;}
 var line=Rect("Baseline",inner,5,135,140,3);line.gameObject.AddComponent<Image>().color=Ink;line.GetComponent<Image>().raycastTarget=false;
 var controller=button.gameObject.AddComponent<StartMenuSortButtonVisualizer>();var so=new SerializedObject(controller);var refs=so.FindProperty("barRects");refs.arraySize=4;for(int i=0;i<4;i++)refs.GetArrayElementAtIndex(i).objectReferenceValue=bars[i];so.ApplyModifiedPropertiesWithoutUndo();
 } else if(index==1){var graphic=art.gameObject.AddComponent<StartMenuGridMapPreviewGraphic>();graphic.raycastTarget=false;Bind(button.gameObject.AddComponent<StartMenuGridMapButtonVisualizer>(),"previewGraphic",graphic);}
 else {var graphic=art.gameObject.AddComponent<StartMenuBinaryTreePreviewGraphic>();graphic.raycastTarget=false;Bind(button.gameObject.AddComponent<StartMenuBinaryTreeButtonVisualizer>(),"previewGraphic",graphic);}
 // 仅保留内部算法动画，按钮本体不缩放。
 }
 static Button Experiment(Transform parent,int index,string name,string title,string english,MenuGraphic.Shape shape,Color accent){var b=Button(name,parent,210+index*304,420,284,365,accent);
 var number=Text("Number",b.transform,"0"+(index+1),26,23,60,28,19,new Color(.42f,.44f,.45f));
 var art=Rect("Algorithm symbol",b.transform,0,0,0,0);Relative(art,new Vector2(.19f,.42f),new Vector2(.81f,.84f),Vector2.zero,Vector2.zero);if(index<3)OriginalAnimation(b,art,index);else {var g=art.gameObject.AddComponent<MenuGraphic>();g.shape=shape;g.color=accent;g.faint=new Color(.16f,.17f,.18f);g.raycastTarget=false;}
 var label=Text("Title",b.transform,title,0,0,0,0,34,Ink,TextAnchor.MiddleCenter);label.fontStyle=FontStyle.Bold;Relative(label.rectTransform,new Vector2(0,.24f),new Vector2(1,.39f),new Vector2(15,0),new Vector2(-15,0));
 var sub=Text("Subtitle",b.transform,english,0,0,0,0,12,new Color(.42f,.45f,.46f),TextAnchor.MiddleCenter);Relative(sub.rectTransform,new Vector2(0,.18f),new Vector2(1,.24f),Vector2.zero,Vector2.zero);
 var play=Text("Play",b.transform,"▶",0,0,0,0,25,accent,TextAnchor.MiddleCenter);Relative(play.rectTransform,new Vector2(.3f,.055f),new Vector2(.7f,.16f),Vector2.zero,Vector2.zero);return b;}
 public static string Run(){if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save user scene first");
 font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ConvexHullDemo/Fonts/GenSenRounded2-M.ttc");
 var previous=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/StartMenuDialog.prefab");string bio=previous.GetComponent<StartMenuDialog>().creditsText;
 var frame=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/StartMenuFrame.prefab");frame.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1920,1080);frame.GetComponent<CanvasScaler>().screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;PrefabUtility.SaveAsPrefabAsset(frame,Root+"/Prefabs/StartMenuFrame.prefab");PrefabUtility.UnloadPrefabContents(frame);
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 var root=Rect("StartMenuPanel",null,0,0,1920,1080);Stretch(root);var panel=root.gameObject.AddComponent<StartMenuPanel>();root.gameObject.AddComponent<CanvasGroup>();
 var background=Image("Img_Background",root,"Assets/Img/BackGround.png",0,0,1920,1080);Stretch(background.rectTransform);background.rectTransform.pivot=new Vector2(.5f,.5f);
 var ratio=background.gameObject.AddComponent<AspectRatioFitter>();ratio.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;ratio.aspectRatio=background.sprite.rect.width/background.sprite.rect.height;
 var layout=Rect("Layout",root,0,0,1920,1080);layout.anchorMin=layout.anchorMax=layout.pivot=new Vector2(.5f,.5f);layout.anchoredPosition=Vector2.zero;
 var title=Image("Img_Title",layout,"Assets/Img/Titles/Title4.png",446,60,1028,259);title.preserveAspect=true;
 var section=Text("Select experiment",layout,"—    选择算法    /    SELECT ALGORITHM    —",210,347,1500,42,20,new Color(.35f,.37f,.39f),TextAnchor.MiddleCenter);
 panel.entries=new[]{Experiment(layout,0,"Btn_SortScene","序列演绎","SORTING",MenuGraphic.Shape.Sorting,new Color(.16f,.62f,.81f)),Experiment(layout,1,"Btn_GridMapNavScene","路径探索","PATHFINDING",MenuGraphic.Shape.Grid,new Color(.18f,.66f,.55f)),Experiment(layout,2,"Btn_BinaryTreeScene","二叉树","BINARY TREE",MenuGraphic.Shape.Tree,new Color(.55f,.38f,.76f)),Experiment(layout,3,"Btn_ConvexHull2DScene","二维凸包","2D CONVEX HULL",MenuGraphic.Shape.Hull2D,new Color(.22f,.46f,.82f)),Experiment(layout,4,"Btn_ConvexHull3DScene","三维凸包","3D CONVEX HULL",MenuGraphic.Shape.Hull3D,new Color(.84f,.48f,.16f))};
 panel.credits=SmallButton("Btn_Producer",layout,624,875,320,66,new Color(.14f,.16f,.17f));Text("Label",panel.credits.transform,"制作人",24,4,272,56,25,Color.white,TextAnchor.MiddleCenter);
 panel.quit=SmallButton("Btn_Exit",layout,976,875,320,66,new Color(.98f,.98f,.97f));Text("Label",panel.quit.transform,"退出游戏",24,4,272,56,25,new Color(.65f,.12f,.18f),TextAnchor.MiddleCenter);
 var footer=Text("Version",layout,"VISUAL ALGO     /     V0.2",210,1000,1500,28,14,new Color(.4f,.41f,.42f),TextAnchor.MiddleCenter);
 var responsive=root.gameObject.AddComponent<StartMenuResponsive>();responsive.layout=layout;responsive.title=title.rectTransform;responsive.section=section.rectTransform;responsive.footer=footer.rectTransform;responsive.credits=panel.credits.GetComponent<RectTransform>();responsive.quit=panel.quit.GetComponent<RectTransform>();responsive.experiments=Array.ConvertAll(panel.entries,b=>b.GetComponent<RectTransform>());
 var main=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/StartMenuPanel.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);
 var dr=Rect("StartMenuDialog",null,0,0,1920,1080);Stretch(dr);var dialog=dr.gameObject.AddComponent<StartMenuDialog>();dialog.creditsText=bio;
 var properties=new SerializedObject(dialog);properties.FindProperty("properties").FindPropertyRelative("priority").enumValueIndex=3;properties.ApplyModifiedPropertiesWithoutUndo();
 var dim=Rect("Dim",dr,0,0,1920,1080);Stretch(dim);var dimImage=dim.gameObject.AddComponent<Image>();dimImage.color=new Color(0,0,0,0);
 var card=Rect("Dialog frame",dr,0,0,900,640);card.anchorMin=card.anchorMax=card.pivot=new Vector2(.5f,.5f);card.anchoredPosition=Vector2.zero;var cg=card.gameObject.AddComponent<LaunchButtonGraphic>();cg.color=new Color(.97f,.97f,.96f);cg.accent=new Color(.2f,.65f,.65f);
 dialog.heading=Text("Heading",card,"",54,45,720,64,38,Ink);dialog.heading.fontStyle=FontStyle.Bold;dialog.body=Text("Body",card,"",54,137,792,340,24,new Color(.26f,.28f,.29f));dialog.body.alignment=TextAnchor.UpperLeft;dialog.body.lineSpacing=1.35f;
 var close=Rect("Close",card,792,32,66,60);var hit=close.gameObject.AddComponent<Image>();hit.color=Color.clear;dialog.close=close.gameObject.AddComponent<Button>();var cross=Text("Label",close,"×",5,2,56,56,40,Ink,TextAnchor.MiddleCenter);dialog.close.targetGraphic=cross;
 dialog.cancel=SmallButton("Cancel",card,54,529,370,64,new Color(.98f,.98f,.97f));Text("Label",dialog.cancel.transform,"返回",20,3,330,58,24,Ink,TextAnchor.MiddleCenter);
 dialog.confirm=SmallButton("Confirm quit",card,476,529,370,64,new Color(.78f,.19f,.25f));Text("Label",dialog.confirm.transform,"确认退出",20,3,330,58,24,Color.white,TextAnchor.MiddleCenter);
 var da=PrefabUtility.SaveAsPrefabAsset(dr.gameObject,Root+"/Prefabs/StartMenuDialog.prefab");UnityEngine.Object.DestroyImmediate(dr.gameObject);
 var setting=AssetDatabase.LoadAssetAtPath<UISetting>(Root+"/StartMenuSetting.asset");var config=new SerializedObject(setting);var screens=config.FindProperty("screensToRegister");screens.arraySize=2;screens.GetArrayElementAtIndex(0).objectReferenceValue=main;screens.GetArrayElementAtIndex(1).objectReferenceValue=da;config.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(setting);
 var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;camera.orthographic=true;camera.cullingMask=0;camera.transform.position=new Vector3(0,0,-10);camera.GetUniversalAdditionalCameraData().SetRenderer(1);
 new GameObject("Start Menu",typeof(GameManager),typeof(StartMenuBootstrap)).GetComponent<StartMenuBootstrap>().setting=setting;
 EditorSceneManager.SaveScene(scene,"Assets/Scenes/00-StartMenu.unity");return "Saved original background/title and seven new game-style buttons using UIFramework";
 }
}
