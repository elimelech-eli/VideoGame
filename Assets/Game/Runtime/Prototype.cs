using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VideoGame.Core;

namespace VideoGame
{
    public sealed class Prototype : MonoBehaviour
    {
        public static Prototype Instance { get; private set; }
        public Mission Mission { get; private set; } = new Mission();
        public bool InMission { get; private set; }
        public Pilot Player { get; private set; }
        public int Seed { get; private set; } = 104729;
        Transform world;
        Camera view;
        Vector3 terminal;
        string message = "Welcome home. Approach the cyan terminal to accept a contract.";
        GUIStyle heading, label;
        readonly List<Material> materials = new List<Material>();
        readonly Dictionary<Color, Material> surfaces = new Dictionary<Color, Material>();
        Material traceMaterial;
        AudioSource audioSource;
        AudioClip shotSound, blastSound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<Prototype>() == null)
                new GameObject("Prototype").AddComponent<Prototype>();
        }
        void Awake()
        {
            Instance = this;
            Application.runInBackground = true;
            view = new GameObject("Isometric Camera").AddComponent<Camera>();
            view.transform.SetParent(transform);
            view.gameObject.AddComponent<AudioListener>();
            audioSource = gameObject.AddComponent<AudioSource>();
            shotSound = Tone("Pulse", 150, .07f);
            blastSound = Tone("Blast", 55, .3f);
            view.orthographic = true;
            view.orthographicSize = 12;
            view.backgroundColor = new Color(.015f, .02f, .045f);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.transform.rotation = Quaternion.Euler(55, 45, 0);
            var light = new GameObject("Key light").AddComponent<Light>();
            light.transform.SetParent(transform);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.intensity = 1.2f;
            RenderSettings.ambientLight = new Color(.3f, .35f, .5f);
            var player = Shape("Pilot", PrimitiveType.Capsule, Vector3.up, new Vector3(.8f, 1, .8f), Color.cyan, transform);
            Destroy(player.GetComponent<Collider>());
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2;
            controller.radius = .4f;
            controller.minMoveDistance = 0;
            Player = player.AddComponent<Pilot>();
            Player.View = view;
            var gun = Shape("Gun", PrimitiveType.Cube, Vector3.zero, new Vector3(.2f, .2f, .9f), Color.white, player.transform);
            gun.transform.localPosition = new Vector3(.45f, .1f, .5f);
            Destroy(gun.GetComponent<Collider>());
            BuildHub();
            Debug.Log("POC_READY: hub, player and mission terminal initialized.");
        }
        public GameObject Shape(string title, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = title;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (!surfaces.TryGetValue(color, out var material))
            {
                material = new Material(Resources.Load<Material>("PrototypeSurface"));
                material.color = color;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * .2f);
                materials.Add(material);
                surfaces.Add(color, material);
            }
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        void ResetWorld()
        {
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            world = new GameObject("World").transform;
            world.SetParent(transform);
            Player.Teleport(new Vector3(0, 1, 0));
            Player.Health = 100;
        }
        public void BuildHub()
        {
            InMission = false;
            ResetWorld();
            Floor(Vector3.zero, 18, new Color(.07f, .11f, .16f));
            terminal = new Vector3(0, 1, 5);
            Shape("Contract terminal", PrimitiveType.Cube, terminal, new Vector3(2, 2, 1), Color.cyan, world);
            for (int i = -1; i <= 1; i += 2)
                Shape("Habitat", PrimitiveType.Cube, new Vector3(i * 6, 2, 5), new Vector3(3, 4, 3), new Color(.2f, .12f, .3f), world);
        }
        void Floor(Vector3 center, float size, Color color)
        {
            Shape("Floor", PrimitiveType.Cube, center + Vector3.down * .25f, new Vector3(size, .5f, size), color, world).layer = 8;
            for (int i = -1; i <= 1; i += 2)
            {
                var strip = Shape("Neon", PrimitiveType.Cube, center + new Vector3(i * (size / 2 - .1f), .02f, 0), new Vector3(.08f, .04f, size), Color.cyan, world);
                Destroy(strip.GetComponent<Collider>());
            }
        }
        public void StartMission()
        {
            if (!Mission.Accept()) return;
            InMission = true;
            ResetWorld();
            Seed++;
            var rooms = MapLayout.Generate(Seed);
            var random = new System.Random(Seed);
            terminal = new Vector3(-4, .5f, -4);
            Shape("Extraction beacon", PrimitiveType.Cylinder, terminal, new Vector3(2, .5f, 2), Color.green, world);
            for (int i = 0; i < rooms.Count; i++)
            {
                Vector3 center = new Vector3(rooms[i].X * 14, 0, rooms[i].Y * 14);
                Floor(center, 14, i % 2 == 0 ? new Color(.1f, .09f, .16f) : new Color(.08f, .14f, .16f));
                if (i == 0) continue;
                for (int j = 0; j < 2; j++)
                {
                    Vector3 point = center + new Vector3((float)random.NextDouble() * 8 - 4, 1, (float)random.NextDouble() * 8 - 4);
                    bool ranged = (i + j) % 3 == 0;
                    var enemy = Shape(ranged ? "Ranged drone" : "Hunter drone", ranged ? PrimitiveType.Sphere : PrimitiveType.Capsule, point, Vector3.one, ranged ? Color.magenta : new Color(1, .3f, .1f), world);
                    enemy.AddComponent<Drone>().Ranged = ranged;
                }
            }
            message = "Contract active: destroy 8 drones. Return to the green beacon to extract.";
        }
        void Update()
        {
            bool interact = Keyboard.current?.eKey.wasPressedThisFrame == true || Gamepad.current?.buttonSouth.wasPressedThisFrame == true;
            if (interact && Vector3.Distance(Player.transform.position, terminal) < 3.5f) Interact();
            if (Player.Health <= 0)
            {
                Mission.Abandon(); BuildHub();
                message = "Mission failed. Recovered at the hub. Accept a new contract to retry.";
            }
        }
        public void Interact()
        {
            if (InMission)
            {
                if (Mission.Stage != MissionStage.Complete) { message = "Eliminate the contract targets before extracting."; return; }
                BuildHub(); message = "Contract complete. Talk to the terminal to collect your reward.";
            }
            else if (Mission.ClaimReward()) message = "Reward received: 100 credits. Another contract is available.";
            else StartMission();
        }
        void LateUpdate()
        {
            view.transform.position = Player.transform.position + new Vector3(-9, 18, -9);
        }
        public void Hit(Vector3 position, Color color, float radius = .3f)
        {
            var effect = Shape("Impact", PrimitiveType.Sphere, position, Vector3.one * radius, color, world);
            Destroy(effect.GetComponent<Collider>());
            Destroy(effect, .12f);
        }
        public void Trace(Vector3 from, Vector3 to, Color color)
        {
            audioSource.PlayOneShot(shotSound, .12f);
            var trace = new GameObject("Tracer").AddComponent<LineRenderer>();
            trace.transform.SetParent(world);
            trace.positionCount = 2; trace.SetPosition(0, from); trace.SetPosition(1, to);
            trace.startWidth = .06f; trace.endWidth = .015f;
            if (traceMaterial == null) { traceMaterial = new Material(Resources.Load<Material>("PrototypeTrace")); materials.Add(traceMaterial); }
            trace.sharedMaterial = traceMaterial; trace.startColor = color; trace.endColor = color;
            Destroy(trace.gameObject, .07f);
        }
        public void Grenade(Vector3 target)
        {
            StartCoroutine(ThrowGrenade(target));
        }
        IEnumerator ThrowGrenade(Vector3 target)
        {
            Vector3 start = Player.transform.position;
            var grenade = Shape("Grenade", PrimitiveType.Sphere, start, Vector3.one * .3f, Color.yellow, world);
            Destroy(grenade.GetComponent<Collider>());
            for (float t = 0; t < 1; t += Time.deltaTime * 2)
            {
                if (grenade == null) yield break;
                grenade.transform.position = Vector3.Lerp(start, target, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 4);
                yield return null;
            }
            if (grenade == null) yield break;
            Destroy(grenade);
            audioSource.PlayOneShot(blastSound, .3f);
            Hit(target, Color.yellow, 7);
            foreach (var collider in Physics.OverlapSphere(target, 4))
                collider.GetComponent<Drone>()?.Damage(80);
        }
        void OnGUI()
        {
            if (Player == null) return;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
                label = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            }
            GUI.Box(new Rect(16, 16, 440, 230), "");
            GUILayout.BeginArea(new Rect(30, 26, 412, 210));
            GUILayout.Label(InMission ? "FIELD CONTRACT" : "HOME / OPERATIONS", heading);
            GUILayout.Label($"Health {Mathf.CeilToInt(Player.Health)} / 100    Credits {Mission.Credits}", label);
            GUILayout.Label($"Drones {Mission.Kills} / {Mission.Target}    Grenade {Player.GrenadeRemaining:0.0}s", label);
            GUILayout.Label(message, label);
            if (Vector3.Distance(Player.transform.position, terminal) < 3.5f)
            {
                string action = InMission ? "Extract" : Mission.Stage == MissionStage.Complete ? "Claim reward" : "Accept contract";
                if (GUILayout.Button(action + " [E / A]", GUILayout.Height(32))) Interact();
            }
            GUILayout.EndArea();
            GUI.Label(new Rect(20, Screen.height - 65, Screen.width - 40, 60), "WASD / Left stick: move  |  RMB: move to point  |  LMB / RT: fire\nQ / RB: grenade  |  Space / B: dash  |  E / A: interact  |  Right stick: aim", label);
        }
        void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
            if (shotSound != null) Destroy(shotSound);
            if (blastSound != null) Destroy(blastSound);
            if (Instance == this) Instance = null;
        }
        static AudioClip Tone(string name, float frequency, float duration)
        {
            int count = Mathf.CeilToInt(22050 * duration);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float envelope = 1f - (float)i / count;
                data[i] = Mathf.Sin(i * frequency * Mathf.PI * 2 / 22050) * envelope * envelope;
            }
            var clip = AudioClip.Create(name, count, 1, 22050, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
