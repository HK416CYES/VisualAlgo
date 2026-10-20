using System;using System.Collections.Generic;using System.Linq;using ConvexHullDemo3D;using UnityEngine;
public static class VerifyTeaching3D {
 static int cases,checks;static long steps;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Test(List<Vector3> input,string name){var expected=ConvexHull3D.Compute(input);foreach(TeachingAlgorithm3D algorithm in Enum.GetValues(typeof(TeachingAlgorithm3D))){cases++;using(var session=new HullTeachingSession3D(input,algorithm)){
  int guard=0;try{while(!session.Finished){session.Advance(4096);if(++guard>12000000)throw new Exception("Infinite session");}}catch(Exception e){throw new Exception(name+" / "+algorithm+" / step "+session.VisibleSteps+": "+e.Message);}
  steps+=session.VisibleSteps;var result=session.Result;Check(result!=null&&result.Dimension==expected.Dimension,name+" dimension "+algorithm);Check(new HashSet<Vector3>(result.Vertices).SetEquals(expected.Vertices),name+" vertices "+algorithm+" got "+result.Vertices.Length+" expected "+expected.Vertices.Length);
  var edges=new Dictionary<(int,int),int>();var directions=new Dictionary<(int,int),int>();for(int i=0;i<result.Triangles.Length;i+=3){int a=result.Triangles[i],b=result.Triangles[i+1],c=result.Triangles[i+2];var n=Vector3.Cross(result.Vertices[b]-result.Vertices[a],result.Vertices[c]-result.Vertices[a]);Check(n.sqrMagnitude>1e-20f,name+" zero area");if(result.Dimension==HullDimension.Volume)foreach(var p in input)Check(Vector3.Dot(n.normalized,p-result.Vertices[a])<.001f,name+" outside point "+algorithm);foreach(var e in new[]{(a,b),(b,c),(c,a)}){var key=(Math.Min(e.Item1,e.Item2),Math.Max(e.Item1,e.Item2));edges.TryGetValue(key,out int count);edges[key]=count+1;directions.TryGetValue(key,out int dir);directions[key]=dir+(e.Item1<e.Item2?1:-1);}}
  if(result.Dimension==HullDimension.Volume)foreach(var e in edges){Check(e.Value==2&&directions[e.Key]==0,name+" nonmanifold "+algorithm);}
  for(int i=0;i<result.Vertices.Length;i++)Check(input.FindIndex(p=>p.Equals(result.Vertices[i]))==result.SourceIndices[i],"Earliest representative");
 }}}
 public static object Run(){cases=checks=0;steps=0;
  Test(new List<Vector3>(),"empty");Test(new List<Vector3>{Vector3.zero,Vector3.zero},"point");Test(new List<Vector3>{Vector3.zero,Vector3.one},"segment");Test(new List<Vector3>{Vector3.one*2,Vector3.zero,Vector3.one,Vector3.one*3},"collinear");
  Test(new List<Vector3>{Vector3.zero,Vector3.right,Vector3.up,Vector3.forward},"tetrahedron");
  var cube=new List<Vector3>();for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)cube.Add(new Vector3(x,y,z)*3);cube.Insert(0,new Vector3(-3,0,0));cube.Insert(0,new Vector3(-3,-3,0));cube.Add(Vector3.zero);cube.Add(cube[4]);Test(cube,"cube + face/edge/internal/duplicate");cube.Reverse();Test(cube,"reversed cube");Test(cube.Select(p=>p+Vector3.one*100000).ToList(),"translated cube");
  Test(new List<Vector3>{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0),Vector3.zero,new Vector3(0,1,0)},"coplanar");
  var rng=new System.Random(213);foreach(int n in new[]{5,12,20,100,1000})for(int seed=0;seed<5;seed++){var points=new List<Vector3>();for(int i=0;i<n;i++)points.Add(new Vector3((float)rng.NextDouble()*10,(float)rng.NextDouble()*10,(float)rng.NextDouble()*10));Test(points,"random "+n+" seed "+seed);}
  var sphere=new List<Vector3>();for(int i=0;i<120;i++){float y=1-2*(i+.5f)/120,r=Mathf.Sqrt(1-y*y),a=i*2.39996323f;sphere.Add(new Vector3(r*Mathf.Cos(a),y,r*Mathf.Sin(a)));}Test(sphere,"all boundary sphere");
  return new{passed=true,cases,checks,steps};
 }
}
