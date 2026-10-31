using UnityEngine;
namespace VideoGame
{
    public sealed class ShipDoor : MonoBehaviour
    {
        public Transform Left,Right;
        public BoxCollider Blocker;
        public float Openness {get;private set;}
        Vector3 leftRest,rightRest;
        float closeAt;
        void Awake(){leftRest=Left.localPosition;rightRest=Right.localPosition;}
        void Update()
        {
            var player=Prototype.Instance?.Player;if(!player)return;
            var p=transform.InverseTransformPoint(player.transform.position);
            bool near=Mathf.Abs(p.x)<2 && Mathf.Abs(p.z)<2.8f;
            // Both sides and the doorway occupancy share one safe opening request.
            if(near)closeAt=Time.time+1.2f;
            bool open=near || Time.time<closeAt;
            Openness=Mathf.MoveTowards(Openness,open?1:0,Time.deltaTime*2);
            Left.localPosition=leftRest+Vector3.left*1.15f*Openness;
            Right.localPosition=rightRest+Vector3.right*1.15f*Openness;
            Blocker.enabled=Openness<.85f;
        }
    }
}
