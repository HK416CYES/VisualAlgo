using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConvexHullDemo3D;
using UnityEngine;
public static class VerifyHull3D {
 static int checks,cases;static double maximumMs;
 static void Check(bool condition,string text){checks++;if(!condition)throw new Exception(text);}
 static double Det(Vector3 a,Vector3 b,Vector3 c)=>((double)a.y*b.z-(double)a.z*b.y)*c.x+((double)a.z*b.x-(double)a.x*b.z)*c.y+((double)a.x*b.y-(double)a.y*b.x)*c.z;
 public static List<Vector3> Cube(){var p=new List<Vector3>();for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)p.Add(new Vector3(x,y,z)*3);return p;}
 static HullResult3D Validate(List<Vector3> input,HullDimension dim,int count=-1,double volume=-1) {
  cases++;var watch=Stopwatch.StartNew();var h=ConvexHull3D.Compute(input);watch.Stop();maximumMs=Math.Max(maximumMs,watch.Elapsed.TotalMilliseconds);
  Check(h.Dimension==dim,"Dimension case "+cases+": "+h.Dimension+" vs "+dim);if(count>=0)Check(h.Vertices.Length==count,"Vertex count case "+cases+": "+h.Vertices.Length+" vs "+count);
  for(int i=0;i<h.Vertices.Length;i++)Check(input.FindIndex(p=>p.Equals(h.Vertices[i]))==h.SourceIndices[i],"Original index / earliest duplicate");
  var edges=new Dictionary<(int,int),(int count,int direction)>();double actualVolume=0;Vector3 reference=h.Vertices.Length>0?h.Vertices[0]:Vector3.zero;
  Vector3 min=input.Count>0?input[0]:Vector3.zero,max=min;foreach(var p in input){min=Vector3.Min(min,p);max=Vector3.Max(max,p);}double tolerance=Math.Max(1e-6,(max-min).magnitude*2e-6);
  for(int i=0;i<h.Triangles.Length;i+=3){int a=h.Triangles[i],b=h.Triangles[i+1],c=h.Triangles[i+2];Vector3 va=h.Vertices[a],vb=h.Vertices[b],vc=h.Vertices[c];var n=Vector3.Cross(vb-va,vc-va);Check(n.sqrMagnitude>1e-20,"Nonzero triangle");
   if(dim==HullDimension.Volume){foreach(var p in input)Check(Vector3.Dot(n.normalized,p-va)<=tolerance,"All points inside/outward normals case "+cases);actualVolume+=Det(va-reference,vb-reference,vc-reference)/6;}
   foreach(var e in new[]{(a,b),(b,c),(c,a)}){var key=(Math.Min(e.Item1,e.Item2),Math.Max(e.Item1,e.Item2));edges.TryGetValue(key,out var prior);edges[key]=(prior.count+1,prior.direction+(e.Item1<e.Item2?1:-1));}
  }
  if(dim==HullDimension.Volume){foreach(var e in edges.Values)Check(e.count==2&&e.direction==0,"Closed oriented manifold case "+cases);Check(h.Vertices.Length-edges.Count+h.Triangles.Length/3==2,"Euler characteristic");Check(actualVolume>0,"Positive volume");if(volume>=0)Check(Math.Abs(actualVolume-volume)<Math.Max(1e-5,volume*1e-5),"Known volume "+actualVolume);}
  return h;
 }
 public static object Algorithm(){checks=cases=0;maximumMs=0;
  Validate(new List<Vector3>(),HullDimension.Empty,0);Validate(new List<Vector3>{Vector3.one,Vector3.one},HullDimension.Point,1);
  Validate(new List<Vector3>{Vector3.zero,Vector3.one,Vector3.one*3,Vector3.one*2},HullDimension.Segment,2);
  Validate(new List<Vector3>{Vector3.zero,Vector3.right,Vector3.up,Vector3.forward},HullDimension.Volume,4,1.0/6);
  Validate(new List<Vector3>{new Vector3(-2,-2,0),new Vector3(2,-2,0),new Vector3(2,2,0),new Vector3(-2,2,0),Vector3.zero,new Vector3(0,2,0)},HullDimension.Planar,4);
  var cube=Cube();cube.Insert(0,new Vector3(-3,0,0));cube.Insert(0,new Vector3(-3,-3,0));cube.Add(Vector3.zero);cube.Add(cube[0]);Validate(cube,HullDimension.Volume,8,216);cube.Reverse();Validate(cube,HullDimension.Volume,8,216);
  var shifted=new List<Vector3>();foreach(var p in cube)shifted.Add(p+new Vector3(100000,-100000,100000));Validate(shifted,HullDimension.Volume,8,216);
  var nearPlane=new List<Vector3>{new Vector3(-2,-2,0),new Vector3(2,-2,0),new Vector3(2,2,0),new Vector3(-2,2,0),new Vector3(0,0,1e-9f)};Validate(nearPlane,HullDimension.Planar,4);
  var rotated=new List<Vector3>();foreach(var p in nearPlane)rotated.Add(Quaternion.Euler(22,37,13)*p);Validate(rotated,HullDimension.Planar,4);
  var random=new System.Random(918);foreach(int n in new[]{4,5,20,100,1000})for(int seed=0;seed<12;seed++){var input=new List<Vector3>();for(int i=0;i<n;i++)input.Add(new Vector3((float)random.NextDouble()*20-10,(float)random.NextDouble()*12-6,(float)random.NextDouble()*20-10));var h=Validate(input,HullDimension.Volume);input.Reverse();Validate(input,HullDimension.Volume,h.Vertices.Length);}
  var sphere=new List<Vector3>();for(int i=0;i<1000;i++){float y=1-2*(i+.5f)/1000,r=Mathf.Sqrt(1-y*y),a=i*2.39996323f;sphere.Add(new Vector3(r*Mathf.Cos(a),y,r*Mathf.Sin(a))*5);}Validate(sphere,HullDimension.Volume,1000);
  bool invalid=false;try{ConvexHull3D.Compute(new[]{new Vector3(float.NaN,0,0)});}catch(ArgumentException){invalid=true;}Check(invalid,"Invalid input rejected");
  return new{passed=true,cases,checks,maximumMs};
 }
}
