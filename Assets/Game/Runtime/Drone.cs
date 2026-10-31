using System.Collections;
using UnityEngine;

namespace VideoGame
{
    public enum EnemyKind { Hunter, Gunner, Rusher, Bombardier }
    public sealed class Drone : MonoBehaviour
    {
        static readonly float[] SteeringAngles = {0, 60, -60, 90, -90};
        public EnemyKind Kind;
        public bool IsLabHeavy { get; private set; }
        public bool IsTransitHeavy {get;private set;}
        public bool IsShieldedHeavy=>IsLabHeavy||IsTransitHeavy;
        public void ConfigureTransitHeavy(){IsTransitHeavy=true;SetLevel(Level);}
        public bool IsLabBoss { get; private set; }
        public float ShieldMaximum { get; private set; }
        public float Shield { get; private set; }
        public bool Enraged => IsLabBoss && Health <= HealthMaximum*.5f;
        public string BossState { get; private set; } = "Ready";
        int bossPattern;
        public void ConfigureLab(bool heavy, bool boss) { IsLabHeavy=heavy; IsLabBoss=boss; SetLevel(Level); }
        public bool Ranged => Kind == EnemyKind.Gunner || Kind == EnemyKind.Bombardier || IsShieldedHeavy || IsLabBoss;
        public float MoveSpeed => IsLabBoss ? 1.6f : IsShieldedHeavy ? 1.4f : Kind == EnemyKind.Rusher ? 6.2f : Kind == EnemyKind.Bombardier ? 1.8f : Ranged ? 2 : 3.5f;
        public const float MaxHealth = 60;
        public int Level {get;private set;}=1;
        public float HealthMaximum {get;private set;}=MaxHealth;
        public void SetLevel(int level){Level=Mathf.Clamp(level,1,100);HealthMaximum=(IsLabBoss?600:IsShieldedHeavy?180:MaxHealth)*(1+(Level-1)*.1f);Health=HealthMaximum;ShieldMaximum=IsShieldedHeavy?90*(1+(Level-1)*.1f):0;Shield=ShieldMaximum;}
        public float Health { get; private set; } = MaxHealth;
        public float LastHitTime { get; private set; } = float.NegativeInfinity;
        public bool HealthBarVisible => Health > 0 && Time.time - LastHitTime < 2;
        public ActorAnimation Motion => GetComponentInChildren<ActorAnimation>();
        public bool WindingUp { get; private set; }
        float attackAt;
        void LateUpdate()
        {
            foreach (var label in GetComponentsInChildren<TextMesh>()) label.transform.rotation = Quaternion.Euler(55,45,0);
        }
        void Update()
        {
            var game = Prototype.Instance;
            if (Time.timeScale == 0 || Health <= 0 || game == null || game.Menus?.IsOpen==true || !game.InMission || game.Player.IsDead || WindingUp) return;
            Vector3 delta = game.Player.transform.position - transform.position; delta.y = 0;
            float distance = delta.magnitude;
            if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(delta);
            if (distance > 28) return;
            float range = IsLabBoss ? 10 : Kind == EnemyKind.Bombardier ? 11 : Ranged ? 7 : 1.6f;
            // Permanent room walls require a route through a doorway. Breakable
            // cover can still be attacked, so enemy fire remains hazardous to it.
            bool blocked = Physics.Linecast(transform.position, game.Player.transform.position, out var obstruction, 1 << 9)
                && obstruction.collider.GetComponentInParent<Destructible>() == null;
            if (distance > range || blocked)
            {
                Vector3 path = game.PursuitTarget(transform.position, game.Player.transform.position) - transform.position;
                path.y = 0;
                foreach (float turn in SteeringAngles)
                {
                    Vector3 direction = Quaternion.Euler(0, turn, 0) * path.normalized;
                    Vector3 next = transform.position + direction * (MoveSpeed * Time.deltaTime);
                    if (Physics.CheckSphere(transform.position + direction * .8f, .5f, 1 << 9)) continue;
                    if (!Physics.Raycast(next + Vector3.up, Vector3.down, 3, 1 << 8)) continue;
                    transform.position = next; break;
                }
            }
            else if (Time.time >= attackAt)
            {
                attackAt = Time.time + (IsLabBoss ? (Enraged?3.2f:4.2f) : IsShieldedHeavy ? 2.2f : Kind == EnemyKind.Bombardier ? 3.4f : Ranged ? 1.5f : Kind == EnemyKind.Rusher ? .95f : 1.2f);
                StartCoroutine(Attack());
            }
        }
        IEnumerator Attack()
        {
            var game = Prototype.Instance;
            if (IsLabBoss) { yield return MedicalBossAttack(game); yield break; }
            WindingUp = true;
            Motion.Act(Kind == EnemyKind.Bombardier ? "Cast" : Ranged ? "Fire" : "Melee");
            Vector3 target = game.Player.transform.position;
            yield return new WaitForSeconds(IsShieldedHeavy ? .45f : Ranged ? .15f : .3f);
            while (game.Menus?.IsOpen == true) yield return null;
            WindingUp = false;
            if (Health <= 0 || !game.InMission || game.Player.IsDead) yield break;
            Vector3 delta = game.Player.transform.position - transform.position; delta.y = 0;
            if (Kind == EnemyKind.Bombardier)
            {
                var shell = new GameObject("Bombardier telegraphed blast"); shell.transform.SetParent(transform.parent);
                shell.AddComponent<EnemyMortar>().Initialize(ShotOrigin, target, this);
            }
            else if (Ranged)
            {
                Shoot(game, delta);
            }
            else if (delta.magnitude < 2.2f)
            {
                game.Player.Damage((Kind == EnemyKind.Rusher ? 12 : 9)*(1+(Level-1)*.06f),transform.position);
                game.Hit(game.Player.transform.position, Color.red);
            }
        }
        public Vector3 ShotOrigin { get { var socket=GetComponentsInChildren<Transform>(); foreach(var t in socket)if(t.name=="ShotOrigin")return t.position; return transform.position; } }
        void Shoot(Prototype game, Vector3 delta)
        {
                var bolt = new GameObject("Enemy bolt"); bolt.transform.SetParent(transform.parent);
                bolt.transform.position = ShotOrigin;
                var trail = bolt.AddComponent<TrailRenderer>(); trail.time = .15f; trail.startWidth = .12f; trail.endWidth = .01f;
                trail.startColor = new Color(1, .2f, .1f); trail.endColor = new Color(1, .1f, .1f, 0);
                trail.sharedMaterial = Resources.Load<Material>("PrototypeTrace");
                var projectile=bolt.AddComponent<EnemyBolt>();projectile.Direction=delta.normalized;projectile.DamageScale=(IsLabBoss?1.3f:1)*(1+(Level-1)*.06f);
                game.Feedback.Shot(ShotOrigin, ShotOrigin + delta.normalized * .4f, new Color(1, .3f, .1f));
                Motion.Kick();
        }
        IEnumerator MedicalBossAttack(Prototype game)
        {
            WindingUp=true; bool pulse=(bossPattern++%2)==0;
            BossState=pulse?"Sterilize":"Burst"; Motion.Act(pulse?"Cast":"Fire");
            yield return new WaitForSeconds(.5f);
            while(game.Menus?.IsOpen==true)yield return null;
            if(Health<=0||!game.InMission||game.Player.IsDead){WindingUp=false;yield break;}
            if(pulse)
            {
                Vector3 target=game.Player.transform.position;
                Vector3 across=Vector3.Cross(Vector3.up,(target-transform.position).normalized);
                int spread=Enraged?1:0;
                for(int i=-spread;i<=spread;i++)
                { var shell=new GameObject("Medical sterilization pulse");shell.transform.SetParent(transform.parent);shell.AddComponent<EnemyMortar>().Initialize(ShotOrigin,target+across*(i*3.5f),this); }
            }
            else for(int i=0;i<(Enraged?4:3);i++)
            {
                while(game.Menus?.IsOpen==true)yield return null;
                if(Health<=0||!game.InMission||game.Player.IsDead)break;
                Shoot(game,game.Player.transform.position-ShotOrigin);
                yield return new WaitForSeconds(.22f);
            }
            BossState="Recover";yield return new WaitForSeconds(.8f);
            WindingUp=false;if(Health>0)BossState="Ready";
        }
        public void Damage(float amount)
        {
            if (Health <= 0 || amount <= 0) return;
            LastHitTime = Time.time;
            float absorbed=Mathf.Min(Shield,amount);Shield-=absorbed;amount-=absorbed;
            if(absorbed>0)Prototype.Instance.Hit(transform.position,Art.Cyan,.6f);
            if(amount<=0){Motion.Act("Hit");return;}
            Health = Mathf.Max(0, Health - amount);
            Prototype.Instance.Hit(transform.position, Color.white, .6f);
            if (Health > 0) { Motion.Act("Hit"); return; }
            Prototype.Instance.Mission.RegisterKill();
            Prototype.Instance.Equipment.Killed(transform.position,Level);
            StopAllCoroutines(); WindingUp = false; if(IsLabBoss)BossState="Dead";
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var sign in GetComponentsInChildren<TextMesh>()) sign.gameObject.SetActive(false);
            Motion.Act("Death");
            Destroy(gameObject, 3);
        }
    }
    public sealed class EnemyBolt : MonoBehaviour
    {
        public Vector3 Direction;
        public float DamageScale=1;
        float life;
        void Update()
        {
            if (Time.timeScale == 0 || Prototype.Instance?.Menus?.IsOpen==true) return;
            life += Time.deltaTime;
            Vector3 before = transform.position;
            Vector3 step = Direction.normalized * (8 * Time.deltaTime);
            var hits = Physics.RaycastAll(before, Direction.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                // Soldiers don't block allied fire. The first solid cover or player hit does.
                if (hit.collider.GetComponentInParent<Drone>() != null) continue;
                var player = hit.collider.GetComponentInParent<Pilot>();
                if (player != null) player.Damage(12*DamageScale,before);
                else hit.collider.GetComponentInParent<Destructible>()?.Damage(12, before, Direction);
                Prototype.Instance.Feedback.Contact(hit.point, player != null);
                Destroy(gameObject); return;
            }
            transform.position += step;
            if (life > 4) Destroy(gameObject);
        }
    }
    public sealed class EnemyMortar : MonoBehaviour
    {
        public const float WarningDuration = 1.3f;
        public const float Radius = 2.7f;
        public Vector3 Target { get; private set; }
        public bool Detonated { get; private set; }
        float started;
        Vector3 launch;
        Drone owner; bool owned;
        public Drone Owner => owner;
        LineRenderer ring, arc;
        public void Initialize(Vector3 source, Vector3 target, Drone sourceActor=null)
        {
            owner=sourceActor;owned=sourceActor!=null;
            launch = source; Target = new Vector3(target.x, .08f, target.z); started = Time.time;
            ring = Line("AOE warning", 65, .09f);
            for (int i = 0; i < 65; i++) ring.SetPosition(i, Target + new Vector3(Mathf.Cos(i * Mathf.PI / 32), 0, Mathf.Sin(i * Mathf.PI / 32)) * Radius);
            arc = Line("Incoming shell", 2, .14f);
        }
        LineRenderer Line(string title, int count, float width)
        {
            var line = new GameObject(title).AddComponent<LineRenderer>(); line.transform.SetParent(transform);
            line.sharedMaterial = Resources.Load<Material>("PrototypeTrace"); line.positionCount = count;
            line.startWidth = line.endWidth = width; line.startColor = line.endColor = new Color(1, .15f, .05f); return line;
        }
        void Update()
        {
            if(owned && (owner==null || owner.Health<=0)){Destroy(gameObject);return;}
            if (ring == null || Time.timeScale == 0 || Prototype.Instance?.Menus?.IsOpen==true) return;
            float t = Mathf.Clamp01((Time.time - started) / WarningDuration);
            float pulse = .08f + .05f * Mathf.Sin(t * 40); ring.startWidth = ring.endWidth = pulse;
            Vector3 point = Vector3.Lerp(launch, Target, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 5;
            arc.SetPosition(0, point); arc.SetPosition(1, point - Vector3.up * .35f);
            if (t < 1) return;
            Detonated = true;
            Prototype.Instance.Explosion(Target, Radius);
            CombatDamage.Area(Target, Radius, 45, 24);
            Destroy(gameObject);
        }
    }
}
