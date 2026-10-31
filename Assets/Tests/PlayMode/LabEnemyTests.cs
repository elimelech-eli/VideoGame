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
 }
 [UnityTest] public IEnumerator DedicatedRosterReplacesEveryRoleWithoutLeakingAcrossBiomes(){
 // Exercise the actual mission spawn path and the same SpawnDrone entry used
 // by reinforcements, including a return to the Lab after generic-biome loads.
 foreach(var biome in new[]{BiomeId.BiotechLab,BiomeId.Transit,BiomeId.NeonStreets,BiomeId.RuinedFactory,BiomeId.CorporateOffice,BiomeId.Undercity,BiomeId.BiotechLab}){
 game.NewSession();game.StartMission((int)biome,6);game.Director.enabled=false;game.Player.enabled=false;
 foreach(var d in Object.FindObjectsByType<Drone>(FindObjectsSortMode.None)){
 d.enabled=false;
 if(biome==BiomeId.BiotechLab)Assert.That(d.Motion.name.Replace("(Clone)",""),Is.EqualTo(d.IsLabBoss?"LabBoss":d.Kind==EnemyKind.Hunter?"LabHeavy":d.Kind==EnemyKind.Bombardier?"LabAoe":d.Kind==EnemyKind.Gunner?"LabHazmat":"LabSpider"),"Initial Lab population must not contain generic bodies");
 }
 foreach(EnemyKind kind in System.Enum.GetValues(typeof(EnemyKind))){
 var d=game.SpawnDrone(new Vector3(0,1,0),kind);d.enabled=false;
 string expected=biome==BiomeId.BiotechLab?(kind==EnemyKind.Hunter?"LabHeavy":kind==EnemyKind.Bombardier?"LabAoe":kind==EnemyKind.Gunner?"LabHazmat":"LabSpider"):biome==BiomeId.Transit?(kind==EnemyKind.Hunter?"TransitHeavy":kind==EnemyKind.Bombardier?"TransitAoe":kind==EnemyKind.Gunner?"TransitGunner":"TransitMelee"):kind==EnemyKind.Hunter?"Crawler":kind==EnemyKind.Rusher?"Roller":kind==EnemyKind.Gunner?"Blaster":"Lobber";
 Assert.AreEqual(expected,d.Motion.name.Replace("(Clone)",""),biome+" / "+kind);
 Assert.AreEqual(kind,d.Kind,"A temporary shared skin must not replace the combat role");
 Assert.AreEqual(kind==EnemyKind.Gunner||kind==EnemyKind.Bombardier||((biome==BiomeId.BiotechLab||biome==BiomeId.Transit)&&kind==EnemyKind.Hunter),d.Ranged);
 Assert.IsNotNull(d.Motion.Player[kind==EnemyKind.Bombardier?"Cast":d.Ranged?"Fire":"Melee"]);
 Object.Destroy(d.gameObject);
 }
 yield return null;
 }
 }
}
