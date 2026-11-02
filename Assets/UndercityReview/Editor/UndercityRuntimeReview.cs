using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using VideoGame.Core;

namespace VideoGame.Editor
{
    public static class UndercityRuntimeReview
    {
        const string Output="artifacts/biome-parallel/undercity-integration/";
        [MenuItem("Nexus/Review/Undercity runtime source recipes")]
        public static void Build()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Undercity runtime prefab recipe audition");
            var measured=new System.Collections.Generic.List<string>();
            string[] recipes={"drainage","pressure","service","refuge"};
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.17f,.19f,.17f);RenderSettings.fog=false;
            var sun=new GameObject("Underground review fill").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.75f,.8f,.77f);sun.intensity=.65f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(60,-30,0);
            QualitySettings.pixelLightCount=12;QualitySettings.shadows=ShadowQuality.All;
            var camera=new GameObject("Runtime source review camera").AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.03f,.026f);camera.farClipPlane=300;camera.nearClipPlane=.1f;camera.transform.rotation=Quaternion.Euler(50,45,0);
            for(int role=0;role<4;role++)
            {
                var room=new Room(role*2,0,14);var center=new Vector3(room.X*20,0,0);
                UndercityBiome.Deck(center,14,14,root.transform);
                foreach(var face in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
                {
                    var normal=new Vector3(face.x,0,face.y);var tangent=new Vector3(face.y,0,-face.x);
                    if(face!=Vector2Int.up)UndercityBiome.Wall(center+normal*7,tangent,14,face,root.transform);
                    else foreach(int side in new[]{-1,1})UndercityBiome.Wall(center+normal*7+tangent*side*4.5f,tangent,5,face,root.transform);
                }
                UndercityBiome.Deck(center+Vector3.forward*10,4,6,root.transform);
                foreach(int side in new[]{-1,1})UndercityBiome.Wall(center+new Vector3(side*2,0,10),Vector3.forward,6,new Vector2Int(side,0),root.transform);
                UndercityBiome.Dress(room,role,0,0,root.transform);
                var src=Resources.Load<GameObject>(PlayerRig.PrefabPath);
                if(src){var actor=UnityEngine.Object.Instantiate(src,root.transform);actor.name="Operator scale reference "+role;foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;actor.transform.position=center+new Vector3(.5f,0,-1);actor.transform.rotation=Quaternion.Euler(0,35,0);var bounds=Art.RenderedBounds(actor);actor.transform.position-=Vector3.up*bounds.min.y;}
                foreach(var box in root.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Undercity / ")&&Vector3.Distance(c.bounds.center,center)<15))measured.Add(recipes[role]+" "+box.name+" min="+box.bounds.min.ToString("F3")+" max="+box.bounds.max.ToString("F3"));
                camera.transform.position=center+new Vector3(0,.6f,1)-camera.transform.forward*40;
                if(role==0)Capture(camera,"runtime-gameplay.png",8.5f);
                camera.transform.position=center+new Vector3(4,1.4f,4)-camera.transform.forward*40;
                if(role==1)Capture(camera,"runtime-detail.png",4.2f);
            }
            File.WriteAllLines(Output+"runtime-bounds.txt",measured);
            EditorSceneManager.SaveScene(scene,"Assets/UndercityReview/UndercityRuntimePreview.unity");
            Debug.Log("UNDERCITY_RUNTIME_REVIEW_READY: four source prefab recipes; static editor captures, not natural playthrough.");
        }
        static void Capture(Camera camera,string file,float size)
        {
            camera.orthographicSize=size;var rt=new RenderTexture(1440,1000,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(1440,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,1000),0,0);tex.Apply();File.WriteAllBytes(Output+file,tex.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
