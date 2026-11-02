using System;
using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Native-source visual adapter only. Shared map topology/combat remains authoritative.
    public static class CorporateBiome
    {
        static readonly Dictionary<string,GameObject> Cache=new Dictionary<string,GameObject>();
        static readonly Dictionary<string,Vector4> FloorBasis=new Dictionary<string,Vector4>();
        static GameObject Model(string key)
        {
            if(!Cache.TryGetValue(key,out var model)||!model)
            {
                model=Resources.Load<GameObject>("Corporate/Prefabs/"+key);
                if(!model)throw new InvalidOperationException("Missing Corporate source module "+key);
                Cache[key]=model;
            }
            return model;
        }
        public static GameObject Place(string key,Transform parent,Vector3 position,float yaw=0)
        {
            var obj=UnityEngine.Object.Instantiate(Model(key),parent);obj.name="Corporate / "+key;
            obj.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            if(key.StartsWith("Floor"))AlignFloorTexture(obj,key,position);
            return obj;
        }
        static void AlignFloorTexture(GameObject obj,string key,Vector3 position)
        {
            if(!FloorBasis.TryGetValue(key,out var basis))
            {
                var mesh=obj.GetComponent<MeshFilter>().sharedMesh;var p=mesh.vertices;var uv=mesh.uv;var t=mesh.triangles;
                for(int i=0;i<t.Length;i+=3)
                {
                    int a=t[i],b=t[i+1],c=t[i+2];var e=p[b]-p[a];var f=p[c]-p[a];float d=e.x*f.z-e.z*f.x;
                    if(Mathf.Abs(d)<.000001f)continue;var u=uv[b]-uv[a];var v=uv[c]-uv[a];
                    var x=(u*f.z-v*e.z)/d;var z=(v*e.x-u*f.x)/d;basis=new Vector4(x.x,x.y,z.x,z.y);break;
                }
                FloorBasis[key]=basis;
            }
            // Preserve the measured source UV density while continuing parquet across module seams.
            var renderer=obj.GetComponent<MeshRenderer>();var material=renderer.sharedMaterial;var scale=material.mainTextureScale;
            var offset=material.mainTextureOffset+Vector2.Scale(new Vector2(position.x*basis.x+position.z*basis.z,position.x*basis.y+position.z*basis.w),scale);
            var block=new MaterialPropertyBlock();block.SetVector("_MainTex_ST",new Vector4(scale.x,scale.y,offset.x,offset.y));renderer.SetPropertyBlock(block);
        }
        public static void Deck(Vector3 center,float width,float depth,Transform parent)
        {
            for(float z=0;z<depth-.01f;)
            {
                int dz=depth-z>=1.99f?2:1;
                for(float x=0;x<width-.01f;){int dx=width-x>=1.99f?2:1;Place("Floor"+dx+"x"+dz,parent,center+new Vector3(-width/2+x+dx/2f,.005f,-depth/2+z+dz/2f));x+=dx;}
                z+=dz;
            }
        }
        public static void Wall(Vector3 center,Vector3 tangent,float length,Vector2Int facing,Transform parent)
        {
            bool low=facing.x<0||facing.y<0;
            float yaw=facing.x!=0?(facing.x>0?90:-90):(facing.y>0?0:180);
            for(float cursor=0;cursor<length-.01f;){int width=length-cursor>=1.99f?2:1;Place("Wall"+(low?"Low":"")+width,parent,center+tangent*(-length/2+cursor+width/2f)+new Vector3(facing.x,0,facing.y)*.09f,yaw);cursor+=width;}
        }
        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        static int Key(string value){int hash=17;foreach(char c in value)unchecked{hash=hash*31+c;}return hash;}
        public static int Dress(Room room,int index,int seed,int turn,Transform parent)
        {
            int role=(index+(seed&3))%4;string[] recipes={"Reception","Work floor","Meeting","Records storage"};
            var root=new GameObject("Corporate room / "+recipes[role]+" / "+index);root.transform.SetParent(parent,false);
            Vector3 center=new Vector3(room.X*20,0,room.Y*20);root.transform.position=center;
            int quarter=index==0?0:turn;Quaternion rotation=Quaternion.Euler(0,quarter*90,0);float half=room.Size/2f;int hash=17+role;
            // Finish the 18cm outer architectural corner; never protrude into walkable space.
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Place(x<0||z<0?"CornerLow":"Corner",root.transform,center+new Vector3(x*(half+.09f),0,z*(half+.09f)));
            GameObject Furniture(string key,Vector3 local,float yaw=0)
            {
                var collider=Model(key).GetComponent<BoxCollider>();Quaternion facing=rotation*Quaternion.Euler(0,yaw,0);
                Vector3 p=center+rotation*local,boundsCenter=p+facing*collider.center;
                Vector3 ext=Abs(facing*Vector3.right)*collider.size.x/2+Abs(facing*Vector3.up)*collider.size.y/2+Abs(facing*Vector3.forward)*collider.size.z/2;
                Vector3 relative=boundsCenter-center;
                if(Mathf.Abs(relative.x)-ext.x<2.1f||Mathf.Abs(relative.z)-ext.z<2.1f)return null;
                if(Mathf.Abs(relative.x)+ext.x>half-.3f||Mathf.Abs(relative.z)+ext.z>half-.3f)return null;
                if(index==0&&relative.x<0&&relative.z<0)return null;
                if(Physics.CheckBox(boundsCenter,collider.size*.49f,facing,1<<9))return null;
                var obj=Place(key,root.transform,p,quarter*90+yaw);Physics.SyncTransforms();
                unchecked{hash=hash*31+Key(key)+Mathf.RoundToInt(p.x*100)+Mathf.RoundToInt(p.z*100)*13;}return obj;
            }
            void Workstation(GameObject furniture)
            {
                if(!furniture)return;var box=furniture.GetComponent<BoxCollider>();float top=box.center.y+box.size.y/2;
                var screen=Place("Monitor",furniture.transform,furniture.transform.TransformPoint(new Vector3(0,top+.005f,0)),furniture.transform.eulerAngles.y+90);
                // Desktop details inherit furniture placement; they never create extra route blockers.
                foreach(var c in screen.GetComponentsInChildren<Collider>())UnityEngine.Object.Destroy(c);
                var keyboard=Place("Keyboard",furniture.transform,furniture.transform.TransformPoint(new Vector3(0,top+.005f,-.4f)),furniture.transform.eulerAngles.y+90);
                foreach(var c in keyboard.GetComponentsInChildren<Collider>())UnityEngine.Object.Destroy(c);
                var cup=Place("Cup",furniture.transform,furniture.transform.TransformPoint(new Vector3(.6f,top+.005f,.2f)),0);
                foreach(var c in cup.GetComponentsInChildren<Collider>())UnityEngine.Object.Destroy(c);
            }
            if(role==0)
            {
                Workstation(Furniture("Cabinet",new Vector3(half-1.7f,0,half-1.1f)));
                Furniture("Bookcase",new Vector3(-half+2.1f,0,half-.8f));
                Furniture("Cabinet",new Vector3(half-1.7f,0,-half+1.1f));
            }
            else if(role==1)
            {
                if(room.Size>=16)Workstation(Furniture("Counter",new Vector3(half-1.7f,0,half-3f)));
                else Workstation(Furniture("Cabinet",new Vector3(half-1.7f,0,half-1.1f)));
                Workstation(Furniture("Cabinet",new Vector3(-half+1.7f,0,half-1.1f)));
                Furniture("Bookcase",new Vector3(half-2.1f,0,-half+.8f));
            }
            else if(role==2)
            {
                if(room.Size>=16)Workstation(Furniture("Counter",new Vector3(half-1.7f,0,half-3f)));
                else Workstation(Furniture("Cabinet",new Vector3(half-1.7f,0,half-1.1f)));
                Furniture("Bookcase",new Vector3(-half+2.1f,0,half-.8f));
            }
            else
            {
                Furniture("Bookcase",new Vector3(half-2.1f,0,half-.8f));Furniture("Bookcase",new Vector3(-half+2.1f,0,half-.8f));
                Furniture("Cabinet",new Vector3(half-1.7f,0,-half+1.1f));
            }
            var lamp=new GameObject("Corporate cyan identity light");lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=new Vector3(half-1.2f,2.2f,half-1.2f);
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.3f,.75f,1);light.intensity=.9f;light.range=6;
            var alert=new GameObject("Corporate lockdown status");alert.transform.SetParent(root.transform,false);alert.transform.localPosition=new Vector3(-half+1.2f,2.2f,half-1.2f);
            var red=alert.AddComponent<Light>();red.type=LightType.Point;red.color=new Color(1,.17f,.13f);red.intensity=.35f;red.range=3;
            alert.AddComponent<CorporateStatusPulse>().Phase=(seed+index)&7;
            var motion=root.AddComponent<BiomeMotion>();motion.Phase=(seed+index)&7;
            return hash;
        }
    }
    public sealed class CorporateStatusPulse:MonoBehaviour
    {
        public float Phase;Light lamp;void Awake()=>lamp=GetComponent<Light>();
        void Update(){if(lamp.enabled&&Time.timeScale>0)lamp.intensity=.35f+.04f*Mathf.Sin(Time.time*.9f+Phase);}
    }
}
