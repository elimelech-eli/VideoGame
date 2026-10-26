using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    public sealed class GameHud : MonoBehaviour
    {
        readonly Color ink = new Color(.025f, .045f, .065f, .94f);
        readonly Color muted = new Color(.5f, .64f, .7f);
        GUIStyle small, body, title, number;
        void Styles()
        {
            small = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            number = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
        }
        void Box(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white; }
        void Text(Rect rect, string text, GUIStyle style, Color color) { var old = GUI.contentColor; GUI.contentColor = color; GUI.Label(rect, text, style); GUI.contentColor = old; }
        void Panel(Rect rect, Color accent)
        {
            Box(rect, ink); Box(new Rect(rect.x, rect.y, 3, rect.height), accent);
            Box(new Rect(rect.x + 3, rect.y, rect.width - 3, 1), new Color(.2f, .4f, .45f, .5f));
        }
        void OnGUI()
        {
            var game = Prototype.Instance;
            if (game == null || game.Player == null) return;
            if (small == null) Styles();
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280*scale)/2, (Screen.height - 720*scale)/2, 0), Quaternion.identity, Vector3.one * scale);
            try
            {
                Text(new Rect(28, 22, 350, 28), "N E X U S", title, Color.white);
                Text(new Rect(30, 52, 350, 22), game.InMission ? "COMBAT ZONE  /  SIGNAL ONLINE" : "OPERATIONS  /  SAFE ZONE", small, Art.Cyan);
                Box(new Rect(30, 79, 180, 2), Art.Cyan);
                Panel(new Rect(944, 24, 308, 145), game.InMission ? Art.Amber : Art.Cyan);
                Text(new Rect(964, 36, 260, 22), game.InMission ? "CONTRACT  /  DRONE SUPPRESSION" : "HOME  /  OPERATIONS", small, muted);
                Text(new Rect(964, 62, 260, 32), game.InMission ? "Clear the sector" : "Awaiting deployment", title, Color.white);
                string objective = game.InMission ? $"DRONES NEUTRALIZED   {game.Mission.Kills:00} / {game.Mission.Target:00}" : game.Mission.Stage == MissionStage.Complete ? "RETURN TO UPLINK  /  CLAIM REWARD" : "VISIT THE CONTRACT UPLINK";
                Text(new Rect(964, 103, 268, 24), objective, small, game.Mission.Stage == MissionStage.Complete ? Art.Cyan : muted);
                Box(new Rect(964, 138, 266, 3), new Color(.12f, .19f, .23f));
                Box(new Rect(964, 138, 266 * (game.InMission ? (float)game.Mission.Kills / game.Mission.Target : 1), 3), game.InMission ? Art.Amber : Art.Cyan);

                Panel(new Rect(28, 602, 270, 88), Art.Cyan);
                Text(new Rect(45, 610, 100, 20), "VITALS", small, muted);
                Text(new Rect(45, 628, 100, 40), Mathf.CeilToInt(game.Player.Health).ToString("000"), number, Color.white);
                Text(new Rect(145, 638, 130, 22), "OPERATIVE / 01", small, Art.Cyan);
                Box(new Rect(45, 675, 230, 4), new Color(.12f, .2f, .25f));
                Box(new Rect(45, 675, 230 * Mathf.Clamp01(game.Player.Health / 100), 4), Art.Cyan);
                Skill(400, "01", "PULSE RIFLE", "LMB / RT", 0, Art.Cyan);
                Skill(560, "02", "FRAG GRENADE", "Q / RB", game.Player.GrenadeRemaining, Art.Amber);
                Skill(720, "03", "GROUND DASH", "SPACE / B", game.Player.DashRemaining, Art.Cyan);
                Text(new Rect(1030, 637, 220, 25), $"{game.Mission.Credits:000000}  CR", title, Color.white);
                Text(new Rect(1033, 666, 200, 22), "ACCOUNT BALANCE", small, muted);

                if (Vector3.Distance(game.Player.transform.position, game.Terminal) < 3.5f)
                {
                    string action = game.InMission ? game.Mission.Stage == MissionStage.Complete ? "EXTRACT TO OPERATIONS" : "OBJECTIVE INCOMPLETE" : game.Mission.Stage == MissionStage.Complete ? "CLAIM 100 CR REWARD" : "ACCEPT CONTRACT";
                    Panel(new Rect(440, 522, 400, 57), Art.Cyan);
                    Text(new Rect(458, 535, 54, 30), "E / A", body, Art.Cyan);
                    Text(new Rect(518, 535, 310, 30), action, body, Color.white);
                    if (GUI.Button(new Rect(440, 522, 400, 57), GUIContent.none, GUIStyle.none)) game.Interact();
                }
                Text(new Rect(30, 567, 335, 26), "WASD / LS   MOVE      RMB   MOVE TO POINT", small, muted);
            }
            finally { GUI.matrix = previous; GUI.color = Color.white; }
        }
        void Skill(float x, string index, string name, string key, float cooldown, Color accent)
        {
            Panel(new Rect(x, 602, 148, 88), cooldown > 0 ? muted : accent);
            Text(new Rect(x+12, 612, 120, 20), index + "  /  " + key, small, muted);
            Text(new Rect(x+12, 639, 126, 24), name, small, Color.white);
            Text(new Rect(x+12, 666, 120, 20), cooldown > 0 ? cooldown.ToString("0.0") + "s  RECHARGING" : "READY", small, cooldown > 0 ? muted : accent);
        }
    }
}
