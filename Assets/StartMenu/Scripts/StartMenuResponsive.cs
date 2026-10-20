using System.Collections.Generic;using UnityEngine;using UnityEngine.UI;
namespace VisualAlgo.StartMenu {
 /// <summary>宽窗口使用 3+2 卡片布局；窄窗口改为两列，保留可读字号。</summary>
 public sealed class StartMenuResponsive:MonoBehaviour {
  [Tooltip("低于该宽高比时使用窄窗口布局")]public float compactAspect=1.3f;
  struct State{public RectTransform rect;public Vector2 size,position;public bool active;public Text text;public int font;public string value;}
  readonly List<State> originals=new();RectTransform layout;bool? compact;
  void Awake(){layout=(RectTransform)transform.Find("Layout");foreach(var r in layout.GetComponentsInChildren<RectTransform>(true)){var t=r.GetComponent<Text>();originals.Add(new State{rect=r,size=r.sizeDelta,position=r.anchoredPosition,active=r.gameObject.activeSelf,text=t,font=t?t.fontSize:0,value=t?t.text:null});}}
  RectTransform R(string name)=>(RectTransform)layout.Find(name);
  void Move(string name,float x,float y,float w,float h){var r=R(name);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
  void Font(string name,int size){R(name).GetComponent<Text>().fontSize=size;}
  void LateUpdate(){var canvas=GetComponentInParent<Canvas>().rootCanvas;var r=((RectTransform)canvas.transform).rect;bool next=r.width/r.height<compactAspect;if(compact==next)return;compact=next;
   foreach(var s in originals){s.rect.sizeDelta=s.size;s.rect.anchoredPosition=s.position;s.rect.gameObject.SetActive(s.active);if(s.text){s.text.fontSize=s.font;s.text.text=s.value;}}
   if(!next)return;
   layout.sizeDelta=new Vector2(1440,1440);Move("Sidebar",0,0,252,1440);
   Move("Sidebar note",36,1060,190,90);Move("Btn_Producer",32,1290,188,46);Move("Btn_Exit",32,1350,188,46);Move("Version",36,1410,200,24);
   Font("Heading",52);Move("Heading",306,105,1040,80);Font("Intro",24);Move("Intro",310,194,1030,65);R("Section").GetComponent<Text>().text="选择一个实验";Font("Section",23);R("Section count").gameObject.SetActive(false);R("Geometry section").gameObject.SetActive(false);R("Geometry count").gameObject.SetActive(false);
   string[] names={"Btn_SortScene","Btn_GridMapNavScene","Btn_BinaryTreeScene","Btn_ConvexHull2DScene","Btn_ConvexHull3DScene"};
   for(int i=0;i<names.Length;i++){
    string n=names[i];float x=i==4?310:(i%2==0?310:835),y=i==4?970:300+(i/2)*330,w=i==4?1030:505,h=300;
    Move(n,x,y,w,h);Move(n+" Shadow",x,y+5,w,h);Move(n+"/Index",24,18,100,30);Font(n+"/Index",20);
    Move(n+"/Title",24,i==4?74:140,w-48,48);Font(n+"/Title",34);
    Move(n+"/Description",24,i==4?130:191,i==4?580:w-48,64);Font(n+"/Description",22);
    Move(n+"/Preview",i==4?740:w-220,20,i==4?250:195,i==4?215:110);
    Move(n+"/Rule",24,h-48,w-48,1);Move(n+"/Tags",24,h-35,w-66,30);Font(n+"/Tags",18);Move(n+"/Arrow",w-48,h-38,30,32);Font(n+"/Arrow",28);
   }
  }
 }
}
