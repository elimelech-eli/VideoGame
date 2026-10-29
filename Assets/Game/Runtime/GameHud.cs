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
            if (game == null || game.Player == null || game.Menus?.IsOpen == true) return;
            if (small == null) Styles();
            DrawEnemyBars(game);
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280*scale)/2, (Screen.height - 720*scale)/2, 0), Quaternion.identity, Vector3.one * scale);
            try
            {
                Text(new Rect(28, 22, 350, 28), "N E X U S", title, Color.white);
                Text(new Rect(30, 52, 350, 22), game.InMission ? Deployment.Name(game.ActiveBiome).ToUpperInvariant() : "OPERATIONS  /  SAFE ZONE", small, Art.Cyan);
                Box(new Rect(30, 79, 180, 2), Art.Cyan);
                if(!game.InMission && GUI.Button(new Rect(30,130,350,46),"NEXT DEPLOYMENT  /  BIOME & SIZE",small))game.Menus.OpenDeployment();
                if (GUI.Button(new Rect(220, 90, 160, 24), "PAUSE [ESC / START]", small)) game.Menus.Pause();
                if (GUI.Button(new Rect(30, 90, 180, 24), game.Music.Enabled ? "MUSIC ON  [M]" : "MUSIC OFF  [M]", small)) game.Music.Toggle();
                Panel(new Rect(944, 24, 308, 145), game.InMission ? Art.Amber : Art.Cyan);
                Text(new Rect(964, 36, 260, 22), game.InMission ? "CONTRACT  /  DRONE SUPPRESSION" : "HOME  /  OPERATIONS", small, muted);
                Text(new Rect(964, 62, 260, 32), game.InMission ? "Clear the sector" : "Awaiting deployment", title, Color.white);
                string objective = game.InMission ? $"DRONES NEUTRALIZED   {game.Mission.Kills:00} / {game.Mission.Target:00}" : game.Mission.Stage == MissionStage.Complete ? "RETURN TO UPLINK  /  CLAIM REWARD" : "VISIT THE CONTRACT UPLINK";
                Text(new Rect(964, 103, 268, 24), objective, small, game.Mission.Stage == MissionStage.Complete ? Art.Cyan : muted);
                Box(new Rect(964, 138, 266, 3), new Color(.12f, .19f, .23f));
                Box(new Rect(964, 138, 266 * (game.InMission ? (float)game.Mission.Kills / game.Mission.Target : 1), 3), game.InMission ? Art.Amber : Art.Cyan);

                if (game.InMission) DrawMap(game);
                Panel(new Rect(28, 602, 270, 88), Art.Cyan);
                Text(new Rect(45, 610, 100, 20), "VITALS", small, muted);
                Text(new Rect(45, 628, 100, 40), Mathf.CeilToInt(game.Player.Health).ToString("000"), number, Color.white);
                Text(new Rect(145, 638, 130, 22), "OPERATIVE / 01", small, Art.Cyan);
                Box(new Rect(45, 675, 230, 4), new Color(.12f, .2f, .25f));
                Box(new Rect(45, 675, 230 * Mathf.Clamp01(game.Player.Health / game.Equipment.Profile.MaxHealth), 4), Art.Cyan);
                Skill(400, "01", game.Player.Hands.State(0), "LMB / RT", 0, Art.Cyan);
                Skill(560, "02", game.Player.Hands.State(1), "RMB / LT", 0, Art.Amber);
                Skill(720, "03", "GROUND DASH", "SPACE / B", game.Player.DashRemaining, Art.Cyan);
                Text(new Rect(1030, 637, 220, 25), $"{game.Equipment.Wallet:000000}  CR", title, Color.white);
                Text(new Rect(1033, 666, 220, 22), "FIELD +"+(game.InMission?game.Equipment.Field.Credits:0)+" / LV "+game.Equipment.Profile.Level, small, muted);
                if(GUI.Button(new Rect(30,340,240,32),"EQUIPMENT [I / SELECT]",small))game.Equipment.Open();

                if (Vector3.Distance(game.Player.transform.position, game.Terminal) < 3.5f)
                {
                    string action = game.InMission ? game.Mission.Stage == MissionStage.Complete ? "EXTRACT TO OPERATIONS" : "OBJECTIVE INCOMPLETE" : game.Mission.Stage == MissionStage.Complete ? "CLAIM 100 CR REWARD" : "ACCEPT CONTRACT";
                    Panel(new Rect(440, 522, 400, 57), Art.Cyan);
                    Text(new Rect(458, 535, 54, 30), "E / A", body, Art.Cyan);
                    Text(new Rect(518, 535, 310, 30), action, body, Color.white);
                    if (GUI.Button(new Rect(440, 522, 400, 57), GUIContent.none, GUIStyle.none)) game.Interact();
                }
                if(!string.IsNullOrEmpty(game.Menus?.Notice))Text(new Rect(30,530,350,35),game.Menus.Notice,small,Art.Cyan);
                Text(new Rect(30, 550, 370, 40), !game.InMission && game.Equipment.Profile.Bag.Exists(i=>i.Condition<=25)?"WARNING: carried gear at 25% condition will be destroyed on your next death. Repair or stash it.":game.Equipment.Notice, small, Art.Cyan);
                Text(new Rect(30, 590, 370, 20), "WASD / LS MOVE   E / A LOOT   Q / RB GRENADE", small, muted);
                if (game.Player.IsDead)
                {
                    Box(new Rect(0, 0, 1280, 720), new Color(.12f, 0, 0, .22f));
                    Panel(new Rect(390, 180, 500, 100), Color.red);
                    Text(new Rect(410, 197, 460, 32), "OPERATIVE LOST", title, Color.white);
                    Text(new Rect(410, 239, 460, 24), "EMERGENCY RECALL TO THE NEXUS", body, Art.Cyan);
                }
            }
            finally { GUI.matrix = previous; GUI.color = Color.white; }
        }
        void DrawMap(Prototype game)
        {
            var rooms = game.Layout; if (rooms == null) return;
            Panel(new Rect(30,130,180,190),Art.Cyan);
            Text(new Rect(40,138,165,20),game.Layout.Count+" ROOMS / " + game.Seed,small,muted);
            int minX=0,maxX=0,minY=0,maxY=0;
            foreach(var room in rooms) { minX=Mathf.Min(minX,room.X);maxX=Mathf.Max(maxX,room.X);minY=Mathf.Min(minY,room.Y);maxY=Mathf.Max(maxY,room.Y); }
            float unit=Mathf.Min(144f/(maxX-minX+1),140f/(maxY-minY+1));
            Vector2 Origin(float x,float y) => new Vector2(48+(x-minX+.5f)*unit,300-(y-minY+.5f)*unit);
            foreach(var room in rooms)
            {
                var a=Origin(room.X,room.Y);
                foreach(var other in rooms)
                {
                    if(!MapLayout.Adjacent(room,other))continue;
                    var b=Origin(other.X,other.Y);
                    Box(new Rect(Mathf.Min(a.x,b.x)-1,Mathf.Min(a.y,b.y)-1,Mathf.Abs(a.x-b.x)+2,Mathf.Abs(a.y-b.y)+2),muted);
                }
            }
            foreach(var room in rooms)
            {
                var point=Origin(room.X,room.Y);float size=room.Size/(float)MapLayout.Spacing*unit;
                Box(new Rect(point.x-size/2,point.y-size/2,size,size),room.X==0&&room.Y==0 ? new Color(.1f,.5f,.4f) : new Color(.17f,.28f,.35f));
            }
            var player=Origin(game.Player.transform.position.x/MapLayout.Spacing,game.Player.transform.position.z/MapLayout.Spacing);
            Box(new Rect(player.x-3,player.y-3,6,6),Art.Amber);
            var exit=Origin(game.Terminal.x/MapLayout.Spacing,game.Terminal.z/MapLayout.Spacing);
            Box(new Rect(exit.x-2,exit.y-2,4,4),Color.green);
        }
        void DrawEnemyBars(Prototype game)
        {
            float scale = Mathf.Clamp(Screen.height / 720f, .65f, 2);
            foreach (var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))
            {
                if (!enemy.HealthBarVisible) continue;
                var collider = enemy.GetComponent<Collider>();
                Vector3 anchor = enemy.transform.position; anchor.y = collider.bounds.max.y + .25f;
                Vector3 point = game.Player.View.WorldToScreenPoint(anchor);
                if (point.z <= 0 || point.x < 0 || point.x > Screen.width || point.y < 0 || point.y > Screen.height) continue;
                float width = 64 * scale, height = 5 * scale;
                float x = point.x - width / 2, y = Screen.height - point.y;
                Box(new Rect(x - 2, y - 2, width + 4, height + 4), new Color(.015f, .02f, .03f, .95f));
                Box(new Rect(x, y, width, height), new Color(.18f, .08f, .08f));
                Box(new Rect(x, y, width * enemy.Health / enemy.HealthMaximum, height), new Color(1, .24f, .12f));
            }
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
