using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VideoGame.Editor
{
    public static partial class NexusShipReviewAssets
    {
        static readonly Vector2[] CutterHull={new Vector2(-4,-14),new Vector2(4,-14),new Vector2(7,-10),new Vector2(8,-4),new Vector2(8,3),new Vector2(6.5f,8),new Vector2(4,14),new Vector2(2,16),new Vector2(-2,16),new Vector2(-4,14),new Vector2(-6.5f,8),new Vector2(-8,3),new Vector2(-8,-4),new Vector2(-7,-10)};
        static readonly Dictionary<string,float> DeckAreas=new Dictionary<string,float>();
        struct DeckVertex { public Vector3 p;public Vector2 uv;public DeckVertex(Vector3 position,Vector2 tex){p=position;uv=tex;} }
        [MenuItem("Nexus/Review/Build expedition cutter architecture")]
        public static void BuildArchitecture()
        {
            Build(); // Initialize the approved downloaded material family; preserve the room reference on disk.
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Directory.CreateDirectory(Root+"Architecture");AssetDatabase.Refresh();
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.37f,.41f,.47f);RenderSettings.fog=false;
            var light=new GameObject("Architecture review fill").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.color=new Color(.75f,.84f,1);light.transform.rotation=Quaternion.Euler(55,-25,0);light.shadows=LightShadows.Soft;
            QualitySettings.antiAliasing=4;QualitySettings.pixelLightCount=12;
            int pieces=0;DeckAreas.Clear();
            for(int x=-6;x<=6;x+=4)for(int z=-12;z<=16;z+=4)
            {
                pieces+=CroppedDeck("CARGO", "Platform_DarkPlates",x,z,-20,20,-14,-6);
                pieces+=CroppedDeck("COMMAND", "Platform_Metal",x,z,-20,20,5,16);
                pieces+=CroppedDeck("OPERATIONS", "Platform_Metal",x,z,-20,-1.6f,-6,5);
                pieces+=CroppedDeck("CAPTAIN", "Platform_Squares",x,z,1.6f,20,-6,5);
                pieces+=CroppedDeck("SPINE", "Platform_Metal",x,z,-1.6f,1.6f,-6,5);
            }
            float expectedArea=0;for(int i=0;i<CutterHull.Length;i++){var a=CutterHull[i];var b=CutterHull[(i+1)%CutterHull.Length];expectedArea+=(a.x*b.y-b.x*a.y)*.5f;}
            float deckArea=0;foreach(float area in DeckAreas.Values)deckArea+=area;
            if(Mathf.Abs(expectedArea-deckArea)>.02f)throw new Exception("Fitted deck coverage mismatch: "+deckArea+" vs "+expectedArea);
            var viewport=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/ShipViewport.mat");
            if(!viewport){viewport=new Material(Shader.Find("Hidden/Nexus/ShipViewport"));AssetDatabase.CreateAsset(viewport,Root+"Materials/ShipViewport.mat");}
            for(int i=0;i<CutterHull.Length;i++)
            {
                var a=CutterHull[i];var b=CutterHull[(i+1)%CutterHull.Length];var delta=b-a;var mid=(a+b)/2;
                float yaw=Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg;
                // Near-facing shell is a deliberate consistent cutaway; far-facing shell retains full height.
                bool near=delta.x-delta.y>.01f;
                float height=near?1:3;
                Bulkhead("Keel belt "+i,new Vector3(mid.x,-1,mid.y),delta.magnitude+.12f,1,yaw);
                if(!near&&mid.y>7)
                {
                    var wall=Place("WallAstra_Straight_Window",Vector3.zero,yaw);
                    wall.transform.localScale=Vector3.Scale(wall.transform.localScale,new Vector3(1,1,delta.magnitude/4));
                    CenterAt(wall,new Vector3(mid.x,1.5f,mid.y));wall.name="Forward observation frontage "+i;
                    foreach(var renderer in wall.GetComponentsInChildren<Renderer>())
                    {
                        var slots=renderer.sharedMaterials;for(int s=0;s<slots.Length;s++)if(slots[s]&&slots[s].name=="M_Glass")slots[s]=viewport;renderer.sharedMaterials=slots;
                    }
                }
                else Bulkhead("Continuous hull shell "+i,new Vector3(mid.x,0,mid.y),delta.magnitude+.12f,height,yaw);
                var post=Place("Column_Simple",new Vector3(a.x,0,a.y),yaw,.6f);
                bool previousNear=(CutterHull[i]-CutterHull[(i+CutterHull.Length-1)%CutterHull.Length]).x-(CutterHull[i]-CutterHull[(i+CutterHull.Length-1)%CutterHull.Length]).y>.01f;
                float postHeight=near&&previousNear?1:3;post.transform.localScale=Vector3.Scale(post.transform.localScale,new Vector3(1,postHeight/3,1));CargoFinish(post);
            }
            // Rooms follow the hull. A straight axial route connects the forward rooms and future aft access.
            ArchitectureDoor("COMMAND DECK",new Vector3(0,0,5),14.8f,0,true);
            ArchitectureDoor("AFT CARGO",new Vector3(0,0,-6),15.2f,0,true);
            SideDoor("PORT OPERATIONS",-1.6f);
            SideDoor("CAPTAIN'S QUARTERS",1.6f);
            // Keep this architecture stage empty: room purpose is shown by restrained deck markings.
            Sign("COMMAND DECK",new Vector3(0,.035f,9),.14f);
            Sign("OPERATIONS",new Vector3(-4.6f,.035f,-.4f),.14f);
            Sign("CAPTAIN",new Vector3(4.6f,.035f,-.4f),.12f);
            Sign("CARGO",new Vector3(0,.035f,-10),.13f);
            Sign("FORWARD",new Vector3(0,.035f,2.2f),.09f);
            // A scale reference is essential when judging the enlarged bridge and spine.
            var actor=Existing("Assets/Game/Resources/Detailed/Prefabs/OperativeV03.prefab",new Vector3(0,0,8),155,2.15f,"Operator scale reference — command deck");
            foreach(var behavior in actor.GetComponentsInChildren<MonoBehaviour>())behavior.enabled=false;
            Point("Command cool fill",new Vector3(0,2.5f,10),new Color(.4f,.7f,1),3,10);
            Point("Cargo warm fill",new Vector3(-5,2,0),new Color(1,.62f,.3f),2.7f,7);
            Point("Crew warm fill",new Vector3(5,2,0),new Color(1,.75f,.46f),2.5f,7);
            Point("Engineering warm fill",new Vector3(0,2,-10),new Color(1,.48f,.18f),3,8);
            var camera=new GameObject("Expedition cutter review camera").AddComponent<Camera>();camera.orthographic=true;camera.transform.rotation=Quaternion.Euler(50,45,0);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.024f,.035f);camera.nearClipPlane=.1f;camera.farClipPlane=150;
            Focus(camera,new Vector3(0,.6f,1));camera.orthographicSize=16.5f;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,Root+"NexusCutterArchitecture.unity");
            Capture(camera,"Art/NexusShipReview/cutter-architecture.png",16.5f);
            Focus(camera,new Vector3(0,1,10));Capture(camera,"Art/NexusShipReview/cutter-command.png",9.5f);
            camera.transform.rotation=Quaternion.Euler(90,0,0);Focus(camera,new Vector3(0,0,1));Capture(camera,"Art/NexusShipReview/cutter-plan.png",17);
            string areas="";foreach(var pair in DeckAreas)areas+=pair.Key+": "+pair.Value.ToString("F2")+" square metres\n";
            File.WriteAllText("Art/NexusShipReview/cutter-report.txt","Expedition cutter: 16m maximum beam / 30m length. Command deck Z5..16; uninterrupted axial circulation X+-1.6.\nDownloaded deck meshes cropped to a convex hull and compartment boundaries with UVs preserved. "+pieces+" clipped deck sections.\nMeasured fitted triangle area "+deckArea.ToString("F2")+" / hull area "+expectedArea.ToString("F2")+" square metres; coverage tolerance .02.\n"+areas+"Static architecture review only; no gameplay or furniture. No player/build/collision validation.\n");
            Debug.Log("NEXUS CUTTER ARCHITECTURE COMPLETE: "+pieces+" deck sections");
        }
        static void ArchitectureDoor(string name,Vector3 center,float span,float yaw,bool cutaway)
        {
            float half=2.42672491f*.6f;float length=span/2-half;var dir=Quaternion.Euler(0,yaw,0)*Vector3.right;
            foreach(int side in new[]{-1,1})Bulkhead(name+" fitted partition "+side,center+dir*side*(half+length/2),length,cutaway?1:3,yaw+90);
            var door=Place("Door_Frame_Square",center,yaw,.6f);door.name=name+" doorway";CargoFinish(door);
            Place("Prop_Vent_Wide",center+Vector3.up*.015f,yaw,1.5f);
        }
        static void SideDoor(string name,float x)
        {
            float half=2.42672491f*.6f;
            Bulkhead(name+" aft cutaway",new Vector3(x,0,(-6-half)/2),6-half,1,0);
            Bulkhead(name+" forward partition",new Vector3(x,0,(5+half)/2),5-half,1,0);
            var frame=Place("Door_Frame_Square",new Vector3(x,0,0),90,.6f);CargoFinish(frame);frame.name=name+" threshold";
            Place("Prop_Vent_Wide",new Vector3(x,.015f,0),90,1.5f);
        }
        static int CroppedDeck(string zone,string sourceName,int x,int z,float minX,float maxX,float minZ,float maxZ)
        {
            var donor=Place(sourceName,Vector3.zero);var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            var rectangle=new[]{new Vector2(minX,minZ),new Vector2(maxX,minZ),new Vector2(maxX,maxZ),new Vector2(minX,maxZ)};
            var materials=new List<Material>();
            foreach(var filter in donor.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;var positions=mesh.vertices;var tex=mesh.uv;var matrix=filter.transform.localToWorldMatrix;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    var indices=mesh.GetTriangles(sub);
                    for(int t=0;t<indices.Length;t+=3)
                    {
                        var poly=new List<DeckVertex>();for(int v=0;v<3;v++){int index=indices[t+v];poly.Add(new DeckVertex(matrix.MultiplyPoint3x4(positions[index])+new Vector3(x,0,z),tex[index]));}
                        poly=ClipDeck(poly,CutterHull);poly=ClipDeck(poly,rectangle);
                        if(poly.Count<3)continue;
                        int start=vertices.Count;foreach(var point in poly){vertices.Add(point.p);uv.Add(point.uv);}for(int v=1;v<poly.Count-1;v++)
                        {
                            float area=Vector3.Cross(poly[v].p-poly[0].p,poly[v+1].p-poly[0].p).magnitude*.5f;
                            if(area<.000001f)continue;
                            if(!DeckAreas.ContainsKey(zone))DeckAreas[zone]=0;DeckAreas[zone]+=area;
                            triangles.Add(start);triangles.Add(start+v);triangles.Add(start+v+1);
                        }
                    }
                }
                materials.AddRange(filter.GetComponent<Renderer>().sharedMaterials);
            }
            UnityEngine.Object.DestroyImmediate(donor);if(triangles.Count==0)return 0;
            string path=Root+"Architecture/"+zone+"_"+x+"_"+z+".asset";var result=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!result){result=new Mesh();AssetDatabase.CreateAsset(result,path);}else result.Clear();
            result.SetVertices(vertices);result.SetUVs(0,uv);result.SetTriangles(triangles,0);result.RecalculateNormals();result.RecalculateTangents();result.RecalculateBounds();EditorUtility.SetDirty(result);
            var go=new GameObject(zone+" fitted deck "+x+" / "+z);go.AddComponent<MeshFilter>().sharedMesh=result;go.AddComponent<MeshRenderer>().sharedMaterial=materials[0];
            return 1;
        }
        static List<DeckVertex> ClipDeck(List<DeckVertex> input,Vector2[] boundary)
        {
            for(int edge=0;edge<boundary.Length&&input.Count>0;edge++)
            {
                var output=new List<DeckVertex>();var a=boundary[edge];var d=boundary[(edge+1)%boundary.Length]-a;
                for(int i=0;i<input.Count;i++)
                {
                    var p=input[i];var q=input[(i+1)%input.Count];float pd=d.x*(p.p.z-a.y)-d.y*(p.p.x-a.x),qd=d.x*(q.p.z-a.y)-d.y*(q.p.x-a.x);
                    if(pd>=-0.00001f)output.Add(p);
                    if((pd>=0)!=(qd>=0)){float t=pd/(pd-qd);output.Add(new DeckVertex(Vector3.Lerp(p.p,q.p,t),Vector2.Lerp(p.uv,q.uv,t)));}
                }
                input=output;
            }
            return input;
        }
    }
}
