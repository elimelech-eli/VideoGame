using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using VideoGame.Core;
using Random = System.Random;

namespace VideoGame
{
    [DefaultExecutionOrder(-900)] // Hotkey ownership changes before actors and spawners update.
    public sealed class EquipmentSystem : MonoBehaviour
    {
        public GearProfile Profile {get;private set;}=GearProfile.Starter();
        public GearMission Field {get;private set;}
        public string Notice {get;private set;}="Equipment can only be changed at the Nexus.";
        public int MissionLevel => Field?.EntryLevel??Profile.Level;
        public int Wallet => Profile.Credits;
        public bool IsOpen => game.Menus?.Page==MenuPage.Equipment;
        public string Section {get;private set;}="Loadout";
        public GearItem Selected {get;private set;}
        public int PendingRewards => rewards.Count;
        public int LiveDrops => drops.Count;
        Prototype game;
        Random rng=new Random(17);
        int page;
        readonly List<GearItem> rewards=new List<GearItem>();
        readonly List<GearPickup> drops=new List<GearPickup>();
        readonly List<GameObject> wearables=new List<GameObject>();
        const int DropCap=48;
        public void Initialize(Prototype owner){game=owner;Profile.Restock(17);}
        public void ResetProfile(){ClearDrops();Profile=GearProfile.Starter();Profile.Restock(17);Field=null;rewards.Clear();RefreshVisuals();}
        public void Restore(GearProfile profile,int legacyCredits)
        {
            ClearDrops();Profile=profile??GearProfile.Starter(legacyCredits);
            if(Profile.Shop.Count==0)Profile.Restock(17);Field=null;rewards.Clear();Notice="Equipment restored. Nexus services are available.";RefreshVisuals();
        }
        public bool Begin(int seed,int difficulty)
        {
            if(Profile.UsedPower>Profile.Capacity || Field?.Active==true)return false;
            ClearDrops();rewards.Clear();rng=new Random(seed^731);Field=new GearMission(Profile,difficulty);game.Player.ResetHands();
            Notice=Profile.Bag.Any(i=>i.Condition<=25)?"WARNING: badly damaged carried items will be destroyed on death.":"Mission gear locked. E / A collects loot; I opens inventory.";return true;
        }
        public int RollEnemyLevel() => Field?.EnemyLevel(rng)??Profile.Level;
        public void Killed(Vector3 point,int level)
        {
            if(Field?.Active!=true)return;
            Profile.AddXP(20+level*5);
            SpawnDrop(point,null,5+level*2);
            if(rng.Next(100)<40)SpawnDrop(point+Vector3.right*.6f,Field.RollLoot(rng,level));
        }
        public bool Return(bool extracted)
        {
            if(Field==null || !Field.Return(extracted))return false;
            if(extracted){Profile.AddXP(75);var item=GearCatalog.Roll(rng,Field.EntryLevel,(GearQuality)Math.Min(4,1+Field.Difficulty));rewards.Add(item);}
            ClearDrops();game.Mission.SetCredits(Profile.Credits);
            Notice=extracted?"Extracted: loot secured. Claim the contract reward at the uplink.":"Recall: 30% field credits lost; carried gear lost 25 condition; new loot lost one quality tier.";
            return true;
        }
        public void ClaimReward()
        {
            Profile.Credits+=100;game.Mission.SetCredits(Profile.Credits);CollectRewards();Changed();
        }
        public void CollectRewards()
        {
            // Contract rewards never disappear when the carried bag is full.
            foreach(var item in rewards){if(!Profile.Pickup(item))Profile.Stash.Add(item);}rewards.Clear();
        }
        public void SpawnDrop(Vector3 point,GearItem item,int credits=0)
        {
            drops.RemoveAll(d=>d==null);
            if(drops.Count>=DropCap)return;
            var go=game.Shape(item==null?"Credit drop":"Loot: "+item.Design.Name,PrimitiveType.Cube,new Vector3(point.x,.35f,point.z),Vector3.one*(item==null?.22f:.4f),item==null?Art.Amber:QualityColor(item.Quality),game.transform);
            var col=go.GetComponent<Collider>();col.isTrigger=true;
            var drop=go.AddComponent<GearPickup>();drop.Initialize(this,item,credits);drops.Add(drop);
        }
        public bool TryPickup(GearPickup drop)
        {
            if(game.Player.IsDead || !game.InMission || Field?.Active!=true || drop==null || !drops.Contains(drop))return false;
            if(drop.Item!=null && !Profile.Pickup(drop.Item)){Notice="Inventory full. Leave loot or drop an item [I].";return false;}
            if(drop.Credits>0)Field.EarnCredits(drop.Credits);
            drops.Remove(drop);drop.gameObject.SetActive(false);Destroy(drop.gameObject);return true;
        }
        public bool TryNearestPickup()
        {
            var nearest=drops.Where(d=>d!=null&&Vector3.Distance(d.transform.position,game.Player.transform.position)<2.5f).OrderBy(d=>(d.transform.position-game.Player.transform.position).sqrMagnitude).FirstOrDefault();
            return nearest!=null && TryPickup(nearest);
        }
        public bool Drop(string id)
        {
            if(!game.InMission || Field?.Active!=true || drops.Count>=DropCap)return false;
            var item=Profile.Bag.Find(i=>i.Id==id);if(item==null)return false;
            Profile.Bag.Remove(item);SpawnDrop(game.Player.transform.position+game.Player.transform.forward,item);return true;
        }
        public void ClearDrops(){foreach(var d in drops)if(d!=null){d.gameObject.SetActive(false);Destroy(d.gameObject);}drops.Clear();}
        void Update()
        {
            if(game==null || game.Player.IsDead)return;
            if(Keyboard.current?.iKey.wasPressedThisFrame==true || Gamepad.current?.selectButton.wasPressedThisFrame==true)
            {
                if(IsOpen)game.Menus.Resume();else if(!game.Menus.IsOpen)Open();
            }
            if(game.InMission && !game.Menus.BlocksGameplay)
                foreach(var d in drops.ToArray())if(d!=null && d.Item==null && Vector3.Distance(d.transform.position,game.Player.transform.position)<1.5f)TryPickup(d);
        }
        public void Open(){Section="Loadout";Selected=null;page=0;GetComponent<CharacterScreen>()?.ResetScreen();game.Menus.OpenEquipment();}
        void Navigate(string section){Section=section;Selected=null;page=0;game.Menus.Highlight(0);}
        public void Changed()
        {
            game.Mission.SetCredits(Profile.Credits);RefreshVisuals();game.Player.Health=Math.Min(game.Player.Health,Profile.MaxHealth);
            if(!game.InMission)game.Player.ResetHands();game.Menus.AutoSave();
        }
        void Result(bool okay,string text){Notice=okay?text:"Unavailable: check credits, space, level, power, or mission restrictions.";if(okay)Changed();}
        public List<MenuEntry> Entries()
        {
            var entries=new List<MenuEntry>();
            void Add(string id,string label,Action action,bool enabled=true,string value=null)=>entries.Add(new MenuEntry{Id=id,Label=label,Activate=action,Enabled=enabled,Value=value});
            bool hub=!game.InMission;
            if(Selected!=null)
            {
                var item=Selected;bool shop=Section=="Shop";bool stash=Section=="Stash";
                if(shop)Add("buy","BUY  /  "+item.Price+" CR",()=>{Result(Profile.Buy(item.Id,game.InMission),"Purchased.");Selected=null;},hub);
                else if(stash)Add("withdraw","MOVE TO INVENTORY",()=>{Result(Profile.Transfer(item.Id,false,game.InMission),"Withdrawn.");Selected=null;},hub);
                else
                {
                    if(GearCatalog.Weapon(item.Design.Kind))
                    {
                        EquipAction(GearSlot.MainHand);if(!item.Design.TwoHanded)EquipAction(GearSlot.OffHand);
                    }
                    else EquipAction((GearSlot)item.Design.Kind);
                    Add("stash","STORE IN NEXUS STASH",()=>{Result(Profile.Transfer(item.Id,true,game.InMission),"Stored.");Selected=null;},hub);
                    Add("repair",item.Condition==100?"REPAIR  /  PRISTINE":"REPAIR  /  "+item.RepairCost+" CR",()=>Result(Profile.Repair(item.Id,game.InMission),"Repaired; rarity unchanged."),hub&&item.Condition<100);
                    Add("sell","SELL  /  "+item.Sale+" CR",()=>{Result(Profile.Sell(item.Id,game.InMission),"Sold.");Selected=null;},hub);
                    Add("drop","DROP IN WORLD",()=>{Result(Drop(item.Id),"Dropped. Pick up with E / A.");Selected=null;},!hub);
                }
                Add("back-list","BACK TO "+Section.ToUpperInvariant(),()=>Selected=null);
                void EquipAction(GearSlot slot)=>Add("equip-"+slot,"EQUIP  /  "+slot,()=>{var error=Profile.Equip(item.Id,slot,game.InMission);Notice=error??"Equipped.";if(error==null){Changed();Selected=null;}},hub);
                return entries;
            }
            if(Section=="Loadout")
            {
                Add("inventory","INVENTORY  /  "+Profile.Bag.Count+" / "+Profile.BagCapacity,()=>Navigate("Inventory"));
                Add("stash-page","NEXUS STASH  /  "+Profile.Stash.Count,()=>Navigate("Stash"),hub);
                Add("shop","EQUIPMENT VENDOR",()=>Navigate("Shop"),hub);
                Add("attributes","LEVEL UP  /  "+Profile.Points+" POINTS",()=>Navigate("Attributes"),hub);
                Add("hands","HAND SLOTS / UNEQUIP",()=>Navigate("Hands"),hub);
                Add("close","RETURN TO GAME",game.Menus.Resume);
            }
            else if(Section=="Attributes")
            {
                foreach(AttributeKind kind in Enum.GetValues(typeof(AttributeKind)))
                {
                    var k=kind;Add("point-"+k,k+"  /  "+Profile.Attributes[(int)k],()=>Result(Profile.Spend(k),"Attribute increased."),hub&&Profile.Points>0);
                }
                Add("reset","RESET ATTRIBUTES  /  "+Profile.ResetPrice+" CR",()=>game.Menus.ConfirmEquipment("Reset all allocated attributes for "+Profile.ResetPrice+" credits? Bag must fit 10 slots and gear must fit 20 power.",()=>Result(Profile.ResetAttributes(),"Attributes reset.")),hub);
                Add("root","BACK TO LOADOUT",()=>Navigate("Loadout"));
            }
            else if(Section=="Hands")
            {
                foreach(var slot in new[]{GearSlot.MainHand,GearSlot.OffHand}){var s=slot;Add("remove-"+s,"UNEQUIP "+s,()=>Result(Profile.Unequip(s,game.InMission),"Unequipped."),Profile.Equipped[(int)s]!=null);}
                Add("root","BACK TO LOADOUT",()=>Navigate("Loadout"));
            }
            else
            {
                var list=Section=="Shop"?Profile.Shop:Section=="Stash"?Profile.Stash:Profile.Bag;
                page=Math.Min(page,Math.Max(0,(list.Count-1)/5));
                foreach(var item in list.Skip(page*5).Take(5)){var i=item;Add(i.Id,i.Quality+" L"+i.Level+" "+i.Design.Name,()=>Selected=i);}
                if(list.Count==0)Add("empty","NO ITEMS",null,false);
                if(list.Count>5)Add("next","PAGE "+(page+1)+" / "+((list.Count+4)/5)+"  / NEXT",()=>page=(page+1)%((list.Count+4)/5));
                Add("root","BACK TO LOADOUT",()=>Navigate("Loadout"));
            }
            return entries;
        }
        public static Color QualityColor(GearQuality q)=>new[]{Color.white,new Color(.25f,1,.45f),new Color(.25f,.6f,1),new Color(.8f,.35f,1),new Color(1,.75f,.15f)}[(int)q];
        public void RefreshVisuals()
        {
            // The shared operative mesh already has fitted armor and a helmet.
            // Do not place primitive equipment boxes over it. Proper modular
            // meshes are separate art work; UI slots still show actual loadout.
            foreach(var go in wearables)if(go!=null){go.SetActive(false);Destroy(go);}wearables.Clear();
            if(game?.Player==null)return;
            foreach(var t in game.Player.GetComponentsInChildren<Transform>())
                if(t.name=="SOCKET_weapon_R")foreach(var renderer in t.GetComponentsInChildren<Renderer>())
                    renderer.enabled=Profile.Equipped[3]?.Design.Kind==GearKind.Rifle;
            GetComponent<CharacterScreen>()?.RefreshPreview();
        }
        void OnDestroy(){ClearDrops();}
    }
    public sealed class GearPickup : MonoBehaviour
    {
        public GearItem Item {get;private set;}
        public int Credits {get;private set;}
        EquipmentSystem owner;
        public void Initialize(EquipmentSystem system,GearItem item,int credits){owner=system;Item=item;Credits=credits;}
        void OnGUI()
        {
            if(owner==null || !Prototype.Instance.InMission || Prototype.Instance.Menus.IsOpen || Vector3.Distance(transform.position,Prototype.Instance.Player.transform.position)>2.5f)return;
            var p=Prototype.Instance.Player.View.WorldToScreenPoint(transform.position+Vector3.up*.5f);if(p.z<=0)return;
            GUI.contentColor=Item==null?Art.Amber:EquipmentSystem.QualityColor(Item.Quality);
            GUI.Label(new Rect(p.x-130,Screen.height-p.y,270,40),Item==null?Credits+" CR":"[E / A] "+Item.Quality+" "+Item.Design.Name);GUI.contentColor=Color.white;
        }
    }
}
