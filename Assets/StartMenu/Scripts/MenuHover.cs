using UnityEngine;using UnityEngine.EventSystems;
namespace VisualAlgo.StartMenu {
 public sealed class MenuHover:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
  bool hovered,selected;Vector3 initial;
  void Awake(){initial=transform.localScale;}
  void OnDisable(){hovered=selected=false;if(initial!=Vector3.zero)transform.localScale=initial;}
  void Update(){transform.localScale=Vector3.Lerp(transform.localScale,initial*((hovered||selected)?1.018f:1),1-Mathf.Exp(-Time.unscaledDeltaTime*18));}
  public void OnPointerEnter(PointerEventData e){hovered=true;}public void OnPointerExit(PointerEventData e){hovered=false;}public void OnSelect(BaseEventData e){selected=true;}public void OnDeselect(BaseEventData e){selected=false;}
 }
}
