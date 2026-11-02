using System.IO;
using UnityEngine;
using VideoGame;
using VideoGame.Core;

// Opt-in actual mission capture. Null-graphics test and CI runs do not render.
public static class BiomeVisualEvidence
{
    public static void Capture(Prototype game,BiomeId biome,int count)
    {
        var dir=System.Environment.GetEnvironmentVariable("BIOME_CAPTURE_DIR");
        if(count!=6||string.IsNullOrEmpty(dir)||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        Directory.CreateDirectory(dir);var cam=game.Player.View;var oldPosition=cam.transform.position;var oldRotation=cam.transform.rotation;var oldSize=cam.orthographicSize;
        try
        {
            cam.transform.rotation=Quaternion.Euler(50,45,0);
            foreach(int index in new[]{0,2})
            {
                var room=game.Layout[index];var p=new Vector3(room.X*20,.6f,room.Y*20);
                cam.transform.position=p-cam.transform.forward*40;cam.orthographicSize=8.5f;
                var rt=new RenderTexture(1440,1000,24){antiAliasing=4};var prior=RenderTexture.active;var priorTarget=cam.targetTexture;
                var texture=new Texture2D(1440,1000,TextureFormat.RGB24,false);
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1440,1000),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(dir,biome+"-room"+index+".png"),texture.EncodeToPNG());}
                finally{cam.targetTexture=priorTarget;RenderTexture.active=prior;rt.Release();Object.Destroy(texture);Object.Destroy(rt);}
            }
        }
        finally{cam.transform.SetPositionAndRotation(oldPosition,oldRotation);cam.orthographicSize=oldSize;}
    }
}
