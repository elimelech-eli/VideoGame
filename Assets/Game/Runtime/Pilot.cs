using UnityEngine;
using UnityEngine.InputSystem;

namespace VideoGame
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class Pilot : MonoBehaviour
    {
        public Camera View;
        public float Health = 100;
        public bool IsDead { get; private set; }
        public ActorAnimation Motion => GetComponentInChildren<ActorAnimation>();
        public float GrenadeRemaining => Mathf.Max(0, grenadeAt - Time.time);
        public float DashRemaining => Mathf.Max(0, dashAt - Time.time);
        CharacterController controller;
        Vector3? destination;
        Vector3 facing = Vector3.forward, dashDirection;
        float shotAt, grenadeAt, dashAt, dashUntil;
        bool controllerAim;
        void Awake() => controller = GetComponent<CharacterController>();
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            destination = null;
            dashUntil = 0;
        }
        Vector3 MousePoint()
        {
            if (Mouse.current == null) return transform.position + facing * 7;
            var ray = View.ScreenPointToRay(Mouse.current.position.ReadValue());
            return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance) ? ray.GetPoint(distance) : transform.position + facing * 7;
        }
        void Update()
        {
            if (IsDead) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;
            Vector2 input = Vector2.zero;
            if (keyboard != null)
                input = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > .04f) input = pad.leftStick.ReadValue();
            if (mouse?.rightButton.wasPressedThisFrame == true) destination = MousePoint();
            Vector3 cameraForward = View.transform.forward; cameraForward.y = 0; cameraForward.Normalize();
            Vector3 movement = View.transform.right * input.x + cameraForward * input.y;
            if (movement.sqrMagnitude > .01f) destination = null;
            else if (destination.HasValue)
            {
                movement = destination.Value - transform.position; movement.y = 0;
                if (movement.magnitude < .25f) { destination = null; movement = Vector3.zero; }
                else movement.Normalize();
            }
            movement = Vector3.ClampMagnitude(movement, 1);
            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > .1f || mouse.leftButton.wasPressedThisFrame)) controllerAim = false;
            if (pad != null && pad.rightStick.ReadValue().sqrMagnitude > .09f)
            {
                Vector2 aim = pad.rightStick.ReadValue(); facing = (View.transform.right * aim.x + cameraForward * aim.y).normalized; controllerAim = true;
            }
            if (!controllerAim)
            {
                Vector3 aim = MousePoint() - transform.position; aim.y = 0;
                if (aim.sqrMagnitude > .1f) facing = aim.normalized;
            }
            transform.rotation = Quaternion.LookRotation(facing);
            bool dash = keyboard?.spaceKey.wasPressedThisFrame == true || pad?.buttonEast.wasPressedThisFrame == true;
            if (dash && Time.time >= dashAt)
            {
                dashDirection = movement.sqrMagnitude > .01f ? movement.normalized : facing;
                dashUntil = Time.time + .16f; dashAt = Time.time + .8f; destination = null;
                Motion.Act("Dash");
            }
            Vector3 velocity = Time.time < dashUntil ? dashDirection * 22 : movement * 7;
            // Refuse steps beyond a floor edge; the same guard applies during a dash.
            Vector3 next = transform.position + velocity * Time.deltaTime;
            if (!Physics.Raycast(next + Vector3.up * 2, Vector3.down, 5, 1 << 8)) velocity = Vector3.zero;
            controller.Move((velocity + Vector3.down * 3) * Time.deltaTime);
            if (transform.position.y < -5) { Prototype.Instance.Mission.Abandon(); Prototype.Instance.BuildHub(); }
            bool fire = mouse?.leftButton.isPressed == true || pad?.rightTrigger.isPressed == true;
            if (fire && Time.time >= shotAt && Prototype.Instance.InMission)
            {
                shotAt = Time.time + .14f;
                Motion.Act("Fire");
                Vector3 start = transform.position + Vector3.up * .25f;
                Vector3 direction = (GunAimPoint(start) - start).normalized;
                Vector3 end = start + direction * 22;
                var hits = Physics.RaycastAll(start, direction, 22, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider.GetComponentInParent<Pilot>() == this) continue;
                    end = hit.point; CombatDamage.Apply(hit.collider, 20, start, direction);
                    Prototype.Instance.Hit(end, Color.cyan);
                    break;
                }
                Prototype.Instance.Trace(start, end, Color.cyan);
            }
            bool grenade = keyboard?.qKey.wasPressedThisFrame == true || pad?.rightShoulder.wasPressedThisFrame == true;
            if (grenade && Time.time >= grenadeAt && Prototype.Instance.InMission)
            {
                grenadeAt = Time.time + 4;
                Motion.Act("Cast");
                Vector3 target = controllerAim ? transform.position + AssistedDirection() * 7 : MousePoint();
                Vector3 delta = target - transform.position; delta.y = 0;
                target = transform.position + Vector3.ClampMagnitude(delta, 10); target.y = .2f;
                Prototype.Instance.Grenade(target);
            }
        }
        Vector3 AssistedDirection()
        {
            if (!controllerAim) return facing;
            float best = .94f;
            Vector3 result = facing;
            foreach (var drone in FindObjectsByType<Drone>(FindObjectsSortMode.None))
            {
                if (drone.Health <= 0) continue;
                Vector3 delta = drone.transform.position - transform.position; delta.y = 0;
                float dot = Vector3.Dot(facing, delta.normalized);
                if (delta.sqrMagnitude < 22 * 22 && dot > best) { best = dot; result = delta.normalized; }
            }
            return result;
        }
        Vector3 GunAimPoint(Vector3 muzzle)
        {
            if (!controllerAim && Mouse.current != null)
            {
                var ray = View.ScreenPointToRay(Mouse.current.position.ReadValue());
                var hits = Physics.RaycastAll(ray, 200, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider.GetComponentInParent<Pilot>() == this) continue;
                    if (hit.collider.GetComponentInParent<Drone>() != null || hit.collider.GetComponentInParent<Destructible>() != null) return hit.collider.bounds.center;
                    if (hit.collider.gameObject.layer == 8) return GroundClickTarget(hit.point);
                    return hit.point;
                }
                return MousePoint();
            }
            Collider target = null; float nearest = 22;
            foreach (var collider in Physics.OverlapSphere(transform.position, 22))
            {
                var enemy = collider.GetComponentInParent<Drone>();
                var prop = collider.GetComponentInParent<Destructible>();
                if ((enemy == null || enemy.Health <= 0) && (prop == null || prop.IsDestroyed)) continue;
                Vector3 delta = collider.bounds.center - transform.position; delta.y = 0;
                float distance = delta.magnitude;
                if (distance < nearest && Vector3.Dot(facing, delta.normalized) > .94f) { nearest = distance; target = collider; }
            }
            return target != null ? target.bounds.center : muzzle + facing * 22;
        }
        Vector3 GroundClickTarget(Vector3 point)
        {
            // Isometric players often click the feet/ground shadow rather than
            // the raised hitbox, especially with a round ranged-enemy collider.
            Collider target = null; float nearest = float.PositiveInfinity;
            foreach (var collider in Physics.OverlapSphere(point + Vector3.up * .6f, 2))
            {
                var enemy = collider.GetComponentInParent<Drone>();
                var prop = collider.GetComponentInParent<Destructible>();
                if ((enemy == null || enemy.Health <= 0) && (prop == null || prop.IsDestroyed)) continue;
                Vector3 delta = collider.bounds.center - point; delta.y = 0;
                float radius = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z) + .25f;
                if (delta.sqrMagnitude < radius * radius && delta.sqrMagnitude < nearest) { nearest = delta.sqrMagnitude; target = collider; }
            }
            return target != null ? target.bounds.center : point;
        }
        public void Damage(float amount)
        {
            if (IsDead || amount <= 0 || Time.time < dashUntil) return;
            Health = Mathf.Max(0, Health - amount);
            if (Health > 0) Motion.Act("Hit");
            else
            {
                IsDead = true; destination = null;
                controller.enabled = false; Motion.Act("Death");
                Prototype.Instance.BeginPlayerDeath();
            }
        }
        public bool Heal(float amount)
        {
            if (IsDead || amount <= 0 || Health >= 100) return false;
            Health = Mathf.Min(100, Health + amount); return true;
        }
        public void Revive()
        {
            IsDead = false; Health = 100; controller.enabled = true;
            shotAt = grenadeAt = dashAt = dashUntil = 0; destination = null;
            Motion.Revive();
        }
    }
}
