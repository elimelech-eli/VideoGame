using UnityEngine;

namespace VideoGame
{
    public sealed class MenuView : MonoBehaviour
    {
        MenuFlow menu;
        GUIStyle heading, subtitle, row, detail, paragraph, arrow;
        readonly Color cyan=new Color(.12f,.88f,1), muted=new Color(.48f,.64f,.7f);
        void Awake()=>menu=GetComponent<MenuFlow>();
        void Start()
        {
            string directory=System.Environment.GetEnvironmentVariable("POC_CAPTURE_MENUS");
            if(!string.IsNullOrEmpty(directory))StartCoroutine(Capture(directory));
        }
        System.Collections.IEnumerator Capture(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            menu.UseStorage(System.IO.Path.Combine(directory,"preview-profile"));
            menu.OpenMain(false);
            yield return new WaitForSecondsRealtime(2);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"main-menu.png"));
            yield return new WaitForSecondsRealtime(.4f);
            menu.Activate("options");menu.Activate("audio");
            yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"audio-options.png"));
            yield return new WaitForSecondsRealtime(.3f);menu.Back();menu.Activate("display");
            yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"display-options.png"));
            yield return new WaitForSecondsRealtime(.3f);menu.Back();menu.Draft.Width=1600;menu.Draft.Height=900;menu.Activate("apply");
            yield return new WaitForSecondsRealtime(.8f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"display-confirm.png"));
            yield return new WaitForSecondsRealtime(.3f);menu.Back();
            yield return new WaitForSecondsRealtime(.6f);menu.Back();menu.NewGame();
            yield return new WaitForSecondsRealtime(.4f);
            var terminalGame=Prototype.Instance;terminalGame.Player.Teleport(terminalGame.Terminal+Vector3.back*2);terminalGame.Interact();
            yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"terminal-missions.png"));
            yield return new WaitForSecondsRealtime(.4f);menu.Back();
            yield return new WaitForSecondsRealtime(.4f);menu.Pause();
            yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"pause-nexus.png"));
            yield return new WaitForSecondsRealtime(.3f);menu.Activate("save");menu.Save(1);
            yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"save-slots.png"));
            yield return new WaitForSecondsRealtime(.3f);menu.Back();menu.Resume();
            yield return new WaitForSecondsRealtime(.4f);
            var game=Prototype.Instance;game.StartMission();game.Director.enabled=false;
            foreach(var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))enemy.enabled=false;
            menu.Pause();
            yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"pause-mission.png"));
            yield return new WaitForSecondsRealtime(.3f);menu.Activate("difficulty");
            yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"difficulty.png"));
        }
        void Styles()
        {
            heading=new GUIStyle(GUI.skin.label){fontSize=48,fontStyle=FontStyle.Bold};
            subtitle=new GUIStyle(GUI.skin.label){fontSize=12,fontStyle=FontStyle.Bold};
            row=new GUIStyle(GUI.skin.label){fontSize=17,fontStyle=FontStyle.Bold};
            detail=new GUIStyle(GUI.skin.label){fontSize=12,alignment=TextAnchor.MiddleRight};
            paragraph=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            arrow=new GUIStyle(GUI.skin.label){fontSize=23,alignment=TextAnchor.MiddleCenter};
        }
        static void Fill(Rect area,Color color){GUI.color=color;GUI.DrawTexture(area,Texture2D.whiteTexture);GUI.color=Color.white;}
        static void Text(Rect area,string value,GUIStyle style,Color color){GUI.contentColor=color;GUI.Label(area,value,style);GUI.contentColor=Color.white;}
        void OnGUI()
        {
            if(menu==null || !menu.IsOpen)return;
            if(heading==null)Styles();
            GUI.depth=-100;
            var previous=GUI.matrix;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.005f,.012f,.026f,.77f));
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            try
            {
                bool main=menu.Page==MenuPage.Main;
                Rect panel=new Rect(64,52,main?580:720,616);
                Fill(panel,new Color(.018f,.035f,.055f,.96f));Fill(new Rect(panel.x,panel.y,3,panel.height),cyan);
                string title=main?"N E X U S":Title();
                Text(new Rect(94,76,640,64),title,heading,Color.white);
                Text(new Rect(98,144,640,24),main?"OPERATIONS / SINGLE PLAYER":"OPERATIONS / "+(menu.Page==MenuPage.KeepDisplay?"DISPLAY CONFIRMATION":menu.Page.ToString().ToUpperInvariant()),subtitle,cyan);
                Fill(new Rect(98,182,main?510:640,1),new Color(.15f,.33f,.4f));
                var items=menu.Items();
                float y=204,width=main?510:640;
                float height=items.Count>6?Mathf.Min(47,330f/items.Count):52;
                for(int i=0;i<items.Count;i++)
                {
                    var item=items[i];Rect area=new Rect(98,y,width,height-5);
                    bool hover=area.Contains(Event.current.mousePosition);
                    if(hover && Event.current.type==EventType.MouseMove)menu.Highlight(i);
                    bool selected=i==menu.Selected && item.Enabled;
                    Fill(area,selected?new Color(.08f,.25f,.31f,.95f):new Color(.035f,.065f,.09f,.7f));
                    if(selected)Fill(new Rect(area.x,area.y,3,area.height),cyan);
                    Color text=item.Enabled?(selected?Color.white:muted):new Color(.25f,.32f,.36f);
                    bool slots=menu.Page==MenuPage.Load || menu.Page==MenuPage.Save;
                    Text(new Rect(area.x+17,area.y+(slots?1:9),area.width-30,27),item.Label,row,text);
                    if(!string.IsNullOrEmpty(item.Value))
                        Text(new Rect(area.x+12,area.y+(slots?24:5),area.width-(item.Adjust!=null?100:28),slots?17:33),item.Value,detail,item.Enabled?cyan:muted);
                    if(item.Adjust!=null)
                    {
                        Text(new Rect(area.xMax-64,area.y,32,area.height),"‹",arrow,text);
                        Text(new Rect(area.xMax-32,area.y,32,area.height),"›",arrow,text);
                    }
                    y+=height;
                }
                string hint=Hint();
                Text(new Rect(98,main?510:552,main?510:640,66),hint,paragraph,muted);
                Text(new Rect(98,main?584:615,main?510:640,38),menu.Notice,paragraph,cyan);
                Text(new Rect(96,678,1000,24),"↑ ↓ / D-PAD   SELECT     ENTER / A   CONFIRM     ← →   ADJUST     ESC / B   BACK",subtitle,muted);
                Text(new Rect(848,76,340,24),"N E X U S   /   OPERATIVE 01",subtitle,cyan);
                Text(new Rect(848,110,340,120),main?"Return to the signal.":"Simulation paused.\nYour next move can wait.",new GUIStyle(heading){fontSize=27,wordWrap=true},Color.white);
                Fill(new Rect(848,254,330,1),new Color(.15f,.33f,.4f));
                Text(new Rect(848,275,330,90),"NEXUS CHECKPOINTS\nSave at the hub. Continue returns you to the latest checkpoint.",paragraph,muted);
                if(menu.HasSession)
                {
                    var game=Prototype.Instance;
                    Text(new Rect(848,390,330,110),(game.InMission?"CONTRACT ACTIVE":"AT THE NEXUS")+"\n"+game.Mission.Kills+" / "+game.Mission.Target+" targets\n"+game.Mission.Credits+" credits\n"+menu.Difficulty+" difficulty",paragraph,Color.white);
                }
            }
            finally{GUI.matrix=previous;GUI.color=Color.white;GUI.contentColor=Color.white;}
        }
        public void Pointer(Vector2 point,bool activate)
        {
            var items=menu.Items();float height=items.Count>6?Mathf.Min(47,330f/items.Count):52;
            for(int i=0;i<items.Count;i++)
            {
                Rect area=new Rect(98,204+i*height,menu.Page==MenuPage.Main?510:640,height-5);
                if(!area.Contains(point) || !items[i].Enabled)continue;
                menu.Highlight(i);
                if(activate)
                {
                    if(items[i].Adjust!=null && point.x>=area.xMax-64)items[i].Adjust(point.x<area.xMax-32?-1:1);
                    else menu.Activate(items[i].Id);
                }
                return;
            }
        }
        string Title()
        {
            switch(menu.Page)
            {
                case MenuPage.Deployment:return "M I S S I O N S";
                case MenuPage.Pause:return "P A U S E D";
                case MenuPage.Save:return "S A V E";
                case MenuPage.Load:return "L O A D";
                case MenuPage.Confirm:return "C O N F I R M";
                case MenuPage.KeepDisplay:return "K E E P   D I S P L A Y ?";
                default:return menu.Page.ToString().ToUpperInvariant();
            }
        }
        string Hint()
        {
            switch(menu.Page)
            {
                case MenuPage.Main:return "Start a new operation, or continue from a Nexus checkpoint.";
                case MenuPage.Pause:case MenuPage.Save:return menu.SaveHint;
                case MenuPage.Load:return "Load a Nexus checkpoint. You return to the hub; missions are not resumed.";
                case MenuPage.Confirm:return menu.Question;
                case MenuPage.KeepDisplay:return "Reverting in "+Mathf.CeilToInt(menu.DisplaySeconds)+" seconds unless you keep these settings.";
                case MenuPage.Difficulty:return "Damage received: Story 50%, Standard 100%, Hard 150%, Nightmare 250%. Changes apply immediately; rewards stay the same.";
                case MenuPage.Display:return "Apply changes from Options. Display mode changes require confirmation within 15 seconds. VSync takes priority over the frame-rate limit.";
                case MenuPage.Audio:return "Volume changes take effect when you select Apply Changes.";
                case MenuPage.Deployment:return "Choose a destination. Eliminate 8 targets, then extract. Map size is randomized for each mission: 6, 10 or 20 rooms, plus corridors.";
                default:return "Adjust with ← → or the arrow buttons. Apply Changes to keep your preferences, or Cancel to discard them.";
            }
        }
    }
}
