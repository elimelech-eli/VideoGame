using System.Collections;
using System.IO;
using UnityEngine;

namespace VideoGame
{
    // Explicit opt-in review fixture. Normal missions do not load this kit.
    [DefaultExecutionOrder(200)]
    public sealed class LabTileReview : MonoBehaviour
    {
        public Transform Kit { get; private set; }
        Prototype game;
        string capture;
        bool ready, staging;
        Vector3 focus;
        float zoom=7.5f;
        static readonly Vector3 Origin=new Vector3(1000,0,1000);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("POC_LAB_REVIEW")))
                new GameObject("Lab tile review").AddComponent<LabTileReview>();
        }
        public static GameObject Assemble(Transform parent)
        {
            var root=new GameObject("LAB / room-corridor-junction");root.transform.SetParent(parent,false);
            void Place(string name,Vector3 position,float yaw=0)
            {
                var prefab=Resources.Load<GameObject>("LabReview/Prefabs/"+name);
                if(prefab==null)throw new System.InvalidOperationException("Missing lab review module: "+name);
                var tile=Object.Instantiate(prefab,root.transform);tile.name=name;
                tile.transform.localPosition=position;tile.transform.localRotation=Quaternion.Euler(0,yaw,0);
                tile.layer=name=="Floor4"?8:9;
            }
            for(int x=-4;x<=4;x+=4)for(int z=-4;z<=4;z+=4)Place("Floor4",new Vector3(x,0,z));
            Place("Floor4",new Vector3(0,0,-8));Place("Floor4",new Vector3(0,0,-12));
            foreach(var p in new[]{new Vector3(0,0,-16),new Vector3(0,0,-20),new Vector3(0,0,-24),new Vector3(-4,0,-20),new Vector3(4,0,-20)})Place("Floor4",p);
            // Rear walls remain full-height. Camera-facing sides are deliberate cutaways.
            for(int n=-4;n<=4;n+=4){Place("Wall4",new Vector3(n,0,6));Place("Wall4",new Vector3(6,0,n),90);}
            Place("Wall4",new Vector3(2,0,-8),90);Place("Wall4",new Vector3(2,0,-12),90);
            Place("Wall4",new Vector3(6,0,-20),90);Place("Wall4",new Vector3(0,0,-26));
            Place("Door",new Vector3(0,.09f,5.7f),180);
            Place("Console",new Vector3(2.7f,.09f,.65f),90);
            Place("Containment",new Vector3(2.9f,.09f,3.5f));
            Place("SpecimenRack",new Vector3(-3.4f,.09f,4.65f),180);
            Place("Examination",new Vector3(-3.6f,.09f,.8f),90);
            Place("ControlBank",new Vector3(4.85f,.09f,-1.8f),180);
            Place("Cabinet",new Vector3(-3.4f,.09f,-4.5f),90);
            Place("UtilityPipe",new Vector3(3.8f,.09f,-20),90);
            Place("UtilityPipe",new Vector3(5.2f,.09f,3.6f));
            Place("SampleJar",new Vector3(-3.5f,.78f,.15f),20);
            Place("SampleVial",new Vector3(-3.65f,.78f,.6f),-15);
            return root;
        }
        IEnumerator Start()
        {
            yield return null;game=Object.FindFirstObjectByType<Prototype>();
            if(game==null)throw new System.InvalidOperationException("Lab review requires the game bootstrap");
            capture=System.Environment.GetEnvironmentVariable("POC_LAB_REVIEW");
            game.Menus.enabled=false;
            foreach(var hud in game.GetComponents<GameHud>())hud.enabled=false;
            if(game.Director!=null)game.Director.enabled=false;
            Kit=Assemble(transform).transform;Kit.position=Origin;
            game.Player.Teleport(Origin+new Vector3(0,1.1f,-2));
            RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.065f,.095f,.13f);RenderSettings.ambientEquatorColor=new Color(.025f,.04f,.055f);RenderSettings.ambientGroundColor=new Color(.012f,.018f,.025f);
            game.Ambience.enabled=false;var key=game.Ambience.KeyLight;key.color=new Color(.8f,.88f,1);key.intensity=.32f;
            key.shadows=LightShadows.Soft;key.shadowBias=.025f;key.shadowNormalBias=.15f;
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=45;
            void Practical(string name,Vector3 point,Color color,float intensity,float range,Vector3? target=null)
            {
                var lamp=new GameObject(name).AddComponent<Light>();lamp.transform.SetParent(Kit,false);lamp.transform.localPosition=point;
                lamp.color=color;lamp.intensity=intensity;lamp.range=range;
                lamp.type=target.HasValue?LightType.Spot:LightType.Point;
                if(target.HasValue){lamp.transform.LookAt(Origin+target.Value);lamp.spotAngle=65;lamp.shadows=LightShadows.Soft;lamp.shadowBias=.025f;lamp.shadowNormalBias=.15f;}
            }
            Practical("Containment inspection light",new Vector3(.8f,4.5f,1),new Color(.4f,.7f,1),5.5f,10,new Vector3(2.9f,1.7f,3.5f));
            Practical("Examination task light",new Vector3(-2.8f,3.8f,1.6f),new Color(1,.72f,.44f),4.5f,8,new Vector3(-3.6f,.7f,.8f));
            Practical("Tank practical",new Vector3(2.9f,1.9f,3.5f),new Color(.08f,.7f,.85f),1.3f,4);
            Practical("Corridor practical",new Vector3(0,2.7f,-10),new Color(.25f,.5f,.8f),1.6f,7);
            Practical("Service practical",new Vector3(0,2.7f,-20),new Color(.6f,.45f,.3f),1.5f,7);
            var probe=new GameObject("Lab local reflection").AddComponent<ReflectionProbe>();probe.transform.SetParent(Kit,false);probe.transform.localPosition=new Vector3(0,2.2f,1);
            probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;probe.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode=UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;probe.resolution=128;probe.size=new Vector3(14,8,14);probe.boxProjection=true;probe.hdr=true;probe.farClipPlane=22;probe.clearFlags=UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.025f,.04f,.055f);
            yield return null;int probeRender=probe.RenderProbe();int waited=0;while(!probe.IsFinishedRendering(probeRender)&&waited++<120)yield return null;
            ready=true;Debug.Log("LAB_REVIEW_READY: credited room, 8m corridor and four-way junction; normal biomes unchanged.");
            if(capture=="play")yield break;
            Directory.CreateDirectory(capture);staging=true;game.Player.enabled=false;
            foreach(string name in new[]{"room","corner","corridor","junction","overview"})
            {
                focus=name=="corner"?new Vector3(2.6f,0,2):name=="room"?new Vector3(0,0,1):name=="corridor"?new Vector3(0,0,-10):name=="junction"?new Vector3(0,0,-20):new Vector3(0,0,-10);
                zoom=name=="overview"?19:name=="corner"?4.2f:name=="room"?7.5f:6;
                yield return new WaitForSeconds(.3f);game.CaptureFrame(Path.Combine(capture,name+".png"));
            }
            staging=false;game.Player.enabled=true;game.Player.View.orthographicSize=7.5f;
            Debug.Log("LAB_REVIEW_CAPTURE_COMPLETE: five staged Unity views; interactive movement available.");
        }
        void LateUpdate()
        {
            if(!ready)return;
            if(staging){game.Player.View.transform.position=Origin+focus+new Vector3(-10.7f,18,-10.7f);game.Player.View.orthographicSize=zoom;}
        }
        void OnGUI()
        {
            if(!ready||staging)return;
            GUI.Box(new Rect(16,16,490,60),"LAB TILE REVIEW / WASD or controller to move\nRoom → corridor → junction. Cutaway sides are intentional.");
        }
    }
}
