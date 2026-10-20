using System;using UnityEditor;using UnityEngine;using VisualAlgo.Managers.UI.StartMenu;
public static class RemoveMenuHoverScale {
 public static string Run(){if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");const string path="Assets/StartMenu/Prefabs/StartMenuPanel.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{int count=0;foreach(var scale in root.GetComponentsInChildren<StartMenuHoverScale>(true)){UnityEngine.Object.DestroyImmediate(scale);count++;}PrefabUtility.SaveAsPrefabAsset(root,path);return "Removed scale components: "+count;}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
