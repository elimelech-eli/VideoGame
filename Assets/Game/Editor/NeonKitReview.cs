using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using VideoGame.Core;
namespace VideoGame.Editor
{
    public static class NeonKitReview
    {
        const string Output="artifacts/biome-parallel/neon-integration/";
        public static void BuildAndReview(){NeonBiomeAssets.Build();Review();}
        public static void Review()
        {
            var results=new List<string>();
            foreach(int count in new[]{6,10,20})
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root=new GameObject("Neon source-kit map "+count);var adapter=root.AddComponent<NeonBiome>();
                const int seed=104;var layout=MapLayout.Generate(seed,count);adapter.Begin(layout,seed,root.transform);
                foreach(var r in layout)NeonBiome.Deck(new Vector3(r.X*20,0,r.Y*20),r.Size,r.Size,root.transform);
                for(int i=0;i<layout.Count;i++)
                {
                    var r=layout[i];var center=new Vector3(r.X*20,0,r.Y*20);
                    foreach(var side in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                    {
                        var normal=new Vector3(side.x,0,side.y);var tangent=new Vector3(side.y,0,-side.x);
                        int neighbor=layout.FindIndex(b=>b.X==r.X+side.x&&b.Y==r.Y+side.y);float half=r.Size/2f;
                        if(neighbor<0)adapter.Wall(center+normal*half,tangent,r.Size,side,root.transform);
                        else
                        {
                            float wing=(r.Size-4)/2f;foreach(int sign in new[]{-1,1})adapter.Wall(center+normal*half+tangent*sign*(2+wing/2),tangent,wing,side,root.transform);
                            if(side.x<0||side.y<0)continue;float length=20-layout[neighbor].Size/2f-half;
                            NeonBiome.Deck(center+normal*(half+length/2),side.x!=0?length:4,side.x!=0?4:length,root.transform);
                            foreach(int sign in new[]{-1,1})adapter.Wall(center+normal*(half+length/2)+tangent*sign*2,normal,length,new Vector2Int(side.y*sign,-side.x*sign),root.transform,true);
                        }
                    }
                }
                Physics.SyncTransforms();
                for(int i=0;i<count;i++)adapter.Dress(layout[i],i,seed,0,root.transform);
                Physics.SyncTransforms();int probes=0;
                foreach(var street in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Neon room / SlumCourtyard")))
                    if(!street.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Neon / SlumBlock")))throw new Exception("Source hut rejected from slum room "+street.name);
                foreach(var a in layout)foreach(var b in layout)
                {
                    if(!MapLayout.Adjacent(a,b))continue;var start=new Vector3(a.X*20,1,a.Y*20);var end=new Vector3(b.X*20,1,b.Y*20);var across=Vector3.Cross(Vector3.up,(end-start).normalized);
                    for(int step=0;step<=20;step++)foreach(float lane in new[]{-1.3f,0,1.3f})
                    {
                        var p=Vector3.Lerp(start,end,step/20f)+across*lane;probes++;
                        if(!Physics.Raycast(p,Vector3.down,2,1<<8))throw new Exception("Neon floor seam "+p);
                        if(Physics.CheckCapsule(p-Vector3.up*.5f,p+Vector3.up*.5f,.4f,1<<9))throw new Exception("Neon central-route obstruction "+p);
                    }
                }
                foreach(var reservation in adapter.Reservations)if(NeonBiome.BlocksGameplayView(reservation.Bounds,layout))throw new Exception("Whole building blocks gameplay view");
                var actor=UnityEngine.Object.Instantiate(Resources.Load<GameObject>(VideoGame.PlayerRig.PrefabPath));foreach(var m in actor.GetComponentsInChildren<MonoBehaviour>())m.enabled=false;
                var renderers=actor.GetComponentsInChildren<Renderer>();var actorBounds=renderers[0].bounds;foreach(var r in renderers)actorBounds.Encapsulate(r.bounds);actor.transform.position+=Vector3.up*(-actorBounds.min.y+.025f);
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.27f,.29f,.34f);RenderSettings.fog=false;
                var sun=new GameObject("Source review skylight").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.85f,.90f,1);sun.intensity=.9f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(55,-30,0);
                QualitySettings.antiAliasing=4;QualitySettings.pixelLightCount=16;
                var cam=new GameObject("Actual map review camera").AddComponent<Camera>();cam.orthographic=true;cam.nearClipPlane=.1f;cam.farClipPlane=600;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.03f,.045f);cam.transform.rotation=Quaternion.Euler(50,45,0);
                foreach(var recipe in new[]{NeonRoomRecipe.SlumCourtyard,NeonRoomRecipe.CorporatePlaza,NeonRoomRecipe.TransitionStreet})
                {
                    int index=Enumerable.Range(0,count).First(i=>NeonBiome.Recipe(i,seed)==recipe);var r=layout[index];var p=new Vector3(r.X*20,0,r.Y*20);actor.transform.position=new Vector3(p.x,.025f,p.z);Capture(cam,p+Vector3.up*.7f,7.5f,count+"-"+recipe+".png");
                }
                results.Add("rooms="+count+" recipeKinds=3 wholeReservations="+adapter.Reservations.Count+" floorAndRouteProbes="+probes+" cameraBuildingProbes=pass status=pass");
                if(count==6){AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,"Assets/NeonProductionReview/NeonSixRoomReview.unity");}
            }
            File.WriteAllLines(Output+"kit-proof.txt",results);
            var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Game/Resources/Neon"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var dependencies=AssetDatabase.GetDependencies(prefabs,true).Where(p=>!p.StartsWith("Packages/")).OrderBy(p=>p).ToArray();
            File.WriteAllLines(Output+"production-dependencies.txt",dependencies.Select(p=>AssetDatabase.AssetPathToGUID(p)+" "+p));Debug.Log("NEON_MAP_KIT_PROOF_PASS: generated6/10/20 room source-kit fixtures; route/floor probes and3recipe renders; not integrated mission playthrough");
        }
        static void Capture(Camera c,Vector3 p,float size,string name)
        {c.orthographicSize=size;c.transform.position=p-c.transform.forward*180;var rt=new RenderTexture(1440,1000,24){antiAliasing=4};c.targetTexture=rt;c.Render();var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1440,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1440,1000),0,0);t.Apply();File.WriteAllBytes(Output+name,t.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(t);}
    }
}
