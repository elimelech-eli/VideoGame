using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using VideoGame;
using VideoGame.Core;

public sealed class SessionStoreTests
{
    string directory;
    SessionStore store;
    [SetUp] public void Setup(){directory=Path.Combine(Path.GetTempPath(),"VideoGameMenuTests-"+Guid.NewGuid());store=new SessionStore(directory);}
    [TearDown] public void Cleanup()
    {
        string full=Path.GetFullPath(directory);
        if(full.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase) && Directory.Exists(full))Directory.Delete(full,true);
    }
    static NexusCheckpoint Data(int credits=0,int stage=0,int kills=0)=>new NexusCheckpoint {
        Format="nexus-checkpoint",Version=1,Id=Guid.NewGuid().ToString(),SavedUtc=DateTime.UtcNow.ToString("O"),Credits=credits,Stage=stage,Kills=kills,Target=8,Seed=104730,Difficulty=1
    };
    [Test] public void AtomicSaveRecoversPreviousValidRevisionAndCanSaveAfterRecovery()
    {
        store.Save(1,Data(10));store.Save(1,Data(20));
        File.WriteAllText(Path.Combine(directory,"checkpoint-1.json"),"broken");
        var recovered=store.Read(1);Assert.IsTrue(recovered.Recovered);Assert.AreEqual(10,recovered.Data.Credits);
        store.Save(1,Data(30));
        File.WriteAllText(Path.Combine(directory,"checkpoint-1.json"),"broken again");
        Assert.AreEqual(10,store.Read(1).Data.Credits,"Saving after recovery must keep a valid backup");
        Assert.IsFalse(File.Exists(Path.Combine(directory,"checkpoint-1.json.tmp")));
    }
    [Test] public void UnsupportedAndIncompleteSavesAreRejectedAndNewestValidSlotWins()
    {
        store.Save(0,Data());var newest=Data(40);newest.SavedUtc=DateTime.UtcNow.AddMinutes(1).ToString("O");store.Save(2,newest);
        Assert.AreEqual(2,store.Latest().Index);
        var invalid=Data();invalid.Version=99;File.WriteAllText(Path.Combine(directory,"checkpoint-3.json"),JsonUtility.ToJson(invalid));
        Assert.IsNull(store.Read(3).Data);Assert.IsNotNull(store.Read(3).Error);
        File.WriteAllText(Path.Combine(directory,"checkpoint-3.json"),"{}");Assert.IsNull(store.Read(3).Data);
        invalid=Data();invalid.Stage=(int)MissionStage.Active;Assert.Throws<InvalidDataException>(()=>store.Save(1,invalid));
        Assert.Throws<ArgumentOutOfRangeException>(()=>store.Read(4));
    }
    [Test] public void PreferencesRoundTripIndependentlyOfCheckpointsAndRecoverFromCorruption()
    {
        var settings=new GameSettings {Master=.3f,Music=.4f,Effects=.5f,AimAssist=false,AimDeadzone=.35f,Quality=0,MusicMuted=true};
        store.SaveSettings(settings);store.Save(1,Data(100));
        var read=store.LoadSettings();Assert.AreEqual(.3f,read.Master);Assert.IsFalse(read.AimAssist);Assert.IsTrue(read.MusicMuted);
        settings.Master=.8f;store.SaveSettings(settings);
        File.WriteAllText(Path.Combine(directory,"settings.json"),"invalid");
        Assert.AreEqual(.3f,store.LoadSettings().Master);Assert.AreEqual(100,store.Read(1).Data.Credits);
    }
    [Test] public void RestoringAHubRewardPreservesExactlyOnceClaiming()
    {
        var pending=Mission.RestoreHub(8,MissionStage.Complete,8,100);
        Assert.IsTrue(pending.ClaimReward());Assert.AreEqual(200,pending.Credits);Assert.IsFalse(pending.ClaimReward());
        var claimed=Mission.RestoreHub(8,MissionStage.Rewarded,8,200);
        Assert.IsFalse(claimed.ClaimReward());Assert.AreEqual(200,claimed.Credits);Assert.IsTrue(claimed.Accept());
        Assert.Throws<ArgumentException>(()=>Mission.RestoreHub(8,MissionStage.Active,3,0));
    }
}
