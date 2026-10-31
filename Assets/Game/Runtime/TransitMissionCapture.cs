using System.Collections;
using System.IO;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Explicit review driver. Normal launches and persistent player profiles are untouched.
    public sealed class TransitMissionCapture : MonoBehaviour
    {
        IEnumerator Start()
        {
            string directory=System.Environment.GetEnvironmentVariable("POC_TRANSIT_MISSION");
            if(string.IsNullOrEmpty(directory))yield break;
            var game=GetComponent<Prototype>();yield return null;
            game.NewSession();game.Menus.Settings.Biome=(int)BiomeId.Transit;
            game.Menus.Settings.SectorSize=6;game.StartMission();
            if(directory=="play"){Debug.Log("TRANSIT_MISSION_READY: normal seeded Transit combat, 6 rooms.");yield break;}
            Directory.CreateDirectory(directory);
            if(System.Environment.GetEnvironmentVariable("POC_TRANSIT_ENEMIES")=="complete"){yield return CaptureEnemies(game,directory);Application.Quit();yield break;}
            foreach(int count in new[]{6,10,20})
            {
                if(count!=6){game.Mission.Abandon();game.BuildHub();yield return null;game.Menus.Settings.SectorSize=count;game.StartMission();}
                game.Director.enabled=false;game.Player.enabled=false;
                foreach(var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
                yield return new WaitForSeconds(.7f);
                game.CaptureFrame(Path.Combine(directory,count+"-arrival.png"));
                var rail=game.GetComponent<TransitBiome>().Reservation;game.Player.Teleport(new Vector3(rail.X,1,rail.Z-14+game.Layout[rail.RoomIndex].Size/2f-1));yield return new WaitForSeconds(.4f);
                var platformView=game.Player.View;float platformSize=platformView.orthographicSize;platformView.orthographicSize=18;
                game.CaptureFrame(Path.Combine(directory,count+"-platform.png"));platformView.orthographicSize=platformSize;
                var room=game.Layout[0];var next=game.Layout.Find(r=>MapLayout.Adjacent(room,r));
                Vector3 corridor=new Vector3((room.X+next.X)*10,1,(room.Y+next.Y)*10);
                game.Player.Teleport(corridor);yield return new WaitForSeconds(.4f);
                game.CaptureFrame(Path.Combine(directory,count+"-corridor.png"));
                var view=game.Player.View;var oldRotation=view.transform.rotation;float oldSize=view.orthographicSize;
                Vector3 low=Vector3.zero,high=Vector3.zero;
                foreach(var r in game.Layout){Vector3 p=new Vector3(r.X*20,0,r.Y*20);low=Vector3.Min(low,p-Vector3.one*8);high=Vector3.Max(high,p+Vector3.one*8);}
                low=Vector3.Min(low,new Vector3(rail.X-17,0,rail.Z-4));high=Vector3.Max(high,new Vector3(rail.X+17,0,rail.Z+4));
                view.transform.position=(low+high)/2+Vector3.up*120;view.transform.rotation=Quaternion.Euler(90,0,0);
                view.orthographicSize=Mathf.Max(high.z-low.z,(high.x-low.x)/view.aspect)*.55f;
                game.CaptureFrame(Path.Combine(directory,count+"-overview.png"));
                view.transform.rotation=oldRotation;view.orthographicSize=oldSize;
            }
            Debug.Log("TRANSIT_MISSION_CAPTURE_COMPLETE: 6/10/20-room actual mission scenes, staged views with AI disabled.");
            Application.Quit();
        }
        IEnumerator CaptureEnemies(Prototype game,string directory)
        {
            game.Director.enabled=false;game.Player.enabled=false;
            foreach(var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            yield return null;
            game.GetComponent<GameHud>().enabled=false;
            game.Player.Teleport(new Vector3(0,1,-3));
            var camera=game.Player.View;
            var kinds=new[]{EnemyKind.Rusher,EnemyKind.Gunner,EnemyKind.Bombardier,EnemyKind.Hunter};
            var names=new[]{"TransitMelee","TransitGunner","TransitAoe","TransitHeavy"};
            for(int i=0;i<kinds.Length;i++)
            {
                var d=game.SpawnDrone(new Vector3(0,1,0),kinds[i]);
                d.enabled=false;d.transform.rotation=Quaternion.LookRotation(new Vector3(-1,0,-1));
                d.Motion.enabled=false;d.Motion.Player.enabled=false;
                yield return new WaitForSeconds(.25f);
                foreach(string state in new[]{"Idle","RunForward","Fire","Cast","Death"})
                {
                    int count=state=="RunForward"?8:1;
                    for(int frame=0;frame<count;frame++)
                    {
                        var animation=d.Motion.Player;animation.Stop();animation.Play(state);
                        animation[state].normalizedTime=state=="Death"?.98f:count==1?.35f:frame/(float)count;animation.Sample();
                        yield return new WaitForEndOfFrame();
                        camera.orthographicSize=3.1f;
                        var focus=Art.RenderedBounds(d.Motion.gameObject).center;camera.transform.rotation=Quaternion.Euler(50,45,0);camera.transform.position=focus-camera.transform.forward*25;
                        if(state=="Idle")Debug.Log(names[i]+" rendered bounds "+Art.RenderedBounds(d.Motion.gameObject)+" actor "+d.transform.position+" camera "+camera.transform.position);
                        game.CaptureFrame(Path.Combine(directory,names[i]+"-"+state+"-"+frame+".png"));
                    }
                }
                Destroy(d.gameObject);yield return null;
            }
            Debug.Log("TRANSIT_COMPLETE_ROSTER_CAPTURE: four distinct Transit skins, sampled idle/run/fire/cast/death in actual Windows player; staged with AI disabled.");
        }
    }
}
