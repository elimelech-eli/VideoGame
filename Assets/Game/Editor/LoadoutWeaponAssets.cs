using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VideoGame.Editor
{
    public static class LoadoutWeaponAssets
    {
        const string Root="Assets/Game/Resources/LoadoutWeapons/";
        [Serializable] class Source { public string name;public Part[] parts; }
        [Serializable] class Part { public string name,role;public float[] vertices,normals;public int[] triangles; }
        public static void Build()
        {
            if(File.Exists(Root+"Shield.prefab"))return;
            Rebuild();
        }
        [MenuItem("Nexus/Rebuild loadout weapons")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            foreach(var name in new[]{"Handgun","Rifle","Sword","Shield"})
            {
                var source=JsonUtility.FromJson<Source>(File.ReadAllText("Art/Weapons/"+name+".json"));
                var root=new GameObject(name);
                string meshPath=Root+name+".asset";
                if(File.Exists(meshPath))AssetDatabase.DeleteAsset(meshPath);
                bool first=true;
                foreach(var p in source.parts)
                {
                    var mesh=new Mesh{name=p.name};var vertices=new Vector3[p.vertices.Length/3];var normals=new Vector3[vertices.Length];
                    for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(p.vertices[i*3],p.vertices[i*3+1],p.vertices[i*3+2]);normals[i]=new Vector3(p.normals[i*3],p.normals[i*3+1],p.normals[i*3+2]);}
                    mesh.vertices=vertices;mesh.triangles=p.triangles;mesh.normals=normals;mesh.RecalculateBounds();
                    if(first){AssetDatabase.CreateAsset(mesh,meshPath);first=false;}else AssetDatabase.AddObjectToAsset(mesh,meshPath);
                    var part=new GameObject(p.name);part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;
                    part.AddComponent<MeshRenderer>().sharedMaterial=Material(p.role);
                }
                Marker(root,"Grip",Vector3.zero);
                if(name=="Rifle"||name=="Handgun")Marker(root,"Muzzle",name=="Rifle"?new Vector3(0,.15f,.66f):new Vector3(0,.14f,.272f));
                PrefabUtility.SaveAsPrefabAsset(root,Root+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }
        static void Marker(GameObject root,string name,Vector3 position){var marker=new GameObject(name);marker.transform.SetParent(root.transform,false);marker.transform.localPosition=position;}
        static Material Material(string role)
        {
            string path=Root+role+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(Shader.Find("Standard")){name=role};
            material.color=role=="Energy"?new Color(.02f,.7f,.9f):role=="Ivory"?new Color(.7f,.75f,.76f):role=="Grip"?new Color(.025f,.035f,.045f):new Color(.27f,.34f,.4f);
            material.SetFloat("_Metallic",role=="Grip"?.15f:.65f);material.SetFloat("_Glossiness",role=="Grip"?.3f:.65f);
            if(role=="Energy"){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",material.color*1.6f);}
            AssetDatabase.CreateAsset(material,path);return material;
        }
    }
}
