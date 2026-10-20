using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
public static class VerifyVisualAlgoMigration {
 public static async Task<object> ExistingScenes(){
  if(!Application.isPlaying)throw new Exception("Requires Play Mode in StartMenu");
  var errors=new List<string>();Application.LogCallback capture=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};
  Application.logMessageReceived+=capture;
  var checkedScenes=new List<string>();
  try{for(int i=1;i<=3;i++){
   var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(manager==null)throw new Exception("Missing menu manager");
   if(i==1)manager.Btn_SortScene();else if(i==2)manager.Btn_GridMapNavScene();else manager.Btn_BinaryTreeScene();
   await Task.Delay(600);
   if(SceneManager.GetActiveScene().buildIndex!=i)throw new Exception("Menu navigation changed at "+i);
   if(Camera.main==null||UnityEngine.EventSystems.EventSystem.current==null)throw new Exception("Missing camera / events");
   var cameraData=Camera.main.GetUniversalAdditionalCameraData();if(cameraData.scriptableRenderer.GetType().Name!="Renderer2D")throw new Exception("Original camera no longer uses Renderer2D");
   checkedScenes.Add(SceneManager.GetActiveScene().path);
   SceneManager.LoadScene(0);await Task.Delay(400);
  }
  if(errors.Count>0)throw new Exception(string.Join("\n",errors));return new{passed=true,checkedScenes,menuNavigation="Original indices 1,2,3 preserved",newErrors=0};
  }finally{Application.logMessageReceived-=capture;}
 }
}
