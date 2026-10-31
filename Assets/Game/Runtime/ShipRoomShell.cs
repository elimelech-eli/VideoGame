using UnityEngine;
namespace VideoGame
{
    public sealed class ShipRoomShell : MonoBehaviour
    {
        public int RoomId;
        public Renderer[] InteriorLabels;
        StarshipLife ship;
        Renderer[] renderers;
        MaterialPropertyBlock properties;
        float opacity;
        void Awake(){ship=GetComponentInParent<StarshipLife>();renderers=GetComponentsInChildren<Renderer>();properties=new MaterialPropertyBlock();opacity=RoomId==4?0:1;}
        void LateUpdate()
        {
            float target=ship.OccupiedRoom==RoomId?0:1;
            // Door transitions reveal adjoining spaces while the opening is occupied.
            var player=Prototype.Instance?.Player;
            if(player)
            {
                var p=ship.transform.InverseTransformPoint(player.transform.position);
                if(Mathf.Abs(p.x)<1.25f && Mathf.Abs(p.z-5)<1.3f && (RoomId==0||RoomId==4))target=0;
                if(Mathf.Abs(p.x)<1.25f && Mathf.Abs(p.z+6)<1.3f && (RoomId==3||RoomId==4))target=0;
                if(Mathf.Abs(p.z)<1.25f && Mathf.Abs(p.x+1.6f)<1.2f && (RoomId==1||RoomId==4))target=0;
                if(Mathf.Abs(p.z)<1.25f && Mathf.Abs(p.x-1.6f)<1.2f && (RoomId==2||RoomId==4))target=0;
            }
            opacity=Mathf.MoveTowards(opacity,target,Time.deltaTime*3);
            properties.SetFloat("_Opacity",opacity);
            foreach(var r in renderers){r.SetPropertyBlock(properties);r.enabled=opacity>.001f;}
            foreach(var label in InteriorLabels)label.enabled=opacity<.5f;
        }
    }
}
