using UnityEngine;using UIFramework;
namespace VisualAlgo.StartMenu {
 public sealed class StartMenuBootstrap:MonoBehaviour {
  public UISetting setting;UIFrame frame;StartMenuPanel panel;StartMenuDialog dialog;CanvasGroup mainGroup;
  void Start(){Time.timeScale=1;frame=setting.CreateUIInstance();panel=frame.GetComponentInChildren<StartMenuPanel>(true);dialog=frame.GetComponentInChildren<StartMenuDialog>(true);panel.owner=this;dialog.owner=this;mainGroup=panel.GetComponent<CanvasGroup>();frame.ShowPanel("StartMenuPanel");}
  public void OpenDialog(bool quitting){dialog.Configure(quitting);mainGroup.interactable=false;mainGroup.blocksRaycasts=false;frame.ShowPanel("StartMenuDialog");}
  public void CloseDialog(){frame.HidePanel("StartMenuDialog");mainGroup.interactable=true;mainGroup.blocksRaycasts=true;}
 }
}
