using System;

namespace VideoGame.Core
{
    public enum MissionStage { Available, Active, Complete, Rewarded }

    public sealed class Mission
    {
        public MissionStage Stage { get; private set; }
        public int Kills { get; private set; }
        public int Target { get; }
        public int Credits { get; private set; }
        public Mission(int target = 8)
        {
            if (target < 1) throw new ArgumentOutOfRangeException(nameof(target));
            Target = target;
        }
        public static Mission RestoreHub(int target, MissionStage stage, int kills, int credits)
        {
            bool valid = stage == MissionStage.Complete || stage == MissionStage.Rewarded ? kills == target : stage == MissionStage.Available && kills == 0;
            if (!valid || credits < 0) throw new ArgumentException("Invalid Nexus mission state.");
            return new Mission(target) { Stage = stage, Kills = kills, Credits = credits };
        }
        public bool Accept()
        {
            if (Stage != MissionStage.Available && Stage != MissionStage.Rewarded) return false;
            Kills = 0;
            Stage = MissionStage.Active;
            return true;
        }
        public void RegisterKill()
        {
            if (Stage != MissionStage.Active) return;
            Kills++;
            if (Kills >= Target) Stage = MissionStage.Complete;
        }
        public bool ClaimReward()
        {
            if (Stage != MissionStage.Complete) return false;
            Credits += 100;
            Stage = MissionStage.Rewarded;
            return true;
        }
        public void Abandon()
        {
            if (Stage != MissionStage.Active && Stage != MissionStage.Complete) return;
            Kills = 0;
            Stage = MissionStage.Available;
        }
    }
}
