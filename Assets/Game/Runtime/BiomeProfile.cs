using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    public sealed class BiomeProfile
    {
        public readonly BiomeId Id;
        public readonly Color Accent, Secondary, Key, Ambient, Fog, Shell;
        public readonly float KeyIntensity, FogDensity;
        public string Name => Deployment.Name(Id);
        BiomeProfile(BiomeId id, string accent, string secondary, string key, string ambient, string fog, string shell, float intensity, float density)
        {
            Id=id; Accent=Hex(accent); Secondary=Hex(secondary); Key=Hex(key); Ambient=Hex(ambient); Fog=Hex(fog); Shell=Hex(shell);
            KeyIntensity=intensity;FogDensity=density;
        }
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        static readonly BiomeProfile[] profiles = {
            new BiomeProfile(BiomeId.NeonStreets,"20D6E5","D246A0","879CBA","28323F","101D2A","39434E",.52f,.013f),
            new BiomeProfile(BiomeId.RuinedFactory,"F6A343","698FBD","A2AAB7","383029","241D19","716556",.6f,.015f),
            new BiomeProfile(BiomeId.CorporateOffice,"56C2D2","D65848","C2D5DD","34434D","1B2833","9DAEB8",.7f,.009f),
            new BiomeProfile(BiomeId.BiotechLab,"47DCCB","9663DC","B6D8D6","34464B","192B35","B9C6C5",.66f,.011f),
            new BiomeProfile(BiomeId.Undercity,"DF9C40","5BA583","8F9A9F","333A31","202A24","6B7161",.48f,.018f),
            new BiomeProfile(BiomeId.Transit,"9271EA","75B3D8","ADC4E1","394757","263447","8696A8",.8f,.008f)
        };
        public static BiomeProfile Get(BiomeId id) => profiles[(int)id];
    }
}
