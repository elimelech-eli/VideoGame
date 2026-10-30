using UnityEngine;

namespace VideoGame
{
    // Presentation clock only: the Pilot's .16s movement/damage window remains authoritative.
    [DefaultExecutionOrder(50)]
    public sealed class DodgeVisual : MonoBehaviour
    {
        public const float Duration = .30f;
        public float Weight { get; private set; }
        public Vector3 Direction { get; private set; }
        public bool Active => Time.time < started + Duration;
        float started = -100;
        Transform hips, chest;
        ActorAnimation motion;
        Pilot pilot;
        readonly TrailRenderer[] trails = new TrailRenderer[2];
        void Awake()
        {
            motion = GetComponent<ActorAnimation>(); pilot = GetComponentInParent<Pilot>();
            foreach (var bone in GetComponentsInChildren<Transform>())
            {
                if (bone.name == "hips") hips = bone;
                if (bone.name == "chest") chest = bone;
            }
            for (int i = 0; i < trails.Length; i++)
            {
                var trail = new GameObject("Evade exhaust").AddComponent<TrailRenderer>();
                trail.transform.SetParent(transform, false);
                trail.sharedMaterial = Resources.Load<Material>("PrototypeTrace");
                trail.time = .10f; trail.minVertexDistance = .06f;
                trail.startWidth = .065f; trail.endWidth = 0;
                trail.startColor = new Color(.35f,.8f,1,.45f);
                trail.endColor = new Color(.15f,.5f,1,0);
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false; trail.emitting = false; trails[i] = trail;
            }
        }
        public void Begin(Vector3 direction)
        {
            Direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            started = Time.time;
            foreach (var trail in trails) { trail.emitting = false; trail.Clear(); }
        }
        void LateUpdate()
        {
            if (Time.timeScale == 0) return;
            float age = Time.time - started;
            bool active = Active && !motion.Dead && motion.CurrentState == "Dash";
            Weight = active ? Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.035f)) * (1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.16f)/.14f))) : 0;
            if (Weight > 0 && hips != null && chest != null)
            {
                // World-space lean follows the locked evade direction, independent of aim.
                var lean = Quaternion.AngleAxis(14*Weight, Vector3.Cross(Vector3.up, Direction));
                hips.rotation = lean * hips.rotation;
                chest.rotation = Quaternion.AngleAxis(5*Weight,Vector3.Cross(Vector3.up,Direction))*chest.rotation;
                hips.position -= Vector3.up * (.045f*Weight);
            }
            bool emit = active && age < .16f && Prototype.Instance?.Menus?.Settings.ReducedEffects != true;
            for (int i = 0; i < trails.Length; i++)
            {
                if (chest != null) trails[i].transform.position = chest.position + pilot.transform.right*((i==0?-.16f:.16f)) - pilot.transform.forward*.14f;
                trails[i].emitting = emit;
            }
        }
        public void Cancel()
        {
            started = -100; Weight = 0;
            foreach (var trail in trails) if (trail != null) { trail.emitting = false; trail.Clear(); }
        }
        void OnDisable() => Cancel();
    }
}
