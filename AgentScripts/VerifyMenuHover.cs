using System;using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEditor;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using VisualAlgo.StartMenu;using VisualAlgo.Managers.UI.StartMenu;
public static class VerifyMenuHover {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static async Task<string> Run(){bool bg=Application.runInBackground;Application.runInBackground=true;try {
 EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();await Task.Delay(400);
 var p=UnityEngine.Object.FindFirstObjectByType<StartMenuPanel>();var e=new PointerEventData(EventSystem.current);
 var sort=p.entries[0].GetComponent<StartMenuSortButtonVisualizer>();var bars=(RectTransform[])typeof(StartMenuSortButtonVisualizer).GetField("barRects",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(sort);var initial=bars.Select(b=>b.anchoredPosition.x).ToArray();
 for(int i=0;i<3;i++)ExecuteEvents.Execute(p.entries[i].gameObject,e,ExecuteEvents.pointerEnterHandler);await Task.Delay(900);
 Check(bars.Where((b,i)=>Mathf.Abs(b.anchoredPosition.x-initial[i])>3).Any(),"Sort bars animate");
 Check(p.entries[1].GetComponentInChildren<StartMenuGridMapPreviewGraphic>().Progress>.8f,"Path reveals");Check(p.entries[2].GetComponentInChildren<StartMenuBinaryTreePreviewGraphic>().Progress>.8f,"Tree adjusts");
 for(int i=0;i<3;i++)ExecuteEvents.Execute(p.entries[i].gameObject,e,ExecuteEvents.pointerExitHandler);await Task.Delay(1000);
 Check(bars.Where((b,i)=>Mathf.Abs(b.anchoredPosition.x-initial[i])>1).Count()==0,"Sort restores");Check(p.entries[1].GetComponentInChildren<StartMenuGridMapPreviewGraphic>().Progress<.05f,"Path retracts");Check(p.entries[2].GetComponentInChildren<StartMenuBinaryTreePreviewGraphic>().Progress<.05f,"Tree restores");
 foreach(var b in p.entries.Skip(3)){Check(!b.GetComponent<MenuHover>()&&!b.GetComponent<StartMenuHoverScale>(),"Hull has no scale animation");ExecuteEvents.Execute(b.gameObject,e,ExecuteEvents.pointerEnterHandler);}await Task.Delay(300);foreach(var b in p.entries.Skip(3))Check(b.transform.localScale==Vector3.one,"Hull button scale stays fixed");
 var boot=UnityEngine.Object.FindFirstObjectByType<StartMenuBootstrap>();boot.OpenDialog(false);await Task.Delay(400);
 Check(!p.GetComponent<CanvasGroup>().blocksRaycasts&&!p.GetComponent<CanvasGroup>().interactable,"Modal blocks input");foreach(var b in p.entries.Concat(new[]{p.credits,p.quit}))Check(b.targetGraphic.canvasRenderer.GetColor()==Color.white,"Button remains untinted: "+b.name);
 var d=UnityEngine.Object.FindFirstObjectByType<StartMenuDialog>();Check(d.transform.Find("Dim").GetComponent<Image>().color.a==0,"No backdrop greying");Check(d.close.GetComponent<Image>().color.a==0&&!d.close.GetComponent<LaunchButtonGraphic>(),"Close only cross");foreach(var b in new[]{p.credits,p.quit,d.cancel,d.confirm})Check(!b.GetComponent<LaunchButtonGraphic>(),"Small button separate design");boot.CloseDialog();return "PASS: original three animations enter/restore; hull button scales fixed; modal blocks input without greying; cross-only close; small-button styles";
 }finally{Application.runInBackground=bg;}}
}
