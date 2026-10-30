using System;
using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;
namespace VideoGame
{
    public sealed class TransitBiome:MonoBehaviour
    {
        static readonly Dictionary<string,GameObject> Models=new Dictionary<string,GameObject>();
        Railway railway;
        public Railway Reservation=>railway;
        public static GameObject Place(string role,Transform parent,Vector3 position,float yaw=0)
        {
            if(!Models.TryGetValue(role,out var model)||model==null)
            {
                model=Resources.Load<GameObject>("Transit/Prefabs/"+role);
                if(model==null)throw new InvalidOperationException("Missing Transit module "+role);
                Models[role]=model;
            }
            var o=Instantiate(model,parent);o.name="Transit / "+role;o.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));return o;
        }
        public void Begin(IReadOnlyList<Room> rooms,Transform parent)
        {
            railway=TransitLayout.Reserve(rooms);var p=new Vector3(railway.X,0,railway.Z);
            var feature=new GameObject("Transit / exterior railway reservation");feature.transform.SetParent(parent,false);
            Place("Track",feature.transform,p);Place("Train",feature.transform,p);
            // Unwalkable scenic track: no new logical room, no rail gap in the traversable graph.
            var r=rooms[railway.RoomIndex];float edge=r.Y*20+r.Size/2f;
            Deck(new Vector3(p.x,0,(edge+p.z-3)/2),r.Size,p.z-3-edge,parent);
        }
        public static void Deck(Vector3 center,float width,float depth,Transform parent)
        {
            for(float z=0;z<depth-.01f;)
            {
                float dz=Mathf.Min(3,depth-z);
                // All room/corridor extents are integral metres, including the exterior platform apron.
                for(float x=0;x<width-.01f;)
                {
                    float remaining=width-x;float dx=remaining>=3.99f?4:remaining>=1.99f?2:1;
                    Place("Floor"+Mathf.RoundToInt(dx)+"x"+Mathf.RoundToInt(dz),parent,center+new Vector3(-width/2+x+dx/2,.005f,-depth/2+z+dz/2));x+=dx;
                }
                z+=dz;
            }
        }
        public void Wall(Vector3 center,Vector3 tangent,float length,Vector2Int facing,Transform parent,bool corridor=false)
        {
            bool platformEdge=facing==Vector2Int.up&&Mathf.Abs(center.z-(railway.Z-14))<8.1f&&Mathf.Abs(center.x-railway.X)<8.1f;
            bool low=facing.x<0||facing.y<0||platformEdge;
            float yaw=facing.x!=0?(facing.x>0?90:-90):(facing.y>0?0:180);
            float cursor=0;
            while(cursor<length-.01f)
            {
                int w=length-cursor>=3.99f?4:length-cursor>=1.99f?2:1;
                var piece=Place("Wall"+(low?"Low":"")+w,parent,center+tangent*(-length/2+cursor+w/2f),yaw);
                {Vector3 normal=new Vector3(facing.x,0,facing.y);var collider=piece.GetComponent<BoxCollider>();Vector3 contact=piece.transform.TransformPoint(collider.center);piece.transform.position+=normal*(.11f-Vector3.Dot(contact-center,normal));}
                cursor+=w;
            }
        }
        public int Dress(Room room,int index,int seed,int turn,Transform parent)
        {
            var role=TransitLayout.Role(index,seed,railway);var root=new GameObject("Transit room / "+role+" / "+index);root.transform.SetParent(parent,false);
            var center=new Vector3(room.X*20,0,room.Y*20);root.transform.position=center;
            float half=room.Size/2f;var rotation=Quaternion.Euler(0,(index==0?0:turn)*90,0);int hash=17+(int)role;
            bool Furniture(string asset,Vector3 local,float yaw=0)
            {
                Vector3 point=center+rotation*local;
                var proto=Resources.Load<GameObject>("Transit/Prefabs/"+asset);var c=proto.GetComponent<BoxCollider>();var facing=rotation*Quaternion.Euler(0,yaw,0);
                Vector3 worldCenter=point+facing*c.center;Vector3 ext=Abs(facing*Vector3.right)*c.size.x/2+Abs(facing*Vector3.forward)*c.size.z/2;
                Vector3 relative=worldCenter-center;
                if(Mathf.Abs(relative.x)-ext.x<2.1f||Mathf.Abs(relative.z)-ext.z<2.1f)return false;
                if(Mathf.Abs(relative.x)+ext.x>half-.2f||Mathf.Abs(relative.z)+ext.z>half-.2f)return false;
                if(Physics.CheckBox(worldCenter,c.size*.5f,facing,1<<9))return false;
                Place(asset,root.transform,point,(index==0?0:turn)*90+yaw);Physics.SyncTransforms();
                unchecked{hash=hash*31+asset.Length*17+Mathf.RoundToInt(point.x*100)+Mathf.RoundToInt(point.z*100)*13;}return true;
            }
            float corner=half-1.3f;
            if(role==StationRole.TicketHall)
            {
                Furniture("Kiosk",new Vector3(3,0,half-1),180);Furniture("Kiosk",new Vector3(4.5f,0,half-1),180);
                Furniture("Bench",new Vector3(-half+1,0,3.8f),90);
            }
            else if(role==StationRole.Maintenance)
            {
                Furniture("PowerCabinet",new Vector3(corner,0,corner));
                Furniture("Compressor",new Vector3(-corner,0,-corner),90);
                Furniture("Electrical",new Vector3(half-1,0,3));
            }
            else
            {
                Furniture("Bench",new Vector3(3.8f,0,corner));
                Furniture("Bench",new Vector3(-corner,0,-3.8f),90);
                if(room.Size>=14)Furniture("Kiosk",new Vector3(corner,0,-corner),180);
            }
            Furniture("FireCabinet",new Vector3(corner,0,corner));
            foreach(float x in new[]{-half+.6f,half-.6f})foreach(float z in new[]{-half+.6f,half-.6f})
                Furniture("Column",new Vector3(x,0,z));
            Furniture("Electrical",new Vector3(-half+.7f,0,half-1),90);
            if(role==StationRole.Concourse||role==StationRole.Waiting)
                Furniture("Bench",new Vector3(half-1,0,-3.8f),90);
            foreach(float x in new[]{-half+1,half-1})
            {
                var pos=center+new Vector3(x,0,half-1);Place("Fixture",root.transform,pos);
                var lamp=new GameObject("Transit fluorescent").AddComponent<Light>();lamp.transform.SetParent(root.transform,false);lamp.transform.position=pos+Vector3.up*2.8f;
                lamp.type=LightType.Point;lamp.color=new Color(.6f,.8f,1);lamp.intensity=2.1f;lamp.range=9;
            }
            var game=GetComponent<Prototype>();
            game.Visuals.Sign(role==StationRole.TicketHall?"NEXUS / TICKETS":role==StationRole.Maintenance?"SERVICE / 04":role==StationRole.Waiting?"DEPARTURES":role==StationRole.Platform?"PLATFORM / 03":"CENTRAL / TRANSFER",center+new Vector3(0,.05f,-half+1),.38f,new Color(.3f,.7f,.85f),root.transform);
            root.AddComponent<BiomeMotion>();return hash;
        }
        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
    }
}
