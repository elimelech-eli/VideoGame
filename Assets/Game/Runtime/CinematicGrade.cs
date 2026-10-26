using UnityEngine;

namespace VideoGame
{
    [RequireComponent(typeof(Camera))]
    public sealed class CinematicGrade : MonoBehaviour
    {
        Material material;
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (System.Environment.GetEnvironmentVariable("POC_RAW_RENDER") == "1") { Graphics.Blit(source, destination); return; }
            if (material == null) material = new Material(Resources.Load<Material>("Cinematic"));
            Graphics.Blit(source, destination, material);
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
