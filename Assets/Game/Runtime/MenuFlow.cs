using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using VideoGame.Core;

namespace VideoGame
{
    public enum MenuPage { Closed, Main, Pause, Options, Audio, Display, Controls, Difficulty, Save, Load, Confirm, KeepDisplay, Deployment }
    public sealed class MenuEntry
    {
        public string Id, Label, Value;
        public bool Enabled=true;
        public Action Activate;
        public Action<int> Adjust;
    }

    [DefaultExecutionOrder(-1000)]
    public sealed class MenuFlow : MonoBehaviour
    {
        public MenuPage Page { get; private set; }
        public GameSettings Settings { get; private set; } = new GameSettings();
        public GameSettings Draft { get; private set; }
        public Difficulty Difficulty { get; private set; } = Difficulty.Standard;
        public SessionStore Store { get; private set; }
        public bool HasSession { get; private set; } = true;
        public int Selected { get; private set; }
        public string Notice { get; private set; } = "";
        public bool IsOpen => Page != MenuPage.Closed;
        public bool BlocksGameplay => IsOpen || Time.frameCount <= resumeFrame || waitingRelease;
        public Vector2 PointerPosition
        {
            get
            {
                if(Mouse.current==null)return new Vector2(-1,-1);
                float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
                Vector2 point=(Mouse.current.position.ReadValue()-new Vector2((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2))/scale;
                point.y=720-point.y;return point;
            }
        }
        public bool BlocksPlayerInput => BlocksGameplay ||
            ((Mouse.current?.leftButton.isPressed==true || Mouse.current?.rightButton.isPressed==true) &&
             (new Rect(30,90,350,24).Contains(PointerPosition) ||
              (!game.InMission && new Rect(30,130,350,46).Contains(PointerPosition)) ||
              (Vector3.Distance(game.Player.transform.position,game.Terminal)<3.5f && new Rect(440,522,400,57).Contains(PointerPosition))));
        public bool CanSave => HasSession && !game.InMission && !game.Player.IsDead;
        public float DamageMultiplier => new[]{.5f,1f,1.5f,2.5f}[(int)Difficulty];
        public float DisplaySeconds => Mathf.Max(0,displayDeadline-Time.realtimeSinceStartup);
        Prototype game;
        MenuPage optionsReturn, loadReturn, confirmReturn;
        Action confirmation;
        string question;
        int resumeFrame=-1;
        bool waitingRelease, displayPreview, autosaveEnabled;
        CheckpointSlot[] slots;
        CheckpointSlot LatestListed()
        {
            if(slots==null)RefreshSlots();
            CheckpointSlot latest=null;
            foreach(var slot in slots) if(slot.Data!=null && (latest==null || DateTime.Parse(slot.Data.SavedUtc).ToUniversalTime()>DateTime.Parse(latest.Data.SavedUtc).ToUniversalTime()))latest=slot;
            return latest;
        }
        void RefreshSlots(){slots=new CheckpointSlot[4];for(int i=0;i<4;i++)slots[i]=Store.Read(i);}
        float repeatAt, displayDeadline;
        GameSettings beforeDisplay;
        public string Question => question;
        public string SaveHint => CanSave ? "Nexus checkpoints restore you at the hub. Missions cannot be resumed." : "Save at the Nexus. Continue returns to your last Nexus checkpoint; current mission progress is not saved.";

        void Awake()
        {
            game=GetComponent<Prototype>();
            Store=new SessionStore(Path.Combine(Application.persistentDataPath,"Profiles"));
            gameObject.AddComponent<MenuView>();
        }
        public void UseStorage(string directory, bool loadPreferences=false)
        {
            Store=new SessionStore(directory);autosaveEnabled=true;RefreshSlots();
            if(loadPreferences){Settings=Store.LoadSettings();ApplySettings(Settings,true);}
        }
        public void OpenMain(bool keepSession=false, bool loadPreferences=false)
        {
            if(loadPreferences){autosaveEnabled=true;Settings=Store.LoadSettings();ApplySettings(Settings,true);}
            HasSession=keepSession; Open(MenuPage.Main);
        }
        public void Pause() { if(HasSession && !IsOpen)Open(MenuPage.Pause); }
        void Open(MenuPage page)
        {
            Page=page;Selected=0;repeatAt=0;
            if(page==MenuPage.Main || page==MenuPage.Save || page==MenuPage.Load)RefreshSlots();
            Time.timeScale=page==MenuPage.Closed?1:0;
            AudioListener.pause=page!=MenuPage.Closed;
            Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
            SelectFirst();
        }
        void SelectFirst()
        {
            var items=Items();
            for(int i=0;i<items.Count;i++)if(items[i].Enabled){Selected=i;break;}
        }
        public void Resume()
        {
            if(!HasSession)return;
            Open(MenuPage.Closed);resumeFrame=Time.frameCount+1;waitingRelease=true;
        }
        public void NewGame()
        {
            Difficulty=Difficulty.Standard;game.NewSession();HasSession=true;
            AutoSave();Resume();
        }
        public void SetDifficulty(Difficulty value)
        {
            if(!Enum.IsDefined(typeof(Difficulty),value))return;
            Difficulty=value;Notice="Difficulty: "+value+". Applies immediately to damage received.";
            if(CanSave)AutoSave();
        }
        public void AutoSave()
        {
            if(autosaveEnabled && CanSave)Save(0);
        }
        public bool Save(int slot)
        {
            if(!CanSave){Notice=SaveHint;return false;}
            try
            {
                var mission=game.Mission;
                Store.Save(slot,new NexusCheckpoint {
                    Format="nexus-checkpoint",Version=1,Id=Guid.NewGuid().ToString(),SavedUtc=DateTime.UtcNow.ToString("O"),
                    Credits=mission.Credits,Kills=mission.Kills,Target=mission.Target,Stage=(int)mission.Stage,Seed=game.Seed,Difficulty=(int)Difficulty
                });
                RefreshSlots();
                Notice=slot==0?"Nexus checkpoint saved.":"Saved to slot "+slot+".";return true;
            }
            catch(Exception error) when(error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { Notice="Could not save. Your previous checkpoint has been kept.";return false; }
        }
        public bool Load(int slot)
        {
            try
            {
                var entry=Store.Read(slot);
                if(entry.Data==null){Notice="No valid checkpoint in this slot. Current game kept.";return false;}
                var data=entry.Data;
                game.RestoreCheckpoint(data);Difficulty=(Difficulty)data.Difficulty;HasSession=true;
                Notice=entry.Recovered?"Recovered the previous checkpoint from its backup.":"Nexus checkpoint loaded.";
                Resume();return true;
            }
            catch(Exception error) when(error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {Notice="Could not load this checkpoint. Current game kept.";return false;}
        }
        public bool Continue()
        {
            var latest=Store.Latest();
            if(latest==null){Notice="No valid checkpoint available.";return false;}
            return Load(latest.Index);
        }
        public void MissionStarted(){Notice="Mission active. Return to the Nexus to save.";}
        public void OpenDeployment(){if(CanSave)Open(MenuPage.Deployment);}
        void PersistDeployment()
        {
            try{Store.SaveSettings(Settings);Notice="Next deployment updated. Accept a contract at the uplink.";}
            catch(Exception error) when(error is IOException || error is UnauthorizedAccessException){Notice="Deployment changed for this session; preferences could not be saved.";}
        }
        public void ReturnToMain()
        {
            game.EndSession();HasSession=false;Open(MenuPage.Main);
        }
        void Confirm(string text,Action action)
        {
            confirmReturn=Page;confirmation=action;question=text;Open(MenuPage.Confirm);
        }
        void OpenOptions(){optionsReturn=Page;Draft=Settings.Copy();Open(MenuPage.Options);}
        public void Back()
        {
            switch(Page)
            {
                case MenuPage.Pause:Resume();break;
                case MenuPage.Options:Draft=null;Open(optionsReturn);break;
                case MenuPage.Audio:case MenuPage.Display:case MenuPage.Controls:Open(MenuPage.Options);break;
                case MenuPage.Difficulty:case MenuPage.Save:Open(MenuPage.Pause);break;
                case MenuPage.Deployment:Open(MenuPage.Pause);break;
                case MenuPage.Load:Open(loadReturn);break;
                case MenuPage.Confirm:confirmation=null;Open(confirmReturn);break;
                case MenuPage.KeepDisplay:RevertDisplay();break;
            }
        }
        public void Highlight(int index)
        {
            var items=Items();if(index>=0 && index<items.Count && items[index].Enabled)Selected=index;
        }
        public void Move(int direction)
        {
            var items=Items();if(items.Count==0)return;
            for(int step=1;step<=items.Count;step++)
            {int next=(Selected+direction*step+items.Count*2)%items.Count;if(items[next].Enabled){Selected=next;return;}}
        }
        public void Activate(string id)
        {
            foreach(var item in Items()) if(item.Id==id && item.Enabled){item.Activate?.Invoke();return;}
        }
        public void AdjustSelected(int direction)
        {
            var items=Items();if(Selected<items.Count && items[Selected].Enabled)items[Selected].Adjust?.Invoke(direction);
        }
        MenuEntry Item(string id,string label,Action action,bool enabled=true,string value=null,Action<int> adjust=null)
            =>new MenuEntry{Id=id,Label=label,Activate=action,Enabled=enabled,Value=value,Adjust=adjust};
        MenuEntry Setting(string id,string label,Func<string> value,Action<int> adjust)
            =>Item(id,label,()=>adjust(1),true,value(),adjust);
        public List<MenuEntry> Items()
        {
            var list=new List<MenuEntry>();
            switch(Page)
            {
                case MenuPage.Main:
                    list.Add(Item("new","NEW GAME",()=> {if(LatestListed()!=null || HasSession)Confirm("Start a new game? The autosave will be replaced. Manual saves are kept.",NewGame);else NewGame();}));
                    list.Add(Item("continue","CONTINUE",()=>Continue(),LatestListed()!=null));
                    list.Add(Item("load","LOAD GAME",()=>{loadReturn=Page;Open(MenuPage.Load);}));
                    list.Add(Item("options","OPTIONS",OpenOptions));
                    list.Add(Item("exit","EXIT TO DESKTOP",()=>Confirm("Exit to desktop?",Quit)));break;
                case MenuPage.Pause:
                    list.Add(Item("resume","RESUME",Resume));
                    list.Add(Item("save","SAVE CHECKPOINT",()=>Open(MenuPage.Save),CanSave));
                    list.Add(Item("load","LOAD GAME",()=>{loadReturn=Page;Open(MenuPage.Load);}));
                    list.Add(Item("difficulty","DIFFICULTY",()=>Open(MenuPage.Difficulty),true,Difficulty.ToString().ToUpperInvariant()));
                    list.Add(Item("options","OPTIONS",OpenOptions));
                    if(CanSave)list.Add(Item("deployment","NEXT DEPLOYMENT",OpenDeployment));
                    list.Add(Item("main","RETURN TO MAIN MENU",()=>Confirm("Return to the main menu? Unsaved progress will be lost.",ReturnToMain)));
                    list.Add(Item("exit","EXIT TO DESKTOP",()=>Confirm("Exit to desktop? Unsaved progress will be lost.",Quit)));break;
                case MenuPage.Options:
                    list.Add(Item("audio","AUDIO",()=>Open(MenuPage.Audio)));
                    list.Add(Item("display","DISPLAY & GRAPHICS",()=>Open(MenuPage.Display)));
                    list.Add(Item("controls","CONTROLLER",()=>Open(MenuPage.Controls)));
                    list.Add(Item("defaults","RESTORE DEFAULTS",()=>{Draft=new GameSettings();Notice="Defaults selected. Apply to keep them.";}));
                    list.Add(Item("apply","APPLY CHANGES",ApplyDraft));
                    list.Add(Item("back","CANCEL / BACK",Back));break;
                case MenuPage.Audio:
                    list.Add(Setting("master","MASTER VOLUME",()=>Percent(Draft.Master),d=>Draft.Master=Mathf.Clamp01(Draft.Master+d*.1f)));
                    list.Add(Setting("music","MUSIC VOLUME",()=>Percent(Draft.Music),d=>Draft.Music=Mathf.Clamp01(Draft.Music+d*.1f)));
                    list.Add(Setting("mute","MUSIC MUTE",()=>Draft.MusicMuted?"ON":"OFF",d=>Draft.MusicMuted=!Draft.MusicMuted));
                    list.Add(Setting("effects","EFFECTS VOLUME",()=>Percent(Draft.Effects),d=>Draft.Effects=Mathf.Clamp01(Draft.Effects+d*.1f)));
                    list.Add(Item("back","BACK",Back));break;
                case MenuPage.Deployment:
                    list.Add(Setting("biome","BIOME",()=>Settings.Biome<0?"RANDOM":VideoGame.Core.Deployment.Name((BiomeId)Settings.Biome),d=>{Settings.Biome=Cycle(Settings.Biome+1,d,7)-1;PersistDeployment();}));
                    list.Add(Setting("size","MAP SIZE",()=>VideoGame.Core.Deployment.SizeName(Settings.SectorSize),d=>{int[] sizes={0,6,10,20};Settings.SectorSize=sizes[Cycle(Array.IndexOf(sizes,Settings.SectorSize),d,4)];PersistDeployment();}));
                    list.Add(Item("back","BACK",Back));break;
                case MenuPage.Display:
                    list.Add(Setting("resolution","RESOLUTION",()=>Draft.Width+" x "+Draft.Height,ChangeResolution));
                    list.Add(Setting("window","WINDOW MODE",()=>new[]{"WINDOWED","BORDERLESS","FULLSCREEN"}[Draft.WindowMode],d=>Draft.WindowMode=Cycle(Draft.WindowMode,d,3)));
                    list.Add(Setting("quality","GRAPHICS QUALITY",()=>new[]{"LOW","MEDIUM","HIGH"}[Draft.Quality],d=>Draft.Quality=Cycle(Draft.Quality,d,3)));
                    list.Add(Setting("vsync","VSYNC",()=>Draft.VSync==1?"ON":"OFF",d=>Draft.VSync=1-Draft.VSync));
                    list.Add(Setting("fps","FRAME RATE LIMIT",()=>Draft.FrameRate<0?"UNLIMITED":Draft.FrameRate.ToString(),d=>{int[] rates={60,120,144,-1};Draft.FrameRate=rates[Cycle(Array.IndexOf(rates,Draft.FrameRate),d,4)];}));
                    list.Add(Setting("shake","SCREEN SHAKE",()=>Percent(Draft.ScreenShake),d=>Draft.ScreenShake=Mathf.Clamp01(Draft.ScreenShake+d*.1f)));
                    list.Add(Setting("reduced","REDUCED EFFECTS",()=>Draft.ReducedEffects?"ON":"OFF",d=>Draft.ReducedEffects=!Draft.ReducedEffects));
                    list.Add(Item("back","BACK",Back));break;
                case MenuPage.Controls:
                    list.Add(Setting("deadzone","AIM DEADZONE",()=>Percent(Draft.AimDeadzone),d=>Draft.AimDeadzone=Mathf.Clamp(Draft.AimDeadzone+d*.05f,.1f,.4f)));
                    list.Add(Setting("assist","AIM ASSIST",()=>Draft.AimAssist?"ON":"OFF",d=>Draft.AimAssist=!Draft.AimAssist));
                    list.Add(Item("back","BACK",Back));break;
                case MenuPage.Difficulty:
                    foreach(Difficulty value in Enum.GetValues(typeof(Difficulty))) {var choice=value;list.Add(Item("difficulty-"+(int)choice,choice.ToString().ToUpperInvariant(),()=>SetDifficulty(choice),true,choice==Difficulty?"ACTIVE":""));}
                    list.Add(Item("back","BACK",Back));break;
                case MenuPage.Save:case MenuPage.Load:
                    int first=Page==MenuPage.Save?1:0;
                    for(int i=first;i<=3;i++)
                    {
                        int slot=i;var saved=slots?[slot] ?? Store.Read(slot);bool saving=Page==MenuPage.Save;
                        string label=(slot==0?"AUTOSAVE":"SLOT "+slot)+"  /  "+(saved.Data!=null?"NEXUS":saved.Exists?"UNAVAILABLE":"EMPTY");
                        string detail=saved.Data==null?(saved.Error??""):DateTime.Parse(saved.Data.SavedUtc).ToLocalTime().ToString("dd MMM HH:mm")+" / "+saved.Data.Credits+" CR"+(saved.Recovered?" / BACKUP":"");
                        list.Add(Item("slot-"+slot,label,()=>
                        {
                            if(saving){if(saved.Exists)Confirm("Overwrite slot "+slot+"?",()=>{Save(slot);Open(MenuPage.Save);});else Save(slot);}
                            else if(HasSession)Confirm("Load this Nexus checkpoint? Unsaved progress will be lost.",()=>Load(slot));else Load(slot);
                        },saving?CanSave:saved.Data!=null,detail));
                    }
                    list.Add(Item("back","BACK",Back));break;
                case MenuPage.Confirm:
                    list.Add(Item("cancel","CANCEL",Back));list.Add(Item("confirm","CONFIRM",()=>{var action=confirmation;confirmation=null;action?.Invoke();}));break;
                case MenuPage.KeepDisplay:
                    list.Add(Item("keep","KEEP CHANGES",KeepDisplay));list.Add(Item("revert","REVERT",RevertDisplay));break;
            }
            return list;
        }
        static int Cycle(int index,int direction,int count)=>(index+direction+count)%count;
        static string Percent(float value)=>Mathf.RoundToInt(value*100)+"%";
        void ChangeResolution(int direction)
        {
            var sizes=new List<Vector2Int>{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)};
            foreach(var resolution in Screen.resolutions)
            {var size=new Vector2Int(resolution.width,resolution.height);if(size.x>=800 && size.y>=600 && !sizes.Contains(size))sizes.Add(size);}
            int current=sizes.FindIndex(size=>size.x==Draft.Width && size.y==Draft.Height);
            var next=sizes[Cycle(Mathf.Max(0,current),direction,sizes.Count)];Draft.Width=next.x;Draft.Height=next.y;
        }
        public void ApplyDraft()
        {
            Draft.Sanitize();beforeDisplay=Settings.Copy();
            bool display=Draft.Width!=Settings.Width || Draft.Height!=Settings.Height || Draft.WindowMode!=Settings.WindowMode;
            Settings=Draft.Copy();ApplySettings(Settings,display);
            if(display){displayPreview=true;displayDeadline=Time.realtimeSinceStartup+15;Open(MenuPage.KeepDisplay);}
            else PersistSettings();
        }
        void PersistSettings()
        {
            try{Store.SaveSettings(Settings);Notice="Options saved.";Draft=Settings.Copy();Open(MenuPage.Options);}
            catch(Exception error) when(error is IOException || error is UnauthorizedAccessException)
            {Draft=Settings.Copy();Open(MenuPage.Options);Notice="Options applied for this session, but could not be written to disk.";}
        }
        public void KeepDisplay(){displayPreview=false;PersistSettings();}
        public void RevertDisplay()
        {
            if(!displayPreview)return;
            displayPreview=false;Settings=beforeDisplay;ApplySettings(Settings,true);Draft=Settings.Copy();
            Notice="Display changes reverted.";Open(MenuPage.Options);
        }
        public void SetMusicMuted(bool muted)
        {
            Settings.MusicMuted=muted;
            if(!autosaveEnabled)return;
            try{Store.SaveSettings(Settings);}
            catch(Exception error) when(error is IOException || error is UnauthorizedAccessException){Notice="Music changed for this session; preferences could not be saved.";}
        }
        void ApplySettings(GameSettings settings,bool display)
        {
            settings.Sanitize();game.Music.SetEnabled(!settings.MusicMuted);
            QualitySettings.pixelLightCount=new[]{2,4,6}[settings.Quality];
            QualitySettings.antiAliasing=new[]{0,2,4}[settings.Quality];
            QualitySettings.shadowDistance=new[]{20f,40f,60f}[settings.Quality];
            QualitySettings.shadows=settings.Quality==0?ShadowQuality.Disable:ShadowQuality.All;
            QualitySettings.vSyncCount=settings.VSync;Application.targetFrameRate=settings.FrameRate;
            if(display && SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null && !Application.isBatchMode)
                Screen.SetResolution(settings.Width,settings.Height,settings.WindowMode==0?FullScreenMode.Windowed:settings.WindowMode==1?FullScreenMode.FullScreenWindow:FullScreenMode.ExclusiveFullScreen);
        }
        void Quit()
        {
            Time.timeScale=1;AudioListener.pause=false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void Update()
        {
            var keyboard=Keyboard.current;var pad=Gamepad.current;
            if(waitingRelease && !(Mouse.current?.leftButton.isPressed==true || Mouse.current?.rightButton.isPressed==true || pad?.rightTrigger.isPressed==true || pad?.rightShoulder.isPressed==true || pad?.buttonSouth.isPressed==true || keyboard?.eKey.isPressed==true || keyboard?.qKey.isPressed==true || keyboard?.spaceKey.isPressed==true))waitingRelease=false;
            if(displayPreview && Time.realtimeSinceStartup>=displayDeadline)RevertDisplay();
            if(keyboard?.escapeKey.wasPressedThisFrame==true || pad?.startButton.wasPressedThisFrame==true)
            {if(!IsOpen)Pause();else Back();return;}
            if(!IsOpen)
            {
                if(Mouse.current?.leftButton.wasPressedThisFrame==true && new Rect(220,90,160,24).Contains(PointerPosition))Pause();
                return;
            }
            if(Mouse.current?.leftButton.wasPressedThisFrame==true){GetComponent<MenuView>().Pointer(PointerPosition,true);return;}
            if(Mouse.current!=null && Mouse.current.delta.ReadValue().sqrMagnitude>.01f)GetComponent<MenuView>().Pointer(PointerPosition,false);
            Vector2 stick=pad?.leftStick.ReadValue()??Vector2.zero;
            Vector2 dpad=pad?.dpad.ReadValue()??Vector2.zero;
            int vertical=keyboard?.upArrowKey.isPressed==true || stick.y>.6f || dpad.y>.5f?-1:keyboard?.downArrowKey.isPressed==true || stick.y<-.6f || dpad.y<-.5f?1:0;
            int horizontal=keyboard?.leftArrowKey.isPressed==true || stick.x<-.6f || dpad.x<-.5f?-1:keyboard?.rightArrowKey.isPressed==true || stick.x>.6f || dpad.x>.5f?1:0;
            if(vertical==0 && horizontal==0)repeatAt=0;
            else if(Time.realtimeSinceStartup>=repeatAt){if(vertical!=0)Move(vertical);else AdjustSelected(horizontal);repeatAt=Time.realtimeSinceStartup+.2f;}
            if(keyboard?.tabKey.wasPressedThisFrame==true)Move(keyboard.shiftKey.isPressed?-1:1);
            if(pad?.buttonEast.wasPressedThisFrame==true) {Back();return;}
            if(keyboard?.enterKey.wasPressedThisFrame==true || pad?.buttonSouth.wasPressedThisFrame==true)
            {var items=Items();if(Selected<items.Count && items[Selected].Enabled)items[Selected].Activate?.Invoke();}
        }
        void OnDestroy()
        {
            if(displayPreview && beforeDisplay!=null)ApplySettings(beforeDisplay,true);
            Time.timeScale=1;AudioListener.pause=false;
        }
    }
}
