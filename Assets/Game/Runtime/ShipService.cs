using UnityEngine;
namespace VideoGame
{
    public enum ShipServiceKind { Stash, Sell }
    public sealed class ShipService : MonoBehaviour
    {
        public ShipServiceKind Kind;
        public Vector3 Approach;
        public string Prompt=>Kind==ShipServiceKind.Stash?"OPEN PLAYER STASH":"SELL GEAR";
        public bool Available(Prototype game)=>game!=null && !game.InMission && !game.Player.IsDead && !game.Menus.BlocksGameplay &&
            (game.Player.transform.position-transform.TransformPoint(Approach)).sqrMagnitude<2.25f;
        public bool Open(Prototype game)
        {
            if(!Available(game))return false;
            game.Equipment.Open();
            var screen=game.GetComponent<CharacterScreen>();
            if(Kind==ShipServiceKind.Stash)screen.SetStorage(CharacterStorage.Stash);else screen.OpenSelling();
            return true;
        }
    }
}
