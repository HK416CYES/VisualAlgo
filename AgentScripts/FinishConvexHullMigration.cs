using System;
using UnityEditor;
public static class FinishConvexHullMigration {
 public static string Clean(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
  foreach(var p in new[]{"Assets/Hull2DMigrationCheck.png","Assets/Hull3DMigrationCheck.png"})if(AssetDatabase.LoadMainAssetAtPath(p)!=null)AssetDatabase.DeleteAsset(p);
  return "Generated preview images removed. Existing scene, font and material assets are not saved.";
 }
}
