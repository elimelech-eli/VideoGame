using System;
using System.IO;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    public enum Difficulty { Story, Standard, Hard, Nightmare }

    [Serializable]
    public sealed class GameSettings
    {
        public int Version = 1;
        public float Master = 1, Music = 1, Effects = 1;
        public int Quality = 2, Width = 1280, Height = 720, WindowMode, VSync, FrameRate = 120;
        public float AimDeadzone = .2f;
        public float ScreenShake = .5f;
        public bool ReducedEffects;
        public bool AimAssist = true, MusicMuted;
        public int Biome = -1, SectorSize;
        public GameSettings Copy() => JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(this));
        public void Sanitize()
        {
            Master = SafeVolume(Master); Music = SafeVolume(Music); Effects = SafeVolume(Effects);
            ScreenShake = float.IsNaN(ScreenShake) || float.IsInfinity(ScreenShake) ? .5f : Mathf.Clamp01(ScreenShake);
            if(Biome < -1 || Biome > 5)Biome=-1;
            if(SectorSize!=0 && SectorSize!=6 && SectorSize!=10 && SectorSize!=20)SectorSize=0;
            Quality = Mathf.Clamp(Quality,0,2); WindowMode = Mathf.Clamp(WindowMode,0,2);
            Width = Mathf.Clamp(Width,800,7680); Height = Mathf.Clamp(Height,600,4320);
            VSync = Mathf.Clamp(VSync,0,1);
            if (FrameRate != 60 && FrameRate != 120 && FrameRate != 144 && FrameRate != -1) FrameRate = 120;
            AimDeadzone = float.IsNaN(AimDeadzone) || float.IsInfinity(AimDeadzone) ? .2f : Mathf.Clamp(AimDeadzone,.1f,.4f);
        }
        static float SafeVolume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1 : Mathf.Clamp01(value);
    }

    [Serializable]
    public sealed class NexusCheckpoint
    {
        public string Format, SavedUtc, Id;
        public int Version, Credits, Kills, Target, Stage, Seed, Difficulty;
        public GearProfile Equipment;
        public void NormalizeEmptySlots()
        {
            // JsonUtility represents null entries of inline serializable arrays as
            // empty objects. Only an entirely empty slot is a legal placeholder.
            if(Equipment?.Equipped==null)return;
            for(int i=0;i<Equipment.Equipped.Length;i++)
            {
                var item=Equipment.Equipped[i];
                if(item!=null && string.IsNullOrEmpty(item.Id) && string.IsNullOrEmpty(item.Definition) && (item.Bonuses==null || item.Bonuses.Count==0))Equipment.Equipped[i]=null;
            }
        }
        public bool Valid()
        {
            if (Format != "nexus-checkpoint" || (Version != 1 && Version != 2) || !Guid.TryParse(Id,out _) ||
                !DateTime.TryParse(SavedUtc,null,System.Globalization.DateTimeStyles.RoundtripKind,out _) ||
                Credits < 0 || Target != 8 || Difficulty < 0 || Difficulty > 3) return false;
            if(Version==2 && (Equipment==null || !Equipment.Valid() || Equipment.Credits!=Credits))return false;
            return Stage == (int)MissionStage.Complete ? Kills == Target :
                (Stage == (int)MissionStage.Available && Kills == 0) || (Stage == (int)MissionStage.Rewarded && Kills == Target);
        }
    }
    public sealed class CheckpointSlot
    {
        public int Index;
        public NexusCheckpoint Data;
        public bool Recovered, Exists;
        public string Error;
    }

    // Preferences and checkpoints are independent. Writes use a same-directory
    // temporary file and atomic replacement; one previous valid revision is kept.
    public sealed class SessionStore
    {
        public string DirectoryPath { get; }
        public SessionStore(string directory) { DirectoryPath = directory; }
        string PathFor(int slot)
        {
            if(slot < 0 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(DirectoryPath,"checkpoint-"+slot+".json");
        }
        public static void AtomicWrite(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path+".tmp";
            try
            {
                using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
                {
                    var bytes=System.Text.Encoding.UTF8.GetBytes(text);stream.Write(bytes,0,bytes.Length);stream.Flush(true);
                }
                if(File.Exists(path)) File.Replace(temporary,path,path+".bak");
                else File.Move(temporary,path);
            }
            finally { if(File.Exists(temporary)) File.Delete(temporary); }
        }
        public void Save(int slot,NexusCheckpoint data)
        {
            if(data == null || !data.Valid()) throw new InvalidDataException("Invalid Nexus checkpoint.");
            string path=PathFor(slot);
            if(Read(slot).Recovered)File.Copy(path+".bak",path,true);
            string json;
            try{json=JsonUtility.ToJson(data,true);}
            finally{data.NormalizeEmptySlots();}
            AtomicWrite(path,json);
        }
        public CheckpointSlot Read(int slot)
        {
            var result = new CheckpointSlot {Index=slot};
            string path=PathFor(slot);
            foreach(string candidate in new[]{path,path+".bak"})
            {
                if(!File.Exists(candidate))continue;
                result.Exists=true;
                try
                {
                    var data=JsonUtility.FromJson<NexusCheckpoint>(File.ReadAllText(candidate));
                    data?.NormalizeEmptySlots();
                    if(data!=null && data.Valid()) {result.Data=data;result.Recovered=candidate!=path;return result;}
                }
                catch(Exception error) when(error is IOException || error is ArgumentException || error is UnauthorizedAccessException) { }
            }
            if(result.Exists) result.Error="Unreadable or unsupported checkpoint";
            return result;
        }
        public CheckpointSlot Latest()
        {
            CheckpointSlot latest=null;
            for(int i=0;i<4;i++)
            {
                var slot=Read(i);
                if(slot.Data!=null && (latest==null || DateTime.Parse(slot.Data.SavedUtc).ToUniversalTime()>DateTime.Parse(latest.Data.SavedUtc).ToUniversalTime()))latest=slot;
            }
            return latest;
        }
        public GameSettings LoadSettings()
        {
            foreach(string suffix in new[]{"",".bak"})
            {
                try
                {
                    string path=Path.Combine(DirectoryPath,"settings.json")+suffix;
                    if(!File.Exists(path))continue;
                    var settings=JsonUtility.FromJson<GameSettings>(File.ReadAllText(path));
                    if(settings!=null && settings.Version==1){settings.Sanitize();return settings;}
                }
                catch(Exception error) when(error is IOException || error is ArgumentException || error is UnauthorizedAccessException) { }
            }
            return new GameSettings();
        }
        public void SaveSettings(GameSettings settings) => AtomicWrite(Path.Combine(DirectoryPath,"settings.json"),JsonUtility.ToJson(settings,true));
    }
}
