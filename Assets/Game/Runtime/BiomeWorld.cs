using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    public sealed class BiomeWorld : MonoBehaviour
    {
        readonly Dictionary<string,GameObject> models=new Dictionary<string,GameObject>();
        public int DressingHash {get;private set;}
        public void ResetDressing()=>DressingHash=17;
        public GameObject Place(BiomeId biome,string role,Transform parent,Vector3 point,Quaternion rotation)
        {
            string key=(int)biome+"_"+role;
            if(!models.TryGetValue(key,out var model))
            {
                model=Resources.Load<GameObject>("Biomes/Prefabs/"+key);
                if(model==null)throw new System.InvalidOperationException("Missing biome prefab "+key);
                models.Add(key,model);
            }
            // Keep the FBX importer's axis/unit transform inside the placement root.
            var placed=new GameObject(key);placed.transform.SetParent(parent,false);
            placed.transform.SetPositionAndRotation(point,rotation);
            Instantiate(model,placed.transform,false);
            return placed;
        }
        public void Deck(BiomeId biome,Vector3 center,float width,float depth,Transform parent)
        {
            int columns=Mathf.CeilToInt(width/4),rows=Mathf.CeilToInt(depth/4);
            float x=width/columns,z=depth/rows;
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                var tile=Place(biome,"Floor",parent,center+new Vector3(-width/2+x*(col+.5f),.02f,-depth/2+z*(row+.5f)),Quaternion.identity);
                tile.transform.localScale=new Vector3(x/4,1,z/4);
            }
        }
        public void Wall(BiomeId biome,Vector3 center,Vector3 tangent,float length,Vector2Int facing,Transform parent)
        {
            int pieces=Mathf.CeilToInt(length/4);float width=length/pieces;
            for(int i=0;i<pieces;i++)
            {
                float yaw=facing.x!=0?(facing.x>0?-90:90):(facing.y>0?180:0);
                var wall=Place(biome,"Wall",parent,center+tangent*(-length/2+width*(i+.5f)),Quaternion.Euler(0,yaw,0));
                wall.transform.localScale=new Vector3(width/4,facing.x<0||facing.y<0?.28f:1,1);
                Solid(wall);
            }
        }
        public void Dress(BiomeId biome,Room room,int index,int seed,int turn,Transform parent)
        {
            var random=new System.Random(Deployment.DressingSeed(seed,index));
            Vector3 center=new Vector3(room.X*MapLayout.Spacing,0,room.Y*MapLayout.Spacing);
            var profile=BiomeProfile.Get(biome);var motion=new GameObject("Living room / "+profile.Name).AddComponent<BiomeMotion>();
            motion.transform.SetParent(parent);motion.transform.position=center;motion.Phase=(float)random.NextDouble()*6;
            float edge=room.Size/2f-1.65f;
            for(int corner=0;corner<3;corner++)
            {
                if(corner==1&&random.Next(3)==0)continue;
                int pick=corner==0?0:corner==1?random.Next(1,3):2;float yaw=random.Next(4)*90;
                // Keep spawn/extraction and the complete central cross free of decoration.
                Vector3 local=corner==0?new Vector3(edge,0,edge):corner==1?new Vector3(edge-2.1f,0,edge):new Vector3(edge,0,edge-2.1f);
                local+=new Vector3((float)random.NextDouble()*.14f-.07f,0,(float)random.NextDouble()*.14f-.07f);
                Vector3 point=center+Quaternion.Euler(0,turn*90,0)*local;
                yaw=Mathf.Atan2(center.x-point.x,center.z-point.z)*Mathf.Rad2Deg+(yaw/90-1.5f)*5;
                string role=pick==0?"Feature":pick==1?"Service":"Vent";
                var prop=Place(biome,role,parent,point,Quaternion.Euler(0,yaw,0));
                prop.transform.localScale=Vector3.one*(.88f+(float)random.NextDouble()*.12f);
                Solid(prop);
                foreach(var node in prop.GetComponentsInChildren<Transform>())if(node.name=="Rotor")motion.Rotors.Add(node);
                unchecked{DressingHash=DressingHash*31+(int)biome*59+pick*7+(int)yaw+Mathf.RoundToInt(point.x*100)+Mathf.RoundToInt(point.z*100)*13;}
                if(pick==0)
                    GetComponent<Prototype>().Visuals.Sign(biome==BiomeId.NeonStreets?"NIGHT / MARKET":biome==BiomeId.Transit?"LINE / 03":biome==BiomeId.BiotechLab?"ISOLATION":biome==BiomeId.CorporateOffice?"AUTHORIZED":biome==BiomeId.Undercity?"PRESSURE": "AUTOMATION",point+Vector3.up*2.3f,.24f,profile.Accent,parent);
            }

        }
        static void Solid(GameObject prop)
        {
            prop.layer=9;var collider=prop.AddComponent<BoxCollider>();
            var bounds=new Bounds();bool first=true;
            foreach(var filter in prop.GetComponentsInChildren<MeshFilter>())
            {
                var local=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    Vector3 sign=new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1);
                    Vector3 point=prop.transform.InverseTransformPoint(filter.transform.TransformPoint(local.center+Vector3.Scale(local.extents,sign)));
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            }
            collider.center=bounds.center;collider.size=bounds.size;
        }
    }
    public sealed class BiomeMotion : MonoBehaviour
    {
        public readonly List<Transform> Rotors=new List<Transform>();
        public float Phase;
        public float MotionTime {get;private set;}
        Light[] lamps;
        void Start()=>lamps=GetComponentsInChildren<Light>();
        void Update()
        {
            if(Time.timeScale==0)return;
            var player=Prototype.Instance?.Player;
            bool near=player!=null&&(player.transform.position-transform.position).sqrMagnitude<30*30;
            if(lamps!=null)foreach(var lamp in lamps)lamp.enabled=near;
            if(!near)return;
            MotionTime+=Time.deltaTime;
            foreach(var rotor in Rotors)if(rotor!=null)rotor.Rotate(Vector3.up,(95+Phase*12)*Time.deltaTime,Space.World);
        }
    }
}
