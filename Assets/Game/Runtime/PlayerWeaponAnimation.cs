using System.Collections.Generic;
using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Legacy clips own locomotion/body reactions. This late arm layer consumes
    // accepted combat events; it never moves the actor or applies damage.
    [DefaultExecutionOrder(100)]
    public sealed class PlayerWeaponAnimation : MonoBehaviour
    {
        public const float SwingDuration=.50f, SwordContactTime=.14f;
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        readonly Transform[] weapons=new Transform[2],muzzles=new Transform[2];
        readonly TrailRenderer[] trails=new TrailRenderer[2];
        readonly GearKind?[] kinds=new GearKind?[2];
        readonly float[] fired={-100,-100},swung={-100,-100},blocked={-100,-100},weights={1,1};
        Pilot pilot;
        ActorAnimation motion;
        Transform assembly;
        Quaternion gripBasis;
        float height,guardBlend,braceBlend;
        public float GuardWeight => guardBlend;
        public float BraceWeight => braceBlend;
        public Transform Weapon(int hand)=>weapons[hand];
        public float SwingAge(int hand)=>Time.time-swung[hand];
        public Vector3 Muzzle(int hand)=>muzzles[hand]!=null?muzzles[hand].position:pilot.transform.position+Vector3.up*.25f;
        void Awake()
        {
            pilot=GetComponentInParent<Pilot>();motion=GetComponent<ActorAnimation>();
            foreach(var t in GetComponentsInChildren<Transform>())bones[t.name]=t;
            foreach(var required in new[]{"chest","hips","foot_L","upper_arm_R","forearm_R","hand_R","upper_arm_L","forearm_L","hand_L","SOCKET_weapon_R"})
                if(!bones.ContainsKey(required))throw new System.InvalidOperationException("Weapon rig requires "+required);
            height=Vector3.Distance(bones["hips"].position,bones["foot_L"].position);
            gripBasis=bones["SOCKET_weapon_R"].localRotation;
            assembly=new GameObject("Player held equipment").transform;assembly.SetParent(pilot.transform.parent,false);
        }
        public void Refresh(GearProfile profile)
        {
            foreach(var renderer in bones["SOCKET_weapon_R"].GetComponentsInChildren<Renderer>())renderer.enabled=false;
            for(int h=0;h<2;h++)
            {
                GearKind? kind=profile.Equipped[h+3]?.Design.Kind;
                if(kinds[h]==kind && (kind==null || weapons[h]!=null))continue;
                if(weapons[h]!=null){weapons[h].gameObject.SetActive(false);Destroy(weapons[h].gameObject);}
                kinds[h]=kind;weapons[h]=muzzles[h]=null;trails[h]=null;
                if(kind==null)continue;
                var source=Resources.Load<GameObject>("LoadoutWeapons/"+kind.Value);
                if(source==null)throw new System.InvalidOperationException("Missing held weapon "+kind);
                weapons[h]=Instantiate(source,assembly).transform;weapons[h].name="Held "+(h==0?"R ":"L ")+kind;
                weapons[h].localScale=Vector3.one*height;muzzles[h]=weapons[h].Find("Muzzle");
                if(kind==GearKind.Sword)
                {
                    var tip=new GameObject("Sword tip trail");tip.transform.SetParent(weapons[h],false);tip.transform.localPosition=Vector3.up*.98f;
                    var trail=tip.AddComponent<TrailRenderer>();trail.sharedMaterial=Resources.Load<Material>("PrototypeTrace");trail.time=.12f;trail.minVertexDistance=.02f;
                    trail.startWidth=.06f;trail.endWidth=0;trail.startColor=h==0?Art.Cyan:Art.Amber;trail.endColor=new Color(0,0,0,0);
                    trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;trail.emitting=false;trails[h]=trail;
                }
            }
            FollowWeapons();
        }
        public void ResetActions()
        {
            for(int h=0;h<2;h++){fired[h]=swung[h]=blocked[h]=-100;weights[h]=1;if(trails[h]!=null){trails[h].emitting=false;trails[h].Clear();}}
            guardBlend=braceBlend=0;
        }
        public void Shot(int hand){fired[hand]=Time.time;}
        public void Swing(int hand){swung[hand]=Time.time;}
        public void Block(int hand){blocked[hand]=Time.time;}
        void LateUpdate()
        {
            if(pilot==null || Time.timeScale==0 || Prototype.Instance?.Menus?.IsOpen==true)return;
            if(!motion.Dead && motion.enabled)
            {
                guardBlend=Mathf.MoveTowards(guardBlend,pilot.Hands.Guarding?1:0,Time.deltaTime*10);
                braceBlend=Mathf.MoveTowards(braceBlend,pilot.Hands.Bracing?1:0,Time.deltaTime*14);
                bool casting=motion.Player.IsPlaying("Cast") || motion.CurrentState=="Dash";
                Vector3 aim=pilot.transform.position+pilot.transform.forward*22;
                if(kinds[0]==GearKind.Handgun || kinds[0]==GearKind.Rifle || kinds[1]==GearKind.Handgun)
                    aim=pilot.GunAimPoint(pilot.transform.position+Vector3.up*.25f);
                // The body keeps its sampled locomotion/hit motion; only add a
                // small attack twist before solving the two independent arms.
                float twist=0;
                for(int h=0;h<2;h++)if(kinds[h]==GearKind.Sword && SwingAge(h)<SwingDuration)
                    twist+=(h==0?1:-1)*Mathf.Sin(Mathf.Clamp01(SwingAge(h)/SwingDuration)*Mathf.PI)*10;
                bones["chest"].rotation=Quaternion.AngleAxis(twist,pilot.transform.up)*bones["chest"].rotation;
                for(int h=0;h<2;h++)
                {
                    bool active=Time.time-fired[h]<.2f || SwingAge(h)<SwingDuration || kinds[h]==GearKind.Shield&&guardBlend>.01f || kinds[0]==GearKind.Rifle&&Time.time-fired[0]<.2f;
                    weights[h]=Mathf.MoveTowards(weights[h],casting&&!active?0:1,Time.deltaTime*12);
                    PoseArm(h,aim,weights[h]);
                }
            }
            FollowWeapons();
            for(int h=0;h<2;h++)if(trails[h]!=null)trails[h].emitting=!motion.Dead&&SwingAge(h)>=.12f&&SwingAge(h)<.25f;
        }
        void PoseArm(int hand,Vector3 aim,float weight)
        {
            string side=hand==0?"R":"L";float sign=hand==0?1:-1;var kind=kinds[hand];
            bool rifle=kinds[0]==GearKind.Rifle;bool support=hand==1&&rifle;
            float recoil=Pulse(Time.time-fired[support?0:hand],.18f);
            float block=Pulse(Time.time-blocked[hand],.22f);
            Vector3 offset;Quaternion local=Quaternion.identity;
            if(support)offset=new Vector3(-.04f,-.065f,.48f);
            else if(rifle&&hand==0)offset=new Vector3(.13f,-.05f,.24f);
            else if(kind==GearKind.Handgun)offset=new Vector3(sign*.27f,-.10f,.35f);
            else if(kind==GearKind.Sword){offset=new Vector3(sign*.34f,-.41f,.12f);local=Quaternion.Euler(8,0,-sign*24);}
            else if(kind==GearKind.Shield)
            {
                offset=Vector3.Lerp(new Vector3(-.34f,-.48f,.10f),new Vector3(-.23f,-.08f,.35f),guardBlend);
                offset.z-=block*.08f;local=Quaternion.Euler(Mathf.Lerp(20,-10,guardBlend)+block*12,-10,Mathf.Lerp(-15,5,guardBlend));
            }
            else offset=new Vector3(sign*.30f,-.61f,.02f);
            float age=SwingAge(hand);
            if(kind==GearKind.Sword && age<SwingDuration)
            {
                var rest=offset;var restRotation=local;
                Vector3 windup=new Vector3(sign*.43f,-.02f,.08f),finish=new Vector3(-sign*.20f,-.27f,.45f);
                if(hand==1){windup.y-=.08f;finish.y-=.08f;}
                Quaternion high=Quaternion.Euler(-30,-sign*55,-sign*15),low=Quaternion.Euler(25,sign*55,-sign*88);
                if(age<.12f){float t=Ease(age/.12f);offset=Vector3.Lerp(rest,windup,t);local=Quaternion.Slerp(restRotation,high,t);}
                else if(age<.25f){float t=Ease((age-.12f)/.13f);offset=Vector3.Lerp(windup,finish,t);local=Quaternion.Slerp(high,low,t);}
                else {float t=Ease((age-.25f)/.25f);offset=Vector3.Lerp(finish,rest,t);local=Quaternion.Slerp(low,restRotation,t);}
            }
            bool gun=kind==GearKind.Handgun || kind==GearKind.Rifle;
            if(gun || support)
            {
                offset.z-=recoil*.055f;
                if(rifle)
                {
                    offset.y-=braceBlend*.08f;offset.z-=braceBlend*.08f;
                    if(!support)local=Quaternion.Slerp(local,Quaternion.Euler(8,-55,-10),braceBlend*(1-recoil));
                }
                if(pilot.Hands.IsReloading(support?0:hand))
                {
                    float cycle=Mathf.Sin(pilot.Hands.ReloadProgress(support?0:hand)*Mathf.PI);
                    offset.y-=cycle*(support?.20f:.13f);offset.z-=cycle*.12f;
                    local=Quaternion.Euler(cycle*25,0,sign*cycle*18);
                }
            }
            var upper=bones["upper_arm_"+side];var lower=bones["forearm_"+side];var wrist=bones["hand_"+side];
            Quaternion a=upper.localRotation,b=lower.localRotation,c=wrist.localRotation;
            Vector3 target=bones["chest"].position+pilot.transform.TransformDirection(offset)*height;
            Quaternion rifleRotation=bones["hand_R"].rotation*gripBasis;
            if(support)
            {
                float cycle=pilot.Hands.IsReloading(0)?Mathf.Sin(pilot.Hands.ReloadProgress(0)*Mathf.PI):0;
                Vector3 grip=Vector3.Lerp(new Vector3(0,.065f,.34f),new Vector3(0,-.045f,.15f),cycle);
                target=bones["hand_R"].position+rifleRotation*(grip*height);
            }
            Solve(side,target,pilot.transform.TransformDirection(new Vector3(sign,-.75f,-.1f)));
            if(kind!=null)
            {
                Quaternion rotation=pilot.transform.rotation*local;
                if(gun)
                {
                    Vector3 direction=aim-wrist.position;if(direction.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(direction,pilot.transform.up)*local;
                    rotation*=Quaternion.Euler(-recoil*8,0,0);
                }
                wrist.rotation=rotation*Quaternion.Inverse(gripBasis);
            }
            else if(support)wrist.rotation=rifleRotation*Quaternion.Inverse(gripBasis);
            upper.localRotation=Quaternion.Slerp(a,upper.localRotation,weight);lower.localRotation=Quaternion.Slerp(b,lower.localRotation,weight);wrist.localRotation=Quaternion.Slerp(c,wrist.localRotation,weight);
        }
        static float Ease(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
        static float Pulse(float age,float duration)=>age>=0&&age<duration?Mathf.Pow(1-age/duration,2):0;
        void FollowWeapons()
        {
            for(int h=0;h<2;h++)if(weapons[h]!=null)
            {
                var hand=bones[h==0?"hand_R":"hand_L"];
                weapons[h].SetPositionAndRotation(hand.position,hand.rotation*gripBasis);
            }
        }
        void Aim(string a,string b,Vector3 direction){var bone=bones[a];bone.rotation=Quaternion.FromToRotation(bones[b].position-bone.position,direction)*bone.rotation;}
        void Solve(string side,Vector3 target,Vector3 pole)
        {
            var upper=bones["upper_arm_"+side];var lower=bones["forearm_"+side];var hand=bones["hand_"+side];Vector3 shoulder=upper.position;
            float a=Vector3.Distance(shoulder,lower.position),b=Vector3.Distance(lower.position,hand.position);Vector3 ray=target-shoulder;
            float distance=Mathf.Clamp(ray.magnitude,Mathf.Abs(a-b)+.002f,a+b-.002f);Vector3 axis=ray.normalized,bend=Vector3.ProjectOnPlane(pole,axis).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);Vector3 elbow=shoulder+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Aim("upper_arm_"+side,"forearm_"+side,elbow-shoulder);Aim("forearm_"+side,"hand_"+side,target-lower.position);
        }
        void OnDestroy(){if(assembly!=null){assembly.gameObject.SetActive(false);Destroy(assembly.gameObject);}}
    }
}
