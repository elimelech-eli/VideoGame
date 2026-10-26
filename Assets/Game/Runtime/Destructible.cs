using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VideoGame
{
    public static class CombatDamage
    {
        public static void Apply(Collider collider, float amount)
        {
            if (collider.GetComponentInParent<Drone>() is Drone enemy) enemy.Damage(amount);
            else collider.GetComponentInParent<Destructible>()?.Damage(amount);
        }
        public static void Area(Vector3 center, float radius, float damage, float playerDamage = 0)
        {
            var visited = new HashSet<Object>();
            foreach (var collider in Physics.OverlapSphere(center, radius))
            {
                var enemy = collider.GetComponentInParent<Drone>();
                var prop = collider.GetComponentInParent<Destructible>();
                if (enemy != null && visited.Add(enemy)) enemy.Damage(damage);
                else if (prop != null && visited.Add(prop)) prop.Damage(damage);
            }
            var player = Prototype.Instance.Player;
            if (playerDamage > 0 && Vector3.Distance(player.transform.position, center) < radius) player.Damage(playerDamage);
        }
    }

    public sealed class Destructible : MonoBehaviour
    {
        public bool IsWall;
        public bool IsDestroyed { get; private set; }
        public float Health { get; private set; } = 40;
        public void Damage(float amount)
        {
            if (IsDestroyed || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            if (Health > 0) { Prototype.Instance.Hit(transform.position + Vector3.up * .7f, Art.Amber); return; }
            // Set the guard before applying radial damage so barrel chains cannot recurse forever.
            IsDestroyed = true;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (IsWall) StartCoroutine(Topple());
            else
            {
                var center = transform.position + Vector3.up * .7f;
                Prototype.Instance.Explosion(center, 4.5f);
                CombatDamage.Area(center, 4.5f, 85, 35);
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
        IEnumerator Topple()
        {
            Vector3 direction = transform.position - Prototype.Instance.Player.transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < .01f) direction = transform.forward;
            direction.Normalize();
            Quaternion start = transform.rotation;
            Vector3 axis = Vector3.Cross(Vector3.up, direction);
            var struck = new HashSet<Object>();
            for (float t = 0; t < 1; t += Time.deltaTime / .7f)
            {
                transform.rotation = Quaternion.AngleAxis(Mathf.SmoothStep(0, 86, t), axis) * start;
                if (t > .35f)
                {
                    Vector3 center = transform.position + direction * 1.3f + Vector3.up * .6f;
                    foreach (var collider in Physics.OverlapBox(center, new Vector3(1.25f, .9f, 1.5f), Quaternion.LookRotation(direction)))
                    {
                        var enemy = collider.GetComponentInParent<Drone>();
                        var player = collider.GetComponentInParent<Pilot>();
                        if (enemy != null && struck.Add(enemy)) enemy.Damage(100);
                        if (player != null && struck.Add(player)) player.Damage(35);
                    }
                }
                yield return null;
            }
            transform.rotation = Quaternion.AngleAxis(86, axis) * start;
            Prototype.Instance.Hit(transform.position + direction, new Color(.55f, .45f, .3f), 1);
            Destroy(gameObject, 18);
        }
    }
}
