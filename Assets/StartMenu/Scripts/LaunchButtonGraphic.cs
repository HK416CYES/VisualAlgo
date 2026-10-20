using UnityEngine;
using UnityEngine.UI;
namespace VisualAlgo.StartMenu {
    // 与旧界面一致的切角金属面板。描边由同心多边形组成，尺寸改变时不会拉伸边框。
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LaunchButtonGraphic : MaskableGraphic {
        public Color accent=new Color(.2f,.7f,.9f);
        public bool compact;
        VertexHelper mesh;
        void Polygon(Vector2[] p,Color c) {
            int n=mesh.currentVertCount;
            foreach(var v in p)mesh.AddVert(v,c,Vector2.zero);
            for(int i=1;i<p.Length-1;i++)mesh.AddTriangle(n,n+i,n+i+1);
        }
        Vector2[] Outline(float inset,Vector2 offset) {
            Rect r=rectTransform.rect;float l=r.xMin+inset,t=r.yMax-inset,rr=r.xMax-inset,b=r.yMin+inset;
            float cut=compact?16:25;
            return new[]{new Vector2(l,t-cut)+offset,new Vector2(l+cut,t)+offset,new Vector2(rr-10,t)+offset,new Vector2(rr,t-10)+offset,new Vector2(rr,b+cut)+offset,new Vector2(rr-cut,b)+offset,new Vector2(l+10,b)+offset,new Vector2(l,b+10)+offset};
        }
        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();mesh=vh;
            Polygon(Outline(0,new Vector2(4,-6)),new Color(0,0,0,.13f));
            Polygon(Outline(0,Vector2.zero),new Color(.08f,.09f,.1f));
            Polygon(Outline(3,Vector2.zero),new Color(.5f,.51f,.51f));
            Polygon(Outline(6,Vector2.zero),new Color(.12f,.13f,.14f));
            Polygon(Outline(8,Vector2.zero),color);
            Polygon(Outline(14,Vector2.zero),new Color(.3f,.31f,.32f));
            Polygon(Outline(15,Vector2.zero),color);
            var r=rectTransform.rect;
            float x=r.xMax-Mathf.Min(compact?90:126,r.width*.42f),y=r.yMax-5;
            Polygon(new[]{new Vector2(x,y),new Vector2(r.xMax-12,y),new Vector2(r.xMax-5,y-7),new Vector2(r.xMax-5,y-17),new Vector2(x+10,y-17),new Vector2(x-3,y-5)},accent);
        }
    }
}
