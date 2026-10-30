using System;
using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Placement adapter for the approved, credited source kit. Map topology stays owned by MapLayout.
    public static class LabBiome
    {
        static readonly Dictionary<string,GameObject> Models=new Dictionary<string,GameObject>();
        public static GameObject Place(string role,Transform parent,Vector3 position,float yaw=0)
        {
            if(!Models.TryGetValue(role,out var model)||model==null)
            {
                model=Resources.Load<GameObject>("LabReview/Prefabs/"+role);
                if(model==null)throw new InvalidOperationException("Missing production lab module: "+role);
                Models[role]=model;
            }
            var instance=UnityEngine.Object.Instantiate(model,parent);instance.name="Lab / "+role;
            instance.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            instance.layer=role.StartsWith("Floor",StringComparison.Ordinal)?8:9;
            return instance;
        }
        public static void Deck(Vector3 center,float width,float depth,Transform parent)
        {
            // Every generator extent is even metres. Use cropped source fillers rather than stretching UVs or equipment.
            for(float z=0;z<depth-.01f;)
            {
                float dz=Mathf.Min(4,depth-z);
                for(float x=0;x<width-.01f;)
                {
                    float dx=Mathf.Min(4,width-x);
                    string role=dx==4&&dz==4?"Floor4":dx==2&&dz==2?"Floor2":"Floor2x4";
                    Place(role,parent,center+new Vector3(-width/2+x+dx/2,.005f,-depth/2+z+dz/2),dx==4&&dz==2?90:0);
                    x+=dx;
                }
                z+=dz;
            }
        }
        public static void Wall(Vector3 center,Vector3 tangent,float length,Vector2Int facing,Transform parent)
        {
            bool cutaway=facing.x<0||facing.y<0;
            float yaw=facing.x!=0?(facing.x>0?90:-90):(facing.y>0?0:180);
            // The wall prefab's decorative face points toward -Z. Corners butt at their socket plane.
            float cursor=0;
            while(cursor<length-.01f)
            {
                int width=length-cursor>=3.99f?4:length-cursor>=1.99f?2:1;
                Place("Wall"+(cutaway?"Low":"")+width,parent,center+tangent*(-length/2+cursor+width*.5f),yaw);
                // Full collider follows full geometry; cutaway collision follows its visible low cover.
                cursor+=width;
            }
            // Source door is a sealed panel, not a 4 m passage. Only put it on a closed boundary.
            if(length>=12&&!cutaway)
                Place("Door",parent,center-new Vector3(facing.x,0,facing.y)*.18f+Vector3.up*.11f,yaw+180);
        }
        public static int Dress(Room room,int index,int seed,int turn,Transform parent)
        {
            var root=new GameObject("Lab workspace / "+index);root.transform.SetParent(parent,false);
            Vector3 center=new Vector3(room.X*MapLayout.Spacing,0,room.Y*MapLayout.Spacing);
            root.transform.position=center;
            if(index==0)turn=0;
            Quaternion rotation=Quaternion.Euler(0,turn*90,0);
            var random=new System.Random(Deployment.DressingSeed(seed,index));
            float half=room.Size/2f;
            int hash=17;
            GameObject Furniture(string role,Vector3 local,float yaw)
            {
                Vector3 point=center+rotation*local+Vector3.up*.11f;
                var model=Models.ContainsKey(role)?Models[role]:Resources.Load<GameObject>("LabReview/Prefabs/"+role);
                var collider=model.GetComponent<BoxCollider>();Quaternion facing=rotation*Quaternion.Euler(0,yaw,0);
                Vector3 size=collider.size;
                Vector3 worldCenter=point+facing*collider.center;
                // Keep all four 4 m door lanes, extraction and authored hazard hulls free.
                Vector3 envelope=Abs(facing*Vector3.right)*size.x/2+Abs(facing*Vector3.forward)*size.z/2;
                Vector3 relative=worldCenter-center;
                if(Mathf.Abs(relative.x)-envelope.x<2.05f||Mathf.Abs(relative.z)-envelope.z<2.05f)return null;
                if(Mathf.Abs(relative.x)+envelope.x>half-.15f||Mathf.Abs(relative.z)+envelope.z>half-.15f)return null;
                if(index==0&&new Bounds(worldCenter,size).SqrDistance(new Vector3(-4,.5f,-4))<4)return null;
                if(Physics.CheckBox(worldCenter,size*.49f,facing,1<<9))return null;
                var placed=Place(role,root.transform,point,turn*90+yaw);
                Physics.SyncTransforms();
                unchecked{hash=hash*31+role.Length*17+Mathf.RoundToInt(point.x*100)+Mathf.RoundToInt(point.z*100)*13+(int)yaw;}
                return placed;
            }
            float corner=half-2.05f;
            int variant=index==0?0:(index+random.Next(3))%3;
            string primary=variant==0?"Containment":variant==1?"Examination":"SpecimenRack";
            var feature=Furniture(primary,new Vector3(corner,0,corner),variant==1?90:0);
            if(feature==null)feature=Furniture("Cabinet",new Vector3(corner,0,corner),90);
            // Arrival is a composed workspace; mission rooms retain only collision-safe furnishings.
            if(index==0)
            {
                Furniture("Console",new Vector3(3,0,2.8f),90);
                Furniture("SpecimenRack",new Vector3(-half+1.5f,0,half-1.25f),180);
                var table=Furniture("Examination",new Vector3(-half+1.6f,0,3.4f),90);
                if(table!=null)
                {
                    var jar=Place("SampleJar",root.transform,table.transform.position+new Vector3(0,.69f,-.4f),20);
                    UnityEngine.Object.Destroy(jar.GetComponent<Collider>());
                    var vial=Place("SampleVial",root.transform,table.transform.position+new Vector3(.12f,.69f,.1f),-15);
                    UnityEngine.Object.Destroy(vial.GetComponent<Collider>());
                }
                Furniture("ControlBank",new Vector3(half-1.1f,0,-half+2.5f),180);
            }
            if(room.Size>=14)
                Furniture("UtilityPipe",new Vector3(half-1,0,half-2),0);
            var motion=root.AddComponent<BiomeMotion>();motion.Phase=(float)random.NextDouble()*6;
            Practical(root.transform,"Inspection",rotation*new Vector3(1,4.5f,1),rotation*new Vector3(corner,1.7f,corner),new Color(.4f,.7f,1),5.5f,11);
            if(index==0)Practical(root.transform,"Examination",new Vector3(-3,3.8f,1.6f),new Vector3(-half+1.6f,.7f,3.4f),new Color(1,.72f,.44f),4.5f,9);
            if(variant==0)Practical(root.transform,"Tank practical",rotation*new Vector3(corner,1.9f,corner),null,new Color(.08f,.7f,.85f),1.3f,4);
            return hash;
        }
        static Vector3 Abs(Vector3 value)=>new Vector3(Mathf.Abs(value.x),Mathf.Abs(value.y),Mathf.Abs(value.z));
        static void Practical(Transform parent,string name,Vector3 point,Vector3? target,Color color,float intensity,float range)
        {
            var lamp=new GameObject("Lab "+name).AddComponent<Light>();lamp.transform.SetParent(parent,false);lamp.transform.localPosition=point;
            lamp.color=color;lamp.intensity=intensity;lamp.range=range;lamp.type=target.HasValue?LightType.Spot:LightType.Point;
            if(target.HasValue){lamp.transform.LookAt(parent.TransformPoint(target.Value));lamp.spotAngle=65;lamp.innerSpotAngle=28;}
            // The directional light owns mission shadows; local lamps are culled by BiomeMotion.
            lamp.shadows=LightShadows.None;
        }
    }
}
