using UnityEngine;

namespace VideoGame
{
    public sealed class HealthPickup : MonoBehaviour
    {
        public const float Healing = 30;
        public bool Consumed { get; private set; }
        void Update()
        {
            var player = Prototype.Instance.Player;
            if ((player.transform.position - transform.position).sqrMagnitude < 1.6f * 1.6f) TryCollect(player);
        }
        public bool TryCollect(Pilot player)
        {
            if (Consumed || !player.Heal(Healing)) return false;
            Consumed = true;
            Prototype.Instance.Hit(transform.position, Color.green, .5f);
            gameObject.SetActive(false); Destroy(gameObject); return true;
        }
    }
}
