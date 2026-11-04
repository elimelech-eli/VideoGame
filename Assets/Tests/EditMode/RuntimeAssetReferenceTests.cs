using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RuntimeAssetReferenceTests
{
    [Test]
    public void ShippedPrefabsKeepTheirScriptsMaterialsAndMeshes()
    {
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Resources" })
            .Select(AssetDatabase.GUIDToAssetPath).ToArray();
        Assert.Greater(paths.Length, 0);
        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), path + ": " + t.name);
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                Assert.IsTrue(renderer.sharedMaterials.All(m => m != null), path + ": missing material on " + renderer.name);
            foreach (var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Assert.IsNotNull(skin.sharedMesh, path + ": missing skin mesh on " + skin.name);
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                Assert.IsNotNull(filter.sharedMesh, path + ": missing mesh on " + filter.name);
        }
    }
}
