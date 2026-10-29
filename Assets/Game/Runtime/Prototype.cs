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
        public List<Room> Layout { get; private set; }
        public Art Visuals { get; private set; }
        public Vector3 Terminal => terminal;
        public EncounterDirector Director { get; private set; }
        public Soundtrack Music { get; private set; }
        public Atmosphere Ambience { get; private set; }
        public MenuFlow Menus { get; private set; }
        public EquipmentSystem Equipment { get; private set; }
        Transform world;
        Camera view;
        Vector3 terminal;
        bool fixedPresentationCamera;
        string message = "Welcome home. Approach the cyan terminal to accept a contract.";
        public string Message => message;
        readonly List<Material> materials = new List<Material>();
        readonly Dictionary<Color, Material> surfaces = new Dictionary<Color, Material>();
        public CombatFeedback Feedback { get; private set; }
        public BiomeWorld Biomes { get; private set; }
        public Deployment ActiveDeployment { get; private set; }
        public BiomeId ActiveBiome => ActiveDeployment.Biome;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<Prototype>() == null)
            {
                var game = new GameObject("Prototype").AddComponent<Prototype>();
                bool capture = false;
                foreach(string variable in new[]{"POC_CAPTURE_SCREEN","POC_CAPTURE_ANIMATION","POC_CAPTURE_SECTORS","POC_CAPTURE_ROSTER","POC_CAPTURE_MENUS","POC_CAPTURE_FEEDBACK","POC_CAPTURE_BIOMES"})
                    capture |= !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(variable));
                if(!capture) game.Menus.OpenMain(false,true);
            }
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
            Feedback = gameObject.AddComponent<CombatFeedback>();
            Biomes = gameObject.AddComponent<BiomeWorld>();
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
            Music = gameObject.AddComponent<Soundtrack>();
            Ambience = gameObject.AddComponent<Atmosphere>(); Ambience.KeyLight = light;
            var player = Shape("Pilot", PrimitiveType.Capsule, Vector3.up, new Vector3(.8f, 1, .8f), Color.cyan, transform);
            Destroy(player.GetComponent<Collider>());
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2;
            controller.radius = .4f;
            controller.minMoveDistance = 0;
            Player = player.AddComponent<Pilot>();
            Player.View = view;
            Equipment = gameObject.AddComponent<EquipmentSystem>(); Equipment.Initialize(this);
            player.GetComponent<Renderer>().enabled = false;
            Visuals.Model("Detailed/Prefabs/OperativeV03", player.transform, new Vector3(0, -1, 0), 2.15f, Art.Cyan);
            Visuals.PointLight(new Vector3(0, 2, 0), Art.Cyan, player.transform, 4);
            gameObject.AddComponent<GameHud>();
            BuildHub();
            Menus = gameObject.AddComponent<MenuFlow>();
            Equipment.RefreshVisuals();
            Debug.Log("POC_READY: hub, player and mission terminal initialized.");
            if(!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("POC_CAPTURE_BIOMES")))gameObject.AddComponent<BiomeCapture>();
            string feedbackCapture = System.Environment.GetEnvironmentVariable("POC_CAPTURE_FEEDBACK");
            if (!string.IsNullOrEmpty(feedbackCapture)) StartCoroutine(CaptureFeedback(feedbackCapture));
            string capture = System.Environment.GetEnvironmentVariable("POC_CAPTURE_SCREEN");
            if (!string.IsNullOrEmpty(capture)) StartCoroutine(CapturePresentation(capture));
            string animationCapture = System.Environment.GetEnvironmentVariable("POC_CAPTURE_ANIMATION");
            if (!string.IsNullOrEmpty(animationCapture)) StartCoroutine(CaptureAnimations(animationCapture));
            string sectorsCapture = System.Environment.GetEnvironmentVariable("POC_CAPTURE_SECTORS");
            if (!string.IsNullOrEmpty(sectorsCapture)) StartCoroutine(CaptureSectors(sectorsCapture));
            string rosterCapture = System.Environment.GetEnvironmentVariable("POC_CAPTURE_ROSTER");
            if (!string.IsNullOrEmpty(rosterCapture)) StartCoroutine(CaptureRoster(rosterCapture));
        }
        IEnumerator CaptureFeedback(string directory)
        {
            // Staged presentation evidence only; normal mission/gameplay tests are separate.
            System.IO.Directory.CreateDirectory(directory);
            Time.captureFramerate = 60;
            yield return new WaitForSeconds(1);
            StartMission(); Director.enabled = false; Player.enabled = false;
            foreach (var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None)) enemy.enabled = false;
            yield return new WaitForSeconds(.5f);
            Vector3 point = Player.transform.position + new Vector3(2, 0, 2);
            Feedback.Shot(Player.transform.position + Vector3.up * .25f, point, Art.Cyan);
            Player.Motion.Act("Fire"); Player.Motion.Kick();
            yield return new WaitForSeconds(.025f);
            CaptureFrame(System.IO.Path.Combine(directory,"gunfire.png"));
            yield return new WaitForSeconds(.4f);
            Explosion(point, 4);
            yield return new WaitForSeconds(.065f);
            CaptureFrame(System.IO.Path.Combine(directory,"blast.png"));
            yield return new WaitForSeconds(.25f);
            CaptureFrame(System.IO.Path.Combine(directory,"blast-tail.png"));
            yield return new WaitForSeconds(1.6f);
            var wall = SpawnDestructible(point - Vector3.up, true);
            wall.Damage(40, point - Vector3.forward * 3, Vector3.forward);
            yield return new WaitForSeconds(.8f);
            CaptureFrame(System.IO.Path.Combine(directory,"collapse.png"));
            yield return new WaitForSeconds(.4f);
            Debug.Log("FEEDBACK_CAPTURE_COMPLETE: staged gunfire, blast and collapse.");
            Time.captureFramerate = 0;
        }
        public void CaptureFrame(string path)
        {
            // Render offscreen: hidden Windows players have no screenshot backbuffer.
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var previousTarget = view.targetTexture;
            var previousActive = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                view.targetTexture = target; view.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                view.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); Destroy(image);
            }
        }
        public void NewSession()
        {
            StopAllCoroutines();Mission=new Mission();Seed=104729;Equipment.ResetProfile();BuildHub();
        }
        public void EndSession()
        {
            StopAllCoroutines();Mission=new Mission();BuildHub();
        }
        public void RestoreCheckpoint(NexusCheckpoint data)
        {
            if(data==null || !data.Valid())throw new System.ArgumentException("Invalid Nexus checkpoint.");
            var restored=Mission.RestoreHub(data.Target,(MissionStage)data.Stage,data.Kills,data.Credits);
            StopAllCoroutines();Mission=restored;Seed=data.Seed;Equipment.Restore(data.Version==1?null:data.Equipment,data.Credits);BuildHub();
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
            Feedback?.ResetAll();Biomes?.ResetDressing();
            Director = null;
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            world = new GameObject("World").transform;
            world.SetParent(transform);
            Player.Teleport(new Vector3(0, 1, 0));
            Player.Revive();
        }
        public void BuildHub()
        {
            if(InMission && Equipment?.Field?.Active==true)Equipment.Return(false);
            InMission = false;
            Music.SetBattle(false); Ambience.SetBattle(false);
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
            Ambience.Room(Vector3.zero, world, true);
            StartCoroutine(RefreshReflections());
        }
        void Floor(Vector3 center, float size, Color color)
        {
            Shape("Floor", PrimitiveType.Cube, center + Vector3.down * .25f, new Vector3(size, .5f, size), color, world).layer = 8;
            if(InMission)Biomes.Deck(ActiveBiome,center,size,size,world);else Visuals.Deck(center, size, world);
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
            if (Player.IsDead || Menus?.BlocksGameplay == true) return;
            if (Equipment.Profile.UsedPower>Equipment.Profile.Capacity || InMission) return;
            Menus?.AutoSave();
            if (!Mission.Accept()) return;
            Seed++;
            Equipment.Begin(Seed,(int)(Menus?.Difficulty??Difficulty.Standard));
            ActiveDeployment=new Deployment(Seed,Menus?.Settings.Biome??-1,Menus?.Settings.SectorSize??0);
            InMission = true;
            Music.SetBiome(ActiveBiome);Ambience.SetBiome(ActiveBiome);
            Music.SetBattle(true); Ambience.SetBattle(true);
            ResetWorld();
            var rooms = Layout = MapLayout.Generate(Seed,ActiveDeployment.Rooms);
            var centers = new List<Vector3>();
            var random = new System.Random(Seed);
            terminal = new Vector3(-4, .5f, -4);
            Shape("Extraction beacon", PrimitiveType.Cylinder, terminal - Vector3.up * .4f, new Vector3(2, .1f, 2), new Color(.08f, .4f, .3f), world);
            Visuals.Model("Detailed/Prefabs/station", world, terminal - Vector3.up * .5f, 2, Art.Cyan);
            Visuals.Sign("EXTRACTION", terminal + Vector3.up * 2, .6f, Art.Cyan, world);
            Visuals.PointLight(terminal + Vector3.up * 2, Art.Cyan, world);
            int initialEnemies=0;
            for (int i = 0; i < rooms.Count; i++)
            {
                Vector3 center = new Vector3(rooms[i].X * MapLayout.Spacing, 0, rooms[i].Y * MapLayout.Spacing);
                centers.Add(center);
                Floor(center, rooms[i].Size, rooms[i].Theme == 0 ? new Color(.1f, .09f, .16f) : new Color(.08f, .14f, .16f));
                Ambience.Room(center, world, false);
                // Keep the central cross and all four door approaches clear.
                float corner = rooms[i].Size / 2f - 2.5f;
                int turn = random.Next(4);
                Quaternion dressing = Quaternion.Euler(0, turn * 90, 0);
                Biomes.Dress(ActiveBiome,rooms[i],i,Seed,turn,world);
                if (i != 0) SpawnDestructible(center + dressing * new Vector3(corner, 0, -corner), false);
                if (i != 0 && rooms[i].Theme != 2) SpawnDestructible(center + dressing * new Vector3(corner - 1.2f, 0, -corner), false);
                if (i != 0)
                {
                    var unstable = SpawnDestructible(center + dressing * new Vector3(-corner, 0, corner), true);
                    unstable.transform.rotation = dressing;
                    SpawnMedicalCache(center + dressing * new Vector3(-corner, 0, -corner));
                }
                Physics.SyncTransforms();
                if (i == 0) continue;
                for (int j = 0; j < (i < 4 ? 2 : 1) && initialEnemies<EncounterDirector.PopulationCap; j++)
                {
                    Vector3 point = center + new Vector3((float)random.NextDouble() * 8 - 4, 1, (float)random.NextDouble() * 8 - 4);
                    for (int attempt = 0; attempt < 20 && Physics.CheckSphere(point, .7f, 1 << 9); attempt++)
                        point = center + new Vector3((float)random.NextDouble() * 8 - 4, 1, (float)random.NextDouble() * 8 - 4);
                    SpawnDrone(point, (EnemyKind)Deployment.Enemy(ActiveBiome,i*2+j));initialEnemies++;
                }
            }
            var directorObject = new GameObject("Reinforcements"); directorObject.transform.SetParent(world);
            Director = directorObject.AddComponent<EncounterDirector>(); Director.Initialize(this, centers, Seed);
            DrawBoundaries(rooms);
            StartCoroutine(RefreshReflections());
            Menus?.MissionStarted();
            message = Deployment.Name(ActiveBiome)+" / "+rooms.Count+" rooms. Destroy 8 drones, then return to extraction.";
        }
        public Vector3 PursuitTarget(Vector3 from, Vector3 target)
        {
            if (!InMission || Layout == null) return target;
            Vector3 Center(int i) => new Vector3(Layout[i].X*MapLayout.Spacing,from.y,Layout[i].Y*MapLayout.Spacing);
            int Nearest(Vector3 position)
            {
                int best=0;
                for(int i=1;i<Layout.Count;i++) if((Center(i)-position).sqrMagnitude < (Center(best)-position).sqrMagnitude)best=i;
                return best;
            }
            int start=Nearest(from), end=Nearest(target);
            if(start==end)return target;
            var previous=new int[Layout.Count]; for(int i=0;i<previous.Length;i++)previous[i]=-1;
            var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
            while(queue.Count>0 && previous[end]<0)
            {
                int current=queue.Dequeue();
                for(int i=0;i<Layout.Count;i++) if(previous[i]<0 && MapLayout.Adjacent(Layout[current],Layout[i])) {previous[i]=current;queue.Enqueue(i);}
            }
            if(previous[end]<0)return target;
            int next=end;while(previous[next]!=start)next=previous[next];
            Vector3 normal=(Center(next)-Center(start)).normalized;
            Vector3 tangent=new Vector3(normal.z,0,-normal.x);
            Vector3 offset=from-Center(start);
            // Align with the doorway before crossing a room wall.
            if(Mathf.Abs(Vector3.Dot(offset,tangent))>1)
                return Center(start)+normal*Mathf.Clamp(Vector3.Dot(offset,normal),-Layout[start].Size/2f+1,Layout[start].Size/2f-1);
            return Center(next);
        }
        public Drone SpawnDrone(Vector3 point, bool ranged)
            => SpawnDrone(point, ranged ? EnemyKind.Gunner : EnemyKind.Hunter);
        public Drone SpawnDrone(Vector3 point, EnemyKind kind)
        {
            bool ranged = kind == EnemyKind.Gunner || kind == EnemyKind.Bombardier;
            var enemy = Shape(ranged ? "Ranged drone" : "Hunter drone", ranged ? PrimitiveType.Sphere : PrimitiveType.Capsule, point, Vector3.one, ranged ? Color.magenta : Art.Amber, world);
            enemy.GetComponent<Renderer>().enabled = false;
            string model = kind == EnemyKind.Hunter ? "Crawler" : kind == EnemyKind.Rusher ? "Roller" : kind == EnemyKind.Gunner ? "Blaster" : "Lobber";
            float size = kind == EnemyKind.Bombardier ? 2.6f : kind == EnemyKind.Rusher ? 1.65f : kind == EnemyKind.Hunter ? 2.2f : 2.35f;
            var enemyArt = Visuals.Model("Detailed/Robots/Prefabs/" + model, enemy.transform, new Vector3(0, -1, 0), size, Art.Amber);
            var drone = enemy.AddComponent<Drone>(); drone.Kind = kind;
            if(InMission)drone.SetLevel(Equipment.RollEnemyLevel());
            var originalCollider = enemy.GetComponent<Collider>(); originalCollider.enabled = false; Destroy(originalCollider);
            var bounds = Art.RenderedBounds(enemyArt);
            var body = enemy.AddComponent<BoxCollider>(); body.center = enemy.transform.InverseTransformPoint(bounds.center); body.size = bounds.size;
            Color roleColor = kind == EnemyKind.Rusher ? new Color(1, .25f, .1f) : kind == EnemyKind.Bombardier ? new Color(.75f, .3f, 1) : ranged ? Color.magenta : Art.Amber;
            Visuals.Sign(kind.ToString().ToUpperInvariant(), new Vector3(point.x, bounds.max.y + .35f, point.z), .27f, roleColor, enemy.transform);
            return drone;
        }
        public Destructible SpawnMedicalCache(Vector3 position)
        {
            var cache = new GameObject("Medical supply cache"); cache.transform.SetParent(world); cache.transform.position = position; cache.layer = 9;
            var collider = cache.AddComponent<BoxCollider>(); collider.center = Vector3.up * .4f; collider.size = new Vector3(1.1f, .8f, .9f);
            Visuals.Model("Detailed/Prefabs/container", cache.transform, Vector3.zero, 1.1f, Color.green, solid: false);
            Visuals.Sign("+ MEDICAL", position + Vector3.up * 1.2f, .4f, Color.green, cache.transform);
            Visuals.Strip(position + new Vector3(0, .85f, 0), new Vector3(.65f, .035f, .16f), Color.green, cache.transform);
            Visuals.Strip(position + new Vector3(0, .86f, 0), new Vector3(.16f, .035f, .65f), Color.green, cache.transform);
            var destructible = cache.AddComponent<Destructible>(); destructible.IsMedical = true; return destructible;
        }
        public HealthPickup SpawnHealthPickup(Vector3 position)
        {
            var pickup = new GameObject("Medkit +30"); pickup.transform.SetParent(world); pickup.transform.position = position;
            Visuals.Model("Detailed/Prefabs/container", pickup.transform, Vector3.zero, .55f, Color.green, solid: false);
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
            var model = InMission
                ? Biomes.Place(ActiveBiome,wall?"Unstable":"Volatile",prop.transform,position,Quaternion.identity)
                : Visuals.Model(wall ? "Detailed/Prefabs/Bulkhead" : "Detailed/Prefabs/tank_2", prop.transform, Vector3.zero, wall ? 2.6f : 1.4f, Art.Amber, solid: false);
            if (wall)
            {
                // The mesh was only 1.83m tall inside a 2.4m damage box. Match the
                // visible panel to the collider so falling cover hits where it appears.
                var bounds = Art.RenderedBounds(model);
                model.transform.localScale = Vector3.Scale(model.transform.localScale,new Vector3(2.4f/bounds.size.x,2.4f/bounds.size.y,.3f/bounds.size.z));
                bounds = Art.RenderedBounds(model);
                model.transform.position -= Vector3.up * (bounds.min.y-position.y);
                bounds = Art.RenderedBounds(model);
                collider.center = prop.transform.InverseTransformPoint(bounds.center);
                collider.size = bounds.size;
            }
            Visuals.Sign(wall ? "UNSTABLE" : "VOLATILE", position + Vector3.up * (wall ? 2.8f : 1.8f), .35f, Art.Amber, prop.transform);
            var destructible = prop.AddComponent<Destructible>(); destructible.IsWall = wall;
            return destructible;
        }
        void DressSector(Vector3 center, Room room, int index)
        {
            string[] themes = { "REACTOR", "FREIGHT", "RELAY" };
            Color accent = room.Theme == 0 ? Art.Amber : room.Theme == 1 ? Art.Cyan : new Color(.65f,.3f,1);
            float edge = room.Size / 2f - 1.5f;
            string asset = room.Theme == 0 ? "tank_capsule" : room.Theme == 1 ? "container" : "control_terminal";
            Visuals.Model("Detailed/Prefabs/" + asset, world, center + new Vector3(edge,0,edge), 2, accent, 180);
            Visuals.PointLight(center + new Vector3(edge,2,edge), accent, world);
            Visuals.Sign(index == 0 ? "EXTRACTION / 00" : themes[room.Theme] + " / " + index.ToString("00"), center + new Vector3(0,.06f,-3), .5f, accent, world);
        }
        void DrawBoundaries(List<Room> rooms)
        {
            foreach (var room in rooms)
            {
                Vector3 center = new Vector3(room.X * MapLayout.Spacing,0,room.Y * MapLayout.Spacing);
                foreach (var side in new[] {Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                {
                    int neighbor = rooms.FindIndex(r => r.X == room.X + side.x && r.Y == room.Y + side.y);
                    Vector3 normal = new Vector3(side.x,0,side.y);
                    Vector3 tangent = new Vector3(side.y,0,-side.x);
                    float half = room.Size / 2f;
                    if (neighbor < 0) SectorWall(center + normal * half, tangent, room.Size, side);
                    else
                    {
                        float wing = (room.Size - MapLayout.PassageWidth) / 2f;
                        foreach (float sign in new[] {-1f,1f})
                            SectorWall(center + normal * half + tangent * sign * (MapLayout.PassageWidth / 2f + wing / 2), tangent, wing, side);
                        // Each connection is built once; square deck tiles make seamless passages.
                        if (side.x < 0 || side.y < 0) continue;
                        float end = MapLayout.Spacing - rooms[neighbor].Size / 2f;
                        float length = end - half;
                        var corridor = Shape("Passage floor",PrimitiveType.Cube,center + normal * (half + length / 2) + Vector3.down * .25f,
                            side.x != 0 ? new Vector3(length,.5f,MapLayout.PassageWidth) : new Vector3(MapLayout.PassageWidth,.5f,length),new Color(.08f,.12f,.16f),world);
                        corridor.layer = 8;
                        Biomes.Deck(ActiveBiome,center+normal*(half+length/2),side.x!=0?length:MapLayout.PassageWidth,side.x!=0?MapLayout.PassageWidth:length,world);
                        foreach (float sign in new[] {-1f,1f})
                        {
                            Vector3 point = center + normal * (half + length/2) + tangent * sign * MapLayout.PassageWidth/2f;
                            SectorWall(point,normal,length,new Vector2Int(side.y* (int)sign,-side.x*(int)sign),true);
                            Visuals.Strip(point + Vector3.up*.1f,side.x != 0 ? new Vector3(length,.06f,.08f) : new Vector3(.08f,.06f,length),BiomeProfile.Get(ActiveBiome).Accent,world);
                        }
                    }
                }
            }
        }
        void SectorWall(Vector3 center, Vector3 tangent, float length, Vector2Int facing,bool corridor=false)
        {
            Biomes.Wall(ActiveBiome,center,tangent,length,facing,world,corridor);
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
            if (Player.IsDead || Menus?.BlocksGameplay == true) return;
            bool interact = Keyboard.current?.eKey.wasPressedThisFrame == true || Gamepad.current?.buttonSouth.wasPressedThisFrame == true;
            if (interact && !Equipment.TryNearestPickup() && Vector3.Distance(Player.transform.position, terminal) < 3.5f) Interact();
            if (Player.Health <= 0) Player.Damage(1);
        }
        public void BeginPlayerDeath() => StartCoroutine(RecoverPlayer());
        IEnumerator RecoverPlayer()
        {
            message = "Operative lost. Emergency recall to the Nexus...";
            yield return new WaitForSeconds(1.6f);
            while (Menus?.IsOpen == true) yield return null;
            if (!Player.IsDead) yield break;
            Equipment.Return(false); Mission.Abandon(); BuildHub(); Menus?.AutoSave();
            message = Equipment.Notice;
        }
        public void Interact()
        {
            if (Player.IsDead || Menus?.BlocksGameplay == true) return;
            if (InMission)
            {
                if (Mission.Stage != MissionStage.Complete) { message = "Eliminate the contract targets before extracting."; return; }
                Equipment.Return(true); Equipment.CollectRewards(); BuildHub(); Menus?.AutoSave(); message = "Contract complete. Talk to the terminal to collect your reward.";
            }
            else if (Mission.ClaimReward()) { Equipment.ClaimReward(); Menus?.AutoSave(); message = "Reward received: 100 credits. Another contract is available."; }
            else StartMission();
        }
        void LateUpdate()
        {
            if (fixedPresentationCamera) return;
            Vector3 origin = Player.transform.position + new Vector3(-10.7f, 18, -10.7f);
            Vector3 offset = Feedback.CameraOffset(view);
            // Optional translation cannot move the camera through solid scenery.
            if (offset.sqrMagnitude > 0 && Physics.SphereCast(origin, .15f, offset.normalized, out _, offset.magnitude, 1 << 9)) offset = Vector3.zero;
            view.transform.position = origin + offset;
        }
        public void Hit(Vector3 position, Color color, float radius = .3f)
        {
            Feedback.Impact(position, color, radius);
        }
        public void Trace(Vector3 from, Vector3 to, Color color, bool sound = true)
        {
            if (sound) Feedback.Shot(from, to, color);
            else Feedback.SilentTrace(from, to, color);
        }
        public void Grenade(Vector3 target)
        {
            StartCoroutine(ThrowGrenade(target));
        }
        public void Explosion(Vector3 position, float radius)
        {
            Feedback.Blast(position, radius);
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
            while (Menus?.IsOpen == true) yield return null;
            if (grenade == null) yield break;
            Destroy(grenade);
            Explosion(target, 4);
            CombatDamage.Area(target, 4, 80);
        }
        void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
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
            Player.Teleport(new Vector3(rooms[1].X * MapLayout.Spacing, 1, rooms[1].Y * MapLayout.Spacing));
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
            CaptureFrame(System.IO.Path.Combine(directory, "nexus-game-camera.png"));
            Player.enabled = false; Player.transform.rotation = Quaternion.LookRotation(new Vector3(-1, 0, -1)) * Quaternion.Euler(0, 25, 0);
            view.orthographicSize = 3.6f;
            var motion = Player.Motion; motion.enabled = false; motion.Player.enabled = false;
            foreach (string clip in new[] {"Idle", "RunForward", "RunBack", "RunLeft", "RunRight", "Fire", "Melee", "Cast", "Dash", "Hit", "Death"})
            {
                foreach (float phase in new[] {.15f, .65f})
                {
                    motion.Player.Stop(); motion.Player.Play("Idle"); motion.Player["Idle"].normalizedTime = 0; motion.Player.Sample();
                    motion.Player.Play(clip); motion.Player[clip].normalizedTime = phase; motion.Player.Sample();
                    yield return new WaitForSeconds(.12f);
                    CaptureFrame(System.IO.Path.Combine(directory, clip + "-" + (int)(phase * 100) + ".png"));
                    yield return new WaitForSeconds(.12f);
                }
            }
            string cycleFolder = System.IO.Path.Combine(directory, "RunForward-cycle");
            System.IO.Directory.CreateDirectory(cycleFolder);
            for (int frame = 0; frame < 32; frame++)
            {
                motion.Player.Stop(); motion.Player.Play("RunForward");
                motion.Player["RunForward"].normalizedTime = frame / 32f; motion.Player.Sample();
                CaptureFrame(System.IO.Path.Combine(cycleFolder, frame.ToString("D2") + ".png"));
                yield return null;
            }
            motion.Player.enabled = true; motion.enabled = true; Player.enabled = true; motion.Revive(); view.orthographicSize = 7.5f;
        }
        IEnumerator CaptureSectors(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            StartMission(); Director.enabled=false;Player.enabled=false;
            foreach(var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
            fixedPresentationCamera=true;
            var hud=GetComponent<GameHud>();hud.enabled=false;
            Vector3 low=Vector3.zero,high=Vector3.zero;
            foreach(var room in Layout){var point=new Vector3(room.X*MapLayout.Spacing,0,room.Y*MapLayout.Spacing);low=Vector3.Min(low,point-Vector3.one*8);high=Vector3.Max(high,point+Vector3.one*8);}
            Vector3 midpoint=(low+high)/2;view.orthographicSize=Mathf.Max(high.z-low.z,high.x-low.x)*.6f;
            view.transform.position=midpoint+Vector3.up*100;view.transform.rotation=Quaternion.Euler(90,0,0);
            yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"sector-overview.png"));
            yield return new WaitForSeconds(.5f);
            hud.enabled=true;
            view.orthographicSize=7.5f;view.transform.rotation=Quaternion.Euler(50,45,0);
            var room1=Layout[1];Vector3 center=new Vector3(room1.X*MapLayout.Spacing,0,room1.Y*MapLayout.Spacing);
            Player.Teleport(center+Vector3.up);view.transform.position=Player.transform.position+new Vector3(-10.7f,18,-10.7f);
            yield return new WaitForSeconds(.5f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"sector-room.png"));
            yield return new WaitForSeconds(.5f);
            foreach(float side in new[]{-1f,1f})
            {
                Player.Teleport(new Vector3(0,1,side*1.8f));Player.Health=100;
                var wall=SpawnDestructible(Vector3.zero,true);
                view.transform.position=new Vector3(-7,10,-7);view.transform.LookAt(Vector3.up*.5f);view.orthographicSize=4;
                yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"wall-"+side+"-standing.png"));
                yield return new WaitForSeconds(.2f);wall.Damage(40,new Vector3(0,1,-side*2),Vector3.forward*side);
                yield return new WaitForSeconds(.45f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"wall-"+side+"-falling.png"));
                yield return new WaitForSeconds(.5f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"wall-"+side+"-fallen.png"));
                yield return new WaitForSeconds(.3f);Destroy(wall.gameObject);
            }
        }
        IEnumerator CaptureRoster(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            fixedPresentationCamera = true; view.orthographicSize = 5.8f;
            view.transform.position = Vector3.up + new Vector3(-10.7f,18,-10.7f);
            Player.Teleport(view.transform.right * -6 + Vector3.up); Player.enabled = false;
            Quaternion facing = Quaternion.LookRotation(new Vector3(-1,0,-1)); Player.transform.rotation = facing;
            var signRoot = new GameObject("Operative gallery label"); signRoot.transform.SetParent(Player.transform);
            Visuals.Sign("OPERATIVE", Player.transform.position + Vector3.up * 1.7f,.3f,Art.Cyan,signRoot.transform);
            for(int i=0;i<4;i++)
            {
                var enemy = SpawnDrone(view.transform.right * (-3+i*3) + Vector3.up,(EnemyKind)i);
                enemy.transform.rotation = facing;
            }
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"roster.png"));
            yield return new WaitForSeconds(.5f);
            Destroy(signRoot); Player.enabled=true; fixedPresentationCamera=false; view.orthographicSize=7.5f; BuildHub();
        }
    }
}
