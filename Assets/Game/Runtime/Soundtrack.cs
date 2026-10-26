using UnityEngine;
using UnityEngine.InputSystem;

namespace VideoGame
{
    public sealed class Soundtrack : MonoBehaviour
    {
        public const float FadeSeconds = 1.5f;
        public AudioSource Nexus { get; private set; }
        public AudioSource Battle { get; private set; }
        public bool InBattle { get; private set; }
        public bool Enabled { get; private set; } = true;
        public string Theme => InBattle ? "Battle" : "Nexus";
        bool initialized;
        void Awake()
        {
            Nexus = Channel("Nexus"); Battle = Channel("Battle");
        }
        AudioSource Channel(string theme)
        {
            var source = new GameObject(theme + " soundtrack").AddComponent<AudioSource>(); source.transform.SetParent(transform);
            source.clip = Resources.Load<AudioClip>("Audio/" + theme);
            if (source.clip == null) throw new System.InvalidOperationException("Missing " + theme + " soundtrack");
            source.ignoreListenerPause = true;
            source.loop = true; source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0; source.priority = 64;
            return source;
        }
        public void SetBattle(bool battle)
        {
            if (initialized && battle == InBattle) return;
            initialized = true; InBattle = battle;
            var next = battle ? Battle : Nexus;
            if (!next.isPlaying) next.Play();
        }
        public void Toggle()
        {
            SetEnabled(!Enabled);
            Prototype.Instance.Menus?.SetMusicMuted(!Enabled);
        }
        public void SetEnabled(bool enabled)
        {
            Enabled=enabled;Nexus.mute=Battle.mute=!enabled;
        }
        void Update()
        {
            if (Keyboard.current?.mKey.wasPressedThisFrame == true && Prototype.Instance.Menus?.IsOpen != true) Toggle();
            float volume = Prototype.Instance.Player != null && Prototype.Instance.Player.IsDead ? .06f : InBattle ? .17f : .22f;
            float mix = (Prototype.Instance.Menus?.Settings.Master ?? 1) * (Prototype.Instance.Menus?.Settings.Music ?? 1);
            volume *= mix;
            Nexus.volume = Mathf.MoveTowards(Nexus.volume, InBattle ? 0 : volume, Time.unscaledDeltaTime * .22f / FadeSeconds);
            Battle.volume = Mathf.MoveTowards(Battle.volume, InBattle ? volume : 0, Time.unscaledDeltaTime * .22f / FadeSeconds);
            if (InBattle && Nexus.volume == 0 && Nexus.isPlaying) Nexus.Stop();
            if (!InBattle && Battle.volume == 0 && Battle.isPlaying) Battle.Stop();
        }
    }
}
