using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;using UIFramework.Panel;
namespace VisualAlgo.StartMenu {
 public sealed class StartMenuDialog:PanelController {
  public Text heading,body;public Button close,cancel,confirm;public StartMenuBootstrap owner;
  [TextArea(4,10)] public string creditsText;
  public void Configure(bool quitting){heading.text=quitting?"退出 VisualAlgo？":"关于这个项目";body.text=quitting?"结束这次探索，下次再见。":creditsText;confirm.gameObject.SetActive(quitting);cancel.GetComponentInChildren<Text>().text=quitting?"继续探索":"返回实验室";}
  protected override void AddListeners(){close.onClick.AddListener(Close);cancel.onClick.AddListener(Close);confirm.onClick.AddListener(Quit);}
  protected override void RemoveListeners(){if(close)close.onClick.RemoveListener(Close);if(cancel)cancel.onClick.RemoveListener(Close);if(confirm)confirm.onClick.RemoveListener(Quit);}
  void Update(){if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
  void Close(){owner.CloseDialog();}
  void Quit(){Time.timeScale=1;
#if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
#else
   Application.Quit();
#endif
  }
 }
}
