using UnityEngine;
namespace VideoGame
{
    public sealed class ShipCrewIdle : MonoBehaviour
    {
        public Transform Head,Hand;
        Quaternion headRest,handRest;
        void Awake(){headRest=Head.localRotation;handRest=Hand.localRotation;}
        void LateUpdate(){float t=Time.time+transform.position.x;Head.localRotation=headRest*Quaternion.Euler(Mathf.Sin(t*.7f)*1.5f,Mathf.Sin(t*.3f)*3,0);Hand.localRotation=handRest*Quaternion.Euler(Mathf.Sin(t*1.8f)*2,0,0);}
    }
}
