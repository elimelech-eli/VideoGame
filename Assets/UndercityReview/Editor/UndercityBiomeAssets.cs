using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VideoGame.Editor
{
    public static class UndercityBiomeAssets
    {
        const string Root="Assets/Game/Resources/Undercity/";
        const string SourceRoot="Assets/UndercityReview/";
        static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>();
        struct Vertex { public Vector3 p,n; public Vector2 uv; public Vertex(Vector3 p,Vector3 n,Vector2 uv){this.p=p;this.n=n;this.uv=uv;} public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex(Vector3.Lerp(a.p,b.p,t),Vector3.Lerp(a.n,b.n,t).normalized,Vector2.Lerp(a.uv,b.uv,t)); }
        public static void BuildAndReview(){Build();UndercityRuntimeReview.Build();}
        [MenuItem("Nexus/Rebuild Undercity environment kit")]
        public static void Build()
        {
            foreach(string f in new[]{"Prefabs","Meshes"})Directory.CreateDirectory(Root+f);
            AssetDatabase.Refresh();Materials.Clear();
            foreach(string file in Directory.GetFiles(SourceRoot+"Materials","*.mat")){var m=AssetDatabase.LoadAssetAtPath<Material>(file);Materials[Path.GetFileNameWithoutExtension(file)]=m;}
            foreach(int x in new[]{1,2,4})foreach(int z in new[]{1,2,4})
            {
                string name="Floor"+x+"x"+z;var o=Source("Sewer_floor_ceiling 02");
                // Contact is the source floor's actual upper surface, not its bottom-centred export pivot.
                var b=Bounds(o);foreach(Transform child in o.transform)child.position-=Vector3.up*b.max.y;
                Crop(o,name,new Vector3(x,1,z));
                if(x<4)foreach(int side in new[]{-1,1})Cap(o,new Vector3(side*x/2f,-.105f,0),new Vector3(.012f,.19f,z));
                if(z<4)foreach(int side in new[]{-1,1})Cap(o,new Vector3(0,-.105f,side*z/2f),new Vector3(x,.19f,.012f));
                Save(o,name,8,new Bounds(new Vector3(0,-.1f,0),new Vector3(x,.2f,z)));
            }
            foreach(int width in new[]{1,2,4})foreach(bool low in new[]{false,true})
            {
                string name="Wall"+(low?"Low":"")+width;var o=Source(low?"Sewer_CappedLowWall":"Sewer_wall 03");
                Crop(o,name,new Vector3(width,low?1.1f:4,.36f));var b=Bounds(o);
                foreach(int side in new[]{-1,1})Cap(o,new Vector3(side*width/2f,b.center.y,0),new Vector3(.012f,b.size.y,.34f));
                Save(o,name,9,new Bounds(new Vector3(0,b.size.y/2,0),new Vector3(width,b.size.y,.35f)));
            }
            var props=new Dictionary<string,string>{{"DrainOutlet","Sewer_huge pipe"},{"Rubble","Sewer_brick debris"},{"Barrier","Sewer_support 06"},{"PressurePipe","Sewer_big pipe 01"},{"DrainGrille","Sewer_bars door"}};
            foreach(var pair in props){var o=Source(pair.Value);Save(o,pair.Key,9,Bounds(o));}
            var manifold=new GameObject("Pressure manifold");
            foreach(float x in new[]{-.4f,0,.4f})
            {
                Part(manifold,"Pipes_pipe_200cm_metal",new Vector3(x,0,0));
                Part(manifold,"Pipes_pipe_valve_large_metal",new Vector3(x,2,0));
                Part(manifold,"Pipes_pipe_100cm_metal",new Vector3(x,2.5f,0));
                foreach(float y in new[]{.6f,3})Part(manifold,"Pipes_pipe_support_metal",new Vector3(x,y,.13f));
            }
            Save(manifold,"Manifold",9,Bounds(manifold));
            AssetDatabase.SaveAssets();Debug.Log("UNDERCITY_KIT_READY: 21 source-derived prefabs; native metre sizes, cropped source UVs, capped cutaways; no live routing edits.");
        }
        static GameObject Source(string key)
        {
            var src=AssetDatabase.LoadAssetAtPath<GameObject>(SourceRoot+"Models/"+key+".fbx");if(!src)throw new Exception("Missing approved source "+key);
            var root=new GameObject(key);var imported=UnityEngine.Object.Instantiate(src,root.transform);
            foreach(var r in imported.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>Materials.TryGetValue(m.name.Replace(' ','_'),out var mapped)?mapped:throw new Exception("Missing material "+m.name)).ToArray();
            var b=Bounds(root);imported.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);return root;
        }
        static void Part(GameObject parent,string source,Vector3 p)
        {
            var part=Source(source);part.transform.SetParent(parent.transform,false);part.transform.localPosition=p;
        }
        static Bounds Bounds(GameObject root){var rs=root.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new Exception("No renderers "+root.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        static void Cap(GameObject parent,Vector3 p,Vector3 size)
        {
            // Tiny construction closure uses the source's concrete family; it is not replacement architecture.
            var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);cap.name="Finished source-concrete edge";UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());cap.transform.SetParent(parent.transform,false);cap.transform.localPosition=p;cap.transform.localScale=size;cap.GetComponent<Renderer>().sharedMaterial=Materials["Sewer_Weathered_Concrete"];
        }
        static void Save(GameObject o,string name,int layer,Bounds b)
        {
            o.name=name;foreach(var t in o.GetComponentsInChildren<Transform>())t.gameObject.layer=layer;
            var c=o.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;
            PrefabUtility.SaveAsPrefabAsset(o,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(o);
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
