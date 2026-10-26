using UnityEngine;

namespace VideoGame
{
    public enum CombatCue { Gun, Metal, Robot, Blast, Break, Collapse }

    // Presentation only. No physics bodies, damage, gameplay RNG, or time-scale writes.
    public sealed class CombatFeedback : MonoBehaviour
    {
        public const int EffectCap = 24, VoiceCap = 12;
        public bool Enabled = true;
        public int ActiveEffects { get; private set; }
        public int ActiveVoices { get; private set; }
        public int EventCount { get; private set; }
        public CombatCue LastCue { get; private set; }
        public float Shake { get; private set; }
        sealed class Effect
        {
            public GameObject Root;
            public ParticleSystem Sparks, Smoke;
            public LineRenderer Line;
            public Light Light;
            public float Start, Duration, Radius;
            public int Priority;
            public bool Ring;
            public Color Color;
        }
        readonly Effect[] effects = new Effect[EffectCap];
        readonly AudioSource[] voices = new AudioSource[VoiceCap];
        readonly float[] voiceEnds = new float[VoiceCap];
        readonly int[] voicePriorities = new int[VoiceCap];
        readonly float[] voiceGains = new float[VoiceCap];
        readonly AudioClip[,] clips = new AudioClip[6, 2];
        int sequence;
        Material smokeMaterial;
        Prototype game;
        bool Reduced => game.Menus?.Settings.ReducedEffects == true;
        void Awake()
        {
            game = GetComponent<Prototype>();
            var particle = Resources.Load<Material>("Particle");
            smokeMaterial = new Material(particle);
            smokeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            for (int i = 0; i < EffectCap; i++)
            {
                var root = new GameObject("Pooled combat effect"); root.transform.SetParent(transform);
                var effect = new Effect { Root = root };
                effect.Sparks = Particles(root.transform, particle, false, (uint)(i + 1));
                effect.Smoke = Particles(root.transform, smokeMaterial, true, (uint)(i + 101));
                effect.Line = root.AddComponent<LineRenderer>();
                effect.Line.sharedMaterial = Resources.Load<Material>("PrototypeTrace");
                effect.Line.useWorldSpace = true; effect.Line.enabled = false;
                effect.Light = root.AddComponent<Light>(); effect.Light.color = Art.Amber;
                effect.Light.shadows = LightShadows.None; effect.Light.enabled = false;
                root.SetActive(false); effects[i] = effect;
            }
            for (int i = 0; i < VoiceCap; i++)
            {
                var source = new GameObject("Combat voice").AddComponent<AudioSource>();
                source.transform.SetParent(transform); source.playOnAwake = false;
                source.spatialBlend = 0; voices[i] = source;
            }
            for (int cue = 0; cue < 6; cue++)
                for (int variant = 0; variant < 2; variant++) clips[cue, variant] = Synthesize((CombatCue)cue, variant);
        }
        static ParticleSystem Particles(Transform parent, Material material, bool smoke, uint seed)
        {
            var system = new GameObject(smoke ? "Dust and smoke" : "Impact sparks").AddComponent<ParticleSystem>();
            system.transform.SetParent(parent); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.useAutoRandomSeed = false; system.randomSeed = seed;
            var main = system.main; main.playOnAwake = false; main.loop = false;
            main.maxParticles = smoke ? 12 : 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.gravityModifier = smoke ? -.06f : .8f;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.enabled = false;
            var fade = system.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var size = system.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, smoke ? .4f : 1, 1, smoke ? 1.7f : 0));
            var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.renderMode = smoke ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.3f; renderer.velocityScale = .06f;
            return system;
        }
        Effect Acquire(Vector3 point, float duration, int priority)
        {
            if (!Enabled || Time.timeScale == 0) return null;
            Effect chosen = null;
            foreach (var effect in effects)
            {
                if (!effect.Root.activeSelf) { chosen = effect; break; }
                if (effect.Priority <= priority && (chosen == null || effect.Start < chosen.Start)) chosen = effect;
            }
            if (chosen == null) return null; // Drop optional effects before stealing heavier feedback.
            chosen.Sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            chosen.Smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            chosen.Root.SetActive(true); chosen.Root.transform.position = point;
            chosen.Start = Time.time; chosen.Duration = duration; chosen.Priority = priority;
            chosen.Light.enabled = chosen.Line.enabled = chosen.Ring = false;
            chosen.Sparks.Play(); chosen.Smoke.Play();
            return chosen;
        }
        void Burst(Effect effect, int count, Color color, float speed, float size, bool smoke = false)
        {
            if (effect == null) return;
            var system = smoke ? effect.Smoke : effect.Sparks;
            int amount = Reduced ? Mathf.Min(count, 4) : count;
            for (int i = 0; i < amount; i++)
            {
                float angle = (i * 2.39996f + sequence * .7f);
                Vector3 direction = new Vector3(Mathf.Cos(angle), .3f + (i % 5) * .16f, Mathf.Sin(angle));
                var emit = new ParticleSystem.EmitParams {
                    position = Vector3.zero, velocity = direction * speed * (.5f + (i % 4) * .16f),
                    startColor = color, startSize = size * (.7f + (i % 3) * .3f),
                    startLifetime = smoke ? 1.1f + (i % 3) * .25f : .12f + (i % 6) * .065f
                };
                // EmitParams positions in world simulation space are world coordinates.
                emit.position = effect.Root.transform.position + (smoke ? new Vector3(Mathf.Cos(angle), .1f, Mathf.Sin(angle)) * size * .35f : Vector3.zero);
                system.Emit(emit, 1);
            }
        }
        public void Shot(Vector3 from, Vector3 to, Color color)
        {
            Cue(CombatCue.Gun, from, .32f, 0);
            var effect = Acquire(Vector3.MoveTowards(from, to, .45f), .09f, 0); if (effect == null) return;
            effect.Line.enabled = true; effect.Line.positionCount = 2;
            effect.Line.SetPosition(0, from); effect.Line.SetPosition(1, to);
            effect.Line.startWidth = .045f; effect.Line.endWidth = .008f;
            effect.Line.startColor = color; effect.Line.endColor = new Color(color.r, color.g, color.b, .45f);
            Burst(effect, 5, new Color(.7f, .95f, 1), 1.2f, Reduced ? .07f : .16f);
            Flash(effect, color, 2, 1);
        }
        public void SilentTrace(Vector3 from, Vector3 to, Color color)
        {
            var effect = Acquire(from, .18f, 1); if (effect == null) return;
            effect.Line.enabled = true; effect.Line.positionCount = 2;
            effect.Line.SetPosition(0, from); effect.Line.SetPosition(1, to);
            effect.Line.startWidth = .06f; effect.Line.endWidth = .015f;
            effect.Line.startColor = effect.Line.endColor = color;
        }
        public void Impact(Vector3 point, Color color, float strength = .3f)
        {
            var effect = Acquire(point, .55f, 1);
            Burst(effect, strength > 1 ? 24 : 9, color, strength > 1 ? 5 : 3, strength > 1 ? .18f : .07f);
        }
        public void Contact(Vector3 point, bool robot)
        {
            Impact(point, robot ? new Color(.3f, .85f, 1) : Art.Amber);
            Cue(robot ? CombatCue.Robot : CombatCue.Metal, point, .3f, 1);
        }
        public void Blast(Vector3 point, float radius)
        {
            Cue(CombatCue.Blast, point, .8f, 2); Kick(point, .14f);
            var effect = Acquire(point, 1.8f, 2); if (effect == null) return;
            Burst(effect, 38, Art.Amber, 6, .32f);
            Burst(effect, 8, new Color(.22f, .24f, .28f, .4f), .7f, radius * .5f, true);
            if (!Reduced)
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * .5f;
                    var fire = new ParticleSystem.EmitParams {
                        position = point + new Vector3(Mathf.Cos(angle), .15f, Mathf.Sin(angle)) * radius * .13f,
                        velocity = new Vector3(Mathf.Cos(angle), .8f, Mathf.Sin(angle)),
                        startColor = new Color(1, .48f, .08f, .8f), startSize = radius * .75f, startLifetime = .24f
                    };
                    effect.Smoke.Emit(fire, 1);
                }
            effect.Ring = !Reduced; effect.Radius = radius; effect.Color = Art.Amber;
            if (!Reduced)
            {
                effect.Line.enabled = true; effect.Line.positionCount = 49;
                effect.Line.startWidth = effect.Line.endWidth = .09f;
                Flash(effect, Art.Amber, radius * 2, 3);
            }
        }
        public void Break(Vector3 point, bool medical)
        {
            Cue(CombatCue.Break, point, .45f, 1);
            var effect = Acquire(point, 1.6f, 1);
            Burst(effect, 20, medical ? new Color(.2f, 1, .6f) : Art.Amber, 4, .14f);
            Burst(effect, 6, new Color(.22f, .25f, .28f, .3f), .5f, .7f, true);
        }
        public void Collapse(Vector3 point)
        {
            Cue(CombatCue.Collapse, point, .75f, 3); Kick(point, .09f);
            var effect = Acquire(point, 1.7f, 2);
            Burst(effect, 24, new Color(.75f, .57f, .32f), 4, .12f);
            Burst(effect, 10, new Color(.22f, .2f, .17f, .38f), .8f, 1.1f, true);
        }
        void Flash(Effect effect, Color color, float range, float intensity)
        {
            if (Reduced) return;
            int lights = 0;
            foreach (var slot in effects) if (slot.Light.enabled) lights++;
            if (lights >= 2) return;
            effect.Light.enabled = true; effect.Light.color = color;
            effect.Light.range = range; effect.Light.intensity = intensity;
        }
        void Kick(Vector3 point, float strength)
        {
            if (!Enabled || Reduced || Time.timeScale == 0 || game.Player == null) return;
            float distance = Vector3.Distance(point, game.Player.transform.position);
            Shake = Mathf.Min(.18f, Shake + strength * Mathf.Clamp01(1 - distance / 18));
        }
        public Vector3 CameraOffset(Camera camera)
        {
            if (!Enabled || Reduced || Time.timeScale == 0) return Vector3.zero;
            float scale = game.Menus?.Settings.ScreenShake ?? .5f;
            return (camera.transform.right * Mathf.Sin(Time.time * 71) + Vector3.up * Mathf.Sin(Time.time * 93)) * (Shake * scale);
        }
        void Cue(CombatCue cue, Vector3 point, float volume, int priority)
        {
            if (!Enabled || Time.timeScale == 0) return;
            EventCount++; LastCue = cue;
            int chosen = -1;
            for (int i = 0; i < VoiceCap; i++)
            {
                if (voiceEnds[i] <= Time.time) { chosen = i; break; }
                if (voicePriorities[i] <= priority && (chosen < 0 || voiceEnds[i] < voiceEnds[chosen])) chosen = i;
            }
            if (chosen < 0) return;
            var source = voices[chosen]; source.Stop();
            source.clip = clips[(int)cue, sequence++ % 2];
            Vector3 delta = game.Player != null ? point - game.Player.transform.position : Vector3.zero;
            source.panStereo = Mathf.Clamp(Vector3.Dot(delta, new Vector3(.707f, 0, -.707f)) / 12, -.7f, .7f);
            voiceGains[chosen] = volume * Mathf.Lerp(1, .15f, Mathf.Clamp01(delta.magnitude / 28));
            source.volume = voiceGains[chosen] * (game.Menus?.Settings.Master ?? 1) * (game.Menus?.Settings.Effects ?? 1);
            source.pitch = 1; source.Play();
            voicePriorities[chosen] = priority; voiceEnds[chosen] = Time.time + source.clip.length;
        }
        void Update()
        {
            if (!Enabled) { ResetAll(); return; }
            if (Time.timeScale == 0) return;
            Shake = Mathf.MoveTowards(Shake, 0, Time.deltaTime * .55f);
            ActiveEffects = ActiveVoices = 0;
            foreach (var effect in effects)
            {
                if (!effect.Root.activeSelf) continue;
                float age = Time.time - effect.Start;
                if (age >= effect.Duration) { effect.Root.SetActive(false); continue; }
                ActiveEffects++;
                effect.Light.intensity = Mathf.Max(0, (effect.Priority == 2 ? 3 : 1) * (1 - age / .12f));
                effect.Light.enabled &= !Reduced && age < .12f;
                if (effect.Ring && effect.Line.enabled)
                {
                    effect.Line.enabled = !Reduced && age < .28f;
                    float radius = Mathf.Lerp(.2f, effect.Radius, Mathf.Clamp01(age / .28f));
                    Color tint = effect.Color; tint.a = Mathf.Max(0, .65f * (1 - age / .28f));
                    effect.Line.startColor = effect.Line.endColor = tint;
                    for (int i = 0; i < 49; i++)
                    {
                        float angle = i * Mathf.PI / 24;
                        Vector3 center = effect.Root.transform.position; center.y = .08f;
                        effect.Line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
                    }
                }
            }
            float mix = (game.Menus?.Settings.Master ?? 1) * (game.Menus?.Settings.Effects ?? 1);
            for (int i = 0; i < VoiceCap; i++) voices[i].volume = voiceGains[i] * mix;
            for (int i = 0; i < VoiceCap; i++) if (voiceEnds[i] > Time.time)
            {
                ActiveVoices++;
            }
        }
        public void ResetAll()
        {
            foreach (var effect in effects) if (effect != null)
            {
                effect.Sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.Smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.Root.SetActive(false);
            }
            for (int i = 0; i < VoiceCap; i++) { if (voices[i] != null) voices[i].Stop(); voiceEnds[i] = 0; }
            ActiveEffects = ActiveVoices = 0; Shake = 0;
        }
        public static AudioClip Synthesize(CombatCue cue, int variant)
        {
            float length = cue == CombatCue.Gun ? .16f : cue == CombatCue.Blast ? .85f : cue == CombatCue.Collapse ? .9f : .24f;
            int count = Mathf.CeilToInt(length * 22050); var data = new float[count];
            var noise = new System.Random(7919 + (int)cue * 31 + variant * 101);
            float low = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / 22050f, n = (float)noise.NextDouble() * 2 - 1;
                low = Mathf.Lerp(low, n, .13f);
                float tail = Mathf.Exp(-t * (cue == CombatCue.Blast || cue == CombatCue.Collapse ? 5 : 22));
                float hz = cue == CombatCue.Gun ? 155 : cue == CombatCue.Blast ? 48 : cue == CombatCue.Collapse ? 65 : cue == CombatCue.Robot ? 370 : 760;
                float body = Mathf.Sin(2 * Mathf.PI * (hz * (1 + variant * .06f) * t + 8 * (1 - Mathf.Exp(-t * 30))));
                float transient = n * Mathf.Exp(-t * 95) * .4f;
                float texture = (cue == CombatCue.Blast || cue == CombatCue.Collapse ? low : n) * .5f;
                data[i] = Mathf.Clamp((body * .35f + texture) * tail + transient, -.9f, .9f) * Mathf.Min(1, i / 24f);
            }
            var clip = AudioClip.Create("Combat " + cue + " " + variant, count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }
        void OnDestroy()
        {
            foreach (var clip in clips) if (clip != null) Destroy(clip);
            if (smokeMaterial != null) Destroy(smokeMaterial);
        }
    }
}
