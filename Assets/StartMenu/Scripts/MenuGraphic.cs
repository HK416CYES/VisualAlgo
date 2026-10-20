using UnityEngine;
using UnityEngine.UI;
namespace VisualAlgo.StartMenu {
    // 所有图形都由 UI 网格绘制，随 Canvas 等比缩放，无外部图片依赖。
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuGraphic : MaskableGraphic {
        public enum Shape { Rounded, Sorting, Grid, Tree, Hull2D, Hull3D }
        public Shape shape;
        public float radius=16;
        public Color faint=new Color(.85f,.9f,.91f,1);
        VertexHelper mesh;
        Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);}
        void Triangle(Vector2 a,Vector2 b,Vector2 c,Color tint){int i=mesh.currentVertCount;mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(b,tint,Vector2.zero);mesh.AddVert(c,tint,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);}
        void Line(Vector2 a,Vector2 b,float width,Color tint){var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Triangle(a+n,b+n,b-n,tint);Triangle(a+n,b-n,a-n,tint);}
        void Dot(Vector2 p,float size,Color tint){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Triangle(p,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*size,tint);}}
        void Box(float x,float y,float w,float h,Color tint){var a=P(x,y);var b=P(x+w,y);var c=P(x+w,y+h);var d=P(x,y+h);Triangle(a,b,c,tint);Triangle(a,c,d,tint);}
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();mesh=vh;var r=rectTransform.rect;
            if(shape==Shape.Rounded){float rad=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);Vector2 prev=default,first=default;for(int corner=0;corner<4;corner++){Vector2 center=new Vector2(corner==0||corner==3?r.xMax-rad:r.xMin+rad,corner<2?r.yMax-rad:r.yMin+rad);for(int i=0;i<=8;i++){float a=(corner*90+i*90f/8)*Mathf.Deg2Rad;var p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rad;if(corner==0&&i==0)first=p;else Triangle(r.center,prev,p,color);prev=p;}}Triangle(r.center,prev,first,color);return;}
            if(shape==Shape.Sorting){float[] heights={.25f,.47f,.35f,.78f,.58f,.9f};for(int i=0;i<6;i++)Box(.12f+i*.13f,.12f,.09f,heights[i]*.76f,i==3||i==5?color:faint);Line(P(.1f,.08f),P(.92f,.08f),1,faint);return;}
            if(shape==Shape.Grid){for(int x=0;x<9;x++)for(int y=0;y<5;y++){bool route=(y==1&&x<5)||(x==4&&y>=1&&y<=3)||(y==3&&x>=4);bool wall=x==2&&y>1||x==6&&y<3;Box(.07f+x*.1f,.08f+y*.17f,.077f,.125f,route?color:wall?new Color(.19f,.26f,.3f):faint);}return;}
            if(shape==Shape.Tree){Vector2[] p={P(.5f,.88f),P(.28f,.52f),P(.72f,.52f),P(.13f,.14f),P(.39f,.14f),P(.61f,.14f),P(.87f,.14f)};for(int i=1;i<p.Length;i++)Line(p[(i-1)/2],p[i],2,faint);for(int i=0;i<p.Length;i++){Dot(p[i],i==0?11:8,i%2==0?color:faint);Dot(p[i],3,Color.white);}return;}
            if(shape==Shape.Hull2D){Vector2[] p={P(.12f,.2f),P(.2f,.75f),P(.56f,.9f),P(.88f,.65f),P(.82f,.18f),P(.44f,.08f)};var fill=color;fill.a=.1f;for(int i=1;i<p.Length-1;i++)Triangle(p[0],p[i],p[i+1],fill);for(int i=0;i<p.Length;i++){Line(p[i],p[(i+1)%p.Length],2,color);Dot(p[i],5,color);}Dot(P(.4f,.5f),4,faint);Dot(P(.65f,.35f),4,faint);Dot(P(.62f,.66f),4,faint);return;}
            if(shape==Shape.Hull3D){Vector2[] p={P(.16f,.34f),P(.53f,.09f),P(.87f,.35f),P(.5f,.55f),P(.16f,.73f),P(.53f,.5f),P(.87f,.78f),P(.5f,.96f)};var fill=color;fill.a=.1f;Triangle(p[0],p[1],p[5],fill);Triangle(p[0],p[5],p[4],fill);Triangle(p[4],p[5],p[6],fill);Triangle(p[4],p[6],p[7],fill);for(int i=0;i<4;i++){Line(p[i],p[(i+1)%4],1.7f,color);Line(p[i+4],p[(i+1)%4+4],1.7f,color);Line(p[i],p[i+4],1.7f,color);}for(int i=0;i<p.Length;i++)Dot(p[i],4,color);}
        }
    }
}
