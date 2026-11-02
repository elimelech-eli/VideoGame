using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VideoGame.Editor
{
    public static class NeonBiomeAssets
    {
        const string Root="Assets/Game/Resources/Neon/",SourcesRoot="Assets/NeonSource/";
        static readonly Dictionary<string,Transform> Sources=new Dictionary<string,Transform>();
        static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>();
        static Material CapMaterial;
        [Serializable]class Catalog {public Entry[] materials;}
        [Serializable]class Entry {public string name,albedo,baseColor,normal,packed,emission;public float[] baseColorFactor;public float metallicFactor,roughnessFactor;}
        [MenuItem("Nexus/Rebuild approved Neon source kit")]
        public static void Build()
        {
            // Minimum game-used derivatives only. Publisher originals stay outside the repository.
            if(!File.Exists(SourcesRoot+"Chuck/Models/ChuckBuildings.fbx"))throw new InvalidOperationException("Approved Chuck source derivatives missing; resolve source/license intake before building mixed Neon content.");
            foreach(string folder in new[]{"Prefabs","Meshes","Materials"})Directory.CreateDirectory(Root+folder);
            AssetDatabase.Refresh();Sources.Clear();Materials.Clear();
            LoadMaterials("Slums","S_");LoadMaterials("Chuck","C_");
            Material asphalt=Surface("Asphalt"),brick=Surface("Brick"),metal=Surface("Metal");
            var templates=new List<GameObject>();
            foreach(var family in new[]{"Slums","Chuck"})
            {
                string path=SourcesRoot+family+"/Models/"+(family=="Slums"?"ModularSlums.fbx":"ChuckBuildings.fbx");
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importCameras=false;importer.importLights=false;importer.animationType=ModelImporterAnimationType.None;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                var o=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));templates.Add(o);
                foreach(var r in o.GetComponentsInChildren<MeshRenderer>())Sources[(family=="Slums"?"S_":"C_")+r.name]=r.transform;o.SetActive(false);
            }
            foreach(int x in new[]{1,2})foreach(int z in new[]{1,2})Floor(x,z,asphalt);
            foreach(bool corporate in new[]{false,true})foreach(int width in new[]{1,2})foreach(bool low in new[]{false,true})
            {
                string name=(corporate?"Corporate":"Slum")+"Wall"+(low?"Low":"")+width;
                float height=low?.65f:2.5f;var o=new GameObject(name);
                if(corporate)
                {
                    // Approved source block's metal profile is usable as a capped facade strip;
                    // its complete miniature-city presentation is never used as room furniture.
                    for(int stack=0;stack<3;stack++){var piece=Source("C_block 2");piece.transform.SetParent(o.transform);var b=Bounds(piece);piece.transform.position+=new Vector3(0,stack*b.size.y,b.size.z/2-.125f);}
                    CapMaterial=metal;
                }
                else{var piece=Source("S_D2",1.25f);piece.transform.SetParent(o.transform);piece.transform.position+=Vector3.forward*(Bounds(piece).size.z/2-.125f);CapMaterial=brick;}
                Crop(o,name,new Vector3(width,height,.3f));
                if(o.GetComponent<MeshFilter>().sharedMesh.vertexCount==0)throw new InvalidOperationException("Source façade crop is empty "+name);
                Cap(o,new Vector3(0,height-.015f,0),new Vector3(width,.03f,.30f));
                Cap(o,new Vector3(0,height/2,.135f),new Vector3(width,height,.03f));
                foreach(int sign in new[]{-1,1})Cap(o,new Vector3(sign*(width/2f-.01f),height/2,0),new Vector3(.02f,height,.3f));
                var box=o.AddComponent<BoxCollider>();box.center=Vector3.up*height/2;box.size=new Vector3(width,height,.3f);Save(o,name,9);
            }
            foreach(var key in new[]{"D2","E3"})Solid(Source("S_"+key,1.25f),"SlumBlock"+key);
            foreach(int i in new[]{1,2})AssetDatabase.DeleteAsset(Root+"Prefabs/CorporateBlock"+i+".prefab");
            var cabinet=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Transit/Prefabs/PowerCabinet"));
            foreach(var c in cabinet.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            var cabinetBounds=Bounds(cabinet);cabinet.transform.position-=new Vector3(cabinetBounds.center.x,cabinetBounds.min.y,cabinetBounds.center.z);Solid(cabinet,"PowerCabinet");
            foreach(int i in new[]{1,2,3,4})Solid(Source("C_building "+i),"CorporateBuilding"+i);
            foreach(bool warm in new[]{true,false})
            {
                var o=Source("C_lamp");var lamp=o.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=warm?new Color(1,.42f,.15f):new Color(.12f,.65f,1);lamp.range=7;lamp.intensity=1.8f;Save(o,warm?"LampWarm":"LampCool",0);
            }
            Graphic("SignNoodle","noodle",2.5f,.78f);Graphic("SignSignal","signal",2.5f,.78f);Graphic("SignArcade","arcade",2.5f,.78f);Graphic("GraffitiCity","graffiti_city",2.5f,1.25f);
            foreach(var o in templates)UnityEngine.Object.DestroyImmediate(o);AssetDatabase.SaveAssets();
            Debug.Log("NEON_KIT_READY:4floor,8cappedfaçade,2slumblock,1powercabinet,4intactbuilding,2lamp,4originalgraphic prefabs");
        }
        static void LoadMaterials(string folder,string prefix)
        {
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(SourcesRoot+folder+"/catalog.json"));
            foreach(var e in catalog.materials)
            {
                string path=Root+"Materials/"+prefix+e.name.Replace(" ","_")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.shaderKeywords=new string[0];
                Texture2D Tex(string name,bool normal=false)
                {
                    if(string.IsNullOrEmpty(name))return null;string file=SourcesRoot+folder+"/Textures/"+name;var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                    importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.maxTextureSize=2048;
                    importer.sRGBTexture=!normal&&name!=e.packed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(file);
                }
                m.mainTexture=Tex(folder=="Slums"?e.albedo:e.baseColor);m.color=Color.white;m.SetFloat("_Glossiness",folder=="Slums"?.1f:1-e.roughnessFactor);
                if(Tex(e.normal,true)){m.SetTexture("_BumpMap",Tex(e.normal,true));m.EnableKeyword("_NORMALMAP");}
                if(Tex(e.packed)){m.SetTexture("_MetallicGlossMap",Tex(e.packed));m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_GlossMapScale",1);}
                if(Tex(e.emission)){m.SetTexture("_EmissionMap",Tex(e.emission));m.SetColor("_EmissionColor",Color.white);m.EnableKeyword("_EMISSION");}
                m.enableInstancing=true;EditorUtility.SetDirty(m);Materials[prefix+e.name]=m;
            }
        }
        static Material Surface(string family)
        {
            string folder=SourcesRoot+"Surfaces/"+family+"/",path=Root+"Materials/Surface_"+family+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            foreach(string file in new[]{"color.jpg","normal.jpg","MetalSmooth.png"})
            {var importer=(TextureImporter)AssetImporter.GetAtPath(folder+file);importer.textureType=file=="normal.jpg"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=file=="color.jpg";importer.SaveAndReimport();}
            m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"color.jpg");m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"normal.jpg"));m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"MetalSmooth.png"));m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_GlossMapScale",1);return m;
        }
        static GameObject Source(string key,float scale=1)
        {
            var t=Sources[key];var root=new GameObject(key);var child=UnityEngine.Object.Instantiate(t.gameObject,root.transform);child.SetActive(true);child.transform.rotation=t.rotation;child.transform.localScale=t.lossyScale*scale;
            string prefix=key.Substring(0,2);foreach(var r in child.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>Materials[prefix+m.name]).ToArray();
            var b=Bounds(root);child.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);return root;
        }
        static void Solid(GameObject o,string name){var b=Bounds(o);var c=o.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;Save(o,name,9);}
        static void Save(GameObject o,string name,int layer)
        {o.name=name;foreach(var t in o.GetComponentsInChildren<Transform>())t.gameObject.layer=layer;PrefabUtility.SaveAsPrefabAsset(o,Root+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(o);}
        static Bounds Bounds(GameObject o){var rs=o.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        static void Cap(GameObject o,Vector3 p,Vector3 size){var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());cap.name="Finished source-family cap";cap.transform.SetParent(o.transform,false);cap.transform.localPosition=p;cap.transform.localScale=size;cap.GetComponent<Renderer>().sharedMaterial=CapMaterial;}
        static Mesh Mesh(string name){string path=Root+"Meshes/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!m){m=new Mesh();AssetDatabase.CreateAsset(m,path);}else m.Clear();return m;}
        static void Floor(int width,int depth,Material material)
        {
            string name="Floor"+width+"x"+depth;float x=width/2f,z=depth/2f;var m=Mesh(name);
            var vertices=new[]{new Vector3(-x,0,-z),new Vector3(x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(-x,-.24f,-z),new Vector3(x,-.24f,-z),new Vector3(-x,-.24f,z),new Vector3(x,-.24f,z)};
            m.vertices=vertices;m.uv=vertices.Select(v=>new Vector2(v.x/3,v.z/3)).ToArray();m.triangles=new[]{0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};m.RecalculateNormals();m.RecalculateTangents();
            var o=new GameObject(name);o.AddComponent<MeshFilter>().sharedMesh=m;o.AddComponent<MeshRenderer>().sharedMaterial=material;var c=o.AddComponent<BoxCollider>();c.center=Vector3.down*.12f;c.size=new Vector3(width,.24f,depth);Save(o,name,8);
        }
        static void Graphic(string name,string source,float w,float h)
        {
            var m=Mesh(name);m.vertices=new[]{new Vector3(-w/2,-h/2,0),new Vector3(w/2,-h/2,0),new Vector3(-w/2,h/2,0),new Vector3(w/2,h/2,0)};m.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};m.triangles=new[]{0,2,1,2,3,1};m.RecalculateNormals();
            string path=Root+"Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Unlit/Transparent"));AssetDatabase.CreateAsset(material,path);}material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(SourcesRoot+"StreetGraphics/"+source+".png");
            var o=new GameObject(name);o.AddComponent<MeshFilter>().sharedMesh=m;o.AddComponent<MeshRenderer>().sharedMaterial=material;Save(o,name,0);
        }
        // UV-preserving triangle clipping, shared convention with the accepted Factory kit.
        struct Vertex{public Vector3 p,n;public Vector2 uv;public Vertex(Vector3 p,Vector3 n,Vector2 uv){this.p=p;this.n=n;this.uv=uv;}public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex(Vector3.Lerp(a.p,b.p,t),Vector3.Lerp(a.n,b.n,t).normalized,Vector2.Lerp(a.uv,b.uv,t));}
        static List<Vertex> Clip(List<Vertex> input,int axis,float limit,bool greater)
        {var result=new List<Vertex>();for(int i=0;i<input.Count;i++){var a=input[i];var b=input[(i+1)%input.Count];float da=greater?a.p[axis]-limit:limit-a.p[axis],db=greater?b.p[axis]-limit:limit-b.p[axis];if(da>=-.000001f)result.Add(a);if((da>=0)!=(db>=0))result.Add(Vertex.Lerp(a,b,da/(da-db)));}return result;}
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
                        foreach(int axis in new[]{0,2}){poly=Clip(poly,axis,-size[axis]/2,true);poly=Clip(poly,axis,size[axis]/2,false);}poly=Clip(poly,1,0,true);poly=Clip(poly,1,size.y,false);
                        if(poly.Count<3)continue;int first=vertices.Count;foreach(var v in poly){vertices.Add(v.p);normals.Add(v.n);uv.Add(v.uv);}for(int k=1;k<poly.Count-1;k++){dst.Add(first);dst.Add(first+k);dst.Add(first+k+1);}
                    }
                }
            }
            foreach(var child in o.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);o.transform.rotation=Quaternion.identity;
            var result=Mesh(name);result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,uv);result.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)result.SetTriangles(triangles[i],i);result.RecalculateBounds();result.RecalculateTangents();
            o.AddComponent<MeshFilter>().sharedMesh=result;o.AddComponent<MeshRenderer>().sharedMaterials=mats.ToArray();
        }
    }
}
