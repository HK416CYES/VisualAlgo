using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public static class IntegrateConvexHulls {
 const string RendererPath="Assets/ConvexHull3DDemo/HullForwardRenderer.asset";
 static readonly string[] Scenes={"Assets/Scenes/ConvexHullDemo.unity","Assets/Scenes/ConvexHull3DDemo.unity"};
 public static object Configure(){
  var setup=EditorSceneManager.GetSceneManagerSetup();
  foreach(var s in setup)if(s.isLoaded&&UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty)throw new Exception("Unsaved scene: "+s.path);
  var pipelines=AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset").Select(g=>AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
  if(pipelines.Length!=1)throw new Exception("Review renderer indices for multiple pipelines before proceeding");
  var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
  if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();renderer.name="Hull Forward Renderer";AssetDatabase.CreateAsset(renderer,RendererPath);}
  var so=new SerializedObject(pipelines[0]);var list=so.FindProperty("m_RendererDataList");int index=-1;
  for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)index=i;
  if(index<0){index=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipelines[0]);}
  foreach(var path in new[]{"Assets/ConvexHullDemo/Prefabs/HullUIFrame.prefab","Assets/ConvexHull3DDemo/Prefabs/Hull3DUIFrame.prefab"}){
   var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var c in root.GetComponentsInChildren<UniversalAdditionalCameraData>(true))c.SetRenderer(index);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  var overlayPath="Assets/ConvexHull3DDemo/Prefabs/HullTeachingOverlay3D.prefab";
  var overlay=PrefabUtility.LoadPrefabContents(overlayPath);try{var split=overlay.transform.Find("Merge split plane");if(split!=null){UnityEngine.Object.DestroyImmediate(split.gameObject);PrefabUtility.SaveAsPrefabAsset(overlay,overlayPath);}}finally{PrefabUtility.UnloadPrefabContents(overlay);}
  try{foreach(var path in Scenes){var scene=EditorSceneManager.OpenScene(path);foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<UniversalAdditionalCameraData>(true))c.SetRenderer(index);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}}
  finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  AssetDatabase.SaveAssetIfDirty(renderer);AssetDatabase.SaveAssetIfDirty(pipelines[0]);return new{rendererIndex=index,pipeline=AssetDatabase.GetAssetPath(pipelines[0]),defaultIndex=so.FindProperty("m_DefaultRendererIndex").intValue};
 }
 public static object AuditScenes(){
  var setup=EditorSceneManager.GetSceneManagerSetup();foreach(var s in setup)if(s.isLoaded&&UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty)throw new Exception("Unsaved scene");
  var results=new System.Collections.Generic.List<string>();
  try{foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath)){
   var scene=EditorSceneManager.OpenScene(path);int missing=0;foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
   if(missing!=0)throw new Exception(path+" missing scripts "+missing);results.Add(path+" OK");
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}return results.ToArray();
 }
}
