using UnityEngine;
using VideoGame.Core;

namespace VideoGame
{
    // Per-hand fire/reload clocks permit simultaneous independent handguns.
    public sealed class HandCombat : MonoBehaviour
    {
        readonly float[] ready=new float[2],reload=new float[2];
        readonly int[] ammo=new int[2],reserve=new int[2];
        readonly ShieldCharge[] shields={new ShieldCharge(),new ShieldCharge()};
        readonly bool[] meleePending=new bool[2];
        readonly float[] meleeAt=new float[2],meleeDamage=new float[2];
        public bool Guarding => shields[0].Guarding || shields[1].Guarding;
        public bool Bracing => Time.time<braceUntil;
        public bool IsReloading(int hand)=>reload[hand]>Time.time;
        public float ReloadProgress(int hand)=>IsReloading(hand)?Mathf.Clamp01(1-(reload[hand]-Time.time)/1.5f):0;
        public bool HasPendingMelee(int hand)=>meleePending[hand];
        public int MainShots {get;private set;}
        public int OffShots {get;private set;}
        public float ShieldEnergy => Mathf.Min(shields[0].Charge,shields[1].Charge);
        public string State(int hand)
        {
            var item=Prototype.Instance.Equipment.Profile.Equipped[hand+3];if(item==null)return "EMPTY";
            if(item.Design.Kind==GearKind.Shield)return Mathf.RoundToInt(shields[hand].Charge*100)+"% SHIELD";
            if(item.Design.Kind==GearKind.Sword)return "MELEE";
            return Time.time<reload[hand]?"RELOADING":ammo[hand]+" / "+reserve[hand];
        }
        public void ResetState()
        {
            var p=Prototype.Instance?.Equipment?.Profile;if(p==null)return;
            for(int h=0;h<2;h++)
            {
                var item=p.Equipped[h+3];ammo[h]=item?.Design.Kind==GearKind.Rifle?30:12;
                reserve[h]=Mathf.RoundToInt((item?.Design.Kind==GearKind.Rifle?180:84)*(1+p.Bonus(BonusKind.Ammo)));
                ready[h]=reload[h]=0;shields[h]=new ShieldCharge();
                meleePending[h]=false;
            }
            MainShots=OffShots=0;
            braceUntil=braceReady=0;
        }
        public void Tick(bool main,bool off,float delta)
        {
            var game=Prototype.Instance;var p=game.Equipment.Profile;
            if(game.Menus?.IsOpen==true)return;
            if(!game.InMission || game.Player.IsDead){meleePending[0]=meleePending[1]=false;return;}
            for(int h=0;h<2;h++)
            {
                if(meleePending[h] && Time.time>=meleeAt[h]){meleePending[h]=false;ResolveMelee(meleeDamage[h]);}
                var item=p.Equipped[h+3];bool held=h==0?main:off;
                shields[h].Tick(held&&item?.Design.Kind==GearKind.Shield,delta,1+p.Bonus(BonusKind.Energy),1+p.Bonus(BonusKind.ShieldRecharge));
                if(Time.time>=reload[h] && reload[h]>0)
                {
                    int take=Mathf.Min(item?.Design.Kind==GearKind.Rifle?30:12,reserve[h]);ammo[h]=take;reserve[h]-=take;reload[h]=0;
                }
                if(!game.InMission || !held || item==null || item.Design.Kind==GearKind.Shield || Time.time<ready[h] || Time.time<reload[h])continue;
                bool sword=item.Design.Kind==GearKind.Sword;
                if(!sword && ammo[h]<=0){if(reserve[h]>0)reload[h]=Time.time+1.5f;continue;}
                ready[h]=Time.time+(sword?.72f:item.Design.Kind==GearKind.Rifle?.14f:.28f);
                if(!sword)ammo[h]--;
                if(h==0)MainShots++;else OffShots++;
                float damage=(sword?34:20)*(item.Condition/100f)*(1+(item.Level-1)*.08f)*(1+p.Bonus(BonusKind.Damage));
                if(Random.value<Mathf.Clamp01(.05f+p.Bonus(BonusKind.CriticalChance)))damage*=1.5f+p.Bonus(BonusKind.CriticalDamage);
                var pilot=game.Player;
                if(sword)
                {
                    meleeDamage[h]=damage;meleeAt[h]=Time.time+PlayerWeaponAnimation.SwordContactTime;meleePending[h]=true;
                    pilot.Weapons?.Swing(h);
                }
                else
                {
                    pilot.Weapons?.Shot(h);
                    if(item.Design.Kind==GearKind.Rifle){pilot.Motion.Act("Fire");pilot.Motion.Kick();}
                    Vector3 start=transform.position+Vector3.up*.25f;
                    Vector3 direction=(pilot.GunAimPoint(start)-start).normalized;
                    float spread=(item.Design.Kind==GearKind.Handgun?1.5f:.35f)*(1-p.Accuracy);
                    direction=Quaternion.AngleAxis(Random.Range(-spread,spread),Vector3.up)*direction;
                    Vector3 end=start+direction*22;
                    var hits=Physics.RaycastAll(start,direction,22,~0,QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
                    foreach(var hit in hits)
                    {
                        if(hit.collider.GetComponentInParent<Pilot>()==pilot)continue;
                        end=hit.point;CombatDamage.Apply(hit.collider,damage,start,direction);
                        game.Feedback.Contact(end,hit.collider.GetComponentInParent<Drone>()!=null);break;
                    }
                    game.Trace(pilot.Weapons?.Muzzle(h)??start,end,h==0?Art.Cyan:Art.Amber);
                }
            }
            // Initial rifle secondary: a short brace, deliberately weaker than a shield.
            if(p.Equipped[3]?.Design.TwoHanded==true && off && Time.time>=braceReady)
            {braceUntil=Time.time+.3f;braceReady=Time.time+1.5f;}
        }
        float braceUntil,braceReady;
        void ResolveMelee(float damage)
        {
            var game=Prototype.Instance;
            foreach(var enemy in FindObjectsByType<Drone>(FindObjectsSortMode.None))
            {
                Vector3 to=enemy.transform.position-transform.position;
                if(to.sqrMagnitude<1.9f*1.9f && Vector3.Dot(transform.forward,to.normalized)>.15f && !Physics.Linecast(transform.position,enemy.transform.position,1<<9))enemy.Damage(damage);
            }
        }
        public float ReduceDamage(float amount,Vector3 source)
        {
            bool front=source==Vector3.zero || Vector3.Dot(transform.forward,(source-transform.position).normalized)>.25f;
            if(front)
                for(int h=0;h<2;h++)if(shields[h].Guarding)
                {
                    var item=Prototype.Instance.Equipment.Profile.Equipped[h+3];
                    shields[h].Block(amount/100*(item.SignatureActive?.5f:1));Prototype.Instance.Player.Weapons?.Block(h);return 0;
                }
            return front&&Time.time<braceUntil?amount*.5f:amount;
        }
    }
}
