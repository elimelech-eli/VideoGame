using UnityEngine;

namespace VideoGame
{
    public sealed class HealthPickup : MonoBehaviour
    {
        public const float Healing = 30;
        public bool Consumed { get; private set; }
        void Update()
        {
            if (Prototype.Instance.Menus?.IsOpen == true) return;
            var player = Prototype.Instance.Player;
            if ((player.transform.position - transform.position).sqrMagnitude < 1.6f * 1.6f) TryCollect(player);
        }
        public bool TryCollect(Pilot player)
        {
            if (Prototype.Instance.Menus?.IsOpen == true || Consumed || !player.Heal(Healing)) return false;
            Consumed = true;
            Prototype.Instance.Hit(transform.position, Color.green, .5f);
            gameObject.SetActive(false); Destroy(gameObject); return true;
        }
    }
}
