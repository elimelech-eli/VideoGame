using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VideoGame.Editor
{
    // Isolated art audition: never registered in build settings or live biome generation.
    public static class FactoryRoomReview
    {
        const string Root="Assets/FactoryReview/";
        [Serializable] class Catalog { public Entry[] materials; }
        [Serializable] class Entry { public string name,albedo,normal,packed,emission; }
        static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>();
        static readonly Dictionary<string,Transform> Sources=new Dictionary<string,Transform>();
        static readonly List<string> Measures=new List<string>();
        static GameObject assembly;
        static Texture2D Tex(string file)=>string.IsNullOrEmpty(file)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+file);
        [MenuItem("Nexus/Review/Factory room audition")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory("Art/FactoryIntake");
            AssetDatabase.Refresh();Materials.Clear();Sources.Clear();Measures.Clear();
            foreach(string file in Directory.GetFiles(Root+"Textures","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                importer.textureType=file.EndsWith("_normal.png")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=file.EndsWith("_albedo.png")||file.EndsWith("_emission.png");
                importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.SaveAndReimport();
            }
            foreach(var e in JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"catalog.json")).materials)
            {
                var path=Root+"Materials/"+e.name.Replace(" ","_").Replace("+","plus")+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
                m.shaderKeywords=new string[0];m.color=Color.white;m.enableInstancing=true;
                m.mainTexture=Tex(e.albedo);if(!m.mainTexture)throw new Exception("Missing source albedo: "+e.name);
                m.SetTexture("_BumpMap",Tex(e.normal));if(Tex(e.normal))m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap",Tex(e.packed));if(Tex(e.packed))m.EnableKeyword("_METALLICGLOSSMAP");
                m.SetFloat("_GlossMapScale",1);m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",.25f);
                m.SetTexture("_EmissionMap",Tex(e.emission));m.SetColor("_EmissionColor",Color.white*1.5f);
                if(Tex(e.emission))m.EnableKeyword("_EMISSION");
                Materials[e.name]=m;EditorUtility.SetDirty(m);
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var templates=new List<GameObject>();
            foreach(string file in Directory.GetFiles(Root+"Models","*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(file);
                importer.importCameras=false;importer.importLights=false;importer.animationType=ModelImporterAnimationType.None;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                var template=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(file));
                foreach(var r in template.GetComponentsInChildren<MeshRenderer>())
                {
                    Sources[r.name]=r.transform;
                    var b=r.bounds;
                    Measures.Add(r.name+" size="+b.size.ToString("F4")+" center="+b.center.ToString("F4")+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL")));
                }
                template.SetActive(false);templates.Add(template);
            }
            File.WriteAllLines("Art/FactoryIntake/unity-source-bounds.txt",Measures);
            assembly=new GameObject("Factory assembly hall — APPROVAL AUDITION");
            Cutaway("Solid floor foundation",new Vector3(0,-.135f,0),new Vector3(15,.24f,12.5f));
            Cutaway("Corridor foundation",new Vector3(0,-.135f,8.75f),new Vector3(5,.24f,5));
            // 15 x 12.5m room; 5m service corridor, native 2.5m source grid.
            for(int x=0;x<6;x++)for(int z=0;z<5;z++)Place("Factory_modularKit_24",new Vector3(-6.25f+2.5f*x,0,-5+2.5f*z));
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)Place("Factory_modularKit_24",new Vector3(-1.25f+2.5f*x,0,7.5f+2.5f*z));
            for(int x=0;x<6;x++)if(x<2||x>3)Place("Factory_modularKit_01",new Vector3(-6.25f+2.5f*x,0,6.25f),90);
            for(int z=0;z<5;z++)Place(z==3?"Factory_modularKit_04":"Factory_modularKit_01",new Vector3(7.5f,0,-5+2.5f*z));
            for(int z=0;z<2;z++)foreach(float x in new[]{-2.5f,2.5f})Place("Factory_modularKit_01",new Vector3(x,0,7.5f+2.5f*z));
            for(int x=0;x<2;x++)Place("Factory_modularKit_01",new Vector3(-1.25f+x*2.5f,0,11.25f),90);
            // Finished near-side cutaway, using source brick UVs and solid caps, not exposed sheets.
            Cutaway("South cutaway",new Vector3(0,.26f,-6.25f),new Vector3(15,.52f,.20f));
            Cutaway("West cutaway",new Vector3(-7.5f,.26f,0),new Vector3(.20f,.52f,12.5f));
            foreach(var p in new[]{new Vector3(7.5f,0,6.25f),new Vector3(7.5f,0,-6.25f),new Vector3(-7.5f,0,6.25f)})Place("Factory_modularKit_28",p);
            // Production equipment occupies room edges; center lane and 5m doorway stay clear.
            Place("Generator_02",new Vector3(4.7f,0,3.5f),90);
            Place("Generator_01",new Vector3(5.7f,0,-4.5f),-90);
            Place("Plane.093",new Vector3(4.7f,0,-1.6f),90);
            Place("Plane.095",new Vector3(4.7f,0,.8f),90);
            Place("Shelves_01",new Vector3(-5.3f,0,5.65f));
            Place("Shelves_02",new Vector3(-5.6f,0,3.7f));
            Place("Forklift_01",new Vector3(-4.8f,0,-3.5f),25);
            Place("Crate_01",new Vector3(-5.5f,0,1.5f),-8);
            Place("Crate_03",new Vector3(-4.3f,0,1.5f),8);
            Place("Cylinder.015",new Vector3(6,0,-3.2f));
            Place("Cylinder.016",new Vector3(5.3f,0,-3.4f));
            Place("Pallet_01",new Vector3(-5.7f,0,-.6f));
            Place("Cone_01",new Vector3(3.15f,0,4.5f));
            Place("Cone_01",new Vector3(3.15f,0,2.6f));
            Place("Pipes_02",new Vector3(7.02f,0,1),0);
            Place("Pipes_06",new Vector3(7.02f,0,2.93f));
            Place("Pipes_06",new Vector3(7.02f,0,-.93f),180);
            Place("Cube.092",new Vector3(-5.6f,1.13f,3.7f));
            Place("Cube.083",new Vector3(4.7f,.95f,-1.6f),90);
            Place("Sign_02",new Vector3(2.9f,0,5.1f),15);
            foreach(float x in new[]{-5f,5f})
            {
                Place("Light_01",new Vector3(x,2.25f,6.05f));
                Point("Cold industrial work light",new Vector3(x,2.1f,5.6f),new Color(.58f,.77f,1),1.8f,7);
            }
            Point("Generator warning",new Vector3(5.4f,1.8f,3.6f),new Color(1,.36f,.08f),2,5);
            Point("Service corridor",new Vector3(0,2,9.4f),new Color(.45f,.68f,1),3,6);
            var playerSource=Resources.Load<GameObject>(VideoGame.PlayerRig.PrefabPath);
            if(playerSource)
            {
                var actor=UnityEngine.Object.Instantiate(playerSource);actor.name="Operator — scale reference";
                actor.transform.position=new Vector3(-.8f,0,-1.5f);actor.transform.rotation=Quaternion.Euler(0,35,0);
                foreach(var behavior in actor.GetComponentsInChildren<MonoBehaviour>())behavior.enabled=false;
                var rs=actor.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                actor.transform.position+=Vector3.up*(-b.min.y);Measures.Add("Operator bounds="+b.size.ToString("F3"));
            }
            foreach(var t in templates)UnityEngine.Object.DestroyImmediate(t);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.20f,.23f,.28f);RenderSettings.fog=false;
            var sun=new GameObject("Soft industrial skylight").AddComponent<Light>();sun.type=LightType.Directional;
            sun.color=new Color(.72f,.82f,1);sun.intensity=.9f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(55,-30,0);
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=70;QualitySettings.pixelLightCount=12;QualitySettings.antiAliasing=4;
            var cam=new GameObject("Factory review camera").AddComponent<Camera>();cam.orthographic=true;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.033f,.043f);cam.farClipPlane=150;cam.nearClipPlane=.1f;
            cam.transform.rotation=Quaternion.Euler(50,45,0);Focus(cam,new Vector3(0,.3f,1.5f));cam.orthographicSize=11;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,Root+"FactoryRoomAudition.unity");
            Capture(cam,"Art/FactoryIntake/factory-room-overview.png",11);
            Focus(cam,new Vector3(0,.3f,1));Capture(cam,"Art/FactoryIntake/factory-room-gameplay.png",7.5f);
            Focus(cam,new Vector3(3,.8f,2.8f));Capture(cam,"Art/FactoryIntake/factory-room-detail.png",5.7f);
            cam.transform.rotation=Quaternion.Euler(90,0,0);Focus(cam,new Vector3(0,0,2.5f));Capture(cam,"Art/FactoryIntake/factory-room-plan.png",10.5f);
            File.WriteAllLines("Art/FactoryIntake/unity-assembly.txt",Measures);
            Debug.Log("FACTORY_ROOM_AUDITION_READY: original modular FBX + PBR, native 2.5m grid; runtime integration and playtesting pending.");
        }
        static GameObject Place(string key,Vector3 p,float yaw=0)
        {
            if(!Sources.TryGetValue(key,out var source))throw new Exception("Missing approved source object "+key);
            var parent=new GameObject(key);parent.transform.SetParent(assembly.transform);
            var go=UnityEngine.Object.Instantiate(source.gameObject,parent.transform);go.SetActive(true);
            go.transform.rotation=source.rotation;go.transform.localScale=source.lossyScale;
            foreach(var r in go.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterials=r.sharedMaterials.Select(m=>Materials.TryGetValue(m.name,out var mapped)?mapped:throw new Exception("Unmapped source material "+m.name+" on "+key)).ToArray();
            }
            var b=Bounds(go);go.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
            parent.transform.rotation=Quaternion.Euler(0,yaw,0);parent.transform.position=p;
            var measured=Bounds(parent);Measures.Add("PLACED "+key+" min="+measured.min.ToString("F3")+" max="+measured.max.ToString("F3"));
            if(key!="Factory_modularKit_24")foreach(var f in go.GetComponentsInChildren<MeshFilter>())f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
            return parent;
        }
        static Bounds Bounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        static void Cutaway(string name,Vector3 p,Vector3 size)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(assembly.transform);go.transform.position=p;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=Materials["concrete"];
        }
        static void Point(string n,Vector3 p,Color c,float intensity,float range){var l=new GameObject(n).AddComponent<Light>();l.type=LightType.Point;l.transform.position=p;l.color=c;l.intensity=intensity;l.range=range;}
        static void Focus(Camera c,Vector3 target){c.transform.position=target-c.transform.forward*40;}
        static void Capture(Camera c,string path,float size)
        {
            c.orthographicSize=size;var rt=new RenderTexture(1600,1000,24){antiAliasing=4};c.targetTexture=rt;c.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=prior;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
