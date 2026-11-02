using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    public enum NeonRoomRecipe { SlumCourtyard, CorporatePlaza, TransitionStreet }
    public readonly struct NeonBuildingReservation
    {
        public readonly int RoomIndex; public readonly string Asset; public readonly Vector3 Position,Size; public readonly float Yaw;
        public NeonBuildingReservation(int room,string asset,Vector3 p,Vector3 size,float yaw){RoomIndex=room;Asset=asset;Position=p;Size=size;Yaw=yaw;}
        public Bounds Bounds=>new Bounds(Position+Vector3.up*Size.y/2,Size);
    }
    // Visual adapter only. Existing MapLayout/mission/audio/combat remain authoritative.
    public sealed class NeonBiome:MonoBehaviour
    {
        static readonly Dictionary<string,GameObject> Models=new Dictionary<string,GameObject>();
        static readonly string[] BuildingNames={"CorporateBuilding1","CorporateBuilding3","CorporateBuilding2","CorporateBuilding4"};
        static readonly Vector3[] BuildingSizes={new Vector3(11.213072f,17.170021f,8.874987f),new Vector3(5.670372f,15.615142f,12.509086f),new Vector3(18.436827f,31.958122f,11.016993f),new Vector3(34.55142f,45.895866f,33.199635f)};
        IReadOnlyList<Room> rooms; int seed;
        public IReadOnlyList<NeonBuildingReservation> Reservations {get;private set;}=Array.Empty<NeonBuildingReservation>();
        public static NeonRoomRecipe Recipe(int index,int missionSeed)=>(NeonRoomRecipe)((index+(int)((uint)missionSeed%3))%3);
        static GameObject Model(string key)
        {
            if(!Models.TryGetValue(key,out var model)||!model)
            {
                model=Resources.Load<GameObject>("Neon/Prefabs/"+key);
                if(!model)throw new InvalidOperationException("Missing approved Neon source prefab "+key+". Build NeonBiomeAssets after the source/license intake; no generic art fallback is permitted.");
                Models[key]=model;
            }
            return model;
        }
        public static GameObject Place(string key,Transform parent,Vector3 p,float yaw=0)
        {var o=Instantiate(Model(key),parent);o.name="Neon / "+key;o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));return o;}
        public void Begin(IReadOnlyList<Room> layout,int missionSeed,Transform parent)
        {
            if(layout==null||layout.Count<1)throw new ArgumentException("Neon requires the authoritative mission layout");
            rooms=layout;seed=missionSeed;
            // Every recipe needs its actual near-scale source unit, not a distant skyline substitute.
            Model("SlumBlockD2");Model("PowerCabinet");Model("CorporateBuilding1");
            Reservations=Plan(layout,missionSeed);
            foreach(var reservation in Reservations)
            {
                Deck(reservation.Position,Mathf.Ceil(reservation.Size.x),Mathf.Ceil(reservation.Size.z),parent);
                var o=Place(reservation.Asset,parent,reservation.Position,reservation.Yaw);
                o.name="Neon whole building / room "+reservation.RoomIndex+" / "+reservation.Asset;
            }
        }
        // Pure deterministic footprint plan. Full building volumes are outside EVERY room and passage.
        public static IReadOnlyList<NeonBuildingReservation> Plan(IReadOnlyList<Room> layout,int missionSeed)
        {
            var result=new List<NeonBuildingReservation>();if(layout==null||layout.Count==0)return result;
            var protectedAreas=ProtectedAreas(layout);var viewTargets=ViewTargets(layout).ToArray();int budget=Math.Max(1,layout.Count/4);
            var order=Enumerable.Range(0,layout.Count).OrderBy(i=>Recipe(i,missionSeed)==NeonRoomRecipe.CorporatePlaza?0:1).ThenBy(i=>i).ToArray();
            foreach(int index in order)
            {
                if(Recipe(index,missionSeed)==NeonRoomRecipe.SlumCourtyard)continue;
                var room=layout[index];var center=new Vector3(room.X*MapLayout.Spacing,0,room.Y*MapLayout.Spacing);
                // Positive sides lie behind the gameplay view; near-side towers would hide combat.
                foreach(var side in new[]{Vector2Int.up,Vector2Int.right})
                {
                    if(layout.Any(r=>r.X==room.X+side.x&&r.Y==room.Y+side.y))continue;
                    var normal=new Vector3(side.x,0,side.y);var tangent=new Vector3(side.y,0,-side.x);
                    for(int choice=0;choice<BuildingNames.Length;choice++)
                    {
                        int pick=(choice+(int)((uint)(missionSeed+index)%2))%BuildingNames.Length;
                        float yaw=side.x==0?0:90;Vector3 size=side.x==0?BuildingSizes[pick]:new Vector3(BuildingSizes[pick].z,BuildingSizes[pick].y,BuildingSizes[pick].x);
                        float depth=side.x==0?size.z:size.x;
                        foreach(float shift in new[]{0f,-3f,3f})
                        {
                            var p=center+normal*(room.Size/2f+.7f+depth/2)+tangent*shift;
                            var footprint=new Rect(p.x-size.x/2-.2f,p.z-size.z/2-.2f,size.x+.4f,size.z+.4f);
                            if(protectedAreas.Any(a=>a.Overlaps(footprint)))continue;
                            if(result.Any(r=>XZ(r.Bounds,.4f).Overlaps(footprint)))continue;
                            if(Occludes(new Bounds(p+Vector3.up*size.y/2,size),viewTargets))continue;
                            result.Add(new NeonBuildingReservation(index,BuildingNames[pick],p,size,yaw));goto NextRoom;
                        }
                    }
                }
                NextRoom: if(result.Count>=budget)break;
            }
            return result;
        }
        // Camera clearance is checked against every room and passage, not just a landmark's host room.
        public static bool BlocksGameplayView(Bounds volume,IReadOnlyList<Room> layout)=>Occludes(volume,ViewTargets(layout));
        static bool Occludes(Bounds volume,IEnumerable<Vector3> targets)
        {
            var forward=Quaternion.Euler(50,45,0)*Vector3.forward;
            foreach(var p in targets)foreach(float height in new[]{.35f,1.3f,1.9f})
            {
                var target=p+Vector3.up*height;
                if(volume.IntersectRay(new Ray(target-forward*180,forward),out float distance)&&distance<179.8f)return true;
            }
            return false;
        }
        static IEnumerable<Vector3> ViewTargets(IReadOnlyList<Room> layout)
        {
            for(int i=0;i<layout.Count;i++)
            {
                var a=layout[i];var center=new Vector3(a.X*20,0,a.Y*20);float margin=a.Size/2f-1;
                foreach(float x in new[]{-margin,0,margin})foreach(float z in new[]{-margin,0,margin})yield return center+new Vector3(x,0,z);
                for(int j=i+1;j<layout.Count;j++)
                {
                    var b=layout[j];if(!MapLayout.Adjacent(a,b))continue;var end=new Vector3(b.X*20,0,b.Y*20);var across=Vector3.Cross(Vector3.up,(end-center).normalized);
                    for(int step=0;step<=10;step++)foreach(float lane in new[]{-1.3f,0,1.3f})yield return Vector3.Lerp(center,end,step/10f)+across*lane;
                }
            }
        }
        public static List<Rect> ProtectedAreas(IReadOnlyList<Room> layout)
        {
            var areas=new List<Rect>();
            for(int i=0;i<layout.Count;i++)
            {
                var a=layout[i];float h=a.Size/2f+.35f;areas.Add(new Rect(a.X*20-h,a.Y*20-h,h*2,h*2));
                for(int j=i+1;j<layout.Count;j++)
                {
                    var b=layout[j];if(!MapLayout.Adjacent(a,b))continue;
                    float x=Math.Min(a.X,b.X)*20,z=Math.Min(a.Y,b.Y)*20;
                    areas.Add(a.X!=b.X?new Rect(x,z-2.35f,20,4.7f):new Rect(x-2.35f,z,4.7f,20));
                }
            }
            return areas;
        }
        static Rect XZ(Bounds b,float pad)=>new Rect(b.min.x-pad,b.min.z-pad,b.size.x+pad*2,b.size.z+pad*2);
        public static void Deck(Vector3 center,float width,float depth,Transform parent)
        {
            // Whole 1/2m source-density ground modules; generated room/corridor extents are integral.
            if(Mathf.Abs(width-Mathf.Round(width))>.001f||Mathf.Abs(depth-Mathf.Round(depth))>.001f)throw new ArgumentException("Neon floor extent must be integral metres");
            for(int z=0;z<Mathf.RoundToInt(depth);)
            {int dz=depth-z>=1.99f?2:1;for(int x=0;x<Mathf.RoundToInt(width);){int dx=width-x>=1.99f?2:1;Place("Floor"+dx+"x"+dz,parent,center+new Vector3(-width/2+x+dx/2f,.005f,-depth/2+z+dz/2f));x+=dx;}z+=dz;}
        }
        NeonRoomRecipe NearestRecipe(Vector3 p)
        {if(rooms==null)throw new InvalidOperationException("Call NeonBiome.Begin before map assembly");int closest=0;float distance=float.MaxValue;for(int i=0;i<rooms.Count;i++){float d=(new Vector2(p.x,p.z)-new Vector2(rooms[i].X*20,rooms[i].Y*20)).sqrMagnitude;if(d<distance){distance=d;closest=i;}}return Recipe(closest,seed);}
        public void Wall(Vector3 center,Vector3 tangent,float length,Vector2Int facing,Transform parent,bool corridor=false)
        {
            bool low=facing.x<0||facing.y<0;var recipe=NearestRecipe(center);string family=recipe==NeonRoomRecipe.CorporatePlaza&&!corridor?"Corporate":"Slum";
            float yaw=facing.x!=0?(facing.x>0?90:-90):(facing.y>0?0:180);
            for(float cursor=0;cursor<length-.01f;){int width=length-cursor>=1.99f?2:1;Place(family+"Wall"+(low?"Low":"")+width,parent,center+tangent*(-length/2+cursor+width/2f)+new Vector3(facing.x,0,facing.y)*.15f,yaw);cursor+=width;}
        }
        public int Dress(Room room,int index,int missionSeed,int turn,Transform parent)
        {
            var recipe=Recipe(index,missionSeed);var root=new GameObject("Neon room / "+recipe+" / "+index);root.transform.SetParent(parent,false);
            var center=new Vector3(room.X*20,0,room.Y*20);root.transform.position=center;float half=room.Size/2f;int hash=17+(int)recipe;
            bool Furniture(string key,Vector3 local,float yaw=0)
            {
                var prototype=Model(key);var box=prototype.GetComponent<BoxCollider>();if(!box)throw new InvalidOperationException("Missing measured Neon root collision "+key);
                var rotation=Quaternion.Euler(0,yaw,0);var p=center+local;var c=p+rotation*box.center;
                var ext=Abs(rotation*Vector3.right)*box.size.x/2+Abs(rotation*Vector3.up)*box.size.y/2+Abs(rotation*Vector3.forward)*box.size.z/2;
                var relative=c-center;
                if(Mathf.Abs(relative.x)-ext.x<2.15f||Mathf.Abs(relative.z)-ext.z<2.15f)return false;
                if(Mathf.Abs(relative.x)+ext.x>half-.25f||Mathf.Abs(relative.z)+ext.z>half-.25f)return false;
                if(index==0&&relative.x<0&&relative.z<0)return false;
                if(Physics.CheckBox(c,box.size*.49f,rotation,1<<9))return false;
                Place(key,root.transform,p,yaw);Physics.SyncTransforms();unchecked{hash=hash*31+Stable(key)+Mathf.RoundToInt(p.x*100)+Mathf.RoundToInt(p.z*100)*13;}return true;
            }
            float corner=half-1.55f;
            if(recipe==NeonRoomRecipe.SlumCourtyard)
            {if(Furniture("SlumBlockD2",new Vector3(corner,0,corner),180))Furniture("SlumBlockE3",new Vector3(corner,2.5f,corner),180);Furniture("SlumBlockE3",new Vector3(-corner,0,corner),90);Furniture("PowerCabinet",new Vector3(corner,0,-corner));}
            else if(recipe==NeonRoomRecipe.CorporatePlaza)
            {Furniture("PowerCabinet",new Vector3(corner,0,corner),180);Furniture("PowerCabinet",new Vector3(-corner,0,corner),90);Furniture("PowerCabinet",new Vector3(corner,0,-corner));}
            else
            {Furniture("SlumBlockE3",new Vector3(corner,0,corner),180);Furniture("PowerCabinet",new Vector3(-corner,0,corner),90);Furniture("PowerCabinet",new Vector3(corner,0,-corner));}
            // Mount street graphics on the visible hut face, or above corporate equipment; never in doorway approaches.
            Place(recipe==NeonRoomRecipe.CorporatePlaza?"SignSignal":recipe==NeonRoomRecipe.TransitionStreet?"SignArcade":"SignNoodle",root.transform,center+new Vector3(-corner,recipe==NeonRoomRecipe.SlumCourtyard?1.8f:2.2f,recipe==NeonRoomRecipe.SlumCourtyard?half-2.82f:half-.015f));
            if(recipe!=NeonRoomRecipe.CorporatePlaza)Place("GraffitiCity",root.transform,center+new Vector3(corner,1.05f,half-2.82f));
            var lamp=Place(recipe==NeonRoomRecipe.SlumCourtyard?"LampWarm":"LampCool",root.transform,center+new Vector3(-corner,2.4f,recipe==NeonRoomRecipe.SlumCourtyard?half-2.84f:half-.012f));
            var motion=root.AddComponent<BiomeMotion>();motion.Phase=(missionSeed+index)&7;
            var pulse=lamp.AddComponent<NeonPracticalPulse>();pulse.Phase=motion.Phase;
            return hash;
        }
        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        static int Stable(string s){int h=17;foreach(char c in s)unchecked{h=h*31+c;}return h;}
    }
    public sealed class NeonPracticalPulse:MonoBehaviour
    {public float Phase;Light lamp;void Awake()=>lamp=GetComponentInChildren<Light>();void Update(){if(lamp&&lamp.enabled&&Time.timeScale>0)lamp.intensity=1.7f+.08f*Mathf.Sin(Time.time*.9f+Phase);}}
}
