using System;
using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;
namespace VideoGame
{
    // Factory visual adapter; shared topology, mission rules and combat remain authoritative.
    public static class FactoryBiome
    {
        static readonly Dictionary<string,GameObject> Models=new Dictionary<string,GameObject>();
        static GameObject Model(string key){if(!Models.TryGetValue(key,out var model)||!model){model=Resources.Load<GameObject>("Factory/Prefabs/"+key);if(!model)throw new InvalidOperationException("Missing Factory module "+key);Models[key]=model;}return model;}
        public static GameObject Place(string key,Transform parent,Vector3 p,float yaw=0){var o=UnityEngine.Object.Instantiate(Model(key),parent);o.name="Factory / "+key;o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));return o;}
        public static void Deck(Vector3 center,float width,float depth,Transform parent)
        {
            for(float z=0;z<depth-.01f;){int dz=depth-z>=1.99f?2:1;for(float x=0;x<width-.01f;){int dx=width-x>=1.99f?2:1;Place("Floor"+dx+"x"+dz,parent,center+new Vector3(-width/2+x+dx/2f,.005f,-depth/2+z+dz/2f));x+=dx;}z+=dz;}
        }
        public static void Wall(Vector3 center,Vector3 tangent,float length,Vector2Int facing,Transform parent)
        {
            bool low=facing.x<0||facing.y<0;float yaw=facing.x!=0?(facing.x>0?90:-90):(facing.y>0?0:180);
            for(float cursor=0;cursor<length-.01f;){int width=length-cursor>=1.99f?2:1;Place("Wall"+(low?"Low":"")+width,parent,center+tangent*(-length/2+cursor+width/2f)+new Vector3(facing.x,0,facing.y)*.1f,yaw);cursor+=width;}
        }
        static int StableKey(string key){int hash=17;foreach(char c in key)unchecked{hash=hash*31+c;}return hash;}
        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        public static int Dress(Room room,int index,int seed,int turn,Transform parent)
        {
            int role=(index+(seed&3))%4;string[] names={"Assembly","Stores","Maintenance","Loading"};
            var root=new GameObject("Factory room / "+names[role]+" / "+index);root.transform.SetParent(parent,false);var center=new Vector3(room.X*20,0,room.Y*20);root.transform.position=center;
            int quarter=index==0?0:turn;var rotation=Quaternion.Euler(0,quarter*90,0);float half=room.Size/2f,corner=half-1.5f;int hash=17+role;
            bool Furniture(string key,Vector3 local,float yaw=0)
            {
                var prefab=Model(key);var c=prefab.GetComponent<BoxCollider>();var facing=rotation*Quaternion.Euler(0,yaw,0);Vector3 p=center+rotation*local;var boundsCenter=p+facing*c.center;
                var ext=Abs(facing*Vector3.right)*c.size.x/2+Abs(facing*Vector3.up)*c.size.y/2+Abs(facing*Vector3.forward)*c.size.z/2;var relative=boundsCenter-center;
                // Protect the central 4m cross and all doorway approaches, plus arrival/extraction corner.
                if(Mathf.Abs(relative.x)-ext.x<2.1f||Mathf.Abs(relative.z)-ext.z<2.1f)return false;
                if(Mathf.Abs(relative.x)+ext.x>half-.3f||Mathf.Abs(relative.z)+ext.z>half-.3f)return false;
                if(index==0&&relative.x<0&&relative.z<0)return false;
                if(Physics.CheckBox(boundsCenter,c.size*.49f,facing,1<<9))return false;
                Place(key,root.transform,p,quarter*90+yaw);Physics.SyncTransforms();unchecked{hash=hash*31+StableKey(key)+Mathf.RoundToInt(p.x*100)+Mathf.RoundToInt(p.z*100)*13;}return true;
            }
            if(role==0)
            {
                if(room.Size>=16)Furniture("AssemblyMachine",new Vector3(half-1.4f,0,half-2.6f),90);
                else Furniture("PowerUnit",new Vector3(corner,0,corner));
                Furniture("Workbench",new Vector3(-corner,0,half-1.5f),90);Furniture("Worktable",new Vector3(half-1.5f,0,-corner));
            }
            else if(role==1)
            {
                Furniture("Shelf",new Vector3(corner,0,half-.9f));Furniture("LowShelf",new Vector3(-corner,0,corner),90);
                Furniture("Crate",new Vector3(corner,0,-corner));Furniture("CoveredCrate",new Vector3(corner-1.3f,0,-corner));
            }
            else if(role==2)
            {
                Furniture("PowerUnit",new Vector3(corner,0,corner));Furniture("Workbench",new Vector3(-corner,0,half-1.5f),90);
                Furniture("Barrel",new Vector3(corner,0,-corner));Furniture("Barrel",new Vector3(corner-1,0,-corner));
            }
            else
            {
                Furniture("PalletTruck",new Vector3(corner,0,corner));Furniture("Pallet",new Vector3(-corner,0,corner));Furniture("Crate",new Vector3(corner,0,-corner));
                Furniture("CoveredCrate",new Vector3(-corner,0,corner-1.5f));
            }
            // Door-free corners carry local work lights; shared distance culling owns activation.
            var light=Place("WorkLight",root.transform,center+new Vector3(half-1.4f,2.1f,half-.35f));foreach(var c in light.GetComponentsInChildren<Collider>())UnityEngine.Object.Destroy(c);
            var lamp=light.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(.55f,.74f,1);lamp.range=7;lamp.intensity=1.8f;
            var beacon=new GameObject("Factory warning pulse");beacon.transform.SetParent(root.transform,false);beacon.transform.localPosition=new Vector3(half-1.5f,1.4f,half-1.5f);var warm=beacon.AddComponent<Light>();warm.type=LightType.Point;warm.color=new Color(1,.4f,.08f);warm.range=4;warm.intensity=1.2f;
            var motion=root.AddComponent<BiomeMotion>();motion.Phase=(seed+index)&7;beacon.AddComponent<FactoryWarningPulse>().Phase=motion.Phase;
            return hash;
        }
    }
    public sealed class FactoryWarningPulse:MonoBehaviour
    {
        public float Phase;Light lamp;void Awake()=>lamp=GetComponent<Light>();void Update(){if(lamp.enabled&&Time.timeScale>0)lamp.intensity=1.1f+.18f*Mathf.Sin(Time.time*1.7f+Phase);}
    }
}
