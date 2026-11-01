using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VideoGame.Editor
{
    public static class FactoryKitAssets
    {
        const string Root="Assets/Game/Resources/Factory/";
        static readonly Dictionary<string,Transform> Sources=new Dictionary<string,Transform>();
        static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>();
        struct Vertex {public Vector3 p,n;public Vector2 uv;public Vertex(Vector3 p,Vector3 n,Vector2 uv){this.p=p;this.n=n;this.uv=uv;}public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex(Vector3.Lerp(a.p,b.p,t),Vector3.Lerp(a.n,b.n,t).normalized,Vector2.Lerp(a.uv,b.uv,t));}
        public static void BuildAll(){Build();FactoryEnemyAssets.Build();}
        [MenuItem("Nexus/Rebuild Factory environment kit")]
        public static void Build()
        {
            foreach(string folder in new[]{"Prefabs","Meshes"})Directory.CreateDirectory(Root+folder);AssetDatabase.Refresh();Sources.Clear();Materials.Clear();
            foreach(string file in Directory.GetFiles("Assets/FactoryReview/Materials","*.mat")){var m=AssetDatabase.LoadAssetAtPath<Material>(file);Materials[m.name]=m;}
            var templates=new List<GameObject>();
            foreach(string path in Directory.GetFiles("Assets/FactoryReview/Models","*.fbx"))
            {
                var o=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));templates.Add(o);
                foreach(var r in o.GetComponentsInChildren<MeshRenderer>())Sources[r.name]=r.transform;o.SetActive(false);
            }
            foreach(int x in new[]{1,2})foreach(int z in new[]{1,2})
            {
                var o=Source("Factory_modularKit_24");Crop(o,"Floor"+x+"x"+z,new Vector3(x,1,z));
                var c=o.AddComponent<BoxCollider>();c.center=Vector3.down*.12f;c.size=new Vector3(x,.24f,z);Save(o,"Floor"+x+"x"+z,8);
            }
            foreach(int width in new[]{1,2})foreach(bool low in new[]{false,true})
            {
                string name="Wall"+(low?"Low":"")+width;var o=Source("Factory_modularKit_01",90);Crop(o,name,new Vector3(width,low?.65f:2.5f,1));
                var b=Bounds(o);var c=o.AddComponent<BoxCollider>();c.center=b.center;c.size=new Vector3(width,low?.65f:2.5f,.20f);
                // Finish cut tops and exposed ends with the same concrete construction family.
                Cap(o,new Vector3(0,b.max.y-.015f,0),new Vector3(width,.03f,.20f));
                foreach(int side in new[]{-1,1})Cap(o,new Vector3(side*(width/2f-.01f),b.center.y,0),new Vector3(.02f,b.size.y,.20f));
                Save(o,name,9);
            }
            var props=new Dictionary<string,string>{{"AssemblyMachine","Generator_02"},{"PowerUnit","Generator_01"},{"Workbench","Plane.093"},{"Worktable","Plane.095"},{"Shelf","Shelves_01"},{"LowShelf","Shelves_02"},{"Crate","Crate_01"},{"CoveredCrate","Crate_03"},{"PalletTruck","Forklift_01"},{"Pallet","Pallet_01"},{"Barrel","Cylinder.015"},{"Cone","Cone_01"},{"WorkLight","Light_01"},{"WarningSign","Sign_02"},{"Pipe","Pipes_02"},{"PipeCap","Pipes_06"},{"Toolbox","Cube.092"}};
            foreach(var pair in props){var o=Source(pair.Value);var b=Bounds(o);var c=o.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;Save(o,pair.Key,9);}
            foreach(var o in templates)UnityEngine.Object.DestroyImmediate(o);AssetDatabase.SaveAssets();
            Debug.Log("FACTORY_KIT_READY: 25 measured source modules; clipped native UVs, finished wall caps; no per-piece stretch.");
        }
        static GameObject Source(string key,float yaw=0)
        {
            var t=Sources[key];var root=new GameObject(key);var child=UnityEngine.Object.Instantiate(t.gameObject,root.transform);child.SetActive(true);child.transform.rotation=t.rotation;child.transform.localScale=t.lossyScale;
            foreach(var r in child.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>Materials[Clean(m.name)]).ToArray();
            var b=Bounds(root);child.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
            root.transform.rotation=Quaternion.Euler(0,yaw,0);return root;
        }
        static string Clean(string n)=>n.Replace(" + ","_plus_").Replace(" ","_");
        static Bounds Bounds(GameObject root){var rs=root.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        static void Cap(GameObject parent,Vector3 p,Vector3 size){var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);cap.name="Finished concrete cap";UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());cap.transform.SetParent(parent.transform,false);cap.transform.localPosition=p;cap.transform.localScale=size;cap.GetComponent<Renderer>().sharedMaterial=Materials["concrete"];}
        static void Save(GameObject o,string name,int layer)
        {
            // Source rotation becomes part of the child so every prefab has an identity placement root.
            var root=new GameObject(name);o.transform.SetParent(root.transform,true);root.layer=layer;
            foreach(var t in o.GetComponentsInChildren<Transform>())t.gameObject.layer=layer;
            // Runtime dressing measures the root's box; child geometry colliders are not duplicated.
            var c=o.GetComponent<BoxCollider>();if(c){var box=root.AddComponent<BoxCollider>();box.center=o.transform.TransformPoint(c.center);box.size=new Vector3(Mathf.Abs(o.transform.right.x)*c.size.x+Mathf.Abs(o.transform.forward.x)*c.size.z,c.size.y,Mathf.Abs(o.transform.right.z)*c.size.x+Mathf.Abs(o.transform.forward.z)*c.size.z);UnityEngine.Object.DestroyImmediate(c);}
            PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
        }
        static List<Vertex> Clip(List<Vertex> input,int axis,float limit,bool greater)
        {
            var output=new List<Vertex>();for(int i=0;i<input.Count;i++){var a=input[i];var b=input[(i+1)%input.Count];float da=(greater?a.p[axis]-limit:limit-a.p[axis]),db=(greater?b.p[axis]-limit:limit-b.p[axis]);if(da>=-.000001f)output.Add(a);if((da>=0)!=(db>=0))output.Add(Vertex.Lerp(a,b,da/(da-db)));}return output;
        }
        static void Crop(GameObject o,string name,Vector3 size)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<List<int>>();var mats=new List<Material>();
            foreach(var f in o.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=f.sharedMesh;var p=mesh.vertices;var n=mesh.normals;var tex=mesh.uv;var matrix=f.transform.localToWorldMatrix;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    var indices=mesh.GetTriangles(sub);var dst=new List<int>();triangles.Add(dst);mats.Add(f.GetComponent<MeshRenderer>().sharedMaterials[sub]);
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var poly=new List<Vertex>();for(int k=0;k<3;k++){int j=indices[i+k];poly.Add(new Vertex(matrix.MultiplyPoint3x4(p[j]),matrix.MultiplyVector(n[j]).normalized,tex[j]));}
                        poly=Clip(poly,0,-size.x/2,true);poly=Clip(poly,0,size.x/2,false);poly=Clip(poly,2,-size.z/2,true);poly=Clip(poly,2,size.z/2,false);poly=Clip(poly,1,size.y,false);
                        if(poly.Count<3)continue;int first=vertices.Count;foreach(var v in poly){vertices.Add(v.p);normals.Add(v.n);uv.Add(v.uv);}for(int k=1;k<poly.Count-1;k++){dst.Add(first);dst.Add(first+k);dst.Add(first+k+1);}
                    }
                }
            }
            foreach(var child in o.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);o.transform.rotation=Quaternion.identity;
            string path=Root+"Meshes/"+name+".asset";var result=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!result){result=new Mesh();AssetDatabase.CreateAsset(result,path);}else result.Clear();
            result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,uv);result.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)result.SetTriangles(triangles[i],i);result.RecalculateBounds();result.RecalculateTangents();EditorUtility.SetDirty(result);
            o.AddComponent<MeshFilter>().sharedMesh=result;o.AddComponent<MeshRenderer>().sharedMaterials=mats.ToArray();
        }
    }
}
