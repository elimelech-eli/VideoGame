using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VideoGame;
namespace VideoGame.Editor
{
    public static partial class NexusShipReviewAssets
    {
        public static void CaptureShipLife()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            var ship=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Resources/Nexus/StarshipNexus.prefab"));
            RenderSettings.ambientLight=new Color(.3f,.34f,.4f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            var light=new GameObject("Review light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(50,-30,0);
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.orthographic=true;camera.transform.rotation=Quaternion.Euler(50,45,0);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.02f,.03f);
            foreach(var label in ship.GetComponentsInChildren<TextMesh>())label.GetComponent<Renderer>().enabled=false;
            Focus(camera,new Vector3(0,1,1));Capture(camera,"Art/NexusShipReview/life-exterior.png",17);
            foreach(var shell in ship.GetComponentsInChildren<ShipRoomShell>())if(shell.RoomId==0)shell.gameObject.SetActive(false);
            foreach(var label in ship.GetComponentsInChildren<TextMesh>())label.GetComponent<Renderer>().enabled=StarshipLife.Room(label.transform.position)==0;
            Focus(camera,new Vector3(0,1.3f,11));Capture(camera,"Art/NexusShipReview/life-bridge.png",6.5f);
            foreach(var shell in ship.GetComponentsInChildren<ShipRoomShell>(true))shell.gameObject.SetActive(shell.RoomId!=1);
            foreach(var label in ship.GetComponentsInChildren<TextMesh>())label.GetComponent<Renderer>().enabled=StarshipLife.Room(label.transform.position)==1;
            Focus(camera,new Vector3(-5,1,1.5f));Capture(camera,"Art/NexusShipReview/life-operations.png",4.7f);
            foreach(var shell in ship.GetComponentsInChildren<ShipRoomShell>(true))shell.gameObject.SetActive(shell.RoomId!=3);
            foreach(var label in ship.GetComponentsInChildren<TextMesh>())label.GetComponent<Renderer>().enabled=StarshipLife.Room(label.transform.position)==3;
            Focus(camera,new Vector3(0,1,-9.5f));Capture(camera,"Art/NexusShipReview/cargo-services.png",8);
        }
        static void AddShipLife(GameObject root)
        {
            root.AddComponent<StarshipLife>();
            var donor=root.GetComponentsInChildren<Transform>().First(t=>t.name=="Expansion hatch — closed port leaf").gameObject;
            foreach(var entry in new[]{new Vector3(0,0,5),new Vector3(0,0,-6),new Vector3(-1.6f,0,0),new Vector3(1.6f,0,0)})
            {
                var door=new GameObject("Automatic pressure door");door.transform.SetParent(root.transform);door.transform.localPosition=entry;
                door.transform.localRotation=Quaternion.Euler(0,entry.x==0?0:90,0);
                var script=door.AddComponent<ShipDoor>();
                foreach(int side in new[]{-1,1})
                {
                    var leaf=UnityEngine.Object.Instantiate(donor,door.transform);leaf.name=side<0?"Port sliding leaf":"Starboard sliding leaf";
                    foreach(var c in leaf.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                    leaf.transform.rotation=door.transform.rotation;
                    // Fit to the measured opening, independent of the donor's pivot.
                    leaf.transform.SetParent(null,true);leaf.transform.rotation=Quaternion.identity;
                    var b=BoundsOf(leaf);leaf.transform.localScale=Vector3.Scale(leaf.transform.localScale,new Vector3(1.1f/b.size.x,2.5f/b.size.y,.14f/b.size.z));
                    CenterAt(leaf,new Vector3(side*.55f,1.25f,0));leaf.transform.SetParent(door.transform,false);
                    if(side<0)script.Left=leaf.transform;else script.Right=leaf.transform;
                }
                door.layer=9;script.Blocker=door.AddComponent<BoxCollider>();script.Blocker.center=Vector3.up*1.25f;script.Blocker.size=new Vector3(2.2f,2.5f,.16f);
            }
            var hullMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/ExteriorHull.mat");
            if(!hullMaterial){hullMaterial=new Material(Shader.Find("Nexus/Ship Shell"));AssetDatabase.CreateAsset(hullMaterial,Root+"Materials/ExteriorHull.mat");}
            float[,] zones={{-20,20,5,20},{-20,-1.6f,-6,5},{1.6f,20,-6,5},{-20,20,-20,-6},{-1.6f,1.6f,-6,5}};
            for(int room=0;room<5;room++)
            {
                var polygon=CutterHull.Select(p=>new DeckVertex(new Vector3(p.x,0,p.y),p/4)).ToList();
                polygon=ClipDeck(polygon,new[]{new Vector2(zones[room,0],zones[room,2]),new Vector2(zones[room,1],zones[room,2]),new Vector2(zones[room,1],zones[room,3]),new Vector2(zones[room,0],zones[room,3])});
                var vertices=new List<Vector3>();var triangles=new List<int>();
                foreach(int half in new[]{-1,1})
                {
                    float min=half<0?-20:0,max=half<0?0:20;
                    var part=ClipDeck(polygon,new[]{new Vector2(min,-20),new Vector2(max,-20),new Vector2(max,20),new Vector2(min,20)});
                    int offset=vertices.Count;foreach(var p in part)vertices.Add(RoofPoint(p.p));
                    for(int n=1;n<part.Count-1;n++){triangles.Add(offset);triangles.Add(offset+n+1);triangles.Add(offset+n);}
                }
                for(int n=0;n<polygon.Count;n++)
                {
                    var a=polygon[n].p;var b=polygon[(n+1)%polygon.Count].p;var mid=(a+b)*.5f;
                    bool outside=false;
                    for(int h=0;h<CutterHull.Length;h++)
                    {
                        var c=CutterHull[h];var d=CutterHull[(h+1)%CutterHull.Length];var v=d-c;var q=new Vector2(mid.x,mid.z)-c;
                        if(Mathf.Abs(v.x*q.y-v.y*q.x)<.01f)outside=true;
                    }
                    if(!outside)continue;
                    int k=vertices.Count;vertices.Add(a+Vector3.up*.85f);vertices.Add(b+Vector3.up*.85f);vertices.Add(RoofPoint(b));vertices.Add(RoofPoint(a));
                    triangles.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});
                }
                string path=Root+"Architecture/ExteriorRoom"+room+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                var shell=new GameObject("Exterior shell room "+room);shell.transform.SetParent(root.transform);shell.AddComponent<MeshFilter>().sharedMesh=mesh;shell.AddComponent<MeshRenderer>().sharedMaterial=hullMaterial;
                var visibility=shell.AddComponent<ShipRoomShell>();visibility.RoomId=room;
                visibility.InteriorLabels=root.GetComponentsInChildren<TextMesh>().Where(t=>StarshipLife.Room(t.transform.position)==room).Select(t=>t.GetComponent<Renderer>()).ToArray();
            }
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                var materials=renderer.sharedMaterials;bool screen=false;
                for(int i=0;i<materials.Length;i++)if(materials[i] && materials[i].name.Contains("M_Decal_Screen"))
                {
                    string path=Root+"Materials/LiveConsole.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!mat){mat=new Material(Shader.Find("Nexus/Live Console"));mat.mainTexture=materials[i].mainTexture;AssetDatabase.CreateAsset(mat,path);}materials[i]=mat;screen=true;
                }
                if(screen)renderer.sharedMaterials=materials;
            }
            AddSeatedCrew(root,new Vector3(-1.7f,.55f,12),"Pilot");
            AddSeatedCrew(root,new Vector3(-5,.55f,1.5f),"Scanner operator");
        }
        static Vector3 RoofPoint(Vector3 p)=>new Vector3(p.x,3.15f+.55f*(1-Mathf.Abs(p.x)/8),p.z);
        static void AddSeatedCrew(GameObject ship,Vector3 seat,string role)
        {
            var actor=Existing("Assets/Game/Resources/Detailed/Prefabs/OperativeV03.prefab",Vector3.zero,0,2.05f,"Crew — "+role);
            var rifle=actor.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Equipped rifle");if(rifle)UnityEngine.Object.DestroyImmediate(rifle.gameObject);
            foreach(var c in actor.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            var bones=actor.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            Action<string,string,Vector3> aim=(a,b,d)=>{bones[a].rotation=Quaternion.FromToRotation(bones[b].position-bones[a].position,d)*bones[a].rotation;};
            foreach(string side in new[]{"L","R"})
            {
                aim("thigh_"+side,"shin_"+side,Vector3.forward);aim("shin_"+side,"foot_"+side,Vector3.down);
                aim("upper_arm_"+side,"forearm_"+side,new Vector3(side=="L"?-.12f:.12f,-.3f,.35f));aim("forearm_"+side,"hand_"+side,Vector3.forward);
            }
            actor.transform.position+=seat-bones["hips"].position;actor.transform.SetParent(ship.transform,true);
            var idle=actor.AddComponent<ShipCrewIdle>();idle.Head=bones["head"];idle.Hand=bones["hand_R"];
        }
    }
}
