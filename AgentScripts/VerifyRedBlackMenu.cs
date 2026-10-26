using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using VisualAlgo.Managers.UI.StartMenu;
public static class VerifyRedBlackMenu {
 static Type T=typeof(StartMenuBinaryTreePreviewGraphic);
 static object Field(string name)=>T.GetField(name,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 static int[] Validate(string prefix){var anchors=(Vector2[])Field(prefix+"NodeAnchors");var tones=(Array)Field(prefix+"NodeTones");var edges=(Array)Field(prefix+"Edges");int n=anchors.Length;var children=Enumerable.Range(0,n).Select(_=>new List<int>()).ToArray();var parents=new int[n];
 foreach(var edge in edges){var type=edge.GetType();int a=(int)type.GetField("From").GetValue(edge),b=(int)type.GetField("To").GetValue(edge);children[a].Add(b);parents[b]++;}
 var roots=Enumerable.Range(0,n).Where(i=>parents[i]==0).ToArray();if(roots.Length!=1||parents.Any(p=>p>1))throw new Exception("Not a tree");int root=roots[0];bool Red(int i)=>tones.GetValue(i).ToString()=="Purple";
 if(Red(root))throw new Exception(prefix+": red root");var order=new List<int>();var visited=new HashSet<int>();
 int Walk(int i){if(!visited.Add(i)||children[i].Count>2)throw new Exception("Invalid topology");int left=-1,right=-1;foreach(int c in children[i]){if(Red(i)&&Red(c))throw new Exception("Red parent and child");if(anchors[c].x<anchors[i].x){if(left!=-1)throw new Exception("Two left children");left=c;}else{if(right!=-1)throw new Exception("Two right children");right=c;}}
 int l=left<0?1:Walk(left);order.Add(i);int r=right<0?1:Walk(right);if(l!=r)throw new Exception(prefix+": unequal black heights at "+i);return l+(Red(i)?0:1);}
 if(prefix=="End") {
 var depths=new List<int>();void Full(int i,int depth){if(children[i].Count==0){depths.Add(depth);return;}if(children[i].Count!=2)throw new Exception("End is not full");foreach(int c in children[i])Full(c,depth+1);}Full(root,0);
 if(n!=7||depths.Count!=4||depths.Any(d=>d!=2))throw new Exception("Expected perfect three-level tree");
 }
 Walk(root);if(visited.Count!=n)throw new Exception("Disconnected nodes");return order.ToArray();}
 static void VerifyRotation() {
 var start=(Array)Field("StartEdges");var end=(Array)Field("EndEdges");
 HashSet<string> Edges(Array edges){return new HashSet<string>(edges.Cast<object>().Select(e=>e.GetType().GetField("From").GetValue(e)+":"+e.GetType().GetField("To").GetValue(e)));}
 var rotated=Edges(start);if(!rotated.Remove("3:4")||!rotated.Remove("4:1"))throw new Exception("Missing left-rotation pivot edges");rotated.Add("4:3");rotated.Add("3:1");if(!rotated.SetEquals(Edges(end)))throw new Exception("Not a single left rotation");
 var go=new GameObject("Preview verification",typeof(RectTransform),typeof(CanvasRenderer));try {
 var graphic=go.AddComponent<StartMenuBinaryTreePreviewGraphic>();var colorMethod=T.GetMethod("ResolveNodeColor",BindingFlags.Instance|BindingFlags.NonPublic);var positionsMethod=T.GetMethod("BuildInterpolatedPositions",BindingFlags.Instance|BindingFlags.NonPublic);
 var black=(Color)T.GetField("blackNodeColor",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(graphic);var red=(Color)T.GetField("purpleNodeColor",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(graphic);
 var startTones=(Array)Field("StartNodeTones");var endTones=(Array)Field("EndNodeTones");Color[] previous=new Color[7];
 for(int step=0;step<=200;step++){graphic.Progress=step/200f;for(int i=0;i<7;i++){var c=(Color)colorMethod.Invoke(graphic,new object[]{i});
 if(step==0&&c!=(startTones.GetValue(i).ToString()=="Purple"?red:black))throw new Exception("Wrong start color");
 if(step==200&&c!=(endTones.GetValue(i).ToString()=="Purple"?red:black))throw new Exception("Wrong end color");
 if(step>0&&Vector4.Distance(c,previous[i])>.03f)throw new Exception("Color discontinuity");
 if(step==100&&(i==3||i==4)&&(c==red||c==black))throw new Exception("Expected blended midpoint");previous[i]=c;}

 var positions=(Vector2[])positionsMethod.Invoke(graphic,new object[]{new Rect(0,0,200,180)});int[] order={5,3,1,4,0,2,6};for(int i=1;i<7;i++)if(positions[order[i-1]].x>=positions[order[i]].x)throw new Exception("Visual ordering changed");}
 Vector2[] At(float p){graphic.Progress=p;return (Vector2[])positionsMethod.Invoke(graphic,new object[]{new Rect(0,0,200,180)});}
 var before=At(.499f);var center=At(.5f);var after=At(.501f);
 foreach(int i in new[]{3,4}) {float incoming=(center[i]-before[i]).magnitude;float outgoing=(after[i]-center[i]).magnitude;if(incoming<.03f||outgoing<.03f||Mathf.Abs(incoming-outgoing)>.002f)throw new Exception("Pivot stops or changes speed at midpoint");}
 }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 public static string Run(){VerifyRotation();var a=Validate("Start");var b=Validate("End");if(!a.SequenceEqual(b))throw new Exception("Rotation changes in-order traversal");return "PASS: single left rotation; midpoint movement continuous without stopping; 201 animation samples have continuous colors and unchanged in-order positions; end is a perfect seven-node tree; both states have black roots, no red-red edge, equal black height including NIL, all seven nodes connected, unchanged in-order: "+string.Join(",",a);}
}
