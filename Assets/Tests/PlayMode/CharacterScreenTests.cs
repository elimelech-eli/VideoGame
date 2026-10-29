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
    [UnityTest] public IEnumerator QuickEquipReplacesMainFirstAndShieldAutomaticallyChoosesOffHand()
    {
        var screen=game.GetComponent<CharacterScreen>();game.Equipment.Open();var p=game.Equipment.Profile;
        string rifle=p.Equipped[3].Id;var pistols=p.Bag.Where(i=>i.Definition=="pistol").ToArray();
        Assert.IsTrue(screen.QuickEquip(pistols[0].Id));Assert.AreSame(pistols[0],p.Equipped[3]);Assert.IsTrue(p.Bag.Any(i=>i.Id==rifle));
        screen.SelectItem(pistols[1].Id);screen.ChooseHand(GearSlot.OffHand);
        Assert.IsTrue(screen.QuickEquip(pistols[1].Id));Assert.AreSame(pistols[1],p.Equipped[3]);Assert.IsTrue(p.Bag.Contains(pistols[0]));
        var shield=p.Bag.First(i=>i.Definition=="shield");Assert.IsTrue(screen.SelectItem(shield.Id));Assert.AreEqual(GearSlot.OffHand,screen.CompareSlot);
        screen.ChooseHand(GearSlot.MainHand);Assert.AreEqual(GearSlot.OffHand,screen.CompareSlot);Assert.IsTrue(screen.EquipSelected());
        Assert.IsTrue(screen.UnequipSlot(GearSlot.Helmet));Assert.IsNull(p.Equipped[0]);Assert.IsTrue(p.Valid());
        game.Menus.Resume();yield return null;yield return null;game.StartMission();Assert.IsTrue(game.InMission);game.Equipment.Open();
        var before=p.Bag.Select(i=>i.Id).ToArray();Assert.IsFalse(screen.QuickEquip(pistols[0].Id));Assert.IsFalse(screen.UnequipSlot(GearSlot.MainHand));CollectionAssert.AreEquivalent(before,p.Bag.Select(i=>i.Id));
    }
    [UnityTest] public IEnumerator VisibleMissionDropPreservesInstanceAndCanBePickedUpAgain()
    {
        game.StartMission();game.Equipment.Open();var screen=game.GetComponent<CharacterScreen>();var p=game.Equipment.Profile;
        var item=p.Bag[0];int count=p.Bag.Count;screen.SelectItem(item.Id);Assert.IsTrue(screen.DropSelected());
        Assert.AreEqual(count-1,p.Bag.Count);Assert.IsNull(screen.SelectedItem);Assert.IsFalse(screen.DropSelected());Assert.AreEqual(0,Time.timeScale);
        var drop=Object.FindObjectsByType<GearPickup>(FindObjectsSortMode.None).Single(d=>d.Item==item);
        Assert.IsTrue(game.Equipment.TryPickup(drop));Assert.IsFalse(game.Equipment.TryPickup(drop));Assert.AreEqual(count,p.Bag.Count);Assert.AreSame(item,p.Bag.Single(i=>i.Id==item.Id));yield return null;
    }
    [UnityTest] public IEnumerator EveryHandLoadoutHasFittedMeshesAndForwardFacingFirearms()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("Detailed/Prefabs/OperativeV03"));root.transform.position=new Vector3(1000,-1000,1000);
        var pose=root.AddComponent<Animation>();var clip=Resources.Load<AnimationClip>("OperatorAnimations/Idle");pose.AddClip(clip,"Idle");pose.Play("Idle");pose.Sample();pose.enabled=false;
        var loadout=new PreviewLoadout(root.transform);var p=game.Equipment.Profile;
        foreach(var main in new[]{"rifle","pistol","sword",null})foreach(var off in new[]{"pistol","sword","shield",null})
        {
            if(main=="rifle"&&off!=null)continue;
            p.Equipped[3]=main==null?null:GearCatalog.Roll(new System.Random(1),1,GearQuality.White,main);
            p.Equipped[4]=off==null?null:GearCatalog.Roll(new System.Random(2),1,GearQuality.White,off);
            loadout.Apply(p);yield return null;
            var weapons=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Preview ")).ToArray();
            Assert.AreEqual((main==null?0:1)+(off==null?0:1),weapons.Length,main+" / "+off);
            foreach(var weapon in weapons)
            {
                Assert.Greater(weapon.GetComponentsInChildren<MeshRenderer>().Length,0);
                var hand=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="hand_"+(weapon.name.Contains(" R ")?"R":"L"));
                Assert.Less(Vector3.Distance(hand.position,weapon.position),.001f);
                if(weapon.name.EndsWith("Rifle")||weapon.name.EndsWith("Handgun"))Assert.Greater(Vector3.Dot(weapon.forward,root.transform.forward),.8f);
            }
        }
        Object.Destroy(root);
    }
}
