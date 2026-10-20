using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class FinishUnifiedMenu {
 public static string RestoreView() {
 var asm=typeof(Editor).Assembly; var type=asm.GetType("UnityEditor.GameView"); var view=EditorWindow.GetWindow(type);
 var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 type.GetProperty("selectedSizeIndex",flags).SetValue(view,SessionState.GetInt("UnifiedMenuOriginalView",6));
 var st=asm.GetType("UnityEditor.GameViewSizes");var single=typeof(ScriptableSingleton<>).MakeGenericType(st);
 var sizes=single.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
 var group=st.GetMethod("GetGroup",flags).Invoke(sizes,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});var gt=group.GetType();
 int b=(int)gt.GetMethod("GetBuiltinCount").Invoke(group,null),c=(int)gt.GetMethod("GetCustomCount").Invoke(group,null);
 for(int i=b+c-1;i>=b;i--){var s=gt.GetMethod("GetGameViewSize").Invoke(group,new object[]{i}); if((string)s.GetType().GetProperty("baseText").GetValue(s)=="Hull verification 800x900")gt.GetMethod("RemoveCustomSize").Invoke(group,new object[]{i});}
 view.Focus();view.Repaint();EditorApplication.QueuePlayerLoopUpdate(); return "View restored";
 }
 public static string Cleanup() {
 if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
 AssetDatabase.DeleteAsset("Assets/UnifiedMenuPreview.png");AssetDatabase.DeleteAsset("Assets/UnifiedCreditsPreview.png");AssetDatabase.DeleteAsset("Assets/UnifiedMenuNarrow.png");
 AssetDatabase.ImportPackage("/private/tmp/visualalgo-original-assets.unitypackage",false);
 AssetDatabase.Refresh();return "Temporary previews removed; original cached assets restored";
 }
}
