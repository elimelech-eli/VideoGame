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
        [MenuItem("Nexus/Review/Build full ship preview")]
        public static void BuildFullShip()
        {
            Build(); // Preserve the approved connection, source materials, camera and operator scale.
            // Open the main room's forward and starboard boundaries for fitted connecting doors.
            foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if(go.transform.parent!=null||!go.name.StartsWith("WallAstra_"))continue;
                var p=go.transform.position;
                if(Mathf.Abs(p.x-4)<.01f && (Mathf.Abs(p.z)<.01f||Mathf.Abs(p.z-4)<.01f)
                    ||Mathf.Abs(p.z-4)<.01f && (Mathf.Abs(p.x)<.01f||Mathf.Abs(p.x-4)<.01f))
                    UnityEngine.Object.DestroyImmediate(go);
            }
            // Protected center lane: move mission consoles away from the forward bridge doorway.
            foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if(go.transform.parent==null&&go.name=="Prop_Computer")
                {
                    bool second=go.transform.position.x>3;
                    go.transform.position=new Vector3(second?5:3.8f,0,4.5f);go.transform.rotation=Quaternion.Euler(0,180,0);
                }
            // Bridge: forward cockpit with two stations and a continuous viewport family.
            Floor("Platform_Metal",0,8);Floor("Platform_Metal",4,8);
            Passage("Bridge",new Vector3(2,0,6),8,0,true);
            Place("WallAstra_Straight_Window",new Vector3(0,0,8),-90);
            Place("WallAstra_Straight_Window",new Vector3(4,0,8),-90);
            Place("WallAstra_Straight_Window",new Vector3(4,0,8));
            Bulkhead("Bridge port cutaway",new Vector3(-2,0,8),4,1,0);
            foreach(float x in new[]{.3f,3.7f})
            {
                Place("Prop_Computer",new Vector3(x,0,8.8f),180,1.1f);
                Furniture("chairDesk",new Vector3(x,0,7.7f),180,1.2f);
            }
            foreach(var corner in new[]{new Vector3(6,0,10),new Vector3(-2,0,10)})Post(corner);
            // Engineering: machinery, an actual work surface, and a sealed future access hatch.
            Floor("Platform_DarkPlates",8,0);Floor("Platform_DarkPlates",8,4);
            Passage("Engineering",new Vector3(6,0,2),8,90,false);
            Bulkhead("Engineering outer wall",new Vector3(10,0,2),8,3,0);
            Bulkhead("Engineering forward wall left",new Vector3(6.3f,0,6),.6f,3,90);
            Bulkhead("Engineering forward wall right",new Vector3(9.7f,0,6),.6f,3,90);
            var sealedFrame=Place("Door_Frame_Square",new Vector3(8,0,6),0,.6f);CargoFinish(sealedFrame);
            Place("Door_DarkMetal",new Vector3(8,0,6),0,.71f);
            Place("Door_DarkMetal",new Vector3(8,0,6.02f),180,.71f);
            Bulkhead("Engineering aft cutaway",new Vector3(8,0,-2),4,1,90);
            Existing("Assets/Game/Resources/Detailed/Prefabs/tank_capsule.prefab",new Vector3(9,0,3.7f),0,2.5f,"Reactor coolant assembly");
            Existing("Assets/Game/Resources/Transit/Prefabs/Compressor.prefab",new Vector3(9,0,.8f),90,2,"Engineering compressor");
            Furniture("desk",new Vector3(7.5f,0,-.6f),0,1.9f);
            Place("Prop_Crate4",new Vector3(7.6f,1.05f,-.5f),15,.35f);
            Place("Prop_Chest",new Vector3(8.9f,0,-.7f),0,.8f);
            Place("Prop_Cable_1",new Vector3(8.5f,.02f,2.1f),0,.8f);
            Existing("Assets/Game/Resources/Transit/Prefabs/PowerCabinet.prefab",new Vector3(9.4f,0,5),90,1.6f,"Power distribution cabinet");
            // Personal cabin: no industrial floor change without a corresponding doorway.
            Floor("Platform_Squares",0,-4);Floor("Platform_Squares",4,-4);
            Passage("Crew cabin",new Vector3(2,0,-2),8,0,false);
            Bulkhead("Cabin port cutaway",new Vector3(-2,0,-4),4,1,0);
            Bulkhead("Cabin aft cutaway",new Vector3(2,0,-6),8,1,90);
            Bulkhead("Cabin starboard wall",new Vector3(6,0,-4),4,3,0);
            Furniture("bedSingle",new Vector3(-.4f,0,-4.6f),90,2.3f);
            Furniture("cabinetBed",new Vector3(1.2f,0,-5),0,.65f);
            Furniture("lampRoundTable",new Vector3(1.2f,.7f,-5),0,.42f);
            Furniture("desk",new Vector3(4.5f,0,-3.9f),90,1.6f);
            Furniture("chairDesk",new Vector3(3.3f,0,-3.9f),90,1.05f);
            Furniture("radio",new Vector3(4.5f,.82f,-3.5f),90,.4f);
            Furniture("books",new Vector3(4.5f,.82f,-4.2f),0,.3f);
            Furniture("pottedPlant",new Vector3(5,0,-5.1f),0,.9f);
            // A small mess station gives the central room a lived-in use.
            var messTable=Furniture("sideTable",new Vector3(4.7f,0,.1f),0,1.2f);
            Furniture("kitchenCoffeeMachine",new Vector3(4.7f,BoundsOf(messTable).max.y,.1f),90,.55f);
            var viewport=new Material(Shader.Find("Hidden/Nexus/ShipViewport"));
            string viewportPath=Root+"Materials/ShipViewport.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(viewportPath)){UnityEngine.Object.DestroyImmediate(viewport);viewport=AssetDatabase.LoadAssetAtPath<Material>(viewportPath);}
            else AssetDatabase.CreateAsset(viewport,viewportPath);
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var slots=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<slots.Length;i++)if(slots[i]&&slots[i].name=="M_Glass"){slots[i]=viewport;changed=true;}
                if(changed)renderer.sharedMaterials=slots;
            }
            // Clear floor lettering is part of the fictional ship's signage, not an overlay.
            Sign("CARGO",new Vector3(-4,.035f,4.4f),.16f);
            Sign("MISSION / OPS",new Vector3(2,.035f,.2f),.18f);
            Sign("FLIGHT DECK",new Vector3(2,.035f,6.8f),.15f);
            Sign("REPAIR",new Vector3(8,.035f,2),.14f);
            Sign("CREW",new Vector3(2,.035f,-2.8f),.15f);
            Sign("EXPANSION // SEALED",new Vector3(8,.035f,4.8f),.085f);
            Point("Cabin warm light",new Vector3(.4f,2.4f,-4.2f),new Color(1,.66f,.35f),3.5f,6);
            Point("Cabin desk light",new Vector3(4,2,-4),new Color(1,.78f,.5f),2,4);
            Point("Bridge blue fill",new Vector3(2,2.3f,8.4f),new Color(.35f,.66f,1),3,6);
            Point("Engineering work light",new Vector3(8,2.5f,1),new Color(1,.6f,.25f),3.5f,6);
            Point("Expansion warning light",new Vector3(8,2.6f,5.5f),new Color(1,.25f,.08f),2,3);
            var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single();
            camera.orthographicSize=10.5f;
            Focus(camera,new Vector3(2,1.1f,2));
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Root+"NexusFullShipPreview.unity");
            Capture(camera,"Art/NexusShipReview/ship-overview.png",10.5f);
            Focus(camera,new Vector3(2,1.2f,7.1f));Capture(camera,"Art/NexusShipReview/ship-bridge.png",7.5f);
            Focus(camera,new Vector3(3,1,-2.2f));Capture(camera,"Art/NexusShipReview/ship-cabin-engineering.png",9);
            File.WriteAllText("Art/NexusShipReview/full-ship-report.txt","Static full-ship art preview, Unity "+Application.unityVersion+"\nApproved room connection retained. Bridge, engineering/repair, personal cabin and sealed expansion added.\nThree Camera.Render images. No gameplay or build settings changes. Runtime traversal and service interactions not run.\n");
            Debug.Log("NEXUS FULL SHIP PREVIEW COMPLETE");
        }
        static void Floor(string name,float x,float z)=>Place(name,new Vector3(x,0,z));
        static void Post(Vector3 p)=>Place("Column_Simple",p,0,.6f);
        static void Focus(Camera camera,Vector3 p)=>camera.transform.position=p-camera.transform.forward*35;
        static void Passage(string name,Vector3 center,float span,float yaw,bool cutawayBoth)
        {
            float halfFrame=2.42672491f*.6f;
            // Wall helper's native span follows Z. Portal span follows local X.
            var direction=Quaternion.Euler(0,yaw,0)*Vector3.right;
            float segment=span/2-halfFrame;
            Bulkhead(name+" left cutaway",center-direction*(halfFrame+segment/2),segment,1,yaw+90);
            Bulkhead(name+" right wall",center+direction*(halfFrame+segment/2),segment,cutawayBoth?1:3,yaw+90);
            var frame=Place("Door_Frame_Square",center,yaw,.6f);frame.name=name+" fitted doorway";CargoFinish(frame);
            Place("Prop_Vent_Wide",center+Vector3.up*.015f,yaw,1.5f);
        }
        static GameObject Existing(string path,Vector3 p,float yaw,float size,string name)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Missing existing donor "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=name;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var bounds=BoundsOf(go);float scale=size/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));go.transform.localScale*=scale;
            bounds=BoundsOf(go);go.transform.position+=p-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            return go;
        }
        static GameObject Furniture(string name,Vector3 p,float yaw,float size)
        {
            var go=Existing(Root+"Furniture/"+name+".fbx",p,yaw,size,name);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    string sourceName=slots[i]?slots[i].name:"unknown";
                    string path=Root+"Materials/Furniture_"+sourceName+".mat";
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
                    string role=sourceName.ToLowerInvariant();
                    mat.color=role.Contains("wood")?new Color(.16f,.20f,.22f):role=="carpet"?new Color(.08f,.20f,.23f):role.Contains("carpetwhite")?new Color(.65f,.7f,.68f):role.Contains("metal")?new Color(.33f,.4f,.43f):slots[i]&&slots[i].HasProperty("_Color")?slots[i].color:new Color(.22f,.28f,.3f);
                    mat.SetFloat("_Metallic",role.Contains("metal")||role.Contains("wood")?.5f:.05f);mat.SetFloat("_Glossiness",.25f);EditorUtility.SetDirty(mat);
                    slots[i]=mat;
                }
                renderer.sharedMaterials=slots;
            }
            return go;
        }
        static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        static void Sign(string words,Vector3 p,float size)
        {
            var text=new GameObject("Deck marking — "+words).AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.fontSize=64;text.characterSize=size*.18f;text.color=new Color(.35f,.45f,.47f);text.transform.position=p;text.transform.rotation=Quaternion.Euler(90,0,0);
        }
    }
}
