using System;
using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Utility/drainage visual adapter. Shared map topology and combat remain authoritative.
    public static class UndercityBiome
    {
        static readonly Dictionary<string,GameObject> Models=new Dictionary<string,GameObject>();
        static GameObject Model(string key)
        {
            if(!Models.TryGetValue(key,out var model)||!model)
            {
                model=Resources.Load<GameObject>("Undercity/Prefabs/"+key);
                if(!model)throw new InvalidOperationException("Missing Undercity module "+key);
                Models[key]=model;
            }
            return model;
        }
        public static GameObject Place(string key,Transform parent,Vector3 p,float yaw=0)
        {
            var o=UnityEngine.Object.Instantiate(Model(key),parent);o.name="Undercity / "+key;
            o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));return o;
        }
        public static void Deck(Vector3 center,float width,float depth,Transform parent)
        {
            // Native 4m grid with cropped 2m/1m closures for mixed-size room corridor gaps.
            if(Mathf.Abs(width-Mathf.Round(width))>.001f||Mathf.Abs(depth-Mathf.Round(depth))>.001f)
                throw new ArgumentException("Undercity deck needs whole-metre dimensions");
            for(float z=0;z<depth-.01f;)
            {
                int dz=depth-z>=3.99f?4:depth-z>=1.99f?2:1;
                for(float x=0;x<width-.01f;)
                {
                    int dx=width-x>=3.99f?4:width-x>=1.99f?2:1;
                    Place("Floor"+dx+"x"+dz,parent,center+new Vector3(-width/2+x+dx/2f,.005f,-depth/2+z+dz/2f));x+=dx;
                }
                z+=dz;
            }
        }
        public static void Wall(Vector3 center,Vector3 tangent,float length,Vector2Int facing,Transform parent)
        {
            bool low=facing.x<0||facing.y<0;
            float yaw=facing.x!=0?(facing.x>0?90:-90):(facing.y>0?0:180);
            for(float cursor=0;cursor<length-.01f;)
            {
                int width=length-cursor>=3.99f?4:length-cursor>=1.99f?2:1;
                Place("Wall"+(low?"Low":"")+width,parent,center+tangent*(-length/2+cursor+width/2f)+new Vector3(facing.x,0,facing.y)*.18f,yaw);
                cursor+=width;
            }
        }
        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        static int StableKey(string key){int h=17;foreach(char c in key)unchecked{h=h*31+c;}return h;}
        public static int Dress(Room room,int index,int seed,int turn,Transform parent)
        {
            int role=(index+(seed&3))%4;
            string[] names={"Drainage","Pressure","Service","Refuge"};
            var root=new GameObject("Undercity room / "+names[role]+" / "+index);
            root.transform.SetParent(parent,false);
            var center=new Vector3(room.X*MapLayout.Spacing,0,room.Y*MapLayout.Spacing);root.transform.position=center;
            int quarter=index==0?0:turn;var rotation=Quaternion.Euler(0,quarter*90,0);
            float half=room.Size/2f,corner=half-1.4f;int hash=31+role;
            bool Furniture(string key,Vector3 local,float yaw=0,bool mounted=false)
            {
                var prefab=Model(key);var c=prefab.GetComponent<BoxCollider>();
                var facing=rotation*Quaternion.Euler(0,yaw,0);var p=center+rotation*local;
                var boundsCenter=p+facing*c.center;
                var ext=Abs(facing*Vector3.right)*c.size.x/2+Abs(facing*Vector3.up)*c.size.y/2+Abs(facing*Vector3.forward)*c.size.z/2;
                var relative=boundsCenter-center;
                // Central cross, every door approach and arrival/extraction remain clear.
                if(Mathf.Abs(relative.x)-ext.x<2.1f||Mathf.Abs(relative.z)-ext.z<2.1f)return false;
                float boundary=mounted?half+.01f:half-.3f;
                if(Mathf.Abs(relative.x)+ext.x>boundary||Mathf.Abs(relative.z)+ext.z>boundary)return false;
                if(index==0&&relative.x<0&&relative.z<0)return false;
                if(Physics.CheckBox(boundsCenter,c.size*.49f,facing,1<<9))return false;
                Place(key,root.transform,p,quarter*90+yaw);Physics.SyncTransforms();
                unchecked{hash=hash*31+StableKey(key)+Mathf.RoundToInt(p.x*100)+Mathf.RoundToInt(p.z*100)*13;}
                return true;
            }
            if(role==0)
            {
                Furniture("DrainOutlet",new Vector3(corner,0,half-1.03f),180,true);
                Furniture("Rubble",new Vector3(-half+1.65f,0,half-1.65f),90);
                Furniture("PressurePipe",new Vector3(half-.195f,.35f,-corner),0,true);
            }
            else if(role==1)
            {
                Furniture("Manifold",new Vector3(corner,0,half-.24f),0,true);
                Furniture("Manifold",new Vector3(-corner,0,half-.24f),0,true);
                Furniture("PressurePipe",new Vector3(half-.195f,.7f,-corner),0,true);
            }
            else if(role==2)
            {
                Furniture("PressurePipe",new Vector3(half-.195f,1.3f,corner),0,true);
                Furniture("PressurePipe",new Vector3(-half+.195f,.5f,corner),0,true);
                Furniture("Barrier",new Vector3(corner,0,-corner),90);
                Furniture("Rubble",new Vector3(-half+1.65f,0,-half+1.65f));
            }
            else
            {
                // A screened utility refuge, not a claim of furnished civilian accommodation.
                Furniture("Barrier",new Vector3(corner,0,corner));
                Furniture("Barrier",new Vector3(-corner,0,corner),90);
                Furniture("Manifold",new Vector3(corner,0,-half+.24f),180,true);
            }
            var motion=root.AddComponent<BiomeMotion>();motion.Phase=(seed+index)&7;
            var beacon=new GameObject("Undercity pressure pulse");beacon.transform.SetParent(root.transform,false);
            beacon.transform.localPosition=new Vector3(half-.8f,2.6f,half-.8f);
            var amber=beacon.AddComponent<Light>();amber.type=LightType.Point;amber.color=new Color(1,.57f,.19f);amber.range=7;amber.intensity=1.6f;
            beacon.AddComponent<UndercityPressurePulse>().Phase=motion.Phase;
            var service=new GameObject("Undercity muted green service light");service.transform.SetParent(root.transform,false);
            service.transform.localPosition=new Vector3(-half+.8f,2.3f,half-.8f);
            var green=service.AddComponent<Light>();green.type=LightType.Point;green.color=new Color(.43f,.64f,.44f);green.range=6;green.intensity=1.1f;
            return hash;
        }
    }
    public sealed class UndercityPressurePulse:MonoBehaviour
    {
        public float Phase;Light lamp;
        void Awake()=>lamp=GetComponent<Light>();
        void Update(){if(lamp.enabled&&Time.timeScale>0)lamp.intensity=1.5f+.12f*Mathf.Sin(Time.time*1.35f+Phase);}
    }
}
