using System.Collections;
using System.IO;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Opt-in, isolated staged capture. Does not read/write the normal profile.
    public sealed class BiomeCapture : MonoBehaviour
    {
        IEnumerator Start()
        {
            string directory=System.Environment.GetEnvironmentVariable("POC_CAPTURE_BIOMES");
            if(string.IsNullOrEmpty(directory))yield break;
            Directory.CreateDirectory(directory);Time.captureFramerate=30;
            var game=GetComponent<Prototype>();yield return new WaitForSeconds(.5f);
            for(int biome=0;biome<6;biome++)
            {
                game.NewSession();game.Menus.Settings.Biome=biome;game.Menus.Settings.SectorSize=6;
                game.StartMission();game.Director.enabled=false;game.Player.enabled=false;
                foreach(var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
                yield return new WaitForSeconds(.6f);
                game.CaptureFrame(Path.Combine(directory,biome+"-room.png"));
                // Same camera and white lighting: architecture must read without
                // biome fog, colored lamps or labels being the differentiator.
                RenderSettings.fog=false;RenderSettings.ambientLight=new Color(.45f,.45f,.45f);
                foreach(var lamp in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if(lamp.type!=LightType.Directional)lamp.enabled=false;
                game.Ambience.KeyLight.color=Color.white;game.Ambience.KeyLight.intensity=.8f;
                game.CaptureFrame(Path.Combine(directory,biome+"-neutral.png"));
                yield return new WaitForSeconds(.25f);
            }
            game.NewSession();game.Menus.Settings.Biome=0;game.Menus.Settings.SectorSize=20;
            game.StartMission();game.Director.enabled=false;game.Player.enabled=false;
            foreach(var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
            yield return new WaitForSeconds(.5f);
            var view=game.Player.View;var oldRotation=view.transform.rotation;float oldSize=view.orthographicSize;
            Vector3 low=Vector3.zero,high=Vector3.zero;
            foreach(var room in game.Layout)
            {
                Vector3 center=new Vector3(room.X*20,0,room.Y*20);low=Vector3.Min(low,center-Vector3.one*8);high=Vector3.Max(high,center+Vector3.one*8);
            }
            view.transform.position=(low+high)/2+Vector3.up*120;view.transform.rotation=Quaternion.Euler(90,0,0);
            view.orthographicSize=Mathf.Max(high.z-low.z,(high.x-low.x)/view.aspect)*.55f;
            bool fog=RenderSettings.fog;RenderSettings.fog=false;
            game.CaptureFrame(Path.Combine(directory,"large-20-overview.png"));RenderSettings.fog=fog;
            view.transform.rotation=oldRotation;view.orthographicSize=oldSize;Time.captureFramerate=0;
            Debug.Log("BIOME_CAPTURE_COMPLETE: six staged rooms and 20-room topology.");
        }
    }
}
