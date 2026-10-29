using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class LootVisualTests
{
    Prototype game;
    string directory;
    [UnitySetUp] public IEnumerator Setup()
    {
        var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);yield return null;
        game=new GameObject("Loot visual test").AddComponent<Prototype>();
        directory=Path.Combine(Path.GetTempPath(),"LootVisualTests-"+System.Guid.NewGuid());game.Menus.UseStorage(directory);yield return null;
        game.Menus.Settings.Biome=0;game.StartMission();Assert.IsTrue(game.InMission);game.Director.enabled=false;game.Player.enabled=false;
        foreach(var enemy in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(game!=null)Object.Destroy(game.gameObject);yield return null;
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }
    [UnityTest] public IEnumerator EveryDropUsesItsCategoryMeshAndIndependentRarityMarker()
    {
        var kinds=(GearKind[])System.Enum.GetValues(typeof(GearKind));var origin=game.Player.transform.position;
        for(int n=0;n<kinds.Length;n++)
        {
            var design=GearCatalog.All.First(d=>d.Kind==kinds[n]);
            var item=GearCatalog.Roll(new System.Random(n),1,(GearQuality)(n%5),design.Id);
            game.Equipment.SpawnDrop(origin+new Vector3((n%4-1.5f)*1.5f,0,1.8f+n/4*1.6f),item);
            var drop=Object.FindObjectsByType<GearPickup>(FindObjectsSortMode.None).Single(d=>d.Item==item);var visual=drop.GetComponent<LootVisual>();
            Assert.AreEqual(kinds[n].ToString(),visual.ModelType);Assert.IsNull(drop.GetComponent<MeshRenderer>(),"The interaction root must not render a cube.");
            Assert.IsTrue(drop.GetComponent<SphereCollider>().isTrigger);Assert.IsNull(drop.GetComponent<Rigidbody>());
            Assert.Greater(visual.Model.GetComponentsInChildren<MeshRenderer>().Length,0);Assert.IsEmpty(visual.Model.GetComponentsInChildren<Collider>());
            Assert.Less(Vector4.Distance(EquipmentSystem.QualityColor(item.Quality),visual.RarityRing.startColor),.008f,"LineRenderer stores vertex colors at byte precision.");
            var size=Art.RenderedBounds(visual.Model.gameObject,false).size;
            Assert.Greater(Mathf.Max(size.x,Mathf.Max(size.y,size.z)),.35f);Assert.LessOrEqual(Mathf.Max(size.x,Mathf.Max(size.y,size.z)),.81f);
        }
        game.Equipment.SpawnDrop(origin+new Vector3(2.25f,0,3.4f),null,25);
        var credit=Object.FindObjectsByType<GearPickup>(FindObjectsSortMode.None).Single(d=>d.Item==null);
        Assert.AreEqual("Credits",credit.GetComponent<LootVisual>().ModelType);Assert.Less(Vector4.Distance(Art.Amber,credit.GetComponent<LootVisual>().RarityRing.startColor),.008f);
        yield return null;
        // The same scenario can be run with graphics enabled for the real camera
        // review. It uses only an isolated test profile, never the player's save.
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            yield return new WaitForSeconds(.2f);Directory.CreateDirectory("TestResults");game.CaptureFrame("TestResults/loot-game-camera.png");
        }
    }
    [UnityTest] public IEnumerator VisualMotionPausesAndPickingUpDestroysTheWholePresentationOnce()
    {
        var item=GearCatalog.Roll(new System.Random(1),1,GearQuality.Purple,"pistol");game.Equipment.SpawnDrop(game.Player.transform.position+Vector3.forward*2,item);
        var drop=Object.FindFirstObjectByType<GearPickup>();var visual=drop.GetComponent<LootVisual>();
        yield return new WaitForSeconds(.1f);var before=visual.Model.rotation;game.Equipment.Open();
        yield return new WaitForSecondsRealtime(.15f);Assert.AreEqual(before,visual.Model.rotation);
        game.Menus.Resume();yield return new WaitForSeconds(.1f);Assert.AreNotEqual(before,visual.Model.rotation);
        int count=game.Equipment.Profile.Bag.Count;Assert.IsTrue(game.Equipment.TryPickup(drop));Assert.IsFalse(game.Equipment.TryPickup(drop));
        Assert.AreEqual(count+1,game.Equipment.Profile.Bag.Count);yield return null;Assert.IsTrue(visual==null);
    }
}
