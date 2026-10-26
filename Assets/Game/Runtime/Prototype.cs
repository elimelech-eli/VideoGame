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
        public Art Visuals { get; private set; }
        public Vector3 Terminal => terminal;
        Transform world;
        Camera view;
        Vector3 terminal;
        string message = "Welcome home. Approach the cyan terminal to accept a contract.";
        public string Message => message;
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
            Visuals = gameObject.AddComponent<Art>();
            Application.runInBackground = true;
            view = new GameObject("Isometric Camera").AddComponent<Camera>();
            view.transform.SetParent(transform);
            view.gameObject.AddComponent<AudioListener>();
            audioSource = gameObject.AddComponent<AudioSource>();
            shotSound = Tone("Pulse", 150, .07f);
            blastSound = Tone("Blast", 55, .3f);
            view.orthographic = true;
            view.orthographicSize = 10;
            view.backgroundColor = new Color(.015f, .02f, .045f);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.transform.rotation = Quaternion.Euler(55, 45, 0);
            var light = new GameObject("Key light").AddComponent<Light>();
            light.transform.SetParent(transform);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.color = new Color(.7f, .82f, 1);
            light.intensity = .9f;
            light.shadows = LightShadows.Soft;
            QualitySettings.shadowDistance = 60;
            QualitySettings.pixelLightCount = 4;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.16f, .2f, .28f);
            RenderSettings.reflectionIntensity = .3f;
            var player = Shape("Pilot", PrimitiveType.Capsule, Vector3.up, new Vector3(.8f, 1, .8f), Color.cyan, transform);
            Destroy(player.GetComponent<Collider>());
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2;
            controller.radius = .4f;
            controller.minMoveDistance = 0;
            Player = player.AddComponent<Pilot>();
            Player.View = view;
            player.GetComponent<Renderer>().enabled = false;
            var visual = Visuals.Model("Space/astronautB", player.transform, new Vector3(0, -1, 0), 1.9f, Art.Cyan, 180);
            player.AddComponent<ActorPresentation>().Visual = visual.transform;
            Visuals.Model("Blaster/blaster-e", player.transform, new Vector3(.35f, -.05f, .3f), .95f, Art.Cyan, 180);
            Visuals.PointLight(new Vector3(0, 2, 0), Art.Cyan, player.transform, 4);
            gameObject.AddComponent<GameHud>();
            BuildHub();
            Debug.Log("POC_READY: hub, player and mission terminal initialized.");
            string capture = System.Environment.GetEnvironmentVariable("POC_CAPTURE_SCREEN");
            if (!string.IsNullOrEmpty(capture)) StartCoroutine(CapturePresentation(capture));
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
                material.SetColor("_EmissionColor", color.maxColorComponent > .8f ? color * .7f : Color.black);
                material.SetFloat("_Metallic", .4f);
                material.SetFloat("_Glossiness", .32f);
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
            Visuals.Model("Station/computer-wide", world, new Vector3(0, 0, 5), 3, Art.Cyan, 180);
            Visuals.Strip(new Vector3(0, .02f, 3), new Vector3(4, .04f, .08f), Art.Cyan, world);
            Visuals.Sign("CONTRACT UPLINK", new Vector3(0, 3.2f, 5), .65f, Art.Cyan, world);
            Visuals.Sign("NEXUS  /  OPERATIONS", new Vector3(0, .06f, -4), .9f, new Color(.4f, .62f, .7f), world);
            Visuals.Model("Space/craft_speederA", world, new Vector3(-5.5f, .1f, -2.5f), 4.8f, Art.Cyan, -35);
            Visuals.Model("Station/table-display", world, new Vector3(5, 0, 1), 2.8f, Art.Cyan);
            Visuals.PointLight(new Vector3(0, 3, 5), Art.Cyan, world, 12);
            Visuals.PointLight(new Vector3(-6, 2, -3), Art.Amber, world, 9);
            DressRoom(Vector3.zero, 18, 0, true);
        }
        void Floor(Vector3 center, float size, Color color)
        {
            Shape("Floor", PrimitiveType.Cube, center + Vector3.down * .25f, new Vector3(size, .5f, size), color, world).layer = 8;
            var panels = new GameObject("Deck panels"); panels.transform.SetParent(world);
            int count = Mathf.RoundToInt(size / 2);
            for (int x = 0; x < count; x++)
                for (int z = 0; z < count; z++)
                    Visuals.Model("Station/floor-detail", panels.transform, center + new Vector3(-size/2 + 1 + x*2, -.12f, -size/2 + 1 + z*2), 1.96f, Art.Cyan, ((x + z) % 2) * 90);
            for (int i = -1; i <= 1; i += 2)
            {
                Visuals.Strip(center + new Vector3(i * (size / 2 - .1f), .02f, 0), new Vector3(.04f, .025f, size), new Color(.06f, .24f, .3f), world);
            }
        }
        void DressRoom(Vector3 center, float size, int index, bool hub = false)
        {
            float edge = size / 2 - 1;
            var accent = hub ? Art.Cyan : Art.Amber;
            // Low scenery leaves the central combat lanes and room-to-room joins clear.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 corner = center + new Vector3(side * edge, 0, edge);
                Visuals.Model("Space/machine_generatorLarge", world, corner, 2.4f, accent, 90);
                Visuals.Model("Station/container-tall", world, center + new Vector3(side * edge, 0, -edge), 1.8f, accent);
                Visuals.PointLight(corner + Vector3.up * 2.5f, accent, world, 7);
                Visuals.Strip(corner + Vector3.up * 1.6f, new Vector3(.08f, 1.4f, .08f), accent, world);
                for (int j = 0; j < 2; j++)
                    Visuals.Model("Station/container", world, center + new Vector3(side * (edge - j * 1.35f), 0, -edge), 1.2f, accent, j * 90);
            }
            if (hub)
            {
                for (int x = -6; x <= 6; x += 3)
                    Visuals.Model("Station/wall", world, center + new Vector3(x, 0, edge + .5f), 3, accent);
            }
            else
            {
                Visuals.Sign("SECTOR " + index.ToString("00"), center + new Vector3(0, .06f, 4.5f), .7f, new Color(.5f, .45f, .35f), world);
                var pipe = Visuals.Model("Station/pipe", world, center + new Vector3(edge, .3f, 1), 3, accent, 90);
                pipe.transform.localRotation = Quaternion.Euler(90, 90, 0);
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
            Shape("Extraction beacon", PrimitiveType.Cylinder, terminal - Vector3.up * .4f, new Vector3(2, .1f, 2), new Color(.08f, .4f, .3f), world);
            Visuals.Model("Station/table-display", world, terminal - Vector3.up * .5f, 2, Art.Cyan);
            Visuals.Sign("EXTRACTION", terminal + Vector3.up * 2, .6f, Art.Cyan, world);
            Visuals.PointLight(terminal + Vector3.up * 2, Art.Cyan, world);
            for (int i = 0; i < rooms.Count; i++)
            {
                Vector3 center = new Vector3(rooms[i].X * 14, 0, rooms[i].Y * 14);
                Floor(center, 14, i % 2 == 0 ? new Color(.1f, .09f, .16f) : new Color(.08f, .14f, .16f));
                DressRoom(center, 14, i);
                if (i == 0) continue;
                for (int j = 0; j < 2; j++)
                {
                    Vector3 point = center + new Vector3((float)random.NextDouble() * 8 - 4, 1, (float)random.NextDouble() * 8 - 4);
                    bool ranged = (i + j) % 3 == 0;
                    var enemy = Shape(ranged ? "Ranged drone" : "Hunter drone", ranged ? PrimitiveType.Sphere : PrimitiveType.Capsule, point, Vector3.one, ranged ? Color.magenta : new Color(1, .3f, .1f), world);
                    enemy.GetComponent<Renderer>().enabled = false;
                    var enemyArt = Visuals.Model(ranged ? "Space/turret_double" : "Space/astronautA", enemy.transform, new Vector3(0, ranged ? -.4f : -1, 0), ranged ? 1.7f : 1.85f, ranged ? new Color(1, .15f, .5f) : Art.Amber, 180);
                    var presentation = enemy.AddComponent<ActorPresentation>(); presentation.Visual = enemyArt.transform; presentation.Hover = ranged;
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
            var burst = effect.AddComponent<Burst>(); burst.Duration = .18f; burst.Growth = 2;
            for (int i = 0; i < 5; i++)
            {
                Vector3 delta = new Vector3(Mathf.Cos(i * 2.4f), .3f, Mathf.Sin(i * 2.4f));
                Trace(position, position + delta * radius, color, false);
            }
        }
        public void Trace(Vector3 from, Vector3 to, Color color, bool sound = true)
        {
            if (sound) audioSource.PlayOneShot(shotSound, .12f);
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
            grenade.GetComponent<Renderer>().enabled = false;
            Visuals.Model("Blaster/grenade-a", grenade.transform, Vector3.zero, .35f, Art.Amber);
            for (float t = 0; t < 1; t += Time.deltaTime * 2)
            {
                if (grenade == null) yield break;
                grenade.transform.position = Vector3.Lerp(start, target, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 4);
                yield return null;
            }
            if (grenade == null) yield break;
            Destroy(grenade);
            audioSource.PlayOneShot(blastSound, .3f);
            Hit(target, Art.Amber, 2);
            foreach (var collider in Physics.OverlapSphere(target, 4))
                collider.GetComponent<Drone>()?.Damage(80);
        }
        void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
            if (shotSound != null) Destroy(shotSound);
            if (blastSound != null) Destroy(blastSound);
            if (Instance == this) Instance = null;
        }
        IEnumerator CapturePresentation(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory, "hub-ui.png"));
            yield return new WaitForSeconds(1);
            StartMission();
            var rooms = MapLayout.Generate(Seed);
            Player.Teleport(new Vector3(rooms[1].X * 14, 1, rooms[1].Y * 14));
            foreach (var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None)) enemy.enabled = false;
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory, "field-ui.png"));
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
