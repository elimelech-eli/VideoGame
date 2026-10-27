using System;
using System.Collections.Generic;
using UnityEngine;

namespace VideoGame
{
    public sealed class EncounterDirector : MonoBehaviour
    {
        public const int PopulationCap = 12;
        public const float SpawnInterval = 6;
        public float NextSpawnAt { get; private set; }
        Prototype game;
        List<Vector3> rooms;
        System.Random random;
        public void Initialize(Prototype owner, List<Vector3> centers, int seed)
        {
            game = owner; rooms = centers; random = new System.Random(seed);
            NextSpawnAt = Time.time + SpawnInterval;
        }
        void Update()
        {
            if (Time.timeScale == 0 || game == null || !game.InMission || game.Player.IsDead || Time.time < NextSpawnAt) return;
            NextSpawnAt = Time.time + SpawnInterval;
            SpawnWave();
        }
        public int SpawnWave()
        {
            if (Time.timeScale == 0 || game == null || !game.InMission || game.Player.IsDead || rooms == null) return 0;
            int alive = 0; foreach (var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None)) if (enemy.Health > 0) alive++;
            int amount = Mathf.Min(alive == 0 ? 4 : 2, PopulationCap - alive);
            int spawned = 0;
            Vector3 nearest = rooms[0];
            foreach (var room in rooms) if ((room - game.Player.transform.position).sqrMagnitude < (nearest - game.Player.transform.position).sqrMagnitude) nearest = room;
            for (int i = 0; i < 80 && spawned < amount; i++)
            {
                Vector3 center = rooms[random.Next(rooms.Count)];
                if (Mathf.Abs(center.x - nearest.x) + Mathf.Abs(center.z - nearest.z) > VideoGame.Core.MapLayout.Spacing + .1f) continue;
                Vector3 point = center + new Vector3((float)random.NextDouble() * 10 - 5, 1, (float)random.NextDouble() * 10 - 5);
                float distance = Vector3.Distance(point, game.Player.transform.position);
                if (distance < 9 || distance > 27 || Vector3.Distance(point, game.Terminal) < 6) continue;
                Vector3 screen = game.Player.View.WorldToViewportPoint(point);
                if (screen.z > 0 && screen.x > -.05f && screen.x < 1.05f && screen.y > -.05f && screen.y < 1.05f) continue;
                if (!Physics.Raycast(point + Vector3.up, Vector3.down, 4, 1 << 8)) continue;
                bool occupied = false;
                foreach (var collider in Physics.OverlapSphere(point, 1))
                    if (collider.gameObject.layer == 9 || collider.GetComponentInParent<Drone>() != null) { occupied = true; break; }
                if (occupied) continue;
                game.SpawnDrone(point, (EnemyKind)VideoGame.Core.Deployment.Enemy(game.ActiveBiome,random.Next(6)));
                Physics.SyncTransforms();
                spawned++;
            }
            return spawned;
        }
    }
}
