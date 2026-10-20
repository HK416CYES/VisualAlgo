using UnityEngine;
using UnityEngine.UI;
namespace VisualAlgo.StartMenu {
    /// <summary>宽屏使用一排五个实验入口，窄窗口使用三加二排列。</summary>
    public sealed class StartMenuResponsive : MonoBehaviour {
        public RectTransform layout, title, section, footer, credits, quit;
        public RectTransform[] experiments;
        Canvas canvas;
        CanvasScaler scaler;
        bool initialized, compact;
        // UIFramework 先实例化面板，再挂到 Canvas；不能在 Awake 中访问父 Canvas。
        void LateUpdate() {
            if(!canvas) {
                var parent=GetComponentInParent<Canvas>();
                if(!parent)return;
                canvas=parent.rootCanvas;scaler=canvas.GetComponent<CanvasScaler>();
            }
            var viewport=((RectTransform)canvas.transform).rect;
            float aspect=viewport.width/Mathf.Max(1,viewport.height);
            bool next=aspect<1.4f;
            if(initialized&&next==compact)return;
            initialized=true;compact=next;
            scaler.referenceResolution=compact?new Vector2(1440,1620):new Vector2(1920,1080);
            layout.sizeDelta=scaler.referenceResolution;
            Place(title,compact?230:446,compact?80:60,compact?980:1028,compact?247:259);
            Place(section,compact?160:210,compact?365:347,compact?1120:1500,42);
            for(int i=0;i<experiments.Length;i++) {
                float x=compact?(i<3?160+i*385:352+(i-3)*385):210+i*304;
                float y=compact?(i<3?435:885):420;
                Place(experiments[i],x,y,compact?350:284,compact?405:365);
            }
            Place(credits,compact?352:624,compact?1390:875,320,66);
            Place(quit,compact?737:976,compact?1390:875,320,66);
            Place(footer,compact?160:210,compact?1510:1000,compact?1120:1500,28);
        }
        static void Place(RectTransform r,float x,float y,float w,float h) { if(!r)return;r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h); }
    }
}
