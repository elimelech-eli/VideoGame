using UnityEngine;

namespace VideoGame
{
    public sealed class Drone : MonoBehaviour
    {
        public bool Ranged;
        float health = 60, attackAt;
        void Update()
        {
            var game = Prototype.Instance;
            if (game == null || !game.InMission) return;
            Vector3 delta = game.Player.transform.position - transform.position; delta.y = 0;
            float distance = delta.magnitude;
            if (distance > 19) return;
            float range = Ranged ? 7 : 1.4f;
            if (distance > range)
            {
                Vector3 next = transform.position + delta.normalized * ((Ranged ? 2 : 3.5f) * Time.deltaTime);
                if (Physics.Raycast(next + Vector3.up, Vector3.down, 3, 1 << 8)) transform.position = next;
            }
            else if (Time.time >= attackAt)
            {
                attackAt = Time.time + (Ranged ? 1.5f : .8f);
                if (Ranged)
                {
                    var bolt = game.Shape("Enemy bolt", PrimitiveType.Sphere, transform.position + delta.normalized, Vector3.one * .3f, Color.magenta, transform.parent);
                    Destroy(bolt.GetComponent<Collider>());
                    bolt.AddComponent<EnemyBolt>().Direction = delta.normalized;
                }
                else { game.Player.Damage(9); game.Hit(game.Player.transform.position, Color.red); }
            }
        }
        public void Damage(float amount)
        {
            if (health <= 0) return;
            health -= amount;
            Prototype.Instance.Hit(transform.position, Color.white, .6f);
            if (health > 0) return;
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
