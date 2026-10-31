using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VideoGame.Editor
{
    // Editor-only art audition. Never added to build settings or the live Nexus.
    public static partial class NexusShipReviewAssets
    {
        const string Root="Assets/NexusShipReview/";
        [Serializable] class Catalog { public SourceMaterial[] materials; }
        [Serializable] class SourceMaterial { public string name,albedo,normal,orm,emission; public float[] color; }
        static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>();
        static Texture2D Texture(string name)=>string.IsNullOrEmpty(name)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+name);

        [MenuItem("Nexus/Review/Build ship room audition")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"Materials");
            Directory.CreateDirectory("Art/NexusShipReview");
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Root+"Textures","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                importer.textureType=file.Contains("_Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=!file.Contains("_ORM")&&!file.Contains("_Normal");
                importer.isReadable=true; importer.maxTextureSize=2048; importer.SaveAndReimport();
            }
            Materials.Clear();
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"source-materials.json"));
            foreach(var source in catalog.materials)
            {
                string path=Root+"Materials/"+source.name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
                mat.color=new Color(source.color[0],source.color[1],source.color[2],source.color[3]);
                mat.mainTexture=Texture(source.albedo);
                mat.SetFloat("_Glossiness",.25f); mat.SetFloat("_Metallic",.45f);
                if(Texture(source.normal)){mat.SetTexture("_BumpMap",Texture(source.normal));mat.EnableKeyword("_NORMALMAP");}
                var orm=Texture(source.orm);
                if(orm)
                {
                    // Source ORM packs AO/Roughness/Metal into RGB. Unity uses metal R/smoothness A.
                    var converted=new Texture2D(orm.width,orm.height,TextureFormat.RGBA32,false,true);
                    var pixels=orm.GetPixels();
                    for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(pixels[i].b,pixels[i].r,0,1-pixels[i].g);
                    converted.SetPixels(pixels);converted.Apply();
                    string packed=Root+"Materials/"+source.name+"_MetalSmooth.png";
                    File.WriteAllBytes(packed,converted.EncodeToPNG());UnityEngine.Object.DestroyImmediate(converted);
                    AssetDatabase.ImportAsset(packed);
                    var ti=(TextureImporter)AssetImporter.GetAtPath(packed);ti.sRGBTexture=false;ti.SaveAndReimport();
                    mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed));mat.EnableKeyword("_METALLICGLOSSMAP");
                    mat.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed));
                }
                if(Texture(source.emission)){mat.SetTexture("_EmissionMap",Texture(source.emission));mat.SetColor("_EmissionColor",Color.white*.6f);mat.EnableKeyword("_EMISSION");}
                if(source.name=="M_Light"){mat.color=new Color(.8f,.6f,.3f);mat.SetColor("_EmissionColor",new Color(1,.55f,.15f)*1.8f);mat.EnableKeyword("_EMISSION");}
                if(source.name.Contains("LightFade")){mat.color=new Color(.12f,.035f,.015f);}
                EditorUtility.SetDirty(mat);Materials[source.name]=mat;
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            foreach(string file in Directory.GetFiles(Root+"Models","*.fbx"))
            {
                var sample=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(file));
                var renderers=sample.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                Debug.Log("REVIEW BOUNDS "+Path.GetFileNameWithoutExtension(file)+" center="+bounds.center.ToString("F2")+" size="+bounds.size.ToString("F2"));
                UnityEngine.Object.DestroyImmediate(sample);
            }
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.38f,.43f);
            RenderSettings.fog=false;RenderSettings.reflectionIntensity=.35f;
            QualitySettings.antiAliasing=4;QualitySettings.pixelLightCount=12;
            var sun=new GameObject("Soft ship fill").AddComponent<Light>();sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(55,-25,0);sun.color=new Color(.7f,.8f,1);sun.intensity=1.1f;sun.shadows=LightShadows.Soft;
            // 12x8m room. Camera-near sides intentionally cut away.
            for(int x=-1;x<=1;x++)for(int z=0;z<=1;z++)Place(x==-1?"Platform_DarkPlates":"Platform_Metal",new Vector3(x*4,0,z*4));
            for(int z=0;z<=1;z++)
            {
                Place(z==0?"WallAstra_Straight_Broken":"WallAstra_Straight",new Vector3(4,0,z*4));
            }
            // Source wall spans local Z at X=-2. Rotation puts it on the far Z edge.
            for(int x=0;x<=1;x++)Place(x==1?"WallAstra_Straight_Window":"WallAstra_Straight",new Vector3(x*4,0,4),-90);
            // Cargo is a separate compartment. Every finish transition meets the X=-2 bulkhead.
            // All bulkhead surfaces are paired source panels, with source deck meshes as solid cut caps.
            // Door's native 5m height shares the same .6 uniform scale as the native 5m junction posts.
            const float doorScale=.6f;
            float halfFrame=2.42672491f*doorScale;
            float nearEnd=2-halfFrame,farStart=2+halfFrame;
            Bulkhead("Cargo rear wall",new Vector3(-4,0,6),4,3,90);
            Bulkhead("Cargo exterior cutaway",new Vector3(-6,0,2),8,1,0);
            Bulkhead("Far divider",new Vector3(-2,0,(farStart+6)/2),6-farStart,3,0);
            Bulkhead("Near cutaway divider",new Vector3(-2,0,(-2+nearEnd)/2),nearEnd+2,1,0);
            var doorway=Place("Door_Frame_Square",new Vector3(-2,0,2),90,doorScale);
            doorway.name="Cargo to mission room — open bulkhead doorway";CargoFinish(doorway);
            var threshold=Place("Prop_Vent_Wide",new Vector3(-2,.015f,2),90,1.5f);
            threshold.name="Grated threshold — finish transition";
            foreach(var junction in new[]{new Vector3(6,0,6),new Vector3(-2,0,6)})
            {
                var post=Place("Column_Simple",junction,0,.6f);
                post.name="Fitted bulkhead junction post";
                if(junction.x<0)CargoFinish(post);
            }
            Place("Prop_Computer",new Vector3(2.5f,0,4.8f),180,1.15f);
            Place("Prop_Computer",new Vector3(3.6f,0,4.8f),180);
            Place("Prop_Chest",new Vector3(-4.7f,0,3.8f),90,1.25f);
            Place("Prop_Crate3",new Vector3(-4.8f,.5f,.3f),12);
            Place("Prop_Crate4",new Vector3(-4.6f,.5f,1.5f),-8);
            Place("Prop_Crate3",new Vector3(-4.8f,1.5f,.4f),-4);
            Place("Prop_Barrel_Large",new Vector3(-3.5f,0,.2f));
            Place("Prop_Cable_1",new Vector3(2.8f,.02f,3.8f),30);
            Place("Prop_Cable_3",new Vector3(-5.3f,.02f,2),0,.55f);
            Place("Prop_Vent_Wide",new Vector3(.5f,.015f,4.5f));
            foreach(float x in new[]{-4f,0f,4f}){Place("Prop_Light_Floor",new Vector3(x,.02f,-1.7f));Point("Amber deck light",new Vector3(x,.7f,-1.6f),new Color(1,.47f,.16f),2.8f,3.2f);}
            Point("Mission console glow",new Vector3(3,1.6f,4.3f),new Color(.25f,.7f,1),2.5f,4);
            Point("Cargo work light",new Vector3(-4,2.4f,2.5f),new Color(1,.65f,.32f),3,5);
            Point("Cargo bulkhead fill",new Vector3(-4,2.2f,4.7f),new Color(1,.73f,.46f),2,5);
            var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Resources/Detailed/Prefabs/OperativeV03.prefab");
            if(!player)throw new Exception("Missing approved operator prefab");
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(player);actor.name="Approved operator — scale reference";actor.transform.position=new Vector3(.2f,0,1.4f);actor.transform.rotation=Quaternion.Euler(0,155,0);
            var actorRenderers=actor.GetComponentsInChildren<Renderer>();var actorBounds=actorRenderers[0].bounds;
            foreach(var r in actorRenderers)actorBounds.Encapsulate(r.bounds);
            actor.transform.localScale*=2.15f/Mathf.Max(actorBounds.size.x,Mathf.Max(actorBounds.size.y,actorBounds.size.z));
            actorBounds=actorRenderers[0].bounds;foreach(var r in actorRenderers)actorBounds.Encapsulate(r.bounds);
            actor.transform.position+=new Vector3(.2f-actorBounds.center.x,-actorBounds.min.y,1.4f-actorBounds.center.z);
            foreach(var behavior in actor.GetComponentsInChildren<MonoBehaviour>())behavior.enabled=false;
            var camera=new GameObject("Review camera — gameplay angle 50 / 45").AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=7.5f;camera.transform.rotation=Quaternion.Euler(50,45,0);
            camera.transform.position=new Vector3(0,1,2)-camera.transform.forward*22;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.024f,.035f);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,Root+"NexusRoomAudition.unity");
            Capture(camera,"Art/NexusShipReview/room-gameplay.png",7.5f);
            Capture(camera,"Art/NexusShipReview/room-detail.png",6.5f);
            File.WriteAllText("Art/NexusShipReview/import-report.txt","Unity "+Application.unityVersion+"\n25 source models selected; paired bulkhead faces and deck-mesh cut caps; two Camera.Render captures completed.\nCamera rotation 50/45/0; gameplay orthographic size 7.5.\nStatic art review only; no gameplay, audio, build or player validation.\n");
            Debug.Log("NEXUS ROOM AUDITION COMPLETE");
        }
        static void Bulkhead(string name,Vector3 floorCenter,float length,float height,float yaw)
        {
            const float depth=.36f;
            var root=new GameObject(name);root.transform.position=floorCenter;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            // Source short-wall panels are one-sided; pair their faces instead of exposing backs.
            for(int row=0;row<Mathf.RoundToInt(height);row++)
            foreach(int side in new[]{-1,1})
            {
                var panel=Place("ShortWall_MetalPlates_Straight",Vector3.zero,side<0?0:180);
                panel.transform.localScale=Vector3.Scale(panel.transform.localScale,new Vector3(1,1,length/4));
                CenterAt(panel,new Vector3(side*depth/2,row+.5f,0));CargoFinish(panel);
                panel.transform.SetParent(root.transform,false);
            }
            var cap=Place("Platform_Metal",Vector3.zero);
            cap.transform.localScale=Vector3.Scale(cap.transform.localScale,new Vector3(depth/4,1,length/4));
            CenterAt(cap,new Vector3(0,height,0));CargoFinish(cap);cap.transform.SetParent(root.transform,false);
            foreach(int end in new[]{-1,1})
            {
                var endCap=Place("Platform_Metal",Vector3.zero);
                endCap.transform.rotation=Quaternion.Euler(end<0?90:-90,0,0);
                endCap.transform.localScale=Vector3.Scale(endCap.transform.localScale,new Vector3(depth/4,1,height/4));
                CenterAt(endCap,new Vector3(0,height/2,end*length/2));CargoFinish(endCap);endCap.transform.SetParent(root.transform,false);
            }
        }
        static void CenterAt(GameObject go,Vector3 center)
        {
            var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            go.transform.position+=center-bounds.center;
        }
        static void CargoFinish(GameObject go)
        {
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    string path=Root+"Materials/Cargo_"+slots[i].name+".mat";
                    var dark=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!dark){dark=new Material(slots[i]);AssetDatabase.CreateAsset(dark,path);}
                    dark.CopyPropertiesFromMaterial(slots[i]);dark.color*=new Color(.65f,.68f,.7f,1);EditorUtility.SetDirty(dark);
                    slots[i]=dark;
                }
                renderer.sharedMaterials=slots;
            }
        }
        static GameObject Place(string name,Vector3 position,float angle=0,float scale=1)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+name+".fbx");
            if(!source)throw new Exception("Missing source model "+name);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,angle,0);go.transform.localScale*=scale;
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    string key=slots[i]?slots[i].name:"";
                    if(!Materials.TryGetValue(key,out var material))throw new Exception("Unmapped material "+key+" in "+name);
                    slots[i]=material;
                }
                renderer.sharedMaterials=slots;
            }
            return go;
        }
        static void Point(string name,Vector3 p,Color color,float intensity,float range){var l=new GameObject(name).AddComponent<Light>();l.type=LightType.Point;l.transform.position=p;l.color=color;l.intensity=intensity;l.range=range;}
        static void Capture(Camera camera,string path,float size)
        {
            camera.orthographicSize=size;var rt=new RenderTexture(1600,1000,24);rt.antiAliasing=4;camera.targetTexture=rt;
            camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=prior;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
