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
        public static void ExportStarship()
        {
            var scene=EditorSceneManager.OpenScene(Root+"NexusGalaxyConsoleReview.unity");
            var root=new GameObject("StarshipNexus");
            foreach(var go in scene.GetRootGameObjects())
            {
                if(go==root)continue;
                if(go.GetComponent<Camera>() || go.name=="Architecture review fill" || go.name.StartsWith("Operator scale reference"))
                {UnityEngine.Object.DestroyImmediate(go);continue;}
                go.transform.SetParent(root.transform,true);
            }
            foreach(var collider in root.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            foreach(string name in new[]{"Galaxy mission console — industrial pedestal","Milky Way — hovering projection","Galaxy projector spill"})
                UnityEngine.Object.DestroyImmediate(root.GetComponentsInChildren<Transform>().Single(t=>t.name==name).gameObject);
            int floors=0,solids=0;
            var frames=root.GetComponentsInChildren<Transform>().Where(t=>t.name.EndsWith(" doorway")||t.name.EndsWith(" threshold")).ToArray();
            foreach(var frame in frames)
            {
                var b=BoundsOf(frame.gameObject);bool alongX=b.size.x>b.size.z;
                float span=alongX?b.size.x:b.size.z;float side=(span-2.2f)/2;
                if(side<=0)throw new Exception("Door frame too narrow: "+frame.name);
                foreach(int sign in new[]{-1,1})
                {
                    var go=new GameObject(frame.name+" side collision");go.layer=9;go.transform.SetParent(root.transform);
                    go.transform.position=b.center+(alongX?Vector3.right:Vector3.forward)*sign*(1.1f+side/2);
                    var box=go.AddComponent<BoxCollider>();box.size=alongX?new Vector3(side,b.size.y,b.size.z):new Vector3(b.size.x,b.size.y,side);
                }
                var top=new GameObject(frame.name+" header collision");top.layer=9;top.transform.SetParent(root.transform);top.transform.position=new Vector3(b.center.x,b.max.y-.1f,b.center.z);
                var lintel=top.AddComponent<BoxCollider>();lintel.size=new Vector3(b.size.x,.2f,b.size.z);
            }
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh)continue;
                bool deck=AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(Root+"Architecture/");
                bool projection=filter.GetComponentsInParent<Transform>().Any(t=>t.name=="Milky Way — hovering projection");
                if(projection || filter.GetComponent<TextMesh>())continue;
                if(frames.Any(t=>filter.transform.IsChildOf(t)))continue;
                // Low deck grilles/cables are visual detail, not steps or snagging collision.
                if(!deck && filter.GetComponent<Renderer>() && filter.GetComponent<Renderer>().bounds.max.y<.3f)continue;
                filter.gameObject.layer=deck?8:9;
                var collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;
                if(deck)floors++;else solids++;
            }
            // The approved low cutaway does not visually show a complete hull; full-height
            // invisible perimeter blockers keep the player inside the authored vessel.
            for(int i=0;i<CutterHull.Length;i++)
            {
                var a=CutterHull[i];var b=CutterHull[(i+1)%CutterHull.Length];var d=b-a;var mid=(a+b)/2;
                var go=new GameObject("Hull collision "+i);go.layer=9;go.transform.SetParent(root.transform);
                go.transform.position=new Vector3(mid.x,1.5f,mid.y);go.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg,0);
                var box=go.AddComponent<BoxCollider>();box.size=new Vector3(.36f,3,d.magnitude+.12f);
            }
            Directory.CreateDirectory("Assets/Game/Resources/Nexus");
            var console=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Resources/Nexus/GalaxyConsole.prefab"),root.transform);
            console.name="GalaxyConsole(Clone)";console.transform.localPosition=new Vector3(0,0,9.1f);
            AddCargoServices(root);
            AddShipLife(root);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Resources/Nexus/StarshipNexus.prefab");AssetDatabase.SaveAssets();
            Debug.Log("STARSHIP EXPORTED: "+floors+" deck meshes, "+solids+" solid meshes, "+CutterHull.Length+" hull barriers");
        }
    }
}
