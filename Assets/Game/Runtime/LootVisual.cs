using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Presentation has no pickup authority. The root trigger and GearPickup own
    // interaction; this child preserves item materials and adds a rarity marker.
    public sealed class LootVisual : MonoBehaviour
    {
        public string ModelType {get;private set;}
        public Transform Model {get;private set;}
        public LineRenderer RarityRing {get;private set;}
        Transform pivot;
        float phase;
        public void Initialize(GearItem item)
        {
            ModelType=item?.Design.Kind.ToString()??"Credits";
            var source=Resources.Load<GameObject>("LoadoutWeapons/"+ModelType);
            if(source==null)throw new System.InvalidOperationException("Missing pickup model: "+ModelType);
            pivot=new GameObject("Loot model pivot").transform;pivot.SetParent(transform,false);pivot.localPosition=Vector3.up*.2f;
            Model=Instantiate(source,pivot).transform;Model.name="Pickup "+ModelType;
            // Present the broad side to the isometric camera, rather than hiding
            // rifles behind their muzzle or swords behind a foreshortened blade.
            Model.localRotation=ModelType=="Rifle"||ModelType=="Handgun"?Quaternion.Euler(-15,135,-65):ModelType=="Sword"?Quaternion.Euler(-35,225,-80):Quaternion.Euler(ModelType=="Shield"?-55:-20,225,0);
            var bounds=Art.RenderedBounds(Model.gameObject,false);
            float size=ModelType=="Credits"?.38f:ModelType=="Handgun"?.58f:ModelType=="Helmet"?.58f:.8f;
            float factor=size/Mathf.Max(.001f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
            Model.localScale*=factor;
            bounds=Art.RenderedBounds(Model.gameObject,false);Model.position+=pivot.position-bounds.center;
            foreach(var collider in Model.GetComponentsInChildren<Collider>())Destroy(collider);
            foreach(var renderer in Model.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var ring=new GameObject("Loot rarity ring");ring.transform.SetParent(transform,false);ring.transform.localPosition=Vector3.down*.24f;
            RarityRing=ring.AddComponent<LineRenderer>();RarityRing.useWorldSpace=false;RarityRing.loop=true;RarityRing.positionCount=40;
            RarityRing.sharedMaterial=Resources.Load<Material>("PrototypeTrace");RarityRing.startWidth=RarityRing.endWidth=.025f;
            RarityRing.startColor=RarityRing.endColor=item==null?Art.Amber:EquipmentSystem.QualityColor(item.Quality);
            for(int n=0;n<40;n++){float a=n*Mathf.PI*2/40;RarityRing.SetPosition(n,new Vector3(Mathf.Cos(a)*.42f,0,Mathf.Sin(a)*.42f));}
            RarityRing.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;RarityRing.receiveShadows=false;
        }
        void Update()
        {
            if(pivot==null || Time.timeScale==0 || Prototype.Instance?.Menus?.IsOpen==true)return;
            phase+=Time.deltaTime;pivot.localPosition=Vector3.up*(.2f+Mathf.Sin(phase*2)*.045f);pivot.localRotation=Quaternion.Euler(0,Mathf.Sin(phase*.7f)*12,0);
        }
    }
}
