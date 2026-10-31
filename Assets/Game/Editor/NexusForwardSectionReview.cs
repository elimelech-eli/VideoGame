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
        [MenuItem("Nexus/Review/Build forward section")]
        public static void BuildForwardSection()
        {
            BuildArchitecture();
            foreach(var sign in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(sign.gameObject);
            // Stern is the boundary of this forward section, not the end of the eventual vessel.
            var stern=GameObject.Find("Continuous hull shell 0");if(!stern)throw new Exception("Missing aft shell socket");UnityEngine.Object.DestroyImmediate(stern);
            ArchitectureDoor("SEALED AFT EXPANSION",new Vector3(0,0,-14),8,0,true);
            Place("Door_DarkMetal",new Vector3(0,0,-14),0,.71f).name="Expansion hatch — closed port leaf";
            Place("Door_DarkMetal",new Vector3(0,0,-14.02f),180,.71f).name="Expansion hatch — closed starboard leaf";
            Point("Expansion hatch warning",new Vector3(0,2.7f,-13.6f),new Color(1,.3f,.08f),2.3f,4);
            var fixture=Place("Prop_Light_Wide",Vector3.zero);fixture.transform.rotation=Quaternion.Euler(90,0,0);CenterAt(fixture,new Vector3(0,2.85f,-13.7f));fixture.name="Hatch header fixture";
            // Bridge: flight/navigation toward the bow; mission planning occupies the command floor.
            foreach(float x in new[]{-1.7f,1.7f})
            {
                Place("Prop_Computer",new Vector3(x,0,13.2f),180,1.15f).name="Flight and navigation console";
                Furniture("chairDesk",new Vector3(x,0,12),180,1.2f).name="Pilot station seat";
            }
            var mission=Existing("Assets/Game/Resources/LabReview/Prefabs/ControlBank.prefab",new Vector3(0,0,9.1f),180,2.8f,"Mission-planning command console");
            ReduceConsoleGlow(mission);
            Place("Prop_Computer",new Vector3(4.5f,0,8.1f),-90).name="Bridge communications terminal";
            // Port operations: two separate banks, with the doorway and approach left clear.
            foreach(float x in new[]{-5.6f,-4.4f})
            {
                Place("Prop_Computer",new Vector3(x,0,2.8f),180,1.2f).name="Scanner analysis console";
                Place("Prop_Computer",new Vector3(x,0,-2),180,1.2f).name="Weapons targeting console";
            }
            Furniture("chairDesk",new Vector3(-5,0,1.5f),180,1.1f).name="Scanner operator seat";
            Furniture("chairDesk",new Vector3(-5,0,-3.3f),180,1.1f).name="Targeting operator seat";
            Place("Prop_Vent_Wide",new Vector3(-6,.015f,-.5f),90).name="Operations service grille";
            // Captain's quarters starts sparse: a berth, a plain desk and a personal trunk.
            var berth=Furniture("bedSingle",new Vector3(5,0,-3.7f),90,2.3f);berth.name="Captain's initial berth";BasicCabinFinish(berth);
            var desk=Furniture("desk",new Vector3(5,0,2.8f),90,1.8f);desk.name="Captain's basic desk";BasicCabinFinish(desk);
            Furniture("chairDesk",new Vector3(3.5f,0,2.8f),270,1.05f).name="Captain's desk chair";
            Furniture("radio",new Vector3(5,BoundsOf(desk).max.y,3.1f),90,.4f).name="Personal radio";
            Place("Prop_Chest",new Vector3(3.3f,0,-4.6f),0,.65f).name="Captain's personal trunk";
            // Purposeful edge dressing leaves the command floor and access lanes open.
            var bridgeRack=Existing("Assets/Game/Resources/LabReview/Prefabs/Cabinet.prefab",new Vector3(-4.8f,0,8),90,1.7f,"Bridge equipment rack");ReduceConsoleGlow(bridgeRack);
            Place("Prop_Chest",new Vector3(-4.8f,0,10.2f),90,.9f).name="Bridge emergency equipment case";
            var bench=Existing("Assets/Game/Resources/Transit/Prefabs/Bench.prefab",new Vector3(3.8f,0,6.4f),0,2.3f,"Bridge crew bench");ToneBridgeBench(bench);
            var coffee=Furniture("sideTable",new Vector3(-4.5f,0,6.6f),0,1.05f);coffee.name="Bridge break station";
            Furniture("kitchenCoffeeMachine",new Vector3(-4.5f,BoundsOf(coffee).max.y,6.6f),90,.5f).name="Crew coffee machine";
            var operationsRack=Existing("Assets/Game/Resources/LabReview/Prefabs/Cabinet.prefab",new Vector3(-6.7f,0,.2f),90,1.8f,"Operations equipment rack");ReduceConsoleGlow(operationsRack);
            Existing("Assets/Game/Resources/Transit/Prefabs/Electrical.prefab",new Vector3(-6.7f,0,-4),90,1.6f,"Operations service panel");
            Place("Prop_Cable_1",new Vector3(-6.4f,.02f,-.7f),0,.75f).name="Operations service cable";
            var bedside=Furniture("cabinetBed",new Vector3(6.7f,0,-3.2f),90,.65f);bedside.name="Captain's basic bedside cabinet";BasicCabinFinish(bedside);
            Furniture("lampRoundTable",new Vector3(6.7f,BoundsOf(bedside).max.y,-3.2f),0,.4f).name="Captain's reading lamp";
            Furniture("books",new Vector3(5,BoundsOf(desk).max.y,2.5f),0,.25f).name="Captain's logbooks";
            Point("Captain's reading light",new Vector3(6.7f,1.3f,-3.2f),new Color(1,.7f,.4f),1.5f,3);
            // Cargo hugs the shoulders. The whole center lane remains free to the expansion hatch.
            Existing("Assets/Game/Resources/Detailed/Prefabs/container.prefab",new Vector3(-4.3f,0,-8.5f),0,2.7f,"Port cargo container");
            Existing("Assets/Game/Resources/Detailed/Prefabs/container.prefab",new Vector3(4.1f,0,-8.5f),180,2.6f,"Starboard cargo container");
            foreach(var p in new[]{new Vector3(-3.5f,.5f,-11.5f),new Vector3(-3.5f,1.5f,-11.5f),new Vector3(3.5f,.5f,-11.5f)})Place("Prop_Crate3",p,0).name="Stowed cargo crate";
            Place("Prop_Barrel_Large",new Vector3(4.4f,0,-10.8f)).name="Cargo canister";
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if(light.name=="Cargo warm fill"){light.name="Operations instrument fill";light.color=new Color(.3f,.65f,1);light.intensity=2.3f;}
                if(light.name=="Engineering warm fill")light.name="Cargo bay work light";
            }
            Sign("MISSION PLANNING",new Vector3(0,.035f,7.2f),.18f);
            Sign("SCANNERS",new Vector3(-4.8f,.035f,1.2f),.17f);
            Sign("TARGETING",new Vector3(-4.8f,.035f,-1.5f),.17f);
            Sign("CAPTAIN",new Vector3(4.6f,.035f,.2f),.18f);
            Sign("AFT ACCESS // SEALED",new Vector3(0,.035f,-12.4f),.14f);
            Sign("CARGO — KEEP CLEAR",new Vector3(0,.035f,-8.2f),.14f);
            var actor=GameObject.Find("Operator scale reference — command deck");
            if(actor){var bounds=BoundsOf(actor);actor.transform.position+=new Vector3(0,0,6.5f)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);}
            var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single();camera.transform.rotation=Quaternion.Euler(50,45,0);
            Focus(camera,new Vector3(0,.6f,1));camera.orthographicSize=16.5f;
            VerifyCargoLane();
            VerifyDressingApproaches();
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Root+"NexusForwardSection.unity");
            Capture(camera,"Art/NexusShipReview/forward-overview.png",16.5f);
            Focus(camera,new Vector3(0,1,10));Capture(camera,"Art/NexusShipReview/forward-bridge.png",9.5f);
            Focus(camera,new Vector3(0,1,-.5f));Capture(camera,"Art/NexusShipReview/forward-operations-cabin.png",10.5f);
            Focus(camera,new Vector3(4.8f,.8f,-.3f));Capture(camera,"Art/NexusShipReview/forward-captain.png",6.5f);
            Focus(camera,new Vector3(0,1,-10));Capture(camera,"Art/NexusShipReview/forward-cargo-expansion.png",8.5f);
            File.WriteAllText("Art/NexusShipReview/forward-report.txt","Forward-section visual preview: bridge/mission selection, port scanners and targeting, basic captain's quarters, aft cargo and sealed rear expansion socket.\nCaptain's desk chair corrected to yaw 270. Added edge equipment racks, bench, break station, service panel, bedside cabinet/lamp and logbooks.\n393.5 square metre fitted deck retained; cargo route and added dressing's door approach bounds checked.\nFive static Unity captures. No gameplay, collision, interaction, save or unlock systems implemented. Expansion beyond stern is reserved, not constructed.\n");
            Debug.Log("NEXUS FORWARD SECTION COMPLETE");
        }
        static void ToneBridgeBench(GameObject go)
        {
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    string path=Root+"Materials/ShipBench_"+slots[i].name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!mat){mat=new Material(slots[i]);AssetDatabase.CreateAsset(mat,path);}mat.CopyPropertiesFromMaterial(slots[i]);
                    if(mat.HasProperty("_Color"))mat.color*=new Color(.3f,.4f,.45f,1);EditorUtility.SetDirty(mat);slots[i]=mat;
                }
                renderer.sharedMaterials=slots;
            }
        }
        static void VerifyDressingApproaches()
        {
            var zones=new[]{new Bounds(new Vector3(0,1.2f,5),new Vector3(2.4f,2.4f,3)),new Bounds(new Vector3(0,1.2f,-6),new Vector3(2.4f,2.4f,3)),new Bounds(new Vector3(-1.6f,1.2f,0),new Vector3(3,2.4f,2.4f)),new Bounds(new Vector3(1.6f,1.2f,0),new Vector3(3,2.4f,2.4f))};
            string[] names={"Bridge equipment rack","Bridge emergency equipment case","Bridge crew bench","Bridge break station","Crew coffee machine","Operations equipment rack","Operations service panel","Operations service cable","Captain's basic bedside cabinet","Captain's reading lamp","Captain's logbooks"};
            foreach(string name in names)
            {
                var go=GameObject.Find(name);if(!go)throw new Exception("Missing dressing audit object: "+name);
                var bounds=BoundsOf(go);foreach(var zone in zones)if(bounds.Intersects(zone))throw new Exception("Dressing obstructs doorway approach: "+name);
            }
            Debug.Log("FORWARD DRESSING CLEARANCE: eleven added props clear four door approach envelopes");
        }
        static void ReduceConsoleGlow(GameObject go)
        {
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    string path=Root+"Materials/BridgeConsole_"+slots[i].name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!mat){mat=new Material(slots[i]);AssetDatabase.CreateAsset(mat,path);}mat.CopyPropertiesFromMaterial(slots[i]);
                    if(mat.HasProperty("_EmissionColor"))mat.SetColor("_EmissionColor",mat.GetColor("_EmissionColor")*.3f);
                    EditorUtility.SetDirty(mat);slots[i]=mat;
                }
                renderer.sharedMaterials=slots;
            }
        }
        static void BasicCabinFinish(GameObject go)
        {
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    string path=Root+"Materials/BasicCabin_"+slots[i].name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!mat){mat=new Material(slots[i]);AssetDatabase.CreateAsset(mat,path);}mat.CopyPropertiesFromMaterial(slots[i]);
                    if(slots[i].name=="Furniture_carpet")mat.color=new Color(.16f,.18f,.12f);
                    mat.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(mat);slots[i]=mat;
                }
                renderer.sharedMaterials=slots;
            }
        }
        static void VerifyCargoLane()
        {
            var lane=new Bounds(new Vector3(0,1.2f,-10),new Vector3(2.4f,2.4f,7));int checkedObjects=0;
            foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if(go.transform.parent!=null||!(go.name.Contains("cargo container")||go.name=="Stowed cargo crate"||go.name=="Cargo canister"))continue;
                if(BoundsOf(go).Intersects(lane))throw new Exception("Cargo obstructs reserved aft route: "+go.name);checkedObjects++;
            }
            if(checkedObjects!=6)throw new Exception("Expected six cargo objects in clearance audit; saw "+checkedObjects);
            Debug.Log("FORWARD CARGO CLEARANCE: six cargo bounds clear a 2.4m central route");
        }
    }
}
