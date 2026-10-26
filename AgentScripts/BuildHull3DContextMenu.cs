using System;using UnityEditor;using UnityEngine;using UIFramework;using ConvexHullDemo;using ConvexHullDemo3D;
public static class BuildHull3DContextMenu {
 public static string Run(){if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode");const string path="Assets/ConvexHull3DDemo/Prefabs/HullContextMenu3D.prefab";
 if(!AssetDatabase.LoadAssetAtPath<GameObject>(path))AssetDatabase.CopyAsset("Assets/ConvexHullDemo/Prefabs/HullContextMenu.prefab",path);
 var root=PrefabUtility.LoadPrefabContents(path);try {
 root.name="HullContextMenu3D";var old=root.GetComponent<HullContextMenuPanel>();var panel=root.GetComponent<HullContextMenu3D>();
 if(old){var delete=old.deleteButton;var create=old.createButton;UnityEngine.Object.DestroyImmediate(old);UnityEngine.Object.DestroyImmediate(create.gameObject);panel=root.AddComponent<HullContextMenu3D>();panel.deleteButton=delete;}
 var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(160,38);var b=(RectTransform)panel.deleteButton.transform;b.anchorMin=b.anchorMax=b.pivot=new Vector2(0,1);b.anchoredPosition=new Vector2(3,-3);b.sizeDelta=new Vector2(154,32);
 PrefabUtility.SaveAsPrefabAsset(root,path);
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 var setting=AssetDatabase.LoadAssetAtPath<UISetting>("Assets/ConvexHull3DDemo/Hull3DUISetting.asset");var so=new SerializedObject(setting);var entries=so.FindProperty("screensToRegister");var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);bool exists=false;for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).objectReferenceValue==prefab)exists=true;if(!exists){entries.InsertArrayElementAtIndex(entries.arraySize);entries.GetArrayElementAtIndex(entries.arraySize-1).objectReferenceValue=prefab;so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(setting);}
 return "Saved 3D deletion menu using 2D menu artwork and registered UISetting";
 }
}
