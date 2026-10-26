using UnityEngine;

namespace VideoGame
{
    public sealed class Atmosphere : MonoBehaviour
    {
        public Light KeyLight;
        public bool InBattle { get; private set; }
        Mesh beam;
        Texture2D cookie;
        Material shaft;
        public void SetBattle(bool battle)
        {
            InBattle = battle;
            KeyLight.color = battle ? new Color(.49f,.61f,.88f) : new Color(.58f,.78f,.88f);
            KeyLight.intensity = battle ? .48f : .57f;
            RenderSettings.ambientLight = battle ? new Color(.075f,.065f,.12f) : new Color(.065f,.095f,.12f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = battle ? new Color(.035f,.025f,.065f) : new Color(.025f,.055f,.075f);
            RenderSettings.fogDensity = battle ? .017f : .013f;
        }
        public void Room(Vector3 center, Transform parent, bool hub)
        {
            if (beam == null) BuildBeam();
            Color cool = hub ? new Color(.18f,.7f,1) : new Color(.35f,.3f,1);
            Color warm = hub ? new Color(1,.6f,.25f) : new Color(1,.22f,.07f);
            AddSpot(center + new Vector3(-3,6,1), center + new Vector3(0,0,2), cool, parent, hub);
            AddSpot(center + new Vector3(4,5,-3), center + new Vector3(2,0,-1), warm, parent, false);
            var dust = new GameObject("Ambient dust").AddComponent<ParticleSystem>(); dust.transform.SetParent(parent); dust.transform.position = center + Vector3.up * 2.5f;
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main; main.loop = true; main.startLifetime = 9; main.startSpeed = .035f; main.maxParticles = 48;
            main.startSize = new ParticleSystem.MinMaxCurve(.018f,.04f); main.startColor = new Color(.4f,.65f,.85f,.16f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = dust.emission; emission.rateOverTime = 4;
            var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(11,4,11);
            var noise = dust.noise; noise.enabled = true; noise.strength = .09f; noise.frequency = .15f; noise.scrollSpeed = .1f;
            dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Particle"); dust.Play();
        }
        void AddSpot(Vector3 from, Vector3 to, Color color, Transform parent, bool shadows)
        {
            var lamp = new GameObject("Atmospheric overhead light").AddComponent<Light>(); lamp.transform.SetParent(parent); lamp.transform.position = from;
            lamp.transform.rotation = Quaternion.LookRotation(to - from);
            lamp.type = LightType.Spot; lamp.range = 15; lamp.spotAngle = 65; lamp.innerSpotAngle = 28;
            lamp.color = color; lamp.intensity = 5; lamp.cookie = cookie;
            lamp.shadows = shadows ? LightShadows.Soft : LightShadows.None; lamp.shadowStrength = .55f; lamp.shadowBias = .04f;
            var cone = new GameObject("Light shaft"); cone.transform.SetParent(parent); cone.transform.position = to;
            cone.transform.rotation = Quaternion.FromToRotation(Vector3.up, (from-to).normalized); cone.transform.localScale = new Vector3(3,Vector3.Distance(from,to),3);
            cone.AddComponent<MeshFilter>().sharedMesh = beam;
            var renderer = cone.AddComponent<MeshRenderer>(); renderer.sharedMaterial = shaft;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            var properties = new MaterialPropertyBlock(); properties.SetColor("_Color", new Color(color.r,color.g,color.b,.018f)); renderer.SetPropertyBlock(properties);
        }
        void BuildBeam()
        {
            beam = new Mesh {name="Atmospheric cone"};
            var vertices = new Vector3[66]; var uv = new Vector2[66]; var triangles = new int[32*6];
            for (int i=0;i<=32;i++)
            {
                float angle=i*Mathf.PI*2/32; vertices[i*2]=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)); vertices[i*2+1]=new Vector3(Mathf.Cos(angle)*.02f,1,Mathf.Sin(angle)*.02f);
                uv[i*2]=new Vector2(i/32f,0); uv[i*2+1]=new Vector2(i/32f,1);
                if (i==32) continue; int t=i*6,v=i*2;
                triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v+2;triangles[t+4]=v+1;triangles[t+5]=v+3;
            }
            beam.vertices=vertices;beam.uv=uv;beam.triangles=triangles;beam.RecalculateBounds();
            shaft = new Material(Resources.Load<Material>("Atmosphere"));
            cookie = new Texture2D(64,64,TextureFormat.RGBA32,false); cookie.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float radius=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;
                float grille=(x%12<2 || y%16<2)?.3f:1;
                cookie.SetPixel(x,y,new Color(1,1,1,Mathf.SmoothStep(1,0,radius)*grille));
            }
            cookie.Apply();
        }
        void OnDestroy() { if(beam!=null)Destroy(beam); if(cookie!=null)Destroy(cookie); if(shaft!=null)Destroy(shaft); }
    }
}
