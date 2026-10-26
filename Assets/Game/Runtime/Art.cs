using System.Collections.Generic;
using UnityEngine;

namespace VideoGame
{
    // Imported models are presentation only. Gameplay retains its existing colliders.
    public sealed class Art : MonoBehaviour
    {
        public static readonly Color Cyan = new Color(.12f, .88f, 1);
        public static readonly Color Amber = new Color(1, .47f, .12f);
        readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();

        public GameObject Model(string path, Transform parent, Vector3 position, float size, Color accent, float yaw = 0)
        {
            if (!models.TryGetValue(path, out var source))
            {
                source = Resources.Load<GameObject>("Art/" + path);
                if (source == null) throw new System.InvalidOperationException("Missing art asset: " + path);
                models.Add(path, source);
            }
            var root = new GameObject(path.Substring(path.LastIndexOf('/') + 1));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var model = Instantiate(source, root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(model.transform.position, Vector3.zero);
            bool first = true;
            foreach (var renderer in renderers)
            {
                if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
            }
            float factor = size / Mathf.Max(.001f, Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)));
            model.transform.localScale *= factor;
            // Place the asset's lowest point on its parent's floor plane.
            model.transform.position += (root.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)) * factor;
            foreach (var renderer in renderers)
            {
                var originals = renderer.sharedMaterials;
                var remapped = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    string key = path.Split('/')[0] + "/" + originals[i].name + "/" + accent;
                    if (!palette.TryGetValue(key, out var material))
                    {
                        material = new Material(Resources.Load<Material>("PrototypeSurface"));
                        bool textured = !path.StartsWith("Space/");
                        if (textured)
                        {
                            material.mainTexture = Resources.Load<Texture2D>("Art/" + path.Split('/')[0] + "/Textures/colormap");
                            material.color = new Color(.55f, .65f, .72f);
                        }
                        else
                        {
                            string name = originals[i].name.ToLowerInvariant();
                            material.color = name.Contains("red") ? accent : name == "dark" ? new Color(.035f, .055f, .08f) : name.Contains("dark") ? new Color(.14f, .2f, .26f) : new Color(.4f, .49f, .57f);
                            if (name.Contains("red")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", accent * .45f); }
                        }
                        material.SetFloat("_Metallic", .4f);
                        material.SetFloat("_Glossiness", .35f);
                        material.enableInstancing = true;
                        palette.Add(key, material);
                    }
                    remapped[i] = material;
                }
                renderer.sharedMaterials = remapped;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            foreach (var collider in model.GetComponentsInChildren<Collider>()) Destroy(collider);
            return root;
        }
        public void Strip(Vector3 position, Vector3 scale, Color color, Transform parent)
        {
            var go = Prototype.Instance.Shape("Light strip", PrimitiveType.Cube, position, scale, color, parent);
            var renderer = go.GetComponent<Renderer>();
            string key = "light/" + color;
            if (!palette.TryGetValue(key, out var material))
            {
                material = new Material(Resources.Load<Material>("PrototypeSurface"));
                material.color = color; material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.2f); palette.Add(key, material);
            }
            renderer.sharedMaterial = material;
            Destroy(go.GetComponent<Collider>());
        }
        public void PointLight(Vector3 position, Color color, Transform parent, float range = 8)
        {
            var light = new GameObject("Practical light").AddComponent<Light>();
            light.transform.SetParent(parent); light.transform.position = position;
            light.type = LightType.Point; light.color = color; light.range = range; light.intensity = 2.4f;
        }
        public void Sign(string text, Vector3 position, float size, Color color, Transform parent)
        {
            var sign = new GameObject("Sign: " + text).AddComponent<TextMesh>();
            sign.transform.SetParent(parent); sign.transform.position = position;
            sign.transform.rotation = Quaternion.Euler(55, 45, 0);
            sign.anchor = TextAnchor.MiddleCenter; sign.alignment = TextAlignment.Center;
            sign.fontSize = 64; sign.characterSize = size / 10; sign.color = color; sign.text = text;
            sign.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sign.GetComponent<MeshRenderer>().sharedMaterial = sign.font.material;
        }
        void OnDestroy() { foreach (var material in palette.Values) Destroy(material); }
    }

    public sealed class ActorPresentation : MonoBehaviour
    {
        public Transform Visual;
        public bool Hover;
        Vector3 rest, previous;
        void Start() { rest = Visual.localPosition; previous = transform.position; }
        void LateUpdate()
        {
            float speed = (transform.position - previous).magnitude / Mathf.Max(.001f, Time.deltaTime);
            float bob = Hover ? Mathf.Sin(Time.time * 3) * .12f : Mathf.Sin(Time.time * 18) * Mathf.Min(speed, 7) * .007f;
            Visual.localPosition = rest + Vector3.up * bob;
            previous = transform.position;
        }
    }

    public sealed class Burst : MonoBehaviour
    {
        public float Duration = .3f;
        public float Growth = 3;
        float age;
        Vector3 initial;
        void Start() => initial = transform.localScale;
        void Update()
        {
            age += Time.deltaTime;
            transform.localScale = initial * (1 + age * Growth);
            if (age >= Duration) Destroy(gameObject);
        }
    }
}
