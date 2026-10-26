using UnityEngine;
using UnityEngine.EventSystems;
namespace VisualAlgo.StartMenu {
    public sealed class HullMenuHover : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler {
        public HullMenuPreview preview;
        [Min(.05f)] public float smoothTime=.3f;
        bool hovered;float velocity;
        void Update(){if(!preview)return;float target=hovered?1:0;float next=Mathf.SmoothDamp(preview.Progress,target,ref velocity,smoothTime,Mathf.Infinity,Time.unscaledDeltaTime);if(Mathf.Abs(next-target)<.0005f){next=target;velocity=0;}preview.Progress=next;}
        public void OnPointerEnter(PointerEventData e){hovered=true;}
        public void OnPointerExit(PointerEventData e){hovered=false;}
        void OnDisable(){hovered=false;velocity=0;if(preview)preview.Progress=0;}
    }
}
