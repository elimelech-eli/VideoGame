using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VideoGame
{
    public static class CombatDamage
    {
        public static void Apply(Collider collider, float amount, Vector3? source = null, Vector3? incoming = null)
        {
            if (collider.GetComponentInParent<Drone>() is Drone enemy) enemy.Damage(amount);
            else collider.GetComponentInParent<Destructible>()?.Damage(amount, source, incoming);
        }
        public static void Area(Vector3 center, float radius, float damage, float playerDamage = 0)
        {
            var visited = new HashSet<Object>();
            foreach (var collider in Physics.OverlapSphere(center, radius))
            {
                var enemy = collider.GetComponentInParent<Drone>();
                var prop = collider.GetComponentInParent<Destructible>();
                if (enemy != null && visited.Add(enemy)) enemy.Damage(damage);
                else if (prop != null && visited.Add(prop)) prop.Damage(damage, center);
            }
            var player = Prototype.Instance.Player;
            if (playerDamage > 0 && Vector3.Distance(player.transform.position, center) < radius) player.Damage(playerDamage);
        }
    }

    public sealed class Destructible : MonoBehaviour
    {
        public bool IsWall;
        public bool IsMedical;
        public Vector3 FallDirection { get; private set; }
        public bool IsDestroyed { get; private set; }
        Vector3 lastImpact;
        public float Health { get; private set; } = 40;
        public void Damage(float amount, Vector3? source = null, Vector3? incoming = null)
        {
            if (IsDestroyed || amount <= 0) return;
            if (IsWall)
            {
                Vector3 impulse = incoming ?? (transform.position - (source ?? Prototype.Instance.Player.transform.position));
                impulse.y = 0;
                if (impulse.sqrMagnitude > .0001f) lastImpact = impulse.normalized;
            }
            Health = Mathf.Max(0, Health - amount);
            if (Health > 0) { Prototype.Instance.Hit(transform.position + Vector3.up * .7f, Art.Amber); return; }
            // Set the guard before applying radial damage so barrel chains cannot recurse forever.
            IsDestroyed = true;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (IsMedical)
            {
                Prototype.Instance.SpawnHealthPickup(transform.position + Vector3.up * .3f);
                Prototype.Instance.Hit(transform.position + Vector3.up * .5f, Color.green, .6f);
                gameObject.SetActive(false); Destroy(gameObject);
            }
            else if (IsWall)
            {
                FallDirection = lastImpact.sqrMagnitude > 0 ? lastImpact : transform.forward;
                StartCoroutine(Topple());
            }
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
            Vector3 direction = FallDirection;
            var shape = GetComponent<BoxCollider>();
            Quaternion start = transform.rotation;
            Vector3 axis = Vector3.Cross(Vector3.up, direction);
            var struck = new HashSet<Object>();
            // Telegraph the continuation of the killing impact, from the foot of
            // the panel toward the victim side. This uses the same vector as physics.
            Prototype.Instance.Trace(transform.position + Vector3.up * .08f,
                transform.position + direction * shape.size.y + Vector3.up * .08f, Art.Amber, false);
            for (float t = 0; t < 1; t += Time.deltaTime / .7f)
            {
                if (Time.timeScale == 0) { yield return null; continue; }
                transform.rotation = Quaternion.AngleAxis(Mathf.SmoothStep(0, 86, t), axis) * start;
                Physics.SyncTransforms();
                if (t > .35f)
                {
                    Vector3 center = transform.TransformPoint(shape.center);
                    Vector3 halfSize = Vector3.Scale(shape.size * .5f, transform.lossyScale);
                    foreach (var collider in Physics.OverlapBox(center, halfSize, transform.rotation))
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
