using UnityEngine;

namespace VideoGame
{
    public sealed class StarshipLife : MonoBehaviour
    {
        public static int Room(Vector3 p)=>p.z>=5?0:p.z<=-6?3:p.x< -1.6f?1:p.x>1.6f?2:4;
        public int OccupiedRoom {get;private set;}=4;
        void Update()
        {
            var game=Prototype.Instance;
            if(game==null || game.Player==null)return;
            OccupiedRoom=Room(transform.InverseTransformPoint(game.Player.transform.position));
        }
    }
}
