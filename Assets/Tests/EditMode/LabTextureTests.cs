using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class LabTextureTests
{
    [Test] public void PackedSmoothnessAndGlassKeepTheirAlphaChannels()
    {
        string root=Path.Combine(Application.dataPath,"Game/Resources/LabReview/Textures");
        var packed=Directory.GetFiles(root,"*_Metallic.png");Assert.Greater(packed.Length,0);
        foreach(string file in packed)
        {
            byte[] png=File.ReadAllBytes(file);
            Assert.That(png[25],Is.EqualTo(6),Path.GetFileName(file)+" lost the smoothness alpha channel during export");
        }
        string glass="Assets/Game/Resources/LabReview/Textures/rura_glowna_szybka_Albedo.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(glass);
        Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(),"Containment glass must retain its opacity channel");
    }
}
