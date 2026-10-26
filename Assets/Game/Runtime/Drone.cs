using System.Collections;
using UnityEngine;

namespace VideoGame
{
    public enum EnemyKind { Hunter, Gunner, Rusher, Bombardier }
    public sealed class Drone : MonoBehaviour
    {
        static readonly float[] SteeringAngles = {0, 60, -60, 90, -90};
        public EnemyKind Kind;
        public bool Ranged => Kind == EnemyKind.Gunner || Kind == EnemyKind.Bombardier;
        public float MoveSpeed => Kind == EnemyKind.Rusher ? 6.2f : Kind == EnemyKind.Bombardier ? 1.8f : Ranged ? 2 : 3.5f;
        public const float MaxHealth = 60;
        public float Health { get; private set; } = MaxHealth;
        public float LastHitTime { get; private set; } = float.NegativeInfinity;
        public bool HealthBarVisible => Health > 0 && Time.time - LastHitTime < 2;
        public ActorAnimation Motion => GetComponentInChildren<ActorAnimation>();
        public bool WindingUp { get; private set; }
        float attackAt;
        void LateUpdate()
        {
            foreach (var label in GetComponentsInChildren<TextMesh>()) label.transform.rotation = Quaternion.Euler(55,45,0);
        }
        void Update()
        {
            var game = Prototype.Instance;
            if (Health <= 0 || game == null || !game.InMission || game.Player.IsDead || WindingUp) return;
            Vector3 delta = game.Player.transform.position - transform.position; delta.y = 0;
            float distance = delta.magnitude;
            if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(delta);
            if (distance > 28) return;
            float range = Kind == EnemyKind.Bombardier ? 11 : Ranged ? 7 : 1.6f;
            // Permanent room walls require a route through a doorway. Breakable
            // cover can still be attacked, so enemy fire remains hazardous to it.
            bool blocked = Physics.Linecast(transform.position, game.Player.transform.position, out var obstruction, 1 << 9)
                && obstruction.collider.GetComponentInParent<Destructible>() == null;
            if (distance > range || blocked)
            {
                Vector3 path = game.PursuitTarget(transform.position, game.Player.transform.position) - transform.position;
                path.y = 0;
                foreach (float turn in SteeringAngles)
                {
                    Vector3 direction = Quaternion.Euler(0, turn, 0) * path.normalized;
                    Vector3 next = transform.position + direction * (MoveSpeed * Time.deltaTime);
                    if (Physics.CheckSphere(transform.position + direction * .8f, .5f, 1 << 9)) continue;
                    if (!Physics.Raycast(next + Vector3.up, Vector3.down, 3, 1 << 8)) continue;
                    transform.position = next; break;
                }
            }
            else if (Time.time >= attackAt)
            {
                attackAt = Time.time + (Kind == EnemyKind.Bombardier ? 3.4f : Ranged ? 1.5f : Kind == EnemyKind.Rusher ? .95f : 1.2f);
                StartCoroutine(Attack());
            }
        }
        IEnumerator Attack()
        {
            var game = Prototype.Instance;
            WindingUp = true;
            Motion.Act(Kind == EnemyKind.Bombardier ? "Cast" : Ranged ? "Fire" : "Melee");
            Vector3 target = game.Player.transform.position;
            yield return new WaitForSeconds(Ranged ? .15f : .3f);
            WindingUp = false;
            if (Health <= 0 || !game.InMission || game.Player.IsDead) yield break;
            Vector3 delta = game.Player.transform.position - transform.position; delta.y = 0;
            if (Kind == EnemyKind.Bombardier)
            {
                var shell = new GameObject("Bombardier telegraphed blast"); shell.transform.SetParent(transform.parent);
                shell.AddComponent<EnemyMortar>().Initialize(transform.position + Vector3.up, target);
            }
            else if (Ranged)
            {
                var bolt = new GameObject("Enemy bolt"); bolt.transform.SetParent(transform.parent);
                bolt.transform.position = transform.position;
                var trail = bolt.AddComponent<TrailRenderer>(); trail.time = .15f; trail.startWidth = .12f; trail.endWidth = .01f;
                trail.startColor = new Color(1, .2f, .1f); trail.endColor = new Color(1, .1f, .1f, 0);
                trail.sharedMaterial = Resources.Load<Material>("PrototypeTrace");
                bolt.AddComponent<EnemyBolt>().Direction = delta.normalized;
            }
            else if (delta.magnitude < 2.2f)
            {
                game.Player.Damage(Kind == EnemyKind.Rusher ? 12 : 9);
                game.Hit(game.Player.transform.position, Color.red);
            }
        }
        public void Damage(float amount)
        {
            if (Health <= 0 || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount); LastHitTime = Time.time;
            Prototype.Instance.Hit(transform.position, Color.white, .6f);
            if (Health > 0) { Motion.Act("Hit"); return; }
            Prototype.Instance.Mission.RegisterKill();
            StopAllCoroutines(); WindingUp = false;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var sign in GetComponentsInChildren<TextMesh>()) sign.gameObject.SetActive(false);
            Motion.Act("Death");
            Destroy(gameObject, 3);
        }
    }
    public sealed class EnemyBolt : MonoBehaviour
    {
        public Vector3 Direction;
        float life;
        void Update()
        {
            life += Time.deltaTime;
            Vector3 before = transform.position;
            Vector3 step = Direction.normalized * (8 * Time.deltaTime);
            var hits = Physics.RaycastAll(before, Direction.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                // Soldiers don't block allied fire. The first solid cover or player hit does.
                if (hit.collider.GetComponentInParent<Drone>() != null) continue;
                var player = hit.collider.GetComponentInParent<Pilot>();
                if (player != null) player.Damage(12);
                else hit.collider.GetComponentInParent<Destructible>()?.Damage(12, before, Direction);
                Prototype.Instance.Hit(hit.point, new Color(1, .2f, .1f));
                Destroy(gameObject); return;
            }
            transform.position += step;
            if (life > 4) Destroy(gameObject);
        }
    }
    public sealed class EnemyMortar : MonoBehaviour
    {
        public const float WarningDuration = 1.3f;
        public const float Radius = 2.7f;
        public Vector3 Target { get; private set; }
        public bool Detonated { get; private set; }
        float started;
        Vector3 launch;
        LineRenderer ring, arc;
        public void Initialize(Vector3 source, Vector3 target)
        {
            launch = source; Target = new Vector3(target.x, .08f, target.z); started = Time.time;
            ring = Line("AOE warning", 65, .09f);
            for (int i = 0; i < 65; i++) ring.SetPosition(i, Target + new Vector3(Mathf.Cos(i * Mathf.PI / 32), 0, Mathf.Sin(i * Mathf.PI / 32)) * Radius);
            arc = Line("Incoming shell", 2, .14f);
        }
        LineRenderer Line(string title, int count, float width)
        {
            var line = new GameObject(title).AddComponent<LineRenderer>(); line.transform.SetParent(transform);
            line.sharedMaterial = Resources.Load<Material>("PrototypeTrace"); line.positionCount = count;
            line.startWidth = line.endWidth = width; line.startColor = line.endColor = new Color(1, .15f, .05f); return line;
        }
        void Update()
        {
            if (ring == null) return;
            float t = Mathf.Clamp01((Time.time - started) / WarningDuration);
            float pulse = .08f + .05f * Mathf.Sin(t * 40); ring.startWidth = ring.endWidth = pulse;
            Vector3 point = Vector3.Lerp(launch, Target, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 5;
            arc.SetPosition(0, point); arc.SetPosition(1, point - Vector3.up * .35f);
            if (t < 1) return;
            Detonated = true;
            Prototype.Instance.Explosion(Target, Radius);
            CombatDamage.Area(Target, Radius, 45, 24);
            Destroy(gameObject);
        }
    }
}
