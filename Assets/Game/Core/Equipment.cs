using System;
using System.Collections.Generic;
using System.Linq;

namespace VideoGame.Core
{
    public enum GearKind { Helmet, Armor, Backpack, Handgun, Rifle, Sword, Shield }
    public enum GearQuality { White, Green, Blue, Purple, Gold }
    public enum GearSlot { Helmet, Armor, Backpack, MainHand, OffHand }
    public enum AttributeKind { Health, Inventory, Speed, Aim, Capacity }
    public enum BonusKind { Health, Resistance, Speed, Accuracy, CriticalChance, CriticalDamage, Ammo, Energy, ShieldRecharge, Damage }
    [Serializable] public sealed class GearBonus { public BonusKind Kind; public float Value; }
    public sealed class GearDefinition
    {
        public readonly string Id, Name, Signature;
        public readonly GearKind Kind;
        public readonly int Power;
        public readonly BonusKind Identity;
        public readonly float Value;
        public GearDefinition(string id,string name,GearKind kind,int power,BonusKind identity,float value,string signature="")
        {Id=id;Name=name;Kind=kind;Power=power;Identity=identity;Value=value;Signature=signature;}
        public bool TwoHanded => Kind==GearKind.Rifle;
    }
    public static class GearCatalog
    {
        public static readonly GearDefinition[] All = {
            new GearDefinition("visor","Surveyor Helmet",GearKind.Helmet,2,BonusKind.Accuracy,.08f),
            new GearDefinition("executioner","Executioner Helmet",GearKind.Helmet,3,BonusKind.CriticalDamage,.3f,"Precision: +25% critical chance"),
            new GearDefinition("shell","Patrol Armor",GearKind.Armor,3,BonusKind.Health,0),
            new GearDefinition("heavy","Bulwark Armor",GearKind.Armor,5,BonusKind.Resistance,.22f,"Fortress: +10% resistance"),
            new GearDefinition("vitality","Vitality Armor",GearKind.Armor,4,BonusKind.Health,30),
            new GearDefinition("scout","Scout Armor",GearKind.Armor,2,BonusKind.Speed,.12f),
            new GearDefinition("supply","Supply Backpack",GearKind.Backpack,2,BonusKind.Ammo,.3f),
            new GearDefinition("capacitor","Capacitor Backpack",GearKind.Backpack,3,BonusKind.Energy,.4f,"Reservoir: +50% shield capacity"),
            new GearDefinition("relay","Shield Relay",GearKind.Backpack,3,BonusKind.ShieldRecharge,.35f),
            new GearDefinition("pistol","Service Handgun",GearKind.Handgun,3,BonusKind.Damage,0),
            new GearDefinition("rifle","Assault Rifle",GearKind.Rifle,6,BonusKind.Damage,0),
            new GearDefinition("sword","Arc Sword",GearKind.Sword,3,BonusKind.Damage,0,"Execution: +25% melee damage"),
            new GearDefinition("shield","Aegis Shield",GearKind.Shield,4,BonusKind.Energy,0,"Aegis: half energy cost on blocked hits")
        };
        public static GearDefinition Get(string id) => Array.Find(All,d=>d.Id==id);
        public static bool Weapon(GearKind kind) => kind>=GearKind.Handgun;
        public static bool Suitable(GearKind kind,BonusKind bonus)
        {
            if(bonus==BonusKind.Resistance && kind==GearKind.Shield)return true;
            if(bonus==BonusKind.Ammo)return kind==GearKind.Backpack || kind==GearKind.Handgun || kind==GearKind.Rifle;
            if(bonus==BonusKind.Energy || bonus==BonusKind.ShieldRecharge)return kind==GearKind.Backpack || kind==GearKind.Shield;
            if(bonus==BonusKind.Damage || bonus==BonusKind.CriticalChance || bonus==BonusKind.CriticalDamage)return kind==GearKind.Helmet || (Weapon(kind)&&kind!=GearKind.Shield);
            if(bonus==BonusKind.Accuracy)return kind==GearKind.Helmet || kind==GearKind.Handgun || kind==GearKind.Rifle;
            return !Weapon(kind);
        }
        public static GearItem Roll(Random rng,int level,GearQuality quality,string definition=null)
        {
            var candidates=All.Where(d=>quality!=GearQuality.Gold || d.Signature.Length>0).ToArray();
            var d=Get(definition) ?? candidates[rng.Next(candidates.Length)];
            if(quality==GearQuality.Gold && d.Signature.Length==0)quality=GearQuality.Purple;
            var item=new GearItem{Id=Guid.NewGuid().ToString("N"),Definition=d.Id,Level=Math.Max(1,Math.Min(100,level)),Quality=quality};
            var pool=((BonusKind[])Enum.GetValues(typeof(BonusKind))).Where(b=>Suitable(d.Kind,b)).ToList();
            int count=Math.Min(3,(int)quality);
            for(int n=0;n<count;n++)
            {
                int i=rng.Next(pool.Count);var kind=pool[i];pool.RemoveAt(i);
                item.Bonuses.Add(new GearBonus{Kind=kind,Value=kind==BonusKind.Health ? rng.Next(5,16) : .03f+(float)rng.NextDouble()*.07f});
            }
            return item;
        }
    }
    [Serializable] public sealed class GearItem
    {
        public string Id,Definition;
        public int Level=1,Condition=100;
        public GearQuality Quality;
        public bool Fresh;
        public List<GearBonus> Bonuses=new List<GearBonus>();
        public GearDefinition Design => GearCatalog.Get(Definition);
        public int Power => Design.Power+(int)Quality;
        public int Price => (20+Level*12)*(1+(int)Quality*2);
        public int Sale => Math.Max(1,Price*Condition/400);
        public int RepairCost => Math.Max(1,(100-Condition)*Price/200);
        public bool SignatureActive => Quality==GearQuality.Gold && Design.Signature.Length>0;
        public float Scale => Condition/100f*(1+(Level-1)*.04f);
        public float Bonus(BonusKind kind)
        {
            float value=Design.Identity==kind?Design.Value:0;
            foreach(var b in Bonuses)if(b.Kind==kind)value+=b.Value;
            if(Definition=="heavy" && kind==BonusKind.Speed)value-=.15f;
            if(Definition=="capacitor" && kind==BonusKind.ShieldRecharge)value-=.15f;
            if(SignatureActive)
            {
                if(Definition=="executioner" && kind==BonusKind.CriticalChance)value+=.25f;
                if(Definition=="heavy" && kind==BonusKind.Resistance)value+=.1f;
                if(Definition=="capacitor" && kind==BonusKind.Energy)value+=.5f;
                if(Definition=="sword" && kind==BonusKind.Damage)value+=.25f;
            }
            return value*Scale;
        }
    }
    [Serializable] public sealed class GearProfile
    {
        public int Version=1,Level=1,Experience,Points,Credits,ShopRevision;
        public int[] Attributes=new int[5];
        public List<GearItem> Bag=new List<GearItem>(),Stash=new List<GearItem>(),Shop=new List<GearItem>();
        public GearItem[] Equipped=new GearItem[5];
        public int Capacity => 20+Attributes[(int)AttributeKind.Capacity]*3;
        public int BagCapacity => 10+Attributes[(int)AttributeKind.Inventory]*2;
        public int UsedPower => Equipped.Where(i=>i!=null).Sum(i=>i.Power);
        public int NextLevelXP => Level*100;
        public int ResetPrice => 500*Level;
        public float Bonus(BonusKind kind) => Equipped.Where(i=>i!=null).Sum(i=>i.Bonus(kind));
        public float MaxHealth => 100+Attributes[0]*10+Bonus(BonusKind.Health);
        public float Speed => Math.Max(.4f,1+Attributes[2]*.025f+Bonus(BonusKind.Speed));
        public float Accuracy => Math.Min(.95f,Attributes[3]*.025f+Bonus(BonusKind.Accuracy));
        public static GearProfile Starter(int credits=0)
        {
            var p=new GearProfile{Credits=credits};var r=new Random(17);
            foreach(var pair in new[]{("visor",0),("shell",1),("supply",2),("rifle",3)})p.Equipped[pair.Item2]=GearCatalog.Roll(r,1,GearQuality.White,pair.Item1);
            foreach(string id in new[]{"pistol","pistol","sword","shield"})p.Bag.Add(GearCatalog.Roll(r,1,GearQuality.White,id));
            return p;
        }
        public void AddXP(int amount)
        {
            if(amount<=0)return;
            Experience=Math.Min(1000000,Experience+amount);
            while(Level<100 && Experience>=NextLevelXP){Experience-=NextLevelXP;Level++;Points++;}
        }
        public bool Spend(AttributeKind kind)
        {
            int k=(int)kind;if(k<0 || k>=5 || Points<=0 || Attributes[k]>=50)return false;
            Attributes[k]++;Points--;return true;
        }
        public bool ResetAttributes()
        {
            int spent=Attributes.Sum();if(spent==0 || Credits<ResetPrice)return false;
            // A reset must not invalidate existing storage or equipped power.
            if(UsedPower>20 || Bag.Count>10)return false;
            Credits-=ResetPrice;Points+=spent;Attributes=new int[5];return true;
        }
        public bool Pickup(GearItem item)
        {
            if(item==null || Bag.Count>=BagCapacity || Owns(item.Id))return false;
            Bag.Add(item);return true;
        }
        public bool Owns(string id) => Bag.Concat(Stash).Concat(Equipped.Where(i=>i!=null)).Any(i=>i.Id==id);
        public string EquipError(string id,GearSlot slot,bool inMission)
        {
            if(inMission)return "Equipment is locked during missions.";
            int s=(int)slot;if(s<0 || s>=5)return "Unknown slot.";
            var item=Bag.Find(i=>i.Id==id);if(item==null)return "Select an inventory item.";
            if(item.Level>Level)return "Player level is too low.";
            var kind=item.Design.Kind;
            if(s<3 && (int)kind!=s || s>=3 && !GearCatalog.Weapon(kind))return "Item does not fit this slot.";
            if(s==4 && item.Design.TwoHanded)return "Two-handed weapons require the main slot.";
            if(s==4 && Equipped[3]?.Design.TwoHanded==true)return "Main weapon reserves the off hand.";
            if(kind==GearKind.Shield && Equipped[s==3?4:3]?.Design.Kind==GearKind.Shield)return "Only one shield can be equipped.";
            var replaced=Equipped[s];var off=item.Design.TwoHanded?Equipped[4]:null;
            int bagAfter=Bag.Count-1+(replaced!=null?1:0)+(off!=null?1:0);
            if(bagAfter>BagCapacity)return "Inventory needs room for displaced equipment.";
            int power=UsedPower-(replaced?.Power??0)-(off?.Power??0)+item.Power;
            if(power>Capacity)return "Equipment exceeds power capacity.";
            return null;
        }
        public string Equip(string id,GearSlot slot,bool inMission)
        {
            var error=EquipError(id,slot,inMission);if(error!=null)return error;
            int s=(int)slot;var item=Bag.Find(i=>i.Id==id);
            var replaced=Equipped[s];var off=item.Design.TwoHanded?Equipped[4]:null;
            Bag.Remove(item);if(replaced!=null)Bag.Add(replaced);if(off!=null){Bag.Add(off);Equipped[4]=null;}
            Equipped[s]=item;return null;
        }
        public bool Unequip(GearSlot slot,bool inMission)
        {
            int s=(int)slot;if(inMission || s<3 || s>4 || Equipped[s]==null || Bag.Count>=BagCapacity)return false;
            Bag.Add(Equipped[s]);Equipped[s]=null;return true;
        }
        public bool Transfer(string id,bool toStash,bool inMission)
        {
            if(inMission)return false;
            var source=toStash?Bag:Stash;var dest=toStash?Stash:Bag;
            var item=source.Find(i=>i.Id==id);if(item==null || !toStash && Bag.Count>=BagCapacity)return false;
            source.Remove(item);dest.Add(item);return true;
        }
        public bool Sell(string id,bool inMission)
        {
            if(inMission)return false;var item=Bag.Find(i=>i.Id==id);if(item==null)return false;
            Bag.Remove(item);Credits+=item.Sale;return true;
        }
        public bool Repair(string id,bool inMission)
        {
            if(inMission)return false;var item=Bag.Find(i=>i.Id==id);
            if(item==null || item.Condition>=100 || Credits<item.RepairCost)return false;
            Credits-=item.RepairCost;item.Condition=100;return true;
        }
        public bool Buy(string id,bool inMission)
        {
            if(inMission || Bag.Count>=BagCapacity)return false;var item=Shop.Find(i=>i.Id==id);
            if(item==null || Credits<item.Price || Owns(id))return false;
            Credits-=item.Price;Shop.Remove(item);Bag.Add(item);return true;
        }
        public void Restock(int seed)
        {
            ShopRevision++;var r=new Random(seed^ShopRevision*7919);Shop.Clear();
            foreach(var d in GearCatalog.All)Shop.Add(GearCatalog.Roll(r,Level,GearQuality.White,d.Id));
            for(int i=0;i<4;i++)Shop.Add(GearCatalog.Roll(r,Level,(GearQuality)r.Next(1,5)));
        }
        public bool Valid()
        {
            if(Version!=1 || Level<1 || Level>100 || Experience<0 || Experience>1000000 || Points<0 || Credits<0 || ShopRevision<0 || Attributes==null || Attributes.Length!=5 || Attributes.Any(a=>a<0||a>50) || Attributes.Sum()+Points!=Level-1 || Equipped==null || Equipped.Length!=5 || Bag==null || Stash==null || Shop==null)return false;
            var all=Bag.Concat(Stash).Concat(Shop).Concat(Equipped.Where(i=>i!=null)).ToArray();
            if(all.Any(i=>i==null || !Guid.TryParse(i.Id,out _) || i.Design==null || i.Level<1 || i.Level>100 || i.Condition<1 || i.Condition>100 || (int)i.Quality<0 || (int)i.Quality>4 || i.Fresh || i.Bonuses==null || i.Bonuses.Count!=Math.Min(3,(int)i.Quality) || i.Bonuses.Any(b=>b==null || !Enum.IsDefined(typeof(BonusKind),b.Kind) || !GearCatalog.Suitable(i.Design.Kind,b.Kind) || float.IsNaN(b.Value)||float.IsInfinity(b.Value)||b.Value<0 || b.Value>100)))return false;
            if(all.Select(i=>i.Id).Distinct().Count()!=all.Length || Bag.Count>BagCapacity || UsedPower>Capacity)return false;
            if(Equipped[0]==null || Equipped[1]==null || Equipped[2]==null)return false;
            for(int s=0;s<5;s++)if(Equipped[s]!=null && (Equipped[s].Level>Level || (s<3?(int)Equipped[s].Design.Kind!=s:!GearCatalog.Weapon(Equipped[s].Design.Kind))))return false;
            return !(Equipped[4]?.Design.TwoHanded==true || Equipped[3]?.Design.TwoHanded==true&&Equipped[4]!=null || Equipped[3]?.Design.Kind==GearKind.Shield&&Equipped[4]?.Design.Kind==GearKind.Shield);
        }
    }
    public sealed class GearMission
    {
        public readonly GearProfile Profile;
        public readonly int EntryLevel,Difficulty;
        public bool Active {get;private set;}=true;
        public int Credits {get;private set;}
        public GearMission(GearProfile profile,int difficulty){Profile=profile;EntryLevel=profile.Level;Difficulty=Math.Max(0,Math.Min(3,difficulty));}
        public int EnemyLevel(Random rng) => Math.Max(1,Math.Min(100,EntryLevel+rng.Next(-1,4)+Difficulty-1));
        public void EarnCredits(int amount){if(Active && amount>0)Credits=Math.Min(1000000,Credits+amount);}
        public GearItem RollLoot(Random rng,int enemyLevel)
        {
            int roll=rng.Next(1000)+Difficulty*25;
            var quality=roll<650?GearQuality.White:roll<870?GearQuality.Green:roll<970?GearQuality.Blue:roll<1010?GearQuality.Purple:GearQuality.Gold;
            string preferred=null;
            if(rng.Next(100)<25)
            {
                var choices=Profile.Equipped.Where(i=>i!=null && GearCatalog.Weapon(i.Design.Kind)).ToArray();
                if(choices.Length>0)preferred=choices[rng.Next(choices.Length)].Definition;
            }
            var item=GearCatalog.Roll(rng,enemyLevel,quality,preferred);item.Fresh=true;return item;
        }
        public bool Return(bool extracted)
        {
            if(!Active)return false;Active=false;
            Profile.Credits+=extracted?Credits:Credits-(Credits*30+99)/100;
            for(int n=Profile.Bag.Count-1;n>=0;n--)
            {
                var item=Profile.Bag[n];
                if(!extracted)
                {
                    item.Condition-=25;
                    if(item.Fresh && item.Quality>GearQuality.White)
                    {
                        item.Quality--;int count=Math.Min(3,(int)item.Quality);
                        if(item.Bonuses.Count>count)item.Bonuses.RemoveRange(count,item.Bonuses.Count-count);
                    }
                    if(item.Condition<=0){Profile.Bag.RemoveAt(n);continue;}
                }
                item.Fresh=false;
            }
            Profile.Restock(EntryLevel*104729+Profile.ShopRevision);return true;
        }
    }
    // Deterministic hand-local shield state. Exhaustion requires release and a new press.
    public sealed class ShieldCharge
    {
        public float Charge=1;
        public bool Guarding {get;private set;}
        float rechargeDelay;
        bool needsRelease;
        public void Tick(bool held,float delta,float capacity=1,float recharge=1)
        {
            if(delta<=0)return;
            if(!held)needsRelease=false;
            Guarding=held&&!needsRelease&&Charge>0;
            if(Guarding){Charge=Math.Max(0,Charge-delta/(3*Math.Max(.1f,capacity)));rechargeDelay=1;if(Charge<=0)Break();}
            else{rechargeDelay=Math.Max(0,rechargeDelay-delta);if(rechargeDelay<=0)Charge=Math.Min(1,Charge+delta*Math.Max(.1f,recharge)/3);}
        }
        public void Block(float amount){if(!Guarding)return;Charge=Math.Max(0,Charge-amount);if(Charge<=0)Break();}
        void Break(){Guarding=false;needsRelease=true;rechargeDelay=2;}
    }
}
