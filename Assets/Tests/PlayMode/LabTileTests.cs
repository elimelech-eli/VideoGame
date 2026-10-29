using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VideoGame;

public sealed class LabTileTests
{
    [UnityTest] public IEnumerator LabKitHasMeterScaleAndConnectedClearWalkways()
    {
        var parent=new GameObject("Isolated lab test");parent.transform.position=new Vector3(2000,0,2000);
        var kit=LabTileReview.Assemble(parent.transform);
        try
        {
            yield return null;Physics.SyncTransforms();
            var floor=Resources.Load<GameObject>("LabReview/Prefabs/Floor4");
            Assert.IsNotNull(floor);var bounds=floor.GetComponent<BoxCollider>().size;
            Assert.That(bounds.x,Is.EqualTo(4).Within(.02f));Assert.That(bounds.z,Is.EqualTo(4).Within(.02f));
            Assert.Less(bounds.y,.2f,"Floor exported on the wrong axis");            var bank=Resources.Load<GameObject>("LabReview/Prefabs/ControlBank");
            var hardware=bank.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            Assert.IsNotNull(hardware.mainTexture,"Console hardware lost its source surface detail");
            Assert.IsNotNull(hardware.GetTexture("_BumpMap"),"Console recesses lost their normal detail");
            Assert.IsNotNull(hardware.GetTexture("_EmissionMap"),"Replacement displays lost their UV-matched artwork");
            Assert.IsTrue(hardware.IsKeywordEnabled("_EMISSION"),"Unity material validation disabled the powered display shader variant");
            foreach(var r in kit.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials)Assert.IsNotNull(m,r.name+" lost its material");
            foreach(float z in new[]{-2f,-5.9f,-6.1f,-9f,-13.9f,-14.1f,-18f,-20f,-24f})
            {
                Vector3 p=parent.transform.position+new Vector3(0,1,z);
                Assert.IsTrue(Physics.Raycast(p,Vector3.down,2,1<<8),"Missing ground at room/corridor seam "+z);
                Assert.IsFalse(Physics.CheckCapsule(p-Vector3.up*.25f,p+Vector3.up*.45f,.42f,1<<9),"Blocked central walkway "+z);
            }
            foreach(float x in new[]{-4f,4f})
                Assert.IsTrue(Physics.Raycast(parent.transform.position+new Vector3(x,1,-20),Vector3.down,2,1<<8),"Junction branch missing");
        }
        finally{Object.Destroy(parent);}
    }
    [UnityTest] public IEnumerator SourceFloorHasVisibleCoverageAndWallFacesIntoRoom()
    {
        var parent=new GameObject("Lab surface geometry test");parent.transform.position=new Vector3(2400,0,2400);
        try
        {
            var floor=Object.Instantiate(Resources.Load<GameObject>("LabReview/Prefabs/Floor4"),parent.transform);
            var wall=Object.Instantiate(Resources.Load<GameObject>("LabReview/Prefabs/Wall4"),parent.transform);wall.transform.localPosition=new Vector3(8,0,0);
            foreach(var box in parent.GetComponentsInChildren<BoxCollider>())box.enabled=false;
            foreach(var mesh in parent.GetComponentsInChildren<MeshFilter>())
            {mesh.gameObject.layer=10;mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;}
            yield return null;Physics.SyncTransforms();
            for(float x=-1.6f;x<1.7f;x+=.8f)for(float z=-1.6f;z<1.7f;z+=.8f)
                Assert.IsTrue(Physics.Raycast(parent.transform.position+new Vector3(x,1,z),Vector3.down,2,1<<10),"Actual floor mesh has a visual hole at "+x+","+z);
            int visibleWall=0;
            foreach(float y in new[]{.5f,1.5f,2.5f,3.5f})
                if(Physics.Raycast(parent.transform.position+new Vector3(8,y,-2),Vector3.forward,4,1<<10))visibleWall++;
            Assert.Greater(visibleWall,0,"Wall triangles face away from the room");
        }
        finally{Object.Destroy(parent);}
    }}
