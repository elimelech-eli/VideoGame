using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class CharacterScreenTests
{
    Prototype game;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);yield return null;
        game=new GameObject("Character screen test").AddComponent<Prototype>();yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(game!=null)Object.Destroy(game.gameObject);yield return null;}
    [UnityTest] public IEnumerator InventoryHardPauseStopsWorldAndReinforcementsAcrossNestedMenus()
    {
        game.StartMission();yield return null;
        var bolt=new GameObject("Pause regression bolt");bolt.transform.SetParent(game.transform);bolt.transform.position=new Vector3(0,1,6);bolt.AddComponent<EnemyBolt>().Direction=Vector3.back;
        var enemies=Object.FindObjectsByType<Drone>(FindObjectsSortMode.None);var positions=enemies.Select(e=>e.transform.position).ToArray();
        float spawn=game.Director.NextSpawnAt;float health=game.Player.Health;Vector3 projectile=bolt.transform.position;
        game.Equipment.Open();float clock=Time.time;
        Time.timeScale=1;Assert.AreEqual(0,game.Director.SpawnWave(),"Menu ownership must block direct spawning even if another component restores the clock.");Time.timeScale=0;
        game.Menus.Activate("inventory");
        yield return new WaitForSecondsRealtime(EncounterDirector.SpawnInterval+.4f);
        Assert.AreEqual(0,Time.timeScale);Assert.AreEqual(clock,Time.time,.03f);Assert.AreEqual(spawn,game.Director.NextSpawnAt);
        Assert.AreEqual(health,game.Player.Health);Assert.AreEqual(projectile,bolt.transform.position);
        Assert.AreEqual(enemies.Length,Object.FindObjectsByType<Drone>(FindObjectsSortMode.None).Length);
        for(int i=0;i<enemies.Length;i++)Assert.AreEqual(positions[i],enemies[i].transform.position);
        game.Menus.Resume();yield return new WaitForSeconds(.1f);Assert.AreEqual(1,Time.timeScale);Assert.AreNotEqual(projectile,bolt.transform.position);
    }
    [UnityTest] public IEnumerator ComparisonsDoNotMutateGearAndEitherHandCanBeChosen()
    {
        var screen=game.GetComponent<CharacterScreen>();game.Equipment.Open();
        var pistol=game.Equipment.Profile.Bag.First(i=>i.Definition=="pistol");string rifle=game.Equipment.Profile.Equipped[3].Id;
        Assert.IsTrue(screen.SelectItem(pistol.Id));Assert.AreEqual(GearSlot.MainHand,screen.CompareSlot);
        Assert.IsNull(screen.EquipRestriction);Assert.Less(screen.PreviewPower,game.Equipment.Profile.UsedPower);
        Assert.AreEqual(rifle,game.Equipment.Profile.Equipped[3].Id);Assert.IsTrue(game.Equipment.Profile.Bag.Contains(pistol));
        screen.ChooseHand(GearSlot.OffHand);Assert.IsNotNull(screen.EquipRestriction);Assert.IsFalse(screen.EquipSelected());
        screen.ChooseHand(GearSlot.MainHand);Assert.IsTrue(screen.EquipSelected());
        Assert.AreEqual("slot-MainHand",screen.FocusId,"Equipping must restore focus to a visible surviving slot.");
        var shield=game.Equipment.Profile.Bag.First(i=>i.Definition=="shield");screen.SelectItem(shield.Id);screen.ChooseHand(GearSlot.OffHand);Assert.IsTrue(screen.EquipSelected());
        Assert.AreEqual("shield",game.Equipment.Profile.Equipped[4].Definition);Assert.AreEqual(0,Time.timeScale);yield return null;
    }
    [UnityTest] public IEnumerator FiltersAndStorageKeepInventoryOwnershipAndMissionLocks()
    {
        var screen=game.GetComponent<CharacterScreen>();game.Equipment.Open();screen.SetFilter(CharacterFilter.Armor);Assert.AreEqual(0,screen.VisibleItems().Count);
        screen.SetFilter(CharacterFilter.Weapons);Assert.AreEqual(4,screen.VisibleItems().Count);
        screen.SetFilter(CharacterFilter.All);screen.SetStorage(CharacterStorage.Vendor);Assert.AreEqual(17,screen.VisibleItems().Count);
        game.Menus.Resume();yield return null;yield return null;game.StartMission();game.Equipment.Open();screen.SetStorage(CharacterStorage.Vendor);
        Assert.AreEqual(CharacterStorage.Inventory,screen.Storage);Assert.IsTrue(screen.Status.Contains("Nexus"));
        var item=game.Equipment.Profile.Bag[0];screen.SelectItem(item.Id);Assert.IsFalse(screen.EquipSelected());
        Assert.AreEqual(4,game.Equipment.Profile.Bag.Count);yield return null;
    }
    [UnityTest] public IEnumerator IconAtlasIsAvailableAndPrimitiveEquipmentIsRemoved()
    {
        Assert.IsNotNull(Resources.Load<Texture2D>("UI/GearIcons"));
        foreach(GearKind kind in System.Enum.GetValues(typeof(GearKind)))
        {
            var uv=CharacterScreen.IconUV(kind);Assert.GreaterOrEqual(uv.x,0);Assert.GreaterOrEqual(uv.y,0);Assert.LessOrEqual(uv.xMax,1.001f);Assert.LessOrEqual(uv.yMax,1.001f);
        }
        game.Equipment.RefreshVisuals();yield return null;
        Assert.IsFalse(game.Player.GetComponentsInChildren<Transform>().Any(t=>GearCatalog.All.Any(d=>t.name=="Equipped "+d.Name)));
    }
}
