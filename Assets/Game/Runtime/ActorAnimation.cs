using UnityEngine;

namespace VideoGame
{
    public sealed class ActorAnimation : MonoBehaviour
    {
        public string CurrentState { get; private set; } = "Idle";
        public bool Dead { get; private set; }
        public Animation Player { get; private set; }
        Transform actor;
        Vector3 previous;
        float actionUntil;
        bool robot;
        float recoil;
        Vector3 recoilRest;
        public void Kick()
        {
            if (recoil <= 0) recoilRest = transform.localPosition;
            recoil = .045f;
        }
        void LateUpdate()
        {
            if (recoil <= 0 || Time.timeScale == 0) return;
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * .5f);
            transform.localPosition = recoilRest - transform.parent.InverseTransformDirection(actor.forward) * recoil;
        }
        void Awake()
        {
            actor = GetComponentInParent<Pilot>()?.transform;
            if (actor == null) actor = transform.parent.parent;
            previous = actor.position;
            Player = gameObject.AddComponent<Animation>();
            Player.cullingType = AnimationCullingType.AlwaysAnimate;
            string model = name.Replace("(Clone)", "").Trim();
            robot = model == "Crawler" || model == "Roller" || model == "Blaster" || model == "Lobber";
            foreach (var clip in Resources.LoadAll<AnimationClip>(robot ? "RobotAnimations/" + model : "Animations")) Player.AddClip(clip, clip.name);
            if (Player["Idle"] == null) throw new System.InvalidOperationException("Missing baked actor animations");
            foreach (string name in new[] {"Fire", "Hit", "Cast"})
            {
                if (robot) continue;
                Player[name].layer = 1;
                Player[name].wrapMode = WrapMode.Once;
                foreach (var bone in GetComponentsInChildren<Transform>())
                    if (bone.name == "chest") { Player[name].AddMixingTransform(bone, true); break; }
            }
            Player.Play("Idle");
            if (robot)
                foreach (string state in new[] {"Fire", "Hit", "Cast", "Melee", "Death", "Dash"})
                    Player[state].speed = Player[state].length / (state == "Death" ? 1.25f : state == "Melee" ? .65f : state == "Cast" ? .8f : state == "Dash" ? .35f : state == "Hit" ? .2f : .45f);
        }
        public void Act(string state)
        {
            if (Dead || Player[state] == null) return;
            if (Player[state].layer == 1)
            {
                Player.CrossFade(state, .025f); Player[state].time = 0; return;
            }
            if (state == "Death") Dead = true;
            if (Dead) foreach (AnimationState clip in Player) if (clip.layer == 1) Player.Stop(clip.name);
            CurrentState = state;
            actionUntil = Time.time + Player[state].length / Mathf.Max(.01f, Player[state].speed);
            Player.CrossFade(state, state == "Death" ? .08f : .045f);
            Player[state].time = 0;
        }
        public void Revive()
        {
            if (recoil > 0) transform.localPosition = recoilRest;
            recoil = 0;
            Dead = false; actionUntil = 0; previous = actor.position;
            CurrentState = "Idle"; Player.Play("Idle"); Player["Idle"].time = 0; Player.Sample();
        }
        void Update()
        {
            if (Time.timeScale == 0) return;
            Vector3 velocity = (actor.position - previous) / Mathf.Max(.001f, Time.deltaTime); velocity.y = 0;
            previous = actor.position;
            if (Dead || Time.time < actionUntil) return;
            string state = "Idle";
            if (velocity.magnitude > .2f && velocity.magnitude < 40)
            {
                Vector3 local = actor.InverseTransformDirection(velocity);
                state = Mathf.Abs(local.x) > Mathf.Abs(local.z) ? local.x > 0 ? "RunRight" : "RunLeft" : local.z > 0 ? "RunForward" : "RunBack";
                Player[state].speed = Mathf.Clamp(velocity.magnitude / 4.5f, .6f, 1.8f);
            }
            if (CurrentState == state) return;
            CurrentState = state; Player.CrossFade(state, .1f);
        }
    }
}
