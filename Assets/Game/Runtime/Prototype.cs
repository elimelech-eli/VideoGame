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
        public EncounterDirector Director { get; private set; }
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
            view.gameObject.AddComponent<CinematicGrade>();
            view.allowHDR = true;
            audioSource = gameObject.AddComponent<AudioSource>();
            shotSound = Tone("Pulse", 150, .07f);
            blastSound = Tone("Blast", 55, .3f);
            view.orthographic = true;
            view.orthographicSize = 7.5f;
            view.backgroundColor = new Color(.015f, .02f, .045f);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.transform.rotation = Quaternion.Euler(50, 45, 0);
            var light = new GameObject("Key light").AddComponent<Light>();
            light.transform.SetParent(transform);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.color = new Color(.7f, .82f, 1);
            light.intensity = .9f;
            light.shadows = LightShadows.Soft;
            QualitySettings.shadowDistance = 60;
            QualitySettings.pixelLightCount = 6;
            QualitySettings.antiAliasing = 4;
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
            var visual = Visuals.Model("Detailed/Prefabs/Operative", player.transform, new Vector3(0, -1, 0), 2.15f, Art.Cyan, 180);
            Visuals.PointLight(new Vector3(0, 2, 0), Art.Cyan, player.transform, 4);
            gameObject.AddComponent<GameHud>();
            BuildHub();
            Debug.Log("POC_READY: hub, player and mission terminal initialized.");
            string capture = System.Environment.GetEnvironmentVariable("POC_CAPTURE_SCREEN");
            if (!string.IsNullOrEmpty(capture)) StartCoroutine(CapturePresentation(capture));
            string animationCapture = System.Environment.GetEnvironmentVariable("POC_CAPTURE_ANIMATION");
            if (!string.IsNullOrEmpty(animationCapture)) StartCoroutine(CaptureAnimations(animationCapture));
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
            Director = null;
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            world = new GameObject("World").transform;
            world.SetParent(transform);
            Player.Teleport(new Vector3(0, 1, 0));
            Player.Revive();
        }
        public void BuildHub()
        {
            InMission = false;
            ResetWorld();
            Floor(Vector3.zero, 18, new Color(.07f, .11f, .16f));
            terminal = new Vector3(0, 1, 5);
            Visuals.Model("Detailed/Prefabs/control_terminal", world, new Vector3(0, 0, 5), 2.5f, Art.Cyan, 180);
            Visuals.Strip(new Vector3(0, .02f, 3), new Vector3(4, .04f, .08f), Art.Cyan, world);
            Visuals.Sign("CONTRACT UPLINK", new Vector3(0, 3.2f, 5), .65f, Art.Cyan, world);
            Visuals.Sign("NEXUS  /  OPERATIONS", new Vector3(0, .06f, -4), .9f, new Color(.4f, .62f, .7f), world);
            Visuals.Model("Detailed/Prefabs/container", world, new Vector3(-5.5f, 0, -2.5f), 4.8f, Art.Cyan, -15);
            Visuals.Model("Detailed/Prefabs/tank_control_panel", world, new Vector3(5, 0, 1), 2.8f, Art.Cyan);
            Visuals.PointLight(new Vector3(0, 3, 5), Art.Cyan, world, 12);
            Visuals.PointLight(new Vector3(-6, 2, -3), Art.Amber, world, 9);
            DressRoom(Vector3.zero, 18, 0, true);
            StartCoroutine(RefreshReflections());
        }
        void Floor(Vector3 center, float size, Color color)
        {
            Shape("Floor", PrimitiveType.Cube, center + Vector3.down * .25f, new Vector3(size, .5f, size), color, world).layer = 8;
            Visuals.Deck(center, size, world);
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
                Visuals.Model("Detailed/Prefabs/tank_capsule", world, corner, 3, accent, 90);
                Visuals.Model("Detailed/Prefabs/vent_2", world, center + new Vector3(side * edge, 0, -edge), 1.8f, accent);
                Visuals.PointLight(corner + Vector3.up * 2.5f, accent, world, 7);
                Visuals.Strip(corner + Vector3.up * 1.6f, new Vector3(.08f, 1.4f, .08f), accent, world);
                for (int j = 0; j < 2; j++)
                    Visuals.Model("Detailed/Prefabs/container", world, center + new Vector3(side * (edge - j * 1.35f), 0, -edge), 1.3f, accent, j * 90);
            }
            if (hub)
            {
                for (int x = -6; x <= 6; x += 3)
                    Visuals.Model("Detailed/Prefabs/Bulkhead", world, center + new Vector3(x, 0, edge + .5f), 3.1f, accent);
            }
            else
            {
                Visuals.Sign("SECTOR " + index.ToString("00"), center + new Vector3(0, .06f, 4.5f), .7f, new Color(.5f, .45f, .35f), world);
                Visuals.Model("Detailed/Prefabs/vent", world, center + new Vector3(edge, 0, 1), 2, accent, 90);
            }
        }
        public void StartMission()
        {
            if (Player.IsDead) return;
            if (!Mission.Accept()) return;
            InMission = true;
            ResetWorld();
            Seed++;
            var rooms = MapLayout.Generate(Seed);
            var centers = new List<Vector3>();
            var random = new System.Random(Seed);
            terminal = new Vector3(-4, .5f, -4);
            Shape("Extraction beacon", PrimitiveType.Cylinder, terminal - Vector3.up * .4f, new Vector3(2, .1f, 2), new Color(.08f, .4f, .3f), world);
            Visuals.Model("Detailed/Prefabs/station", world, terminal - Vector3.up * .5f, 2, Art.Cyan);
            Visuals.Sign("EXTRACTION", terminal + Vector3.up * 2, .6f, Art.Cyan, world);
            Visuals.PointLight(terminal + Vector3.up * 2, Art.Cyan, world);
            for (int i = 0; i < rooms.Count; i++)
            {
                Vector3 center = new Vector3(rooms[i].X * 14, 0, rooms[i].Y * 14);
                centers.Add(center);
                Floor(center, 14, i % 2 == 0 ? new Color(.1f, .09f, .16f) : new Color(.08f, .14f, .16f));
                DressRoom(center, 14, i);
                SpawnDestructible(center + new Vector3(4, 0, -3), false);
                SpawnDestructible(center + new Vector3(2, 0, -3), false);
                SpawnDestructible(center + new Vector3(-5.5f, 0, 2), true);
                SpawnMedicalCache(center + new Vector3(-3, 0, -3));
                Physics.SyncTransforms();
                if (i == 0) continue;
                for (int j = 0; j < 2; j++)
                {
                    Vector3 point = center + new Vector3((float)random.NextDouble() * 8 - 4, 1, (float)random.NextDouble() * 8 - 4);
                    for (int attempt = 0; attempt < 20 && Physics.CheckSphere(point, .7f, 1 << 9); attempt++)
                        point = center + new Vector3((float)random.NextDouble() * 8 - 4, 1, (float)random.NextDouble() * 8 - 4);
                    SpawnDrone(point, (EnemyKind)((i * 2 + j) % 4));
                }
            }
            var directorObject = new GameObject("Reinforcements"); directorObject.transform.SetParent(world);
            Director = directorObject.AddComponent<EncounterDirector>(); Director.Initialize(this, centers, Seed);
            DrawBoundaries(rooms);
            StartCoroutine(RefreshReflections());
            message = "Contract active: destroy 8 drones. Return to the green beacon to extract.";
        }
        public Drone SpawnDrone(Vector3 point, bool ranged)
            => SpawnDrone(point, ranged ? EnemyKind.Gunner : EnemyKind.Hunter);
        public Drone SpawnDrone(Vector3 point, EnemyKind kind)
        {
            bool ranged = kind == EnemyKind.Gunner || kind == EnemyKind.Bombardier;
            var enemy = Shape(ranged ? "Ranged drone" : "Hunter drone", ranged ? PrimitiveType.Sphere : PrimitiveType.Capsule, point, Vector3.one, ranged ? Color.magenta : Art.Amber, world);
            enemy.GetComponent<Renderer>().enabled = false;
            var enemyArt = Visuals.Model(ranged ? "Detailed/Prefabs/Gunner" : "Detailed/Prefabs/Hunter", enemy.transform, new Vector3(0, -1, 0), 2.05f, ranged ? new Color(1, .15f, .5f) : Art.Amber, 180);
            var drone = enemy.AddComponent<Drone>(); drone.Kind = kind;
            if (kind == EnemyKind.Rusher) enemyArt.transform.localScale *= .88f;
            if (kind == EnemyKind.Bombardier) enemyArt.transform.localScale *= 1.12f;
            Color roleColor = kind == EnemyKind.Rusher ? new Color(1, .25f, .1f) : kind == EnemyKind.Bombardier ? new Color(.75f, .3f, 1) : ranged ? Color.magenta : Art.Amber;
            Visuals.Sign(kind.ToString().ToUpperInvariant(), point + Vector3.up * 1.65f, .27f, roleColor, enemy.transform);
            return drone;
        }
        public Destructible SpawnMedicalCache(Vector3 position)
        {
            var cache = new GameObject("Medical supply cache"); cache.transform.SetParent(world); cache.transform.position = position; cache.layer = 9;
            var collider = cache.AddComponent<BoxCollider>(); collider.center = Vector3.up * .4f; collider.size = new Vector3(1.1f, .8f, .9f);
            Visuals.Model("Detailed/Prefabs/container", cache.transform, Vector3.zero, 1.1f, Color.green);
            Visuals.Sign("+ MEDICAL", position + Vector3.up * 1.2f, .4f, Color.green, cache.transform);
            Visuals.Strip(position + new Vector3(0, .85f, 0), new Vector3(.65f, .035f, .16f), Color.green, cache.transform);
            Visuals.Strip(position + new Vector3(0, .86f, 0), new Vector3(.16f, .035f, .65f), Color.green, cache.transform);
            var destructible = cache.AddComponent<Destructible>(); destructible.IsMedical = true; return destructible;
        }
        public HealthPickup SpawnHealthPickup(Vector3 position)
        {
            var pickup = new GameObject("Medkit +30"); pickup.transform.SetParent(world); pickup.transform.position = position;
            Visuals.Model("Detailed/Prefabs/container", pickup.transform, Vector3.zero, .55f, Color.green);
            Visuals.Sign("+30 HEALTH", position + Vector3.up * .7f, .35f, Color.green, pickup.transform);
            Visuals.PointLight(position + Vector3.up * .4f, Color.green, pickup.transform, 3);
            return pickup.AddComponent<HealthPickup>();
        }
        public Destructible SpawnDestructible(Vector3 position, bool wall)
        {
            var prop = new GameObject(wall ? "Unstable bulkhead" : "Volatile fuel barrel");
            prop.transform.SetParent(world); prop.transform.position = position; prop.layer = 9;
            var collider = prop.AddComponent<BoxCollider>();
            collider.center = wall ? new Vector3(0, 1.2f, 0) : new Vector3(0, .7f, 0);
            collider.size = wall ? new Vector3(2.4f, 2.4f, .3f) : new Vector3(.8f, 1.4f, .8f);
            Visuals.Model(wall ? "Detailed/Prefabs/Bulkhead" : "Detailed/Prefabs/tank_2", prop.transform, Vector3.zero, wall ? 2.6f : 1.4f, Art.Amber);
            Visuals.Sign(wall ? "UNSTABLE" : "VOLATILE", position + Vector3.up * (wall ? 2.8f : 1.8f), .35f, Art.Amber, prop.transform);
            var destructible = prop.AddComponent<Destructible>(); destructible.IsWall = wall;
            return destructible;
        }
        void DrawBoundaries(List<Room> rooms)
        {
            var cells = new HashSet<Vector2Int>(); foreach (var room in rooms) cells.Add(new Vector2Int(room.X, room.Y));
            foreach (var cell in cells)
            {
                foreach (var side in new[] {Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down})
                {
                    if (cells.Contains(cell + side)) continue;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 point = new Vector3(cell.x * 14, 0, cell.y * 14) + new Vector3(side.x * 7, 0, side.y * 7);
                        point += side.x != 0 ? new Vector3(0, 0, -5.25f + i * 3.5f) : new Vector3(-5.25f + i * 3.5f, 0, 0);
                        var wall = Visuals.Model("Detailed/Prefabs/Bulkhead", world, point, 3.45f, Art.Cyan, side.x != 0 ? 90 : 0);
                        if (side.x < 0 || side.y < 0) wall.transform.localScale = new Vector3(1, .25f, 1);
                    }
                }
            }
        }
        IEnumerator RefreshReflections()
        {
            Transform targetWorld = world;
            yield return null;
            if (targetWorld == null || !targetWorld.gameObject.activeInHierarchy || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var probe = new GameObject("Local reflections").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(targetWorld); probe.transform.position = Player.transform.position + Vector3.up * 3;
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.size = new Vector3(200, 50, 200); probe.resolution = 128;
            probe.boxProjection = true; probe.intensity = 1;
            probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(.17f, .2f, .26f);
            probe.RenderProbe();
        }
        void Update()
        {
            if (Player.IsDead) return;
            bool interact = Keyboard.current?.eKey.wasPressedThisFrame == true || Gamepad.current?.buttonSouth.wasPressedThisFrame == true;
            if (interact && Vector3.Distance(Player.transform.position, terminal) < 3.5f) Interact();
            if (Player.Health <= 0) Player.Damage(1);
        }
        public void BeginPlayerDeath() => StartCoroutine(RecoverPlayer());
        IEnumerator RecoverPlayer()
        {
            message = "Operative lost. Emergency recall to the Nexus...";
            yield return new WaitForSeconds(1.6f);
            if (!Player.IsDead) yield break;
            Mission.Abandon(); BuildHub();
            message = "Recovered at the Nexus. Contract failed; accept a new contract to retry.";
        }
        public void Interact()
        {
            if (Player.IsDead) return;
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
            view.transform.position = Player.transform.position + new Vector3(-10.7f, 18, -10.7f);
        }
        public void Hit(Vector3 position, Color color, float radius = .3f)
        {
            var effect = new GameObject("Impact particles"); effect.transform.SetParent(world); effect.transform.position = position;
            var particles = effect.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.loop = false; main.duration = .2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.12f, .4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2, radius > 1 ? 9 : 4);
            main.startSize = new ParticleSystem.MinMaxCurve(.04f, radius > 1 ? .6f : .14f);
            main.startColor = color; main.gravityModifier = .5f; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission; emission.rateOverTime = 0; emission.SetBursts(new[] {new ParticleSystem.Burst(0, (short)(radius > 1 ? 55 : 12))});
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .12f;
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(color, 0), new GradientColorKey(color * .4f, 1)}, new[] {new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1)}); fade.color = gradient;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Particle"); particles.Play();
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
        public void Explosion(Vector3 position, float radius)
        {
            audioSource.PlayOneShot(blastSound, .4f);
            Hit(position, Art.Amber, 2);
            var flash = new GameObject("Blast flash").AddComponent<Light>();
            flash.transform.SetParent(world); flash.transform.position = position + Vector3.up;
            flash.color = Art.Amber; flash.range = radius * 3; flash.intensity = 5;
            Destroy(flash.gameObject, .16f);
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
            Explosion(target, 4);
            CombatDamage.Area(target, 4, 80);
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
            Director.enabled = false;
            foreach (var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None)) enemy.enabled = false;
            yield return new WaitForSeconds(1);
            Drone closest = null;
            foreach (var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))
                if (closest == null || (enemy.transform.position - Player.transform.position).sqrMagnitude < (closest.transform.position - Player.transform.position).sqrMagnitude) closest = enemy;
            closest?.Damage(20);
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory, "field-ui.png"));
        }
        IEnumerator CaptureAnimations(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            Player.enabled = false; Player.transform.rotation = Quaternion.identity;
            view.orthographicSize = 3.6f;
            var motion = Player.Motion; motion.enabled = false; motion.Player.enabled = false;
            foreach (string clip in new[] {"Idle", "RunForward", "RunLeft", "Fire", "Melee", "Dash", "Death"})
            {
                foreach (float phase in new[] {.15f, .65f})
                {
                    motion.Player.Stop(); motion.Player.Play(clip); motion.Player[clip].normalizedTime = phase; motion.Player.Sample();
                    yield return new WaitForSeconds(.12f);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory, clip + "-" + (int)(phase * 100) + ".png"));
                    yield return new WaitForSeconds(.12f);
                }
            }
            motion.Player.enabled = true; motion.enabled = true; Player.enabled = true; motion.Revive(); view.orthographicSize = 7.5f;
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
