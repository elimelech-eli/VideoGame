using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;
public sealed class LabEnemyTests {
 Prototype game;
 [UnitySetUp] public IEnumerator Setup(){var old=Object.FindFirstObjectByType<Prototype>();if(old!=null)Object.Destroy(old.gameObject);yield return null;game=new GameObject("Lab enemy test").AddComponent<Prototype>();yield return null;game.NewSession();game.Menus.Settings.Biome=(int)BiomeId.BiotechLab;game.StartMission();game.Director.enabled=false;game.Player.enabled=false;foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None))d.enabled=false;}
 [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;Object.Destroy(game.gameObject);yield return null;}
 [UnityTest] public IEnumerator LabRolesHaveApprovedDistinctSkinsAndRealDeformation(){
 foreach(var kind in new[]{EnemyKind.Rusher,EnemyKind.Gunner}){
 var d=game.SpawnDrone(new Vector3(0,1,0),kind);d.enabled=false;var motion=d.Motion;motion.enabled=false;Assert.AreEqual(kind==EnemyKind.Rusher?"LabSpider":"LabHazmat",motion.name.Replace("(Clone)",""));
 var skins=d.GetComponentsInChildren<SkinnedMeshRenderer>();Assert.Greater(skins.Length,0);
 foreach(var r in skins)foreach(var m in r.sharedMaterials){Assert.IsNotNull(m.mainTexture);Assert.IsTrue(m.shader.isSupported);}
 var skin=skins.OrderByDescending(s=>s.sharedMesh.vertexCount).First();var a=new Mesh();var b=new Mesh();
 foreach(string state in new[]{"Idle","RunForward","RunBack","RunLeft","RunRight","Melee","Fire","Hit","Death"})Assert.IsNotNull(motion.Player[state],state);
 motion.Player.Play("RunForward");motion.Player["RunForward"].normalizedTime=.2f;motion.Player.Sample();skin.BakeMesh(a);var firstCenter=Art.RenderedBounds(d.gameObject).center;motion.Player["RunForward"].normalizedTime=.7f;motion.Player.Sample();skin.BakeMesh(b);
 Assert.Less(Vector3.Distance(firstCenter,Art.RenderedBounds(d.gameObject).center),.4f,"Locomotion bob must remain in world meters despite imported FBX scale");
 Assert.That(a.vertices.Zip(b.vertices,(x,y)=>Vector3.Distance(x,y)).Max(),Is.GreaterThan(.01f),"Actual weighted skin must move");
 var bounds=Art.RenderedBounds(d.gameObject);Assert.Less(bounds.size.y,3);Assert.Greater(bounds.size.y,.3f);Assert.IsNotNull(d.GetComponent<BoxCollider>());
 motion.Act("Death");Assert.IsTrue(motion.Dead);motion.Revive();Assert.IsFalse(motion.Dead);
 if(kind==EnemyKind.Gunner){var rifle=skins.Where(s=>s.name.StartsWith("Skin_Rifle_")).ToArray();Assert.Greater(rifle.Length,0);Assert.IsTrue(rifle.All(s=>s.bones.Any(bone=>bone.name=="R_hand")),"Rifle must follow the firing hand");}
 Object.Destroy(a);Object.Destroy(b);Object.Destroy(d.gameObject);yield return null;
 }
 }
 [UnityTest] public IEnumerator LabEnemiesStillDamageAndDieThroughCombatRules(){
 var d=game.SpawnDrone(game.Player.transform.position+Vector3.forward*1.3f,EnemyKind.Rusher);
 var before=game.Player.Health;d.enabled=true;yield return new WaitForSeconds(.65f);Assert.Less(game.Player.Health,before,"Rig replacement must not interrupt melee damage");
 d.Damage(Drone.MaxHealth);Assert.AreEqual(0,d.Health);Assert.IsTrue(d.Motion.Dead);Assert.IsFalse(d.GetComponentsInChildren<Collider>().Any(c=>c.enabled));
 }}
