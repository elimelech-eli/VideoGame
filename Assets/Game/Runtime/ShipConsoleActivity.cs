using UnityEngine;
namespace VideoGame
{
    public sealed class ShipConsoleActivity : MonoBehaviour
    {
        public Renderer[] Screens;
        MaterialPropertyBlock properties;
        float phase;
        void Awake(){properties=new MaterialPropertyBlock();phase=transform.position.x*1.73f+transform.position.z*.41f;}
        void Update()
        {
            float value=.55f+.15f*Mathf.Sin(Time.time*.8f+phase)+.08f*Mathf.Sin(Time.time*2.3f+phase);
            properties.SetColor("_EmissionColor",new Color(.12f,.65f,.85f)*value);
            foreach(var screen in Screens)screen.SetPropertyBlock(properties);
        }
    }
}
