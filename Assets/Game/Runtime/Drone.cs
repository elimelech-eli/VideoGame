using UnityEngine;

namespace VideoGame
{
    public sealed class Drone : MonoBehaviour
    {
        static readonly float[] SteeringAngles = {0, 60, -60, 90, -90};
        public bool Ranged;
        public const float MaxHealth = 60;
        public float Health { get; private set; } = MaxHealth;
        public float LastHitTime { get; private set; } = float.NegativeInfinity;
        public bool HealthBarVisible => Health > 0 && Time.time - LastHitTime < 2;
        float attackAt;
        void Update()
        {
            var game = Prototype.Instance;
            if (game == null || !game.InMission) return;
            Vector3 delta = game.Player.transform.position - transform.position; delta.y = 0;
            float distance = delta.magnitude;
            if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(delta);
            if (distance > 28) return;
            float range = Ranged ? 7 : 1.4f;
            if (distance > range)
            {
                foreach (float turn in SteeringAngles)
                {
                    Vector3 direction = Quaternion.Euler(0, turn, 0) * delta.normalized;
                    Vector3 next = transform.position + direction * ((Ranged ? 2 : 3.5f) * Time.deltaTime);
                    if (Physics.CheckSphere(transform.position + direction * .8f, .5f, 1 << 9)) continue;
                    if (!Physics.Raycast(next + Vector3.up, Vector3.down, 3, 1 << 8)) continue;
                    transform.position = next; break;
                }
            }
            else if (Time.time >= attackAt)
            {
                attackAt = Time.time + (Ranged ? 1.5f : .8f);
                if (Ranged)
                {
                    var bolt = new GameObject("Enemy bolt"); bolt.transform.SetParent(transform.parent); bolt.transform.position = transform.position + delta.normalized;
                    var trail = bolt.AddComponent<TrailRenderer>(); trail.time = .15f; trail.startWidth = .12f; trail.endWidth = .01f;
                    trail.startColor = new Color(1, .2f, .1f); trail.endColor = new Color(1, .1f, .1f, 0);
                    trail.sharedMaterial = Resources.Load<Material>("PrototypeTrace");
                    bolt.AddComponent<EnemyBolt>().Direction = delta.normalized;
                }
                else { game.Player.Damage(9); game.Hit(game.Player.transform.position, Color.red); }
            }
        }
        public void Damage(float amount)
        {
            if (Health <= 0 || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            LastHitTime = Time.time;
            Prototype.Instance.Hit(transform.position, Color.white, .6f);
            if (Health > 0) return;
            Prototype.Instance.Mission.RegisterKill();
            gameObject.SetActive(false);
            Destroy(gameObject);
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
            transform.position += Direction * (8 * Time.deltaTime);
            if (Physics.Raycast(before, Direction, out var hit, Vector3.Distance(before, transform.position)))
            {
                var hitPlayer = hit.collider.GetComponentInParent<Pilot>();
                if (hitPlayer != null) hitPlayer.Damage(12);
                else hit.collider.GetComponentInParent<Destructible>()?.Damage(12);
                Prototype.Instance.Hit(hit.point, new Color(1, .2f, .1f));
                Destroy(gameObject); return;
            }
            var player = Prototype.Instance.Player;
            Vector3 closest = Vector3.Lerp(before, transform.position, Mathf.Clamp01(Vector3.Dot(player.transform.position - before, transform.position - before) / Mathf.Max(.0001f, (transform.position - before).sqrMagnitude)));
            if (Vector3.Distance(closest, player.transform.position) < .75f)
            {
                player.Damage(12); Prototype.Instance.Hit(player.transform.position, Color.magenta); Destroy(gameObject);
            }
            if (life > 4) Destroy(gameObject);
        }
    }
}
