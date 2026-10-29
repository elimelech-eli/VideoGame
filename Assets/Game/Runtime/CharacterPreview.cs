using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // One isolated, on-demand render target. It never advances the gameplay rig.
    public sealed class CharacterPreview : MonoBehaviour
    {
        public RenderTexture Texture {get;private set;}
        public bool Ready => model!=null;
        GameObject stage,model;
        Camera cameraView;
        float yaw=15;
        bool dirty=true;
        public void Refresh(){dirty=true;}
        public void Rotate(float degrees){yaw+=degrees;dirty=true;}
        void Ensure()
        {
            if(stage!=null || SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            stage=new GameObject("Character preview studio");stage.transform.SetParent(transform);stage.transform.position=new Vector3(1000,-1000,1000);
            model=Instantiate(Resources.Load<GameObject>("Detailed/Prefabs/OperativeV03"),stage.transform);
            model.name="Operative preview";
            foreach(var col in model.GetComponentsInChildren<Collider>())Destroy(col);
            foreach(var animator in model.GetComponentsInChildren<Animator>())animator.enabled=false;
            var pose=model.AddComponent<Animation>();
            var idle=Resources.Load<AnimationClip>("OperatorAnimations/Idle");
            if(idle!=null){pose.AddClip(idle,"Idle");pose.Play("Idle");pose["Idle"].time=0;pose.Sample();pose.enabled=false;}
            var bounds=Art.RenderedBounds(model);float scale=2.2f/Mathf.Max(.01f,bounds.size.y);model.transform.localScale*=scale;
            bounds=Art.RenderedBounds(model);model.transform.position+=stage.transform.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            var cameraObject=new GameObject("Preview camera");cameraObject.transform.SetParent(stage.transform,false);cameraView=cameraObject.AddComponent<Camera>();
            cameraView.enabled=false;cameraView.cullingMask=1<<30;cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.018f,.028f,.04f,0);
            cameraView.fieldOfView=30;cameraView.nearClipPlane=.1f;cameraView.farClipPlane=10;
            cameraView.transform.localPosition=new Vector3(0,1.2f,4.8f);cameraView.transform.LookAt(stage.transform.position+Vector3.up*1.1f);
            foreach(var spec in new[]{new Vector3(-2,3,3),new Vector3(2,2,1)})
            {
                var lightObject=new GameObject("Preview softbox");lightObject.transform.SetParent(stage.transform,false);lightObject.transform.localPosition=spec;
                var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.cullingMask=1<<30;light.range=8;light.intensity=2;light.color=spec.x<0?new Color(.8f,.9f,1):new Color(.2f,.7f,1);
            }
            var keyObject=new GameObject("Preview portrait key");keyObject.transform.SetParent(stage.transform,false);keyObject.transform.rotation=Quaternion.Euler(30,180,0);
            var key=keyObject.AddComponent<Light>();key.type=LightType.Directional;key.cullingMask=1<<30;key.intensity=1.7f;key.color=new Color(.9f,.95f,1);
            Texture=new RenderTexture(512,768,24,RenderTextureFormat.ARGB32){name="Operative preview",antiAliasing=4};Texture.Create();cameraView.targetTexture=Texture;
        }
        void LateUpdate()
        {
            bool open=Prototype.Instance?.Menus?.Page==MenuPage.Equipment;
            if(!open){if(stage!=null)stage.SetActive(false);return;}
            Ensure();if(stage==null)return;stage.SetActive(true);
            if(!dirty)return;dirty=false;
            model.transform.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name=="SOCKET_weapon_R")
                foreach(var renderer in t.GetComponentsInChildren<Renderer>())renderer.enabled=Prototype.Instance.Equipment.Profile.Equipped[3]?.Design.Kind==GearKind.Rifle;
            cameraView.Render();
        }
        void OnDestroy()
        {
            if(cameraView!=null)cameraView.targetTexture=null;
            if(Texture!=null){Texture.Release();Destroy(Texture);}if(stage!=null)Destroy(stage);
        }
    }
}
