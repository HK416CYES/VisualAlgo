using UnityEngine;using UnityEngine.UI;using UnityEngine.SceneManagement;using UnityEngine.Events;using UIFramework.Panel;
namespace VisualAlgo.StartMenu {
 public sealed class StartMenuPanel:PanelController {
  public Button[] entries;public Button credits,quit;public StartMenuBootstrap owner;
  static readonly string[] scenes={"01-Sort","02-GridMapNav","03-BinaryTree","ConvexHullDemo","ConvexHull3DDemo"};UnityAction[] actions;
  protected override void AddListeners(){actions=new UnityAction[entries.Length];for(int i=0;i<entries.Length;i++){int index=i;actions[i]=()=>{Time.timeScale=1;SceneManager.LoadScene(scenes[index]);};entries[i].onClick.AddListener(actions[i]);}credits.onClick.AddListener(Credits);quit.onClick.AddListener(Quit);}
  protected override void RemoveListeners(){if(actions!=null)for(int i=0;i<entries.Length;i++)if(entries[i]!=null)entries[i].onClick.RemoveListener(actions[i]);if(credits)credits.onClick.RemoveListener(Credits);if(quit)quit.onClick.RemoveListener(Quit);}
  void Credits(){owner.OpenDialog(false);}void Quit(){owner.OpenDialog(true);}
 }
}
