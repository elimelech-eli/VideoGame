using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;
using VideoGame.Core;

public sealed class UndercityBiomeTests
{
    GameObject root;
    [UnitySetUp]public IEnumerator Setup(){root=new GameObject("Undercity adapter fixture");root.transform.position=new Vector3(3000,0,3000);yield return null;}
    [UnityTearDown]public IEnumerator Cleanup(){Object.Destroy(root);yield return null;}
    void Build(int seed,int count)
    {
        var layout=MapLayout.Generate(seed,count);
        foreach(var room in layout)
        {
            var center=root.transform.position+new Vector3(room.X*20,0,room.Y*20);
            UndercityBiome.Deck(center,room.Size,room.Size,root.transform);
            foreach(var face in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
            {
                bool connected=layout.Any(r=>r.X==room.X+face.x&&r.Y==room.Y+face.y);
                var normal=new Vector3(face.x,0,face.y);var tangent=new Vector3(face.y,0,-face.x);var edge=center+normal*room.Size/2f;
                if(!connected)UndercityBiome.Wall(edge,tangent,room.Size,face,root.transform);
                else foreach(int side in new[]{-1,1})UndercityBiome.Wall(edge+tangent*side*(room.Size+4)/4f,tangent,(room.Size-4)/2f,face,root.transform);
            }
        }
        foreach(var a in layout)foreach(var b in layout)
        {
            if(!MapLayout.Adjacent(a,b)||(b.X<a.X)||(b.X==a.X&&b.Y<a.Y))continue;
            var from=new Vector3(a.X*20,0,a.Y*20);var to=new Vector3(b.X*20,0,b.Y*20);var dir=(to-from).normalized;
            var start=from+dir*a.Size/2f;var end=to-dir*b.Size/2f;float length=Vector3.Distance(start,end);var center=root.transform.position+(start+end)/2;
            UndercityBiome.Deck(center,dir.x!=0?length:4,dir.x!=0?4:length,root.transform);
            foreach(int side in new[]{-1,1})
            {
                var face=dir.x!=0?new Vector2Int(0,side):new Vector2Int(side,0);
                UndercityBiome.Wall(center+new Vector3(face.x,0,face.y)*2,dir,length,face,root.transform);
            }
        }
        Physics.SyncTransforms();
    }
    [UnityTest]public IEnumerator SourceArchitectureSupportsGeneratedSixTenTwentyRoomRoutes()
    {
        foreach(int count in new[]{6,10,20})
        {
            Build(404,count);yield return null;
            var layout=MapLayout.Generate(404,count);
            foreach(var a in layout)foreach(var b in layout)
            {
                if(!MapLayout.Adjacent(a,b))continue;
                var from=root.transform.position+new Vector3(a.X*20,1,a.Y*20);var to=root.transform.position+new Vector3(b.X*20,1,b.Y*20);var across=Vector3.Cross(Vector3.up,(to-from).normalized);
                for(int i=0;i<=20;i++)foreach(float lane in new[]{-1.3f,0,1.3f})
                {
                    var p=Vector3.Lerp(from,to,i/20f)+across*lane;
                    Assert.IsTrue(Physics.Raycast(p,Vector3.down,2,1<<8),"Missing source floor "+p);
                    Assert.IsFalse(Physics.CheckCapsule(p-Vector3.up*.5f,p+Vector3.up*.5f,.4f,1<<9),"Blocked generated route "+p);
                }
            }
            var controllerObject=new GameObject("Actual route controller");controllerObject.transform.SetParent(root.transform);controllerObject.transform.position=root.transform.position+Vector3.up*.05f;
            var cc=controllerObject.AddComponent<CharacterController>();cc.radius=.35f;cc.height=1.8f;cc.center=Vector3.up*.9f;cc.skinWidth=.02f;
            var neighbor=layout.First(r=>MapLayout.Adjacent(layout[0],r));var destination=root.transform.position+new Vector3(neighbor.X*20,.05f,neighbor.Y*20);var direction=(destination-controllerObject.transform.position).normalized;
            for(int i=0;i<200;i++)cc.Move(direction*.1f);
            Assert.Less(Vector3.Distance(controllerObject.transform.position,destination),.2f,"Native controller blocked at doorway");
            foreach(Transform child in root.transform)Object.Destroy(child.gameObject);yield return null;
        }
    }
    [UnityTest]public IEnumerator SourceWallCollisionMatchesVisibleCutaway()
    {
        var at=root.transform.position;
        UndercityBiome.Wall(at,Vector3.right,7,Vector2Int.up,root.transform);
        UndercityBiome.Wall(at+Vector3.forward*8,Vector3.right,7,Vector2Int.down,root.transform);
        yield return null;Physics.SyncTransforms();
        foreach(float x in new[]{-3.4f,-2,-.5f,0,1,2.5f,3.4f})Assert.IsTrue(Physics.Raycast(at+new Vector3(x,1.5f,-2),Vector3.forward,4,1<<9),"Full wall gap "+x);
        Assert.IsFalse(Physics.Raycast(at+new Vector3(0,1.5f,6),Vector3.forward,4,1<<9),"Invisible wall above cutaway");
        Assert.IsTrue(Physics.Raycast(at+new Vector3(0,.5f,6),Vector3.forward,4,1<<9));
        foreach(var box in root.GetComponentsInChildren<BoxCollider>())Assert.AreEqual(Vector3.one,box.transform.localScale);
    }
    [UnityTest]public IEnumerator FourRecipesProtectCrossAndRespectExistingHazard()
    {
        // Dress uses world room coordinates, so keep this fixture at origin and far-away room cells.
        for(int i=0;i<4;i++)
        {
            var room=new Room(180+i,180,14);var center=new Vector3(room.X*20,0,room.Y*20);
            var hazard=new GameObject("Existing hazard");hazard.transform.SetParent(root.transform);hazard.layer=9;hazard.transform.position=center+new Vector3(5.6f,.5f,5.6f);hazard.AddComponent<BoxCollider>().size=new Vector3(2,1,2);Physics.SyncTransforms();
            UndercityBiome.Dress(room,i,0,0,root.transform);Physics.SyncTransforms();
            Assert.IsTrue(root.GetComponentsInChildren<BoxCollider>().Any(c=>c.name.StartsWith("Undercity / ")&&Vector3.Distance(c.bounds.center,center)<12),"Recipe produced no source dressing");
            var props=root.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Undercity / ")).ToArray();
            foreach(var prop in props)
            {
                var owners=Physics.OverlapBox(prop.bounds.center,prop.bounds.extents*.96f,Quaternion.identity,1<<9);
                Assert.IsFalse(owners.Any(c=>c!=prop),"Dressing overlaps source scenery/hazard: "+prop.name);
            }
            foreach(float lane in new[]{-1.3f,0,1.3f})for(int step=-6;step<=6;step++)
            {
                Assert.IsFalse(Physics.CheckCapsule(center+new Vector3(step,.5f,lane),center+new Vector3(step,1.5f,lane),.4f,1<<9));
                Assert.IsFalse(Physics.CheckCapsule(center+new Vector3(lane,.5f,step),center+new Vector3(lane,1.5f,step),.4f,1<<9));
            }
        }
        yield return null;
        Assert.AreEqual(4,root.GetComponentsInChildren<BiomeMotion>().Count());
        Assert.AreEqual(4,root.GetComponentsInChildren<UndercityPressurePulse>().Count());
    }
}
