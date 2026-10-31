using System.Linq;
using UnityEditor;
using UnityEngine;
using VideoGame;
namespace VideoGame.Editor
{
    public static partial class NexusShipReviewAssets
    {
        static void AddCargoServices(GameObject root)
        {
            var stash=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Port cargo container");
            var service=stash.gameObject.AddComponent<ShipService>();service.Kind=ShipServiceKind.Stash;
            service.Approach=stash.InverseTransformPoint(new Vector3(-2.4f,1,-8.5f));
            var donor=root.GetComponentsInChildren<Transform>().First(t=>t.name=="Bridge communications terminal").gameObject;
            var console=UnityEngine.Object.Instantiate(donor);console.name="Cargo gear exchange console";
            console.transform.rotation=Quaternion.Euler(0,270,0);var original=BoundsOf(console).size;
            console.transform.localScale*=1.65f/Mathf.Max(original.x,original.y,original.z);
            var measured=BoundsOf(console);console.transform.position+=new Vector3(2,0,-8.5f)-new Vector3(measured.center.x,measured.min.y,measured.center.z);
            foreach(var c in console.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            console.transform.SetParent(root.transform,true);
            var b=BoundsOf(console);var solid=new GameObject("Gear exchange collision");solid.layer=9;solid.transform.SetParent(root.transform);solid.transform.position=b.center;solid.AddComponent<BoxCollider>().size=b.size;
            var sell=console.AddComponent<ShipService>();sell.Kind=ShipServiceKind.Sell;sell.Approach=console.transform.InverseTransformPoint(new Vector3(.5f,1,-8.5f));
            Sign("PLAYER STASH",new Vector3(-2.7f,.04f,-9.6f),.2f);
            Sign("GEAR EXCHANGE",new Vector3(1.8f,.04f,-9.7f),.18f);
            foreach(var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))if(text.transform.parent==null)text.transform.SetParent(root.transform,true);
        }
    }
}
