using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;
using Object=UnityEngine.Object;

public sealed class EquipmentPlayTests
{
    Prototype game;
    string directory;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);yield return null;
        game=new GameObject("Equipment test").AddComponent<Prototype>();
        directory=Path.Combine(Path.GetTempPath(),"EquipmentTests-"+Guid.NewGuid());game.Menus.UseStorage(directory);
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(game!=null)Object.Destroy(game.gameObject);yield return null;
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }
    void Equip(string definition,GearSlot slot)
    {
        var item=game.Equipment.Profile.Bag.First(i=>i.Definition==definition);
        Assert.IsNull(game.Equipment.Profile.Equip(item.Id,slot,false));game.Equipment.Changed();
    }
    void Field()
    {
        game.StartMission();Assert.IsTrue(game.InMission);game.Director.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
        game.Player.enabled=false;
    }
    [UnityTest] public IEnumerator SimultaneousAndAlternatingGunsHaveIndependentClocks()
    {
        Equip("pistol",GearSlot.MainHand);Equip("pistol",GearSlot.OffHand);Field();
        game.Player.Hands.Tick(true,false,.02f);Assert.AreEqual(1,game.Player.Hands.MainShots);Assert.AreEqual(0,game.Player.Hands.OffShots);
        game.Player.Hands.Tick(false,true,.02f);Assert.AreEqual(1,game.Player.Hands.OffShots);
        yield return new WaitForSeconds(.3f);game.Player.Hands.Tick(true,true,.02f);
        Assert.AreEqual(2,game.Player.Hands.MainShots);Assert.AreEqual(2,game.Player.Hands.OffShots);
        Assert.AreEqual("10 / 109",game.Player.Hands.State(0));Assert.AreEqual("10 / 109",game.Player.Hands.State(1));
    }
    [UnityTest] public IEnumerator ShieldCanOccupyMainHandAndBlocksOnlyWhileHeldFromFront()
    {
        Equip("shield",GearSlot.MainHand);Equip("pistol",GearSlot.OffHand);Field();
        game.Player.Hands.Tick(true,false,.1f);game.Player.Damage(20,game.Player.transform.position+game.Player.transform.forward*3);Assert.AreEqual(100,game.Player.Health);
        game.Player.Hands.Tick(false,false,.02f);game.Player.Damage(20);Assert.AreEqual(80,game.Player.Health);
        game.Player.Hands.Tick(true,false,.1f);game.Player.Damage(20,game.Player.transform.position-game.Player.transform.forward*3);Assert.AreEqual(60,game.Player.Health);
        yield return null;
    }
    [UnityTest] public IEnumerator PickupsRespectCapacityAndDropsCannotDuplicateOrRefillAmmo()
    {
        Equip("pistol",GearSlot.MainHand);Field();game.Player.Hands.Tick(true,false,.02f);string ammo=game.Player.Hands.State(0);
        var p=game.Equipment.Profile;
        while(p.Bag.Count<p.BagCapacity)p.Bag.Add(GearCatalog.Roll(new System.Random(),1,GearQuality.White,"sword"));
        var item=GearCatalog.Roll(new System.Random(1),1,GearQuality.Green,"pistol");item.Fresh=true;game.Equipment.SpawnDrop(game.Player.transform.position,item);
        var pickup=Object.FindObjectsByType<GearPickup>(FindObjectsSortMode.None).First(d=>d.Item==item);
        Assert.IsFalse(game.Equipment.TryPickup(pickup));Assert.IsTrue(pickup.gameObject.activeSelf);
        Assert.IsTrue(game.Equipment.Drop(p.Bag[0].Id));Assert.IsTrue(game.Equipment.TryPickup(pickup));Assert.IsFalse(game.Equipment.TryPickup(pickup));Assert.AreEqual(ammo,game.Player.Hands.State(0));
        Assert.IsNotNull(p.Equip(item.Id,GearSlot.OffHand,true));yield return null;
    }
    [UnityTest] public IEnumerator SuccessfulExtractionPersistsGearAndQuestRewardEvenWithFullInventory()
    {
        Field();var p=game.Equipment.Profile;
        while(p.Bag.Count<p.BagCapacity)p.Bag.Add(GearCatalog.Roll(new System.Random(),1,GearQuality.White,"pistol"));
        game.Equipment.Field.EarnCredits(100);for(int n=0;n<8;n++)game.Mission.RegisterKill();
        game.Interact();Assert.IsFalse(game.InMission);Assert.AreEqual(100,p.Credits);Assert.AreEqual(1,p.Stash.Count);
        game.Interact();Assert.AreEqual(200,p.Credits);game.Equipment.Open();game.Menus.Activate("inventory");
        Assert.IsTrue(game.Menus.Save(1));var saved=game.Menus.Store.Read(1).Data;
        Assert.AreEqual(2,saved.Version);Assert.IsTrue(saved.Equipment.Valid());Assert.AreEqual(10,saved.Equipment.Bag.Count);Assert.AreEqual(1,saved.Equipment.Stash.Count);
        Assert.IsTrue(game.Menus.Load(1));Assert.AreEqual(200,game.Equipment.Wallet);Assert.AreEqual(1,game.Equipment.Profile.Stash.Count);yield return null;
    }
    [UnityTest] public IEnumerator DeathRecallAppliesPenaltyOnceAndPersistsEarnedXP()
    {
        Field();game.Equipment.Field.EarnCredits(100);game.Equipment.Profile.AddXP(50);
        game.Player.Damage(10000);yield return new WaitForSeconds(1.9f);
        Assert.IsFalse(game.InMission);Assert.AreEqual(70,game.Equipment.Wallet);Assert.AreEqual(50,game.Equipment.Profile.Experience);
        Assert.IsTrue(game.Equipment.Profile.Bag.All(i=>i.Condition==75));Assert.IsTrue(game.Equipment.Profile.Equipped.Where(i=>i!=null).All(i=>i.Condition==100));
        Assert.IsFalse(game.Equipment.Return(false));Assert.AreEqual(70,game.Equipment.Wallet);Assert.IsTrue(game.Menus.Store.Read(0).Data.Equipment.Valid());
    }
    [UnityTest] public IEnumerator LegacyCheckpointsMigrateAndCorruptEquipmentFallsBackWithoutLoss()
    {
        var legacy=new NexusCheckpoint{Format="nexus-checkpoint",Version=1,Id=Guid.NewGuid().ToString(),SavedUtc=DateTime.UtcNow.ToString("O"),Credits=123,Target=8,Seed=17,Difficulty=1};
        game.Menus.Store.Save(1,legacy);Assert.IsTrue(game.Menus.Load(1));Assert.AreEqual(123,game.Equipment.Wallet);Assert.IsTrue(game.Equipment.Profile.Valid());
        yield return null;yield return null;Assert.IsTrue(game.Menus.Save(1));
        var current=game.Menus.Store.Read(1).Data;Assert.AreEqual(2,current.Version);
        current.Equipment.Bag[0].Condition=0;File.WriteAllText(Path.Combine(directory,"checkpoint-1.json"),JsonUtility.ToJson(current));
        var recovered=game.Menus.Store.Read(1);Assert.IsTrue(recovered.Recovered);Assert.AreEqual(1,recovered.Data.Version);
    }
    [UnityTest] public IEnumerator InventoryMenuPausesAndCannotEquipOrUseNexusServicesInField()
    {
        Field();game.Player.enabled=true;game.Equipment.Open();Assert.AreEqual(MenuPage.Equipment,game.Menus.Page);Assert.AreEqual(0,Time.timeScale);
        game.Menus.Activate("shop");Assert.AreEqual("Loadout",game.Equipment.Section);
        game.Menus.Activate("inventory");var item=game.Equipment.Profile.Bag[0];game.Menus.Activate(item.Id);
        Assert.IsFalse(game.Menus.Items().First(i=>i.Id.StartsWith("equip-")).Enabled);
        Assert.IsFalse(game.Menus.Items().First(i=>i.Id=="sell").Enabled);Assert.IsTrue(game.Menus.Items().First(i=>i.Id=="drop").Enabled);
        game.Menus.Resume();yield return null;Assert.AreEqual(1,Time.timeScale);
    }
}
