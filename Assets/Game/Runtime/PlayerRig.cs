using UnityEngine;

namespace VideoGame
{
    // Model-specific baked clips keep imported skeleton axes out of gameplay.
    public sealed class PlayerRig : MonoBehaviour
    {
        public const string AsunaPrefabPath = "Detailed/Prefabs/OperativeAsuna";
        public static string PrefabPath => Resources.Load<GameObject>(AsunaPrefabPath)!=null?AsunaPrefabPath:"Detailed/Prefabs/OperativeV03";
        public string AnimationFolder = "AsunaAnimations";
        public string WeaponAnimationFolder = "AsunaWeaponAnimations";
        public Quaternion SwordRight = Quaternion.identity;
        public Quaternion SwordLeft = Quaternion.identity;
        public static string WeaponPath(GameObject model,VideoGame.Core.GearKind kind)
            => model.GetComponent<PlayerRig>()!=null&&(kind==VideoGame.Core.GearKind.Rifle||kind==VideoGame.Core.GearKind.Handgun)?"AsunaEquipment/"+kind:"LoadoutWeapons/"+kind;
        public static Quaternion GripBasis(GameObject model,int hand,VideoGame.Core.GearKind? kind,Quaternion fallback)
        {
            var rig=model.GetComponent<PlayerRig>();
            return rig==null||kind!=VideoGame.Core.GearKind.Sword?fallback:hand==0?rig.SwordRight:rig.SwordLeft;
        }
    }
}
