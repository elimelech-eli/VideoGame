using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VideoGame.Editor
{
    public static partial class NexusShipReviewAssets
    {
        public static void BuildGalaxyPlayer()
        {
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/Game/Scenes/Prototype.unity"},"Builds/Windows/VideoGame.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            Debug.Log("GALAXY PLAYER BUILD: "+report.summary.result+"; errors="+report.summary.totalErrors+"; duration="+report.summary.totalTime);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Galaxy player build failed");
        }
        public static void ExportGalaxyConsole()
        {
            EditorSceneManager.OpenScene(Root+"NexusGalaxyConsoleReview.unity");
            var root=new GameObject("Galaxy mission console");root.transform.position=new Vector3(0,0,9.1f);root.layer=9;
            var pedestal=GameObject.Find("Galaxy mission console — industrial pedestal");
            var bounds=BoundsOf(pedestal);
            foreach(string name in new[]{"Galaxy mission console — industrial pedestal","Milky Way — hovering projection","Galaxy projector spill"})
            {
                var child=GameObject.Find(name);if(!child)throw new Exception("Missing approved console part: "+name);
                child.transform.SetParent(root.transform,true);
            }
            foreach(var collider in root.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            var blocker=root.AddComponent<BoxCollider>();blocker.center=root.transform.InverseTransformPoint(bounds.center);blocker.size=bounds.size;
            root.transform.position=Vector3.zero;
            Directory.CreateDirectory("Assets/Game/Resources/Nexus");
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Resources/Nexus/GalaxyConsole.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("GALAXY CONSOLE EXPORTED: approved materials, animation and pedestal-only blocker");
        }
        [MenuItem("Nexus/Review/Build galaxy console")]
        public static void BuildGalaxyConsole()
        {
            EditorSceneManager.OpenScene(Root+"NexusForwardSection.unity");
            var old=GameObject.Find("Mission-planning command console");
            if(!old)throw new Exception("Missing mission console in approved forward review");
            UnityEngine.Object.DestroyImmediate(old);
            var fill=GameObject.Find("Command cool fill");if(fill)fill.GetComponent<Light>().intensity=1.4f;
            var pedestal=Existing("Assets/Game/Resources/Detailed/Prefabs/tank_control_panel.prefab",new Vector3(0,0,9.1f),180,2.4f,"Galaxy mission console — industrial pedestal");
            Debug.Log("PEDESTAL BOUNDS "+BoundsOf(pedestal));
            foreach(var r in pedestal.GetComponentsInChildren<Renderer>())
            {
                var materials=r.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];string mp=Root+"Galaxy/Pedestal_"+i+".mat";
                    var m=AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if(!m){m=new Material(source);AssetDatabase.CreateAsset(m,mp);}
                    m.CopyPropertiesFromMaterial(source);m.color*=new Color(.32f,.38f,.42f,1);
                    if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",Color.black);
                    materials[i]=m;
                }
                r.sharedMaterials=materials;
            }
            var galaxy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Galaxy/MilkyWay.fbx"));
            galaxy.name="Milky Way — hovering projection";
            var initial=BoundsOf(galaxy).size;
            galaxy.transform.localScale*=3.5f/Mathf.Max(initial.x,initial.y,initial.z);
            // Exported Blender XY surface becomes Unity XZ through FBX axis conversion.
            galaxy.transform.position=new Vector3(0,BoundsOf(pedestal).max.y+.6f,9.1f);
            var mat=new Material(Shader.Find("NexusReview/GalaxyProjection"));
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Galaxy/Galaxy.png");
            mat.SetFloat("_Intensity",.85f);
            string path=Root+"Galaxy/Projection.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing){existing.CopyPropertiesFromMaterial(mat);UnityEngine.Object.DestroyImmediate(mat);mat=existing;}
            else AssetDatabase.CreateAsset(mat,path);
            foreach(var r in galaxy.GetComponentsInChildren<Renderer>()){r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
            Debug.Log("GALAXY BOUNDS "+BoundsOf(galaxy));
            if(BoundsOf(galaxy).size.x<3.4f || BoundsOf(galaxy).size.y>.1f)throw new Exception("Invalid galaxy scale/orientation");
            if(BoundsOf(pedestal).min.z<7.5f || BoundsOf(pedestal).max.z>11)throw new Exception("Console intrudes into bridge entrance or pilot approach");
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"Galaxy/AmbientRotation.anim");
            if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Root+"Galaxy/AmbientRotation.anim");}
            clip.legacy=true;clip.wrapMode=WrapMode.Loop;
            clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.y",AnimationCurve.Linear(0,0,40,360));
            var animation=galaxy.AddComponent<Animation>();animation.AddClip(clip,"AmbientRotation");animation.clip=clip;animation.playAutomatically=true;
            Point("Galaxy projector spill",new Vector3(0,1.7f,9.1f),new Color(.2f,.6f,1),.4f,2.5f);
            var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single();
            Focus(camera,new Vector3(0,1.2f,10));camera.orthographicSize=7.5f;
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Root+"NexusGalaxyConsoleReview.unity");
            Capture(camera,"Art/NexusShipReview/galaxy-bridge.png",7.5f);
            Focus(camera,new Vector3(0,1.6f,9.1f));Capture(camera,"Art/NexusShipReview/galaxy-detail.png",4.2f);
            Focus(camera,new Vector3(0,.6f,1));Capture(camera,"Art/NexusShipReview/galaxy-overview.png",16.5f);
            Directory.CreateDirectory("Art/NexusShipReview/GalaxyFrames");
            Focus(camera,new Vector3(0,1.6f,9.1f));
            for(int i=0;i<48;i++)
            {
                clip.SampleAnimation(galaxy,i*40f/48);
                Capture(camera,"Art/NexusShipReview/GalaxyFrames/"+i.ToString("D3")+".png",4.2f);
            }
            Debug.Log("GALAXY REVIEW COMPLETE — 48 frame rotation audition; no mission interaction added");
        }
    }
}
