using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using VideoGame.Core;

namespace VideoGame
{
    public enum CharacterStorage { Inventory, Stash, Vendor, Attributes }
    public enum CharacterFilter { All, Armor, Weapons }

    public sealed class CharacterScreen : MonoBehaviour
    {
        public GearItem SelectedItem {get;private set;}
        public GearSlot CompareSlot {get;private set;}=GearSlot.MainHand;
        public CharacterStorage Storage {get;private set;}
        public bool Selling {get;private set;}
        public void OpenSelling(){SetStorage(CharacterStorage.Inventory);Selling=true;FocusId="storage-bag";Status="Select carried gear to sell. Each sale requires confirmation.";}
        public bool ConfirmSellSelected()
        {
            if(!Open || game.InMission || SelectedItem==null || selectedEquipped>=0 || Storage!=CharacterStorage.Inventory)return false;
            var item=SelectedItem;
            game.Menus.ConfirmEquipment("Sell "+item.Design.Name+" for "+item.Sale+" credits?",()=>Transaction(Profile.Sell(item.Id,game.InMission),"Sold."));
            return true;
        }
        public CharacterFilter Filter {get;private set;}
        public string Status {get;private set;}="Select an item to inspect and compare it.";
        public string FocusId {get;private set;}="tab-character";
        public int Page {get;private set;}
        public bool MoreOpen {get;private set;}
        public CharacterPreview Preview {get;private set;}
        public int PreviewPower => SelectedItem==null?Profile.UsedPower:Profile.UsedPower-(Profile.Equipped[(int)CompareSlot]?.Power??0)-(SelectedItem.Design.TwoHanded?Profile.Equipped[4]?.Power??0:0)+SelectedItem.Power;
        public string EquipRestriction => SelectedItem==null?"Select an inventory item.":Profile.EquipError(SelectedItem.Id,CompareSlot,game.InMission);
        public static Rect IconUV(GearKind kind)
        {
            int n=(int)kind;return new Rect((n%3)/3f,1-(n/3+1)/3f,1/3f,1/3f);
        }
        Prototype game;
        GearProfile Profile => game.Equipment.Profile;
        bool Open => game?.Menus?.Page==MenuPage.Equipment;
        Texture2D icons;
        GUIStyle label,small,title,large;
        readonly Color cyan=new Color(.15f,.88f,.95f),ink=new Color(.025f,.04f,.055f),muted=new Color(.57f,.65f,.7f);
        readonly List<Control> controls=new List<Control>();
        sealed class Control {public string Id;public Rect Rect;public Action Action;public bool Enabled;}
        int sortMode; // 0: saved manual order, 1: quality, 2: name
        readonly List<DropTarget> dropTargets=new List<DropTarget>();
        sealed class DropTarget {public Rect Rect;public string Item;public int Slot=-1;public bool Bag;public int Cell=-1;}
        GearItem dragItem;
        int dragSlot=-1;
        bool dragging;
        Vector2 dragStart;
        public bool IsDragging => dragging;
        void OnApplicationFocus(bool focus){if(!focus)CancelDrag();}
        void OnDisable()=>CancelDrag();
        public void CancelDrag(){dragItem=null;dragSlot=-1;dragging=false;}
        // Both pointer and test drivers use these transactions. Identity, power,
        // capacity and mission restrictions stay owned by GearProfile.
        public bool DropOnGear(string id,GearSlot slot)
        {
            if(Storage!=CharacterStorage.Inventory)return false;
            string error=Profile.Equip(id,slot,game.InMission);
            Status=error??"Equipment updated.";
            if(error!=null)return false;
            SelectedItem=null;selectedEquipped=-1;game.Equipment.Changed();return true;
        }
        public bool DropOnInventory(string id,int sourceSlot,string targetId=null,int emptyCell=-1)
        {
            if(Storage!=CharacterStorage.Inventory)return false;
            Profile.NormalizeBagLayout();
            int destination=targetId==null?emptyCell:Profile.Bag.Find(i=>i.Id==targetId)?.InventorySlot??-1;
            if(targetId!=null && destination<0 || destination>=Profile.BagCapacity)return false;
            if(targetId==null && destination>=0 && Profile.Bag.Any(i=>i.InventorySlot==destination))return false;
            if(sourceSlot>=0)
            {
                if(sourceSlot>4 || Profile.Equipped[sourceSlot]?.Id!=id)return false;
                if(targetId!=null)
                {
                    // Exchange with an occupied cell atomically; a full bag is
                    // legal when the target can replace the equipped item.
                    string error=Profile.Equip(targetId,(GearSlot)sourceSlot,game.InMission);
                    if(error!=null){Status=error;return false;}
                }
                else if(!Profile.Unequip((GearSlot)sourceSlot,game.InMission))
                {Status="Cannot unequip: check inventory space and mission restrictions.";return false;}
            }
            var item=Profile.Bag.Find(i=>i.Id==id);if(item==null)return false;
            Profile.NormalizeBagLayout();
            if(destination<0){destination=0;while(Profile.Bag.Any(i=>i.InventorySlot==destination && i.Id!=id))destination++;}
            if(!Profile.MoveInBag(id,destination))return false;
            sortMode=0;SelectedItem=item;selectedEquipped=-1;Status="Inventory arranged. Order is saved.";
            game.Equipment.Changed();return true;
        }
        static Rect SlotRect(GearSlot slot)
        {
            switch(slot){case GearSlot.Helmet:return new Rect(815,110,120,120);case GearSlot.Armor:return new Rect(326,265,120,120);case GearSlot.Backpack:return new Rect(815,330,120,120);case GearSlot.MainHand:return new Rect(326,650,120,120);default:return new Rect(815,650,120,120);}
        }
        static Rect CellRect(int n)=>new Rect(975+(n%5)*120,225+(n/5)*81,111,74);
        bool CustomGrid=>Storage==CharacterStorage.Inventory && Filter==CharacterFilter.All && sortMode==0;
        GearItem ItemAt(List<GearItem> items,int index)=>CustomGrid?items.Find(i=>i.InventorySlot==index):index<items.Count?items[index]:null;
        void BuildDropTargets()
        {
            dropTargets.Clear();
            foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot)))
                if(slot!=GearSlot.OffHand || Profile.Equipped[3]?.Design.TwoHanded!=true)
                    dropTargets.Add(new DropTarget{Rect=SlotRect(slot),Item=Profile.Equipped[(int)slot]?.Id,Slot=(int)slot});
            if(Storage!=CharacterStorage.Inventory)return;
            var items=VisibleItems();Page=Math.Min(Page,Math.Max(0,((CustomGrid?Profile.BagCapacity:items.Count)-1)/20));
            for(int n=0;n<20;n++)
            {
                int index=Page*20+n;if(index>=Profile.BagCapacity)continue;
                dropTargets.Add(new DropTarget{Rect=CellRect(n),Item=ItemAt(items,index)?.Id,Bag=true,Cell=CustomGrid?index:-1});
            }
        }
        void PointerDrag()
        {
            var mouse=Mouse.current;if(mouse==null || MoreOpen)return;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            Vector2 point=mouse.position.ReadValue();point.y=Screen.height-point.y;
            point=(point-new Vector2((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2))/scale;
            if(mouse.leftButton.wasPressedThisFrame)
            {
                var hit=dropTargets.LastOrDefault(t=>t.Rect.Contains(point));
                if(hit==null)return;
                if(hit.Item==null){if(hit.Slot>=3 && SelectedItem!=null && selectedEquipped<0)ChooseHand((GearSlot)hit.Slot);return;}
                if(mouse.clickCount.ReadValue()>=2)
                {if(hit.Slot>=0)UnequipSlot((GearSlot)hit.Slot);else QuickEquip(hit.Item);CancelDrag();return;}
                if(hit.Slot>=0){SelectedItem=Profile.Equipped[hit.Slot];selectedEquipped=hit.Slot;CompareSlot=(GearSlot)hit.Slot;}
                else SelectItem(hit.Item);
                dragItem=SelectedItem;dragSlot=hit.Slot;dragStart=point;
            }
            if(mouse.leftButton.isPressed && dragItem!=null && Vector2.Distance(dragStart,point)>6)dragging=true;
            if(mouse.leftButton.wasReleasedThisFrame && dragItem!=null)
            {
                if(dragging)
                {
                    var hit=dropTargets.LastOrDefault(t=>t.Rect.Contains(point));
                    if(hit?.Bag==true)DropOnInventory(dragItem.Id,dragSlot,hit.Item,hit.Cell);
                    else if(hit!=null && hit.Slot>=0 && dragSlot<0)DropOnGear(dragItem.Id,(GearSlot)hit.Slot);
                    else Status="Drop cancelled. Items unchanged.";
                }
                CancelDrag();
            }
        }
        void Target(Rect rect,string id,int slot=-1,bool bag=false)
        {
            if(!dragging)return;
            bool valid=bag?(dragSlot<0 || !game.InMission && (id!=null?Profile.EquipError(id,(GearSlot)dragSlot,false)==null:Profile.Bag.Count<Profile.BagCapacity)):
                dragSlot<0 && Profile.EquipError(dragItem.Id,(GearSlot)slot,game.InMission)==null;
            Outline(rect,valid?new Color(.3f,1,.6f):new Color(.55f,.2f,.2f),rect.Contains(Event.current.mousePosition)?3:1);
        }
        bool drawingMore;
        float repeatAt;
        int selectedEquipped=-1;
        void Awake(){game=GetComponent<Prototype>();Preview=gameObject.AddComponent<CharacterPreview>();}
        public void RefreshPreview()=>Preview?.Refresh();
        public bool CloseActions(){if(dragItem!=null){CancelDrag();return true;}if(!MoreOpen)return false;MoreOpen=false;FocusId="more";return true;}
        public void ResetScreen(){Selling=false;CancelDrag();Storage=CharacterStorage.Inventory;Filter=CharacterFilter.All;SelectedItem=null;selectedEquipped=-1;MoreOpen=false;Page=0;FocusId="tab-character";Status="Select an item to inspect and compare it.";RefreshPreview();}
        public void SetStorage(CharacterStorage storage)
        {
            Selling=false;
            if(game.InMission && (storage==CharacterStorage.Stash || storage==CharacterStorage.Vendor)){Status="Stash and vendor services are available at the Nexus.";return;}
            CancelDrag();Storage=storage;Page=0;SelectedItem=null;selectedEquipped=-1;MoreOpen=false;
            FocusId=storage==CharacterStorage.Attributes?"tab-attributes":storage==CharacterStorage.Stash?"storage-stash":storage==CharacterStorage.Vendor?"storage-vendor":"storage-bag";
        }
        public void SetFilter(CharacterFilter filter){CancelDrag();Filter=filter;Page=0;}
        public List<GearItem> VisibleItems()
        {
            Profile.NormalizeBagLayout();
            var list=Storage==CharacterStorage.Stash?Profile.Stash:Storage==CharacterStorage.Vendor?Profile.Shop:Profile.Bag;
            var query=list.Where(i=>Filter==CharacterFilter.All || (Filter==CharacterFilter.Weapons)==GearCatalog.Weapon(i.Design.Kind));
            return sortMode==1?query.OrderByDescending(i=>i.Quality).ThenByDescending(i=>i.Level).ThenBy(i=>i.Design.Name).ToList():sortMode==2?query.OrderBy(i=>i.Design.Name).ToList():query.ToList();
        }
        public bool SelectItem(string id)
        {
            var item=VisibleItems().Find(i=>i.Id==id);if(item==null)return false;
            SelectedItem=item;selectedEquipped=-1;MoreOpen=false;
            if(!GearCatalog.Weapon(item.Design.Kind))CompareSlot=(GearSlot)item.Design.Kind;
            else CompareSlot=item.Design.Kind==GearKind.Shield?GearSlot.OffHand:GearSlot.MainHand;
            Status=game.InMission?"Field inventory: inspection and dropping only.":Selling?"Review the sale price, then confirm the sale.":"Compare the changes below, then equip.";return true;
        }
        public void ChooseHand(GearSlot hand){if(SelectedItem?.Design.Kind==GearKind.Shield){CompareSlot=GearSlot.OffHand;return;}if(hand>=GearSlot.MainHand && hand<=GearSlot.OffHand)CompareSlot=hand;}
        public bool QuickEquip(string id)
        {
            if(Storage!=CharacterStorage.Inventory || !SelectItem(id))return false;
            return EquipSelected();
        }
        public bool UnequipSlot(GearSlot slot)
        {
            bool okay=Profile.Unequip(slot,game.InMission);
            Transaction(okay,"Unequipped to inventory.");
            if(okay)FocusId="slot-"+slot;
            return okay;
        }
        public bool DropSelected()
        {
            if(SelectedItem==null || selectedEquipped>=0 || Storage!=CharacterStorage.Inventory)return false;
            bool okay=game.Equipment.Drop(SelectedItem.Id);Transaction(okay,"Dropped in the world. Pick up again with E / A.");return okay;
        }
        public bool EquipSelected()
        {
            if(SelectedItem==null || Storage!=CharacterStorage.Inventory || selectedEquipped>=0)return false;
            var error=Profile.Equip(SelectedItem.Id,CompareSlot,game.InMission);Status=error??"Equipment updated.";
            if(error!=null)return false;SelectedItem=null;FocusId="slot-"+CompareSlot;game.Equipment.Changed();return true;
        }
        public float CompareBonus(BonusKind bonus)
        {
            if(SelectedItem==null)return 0;
            float result=SelectedItem.Bonus(bonus)-(Profile.Equipped[(int)CompareSlot]?.Bonus(bonus)??0);
            if(SelectedItem.Design.TwoHanded)result-=Profile.Equipped[4]?.Bonus(bonus)??0;return result;
        }
        void Transaction(bool okay,string text)
        {
            Status=okay?text:"Unavailable: check credits, inventory space and mission restrictions.";
            if(!okay)return;SelectedItem=null;selectedEquipped=-1;MoreOpen=false;FocusId=Storage==CharacterStorage.Attributes?"points":Storage==CharacterStorage.Stash?"storage-stash":Storage==CharacterStorage.Vendor?"storage-vendor":"storage-bag";game.Equipment.Changed();
        }
        void Update()
        {
            if(!Open){if(dragItem!=null)CancelDrag();return;}
            BuildDropTargets();PointerDrag();
            var k=Keyboard.current;var p=Gamepad.current;
            int x=k?.rightArrowKey.isPressed==true || p?.dpad.right.isPressed==true?1:k?.leftArrowKey.isPressed==true || p?.dpad.left.isPressed==true?-1:0;
            int y=k?.downArrowKey.isPressed==true || p?.dpad.down.isPressed==true?1:k?.upArrowKey.isPressed==true || p?.dpad.up.isPressed==true?-1:0;
            if(x==0&&y==0)repeatAt=0;
            else if(Time.unscaledTime>=repeatAt){MoveFocus(new Vector2(x,y));repeatAt=Time.unscaledTime+.2f;}
            if(k?.tabKey.wasPressedThisFrame==true)CycleFocus(k.shiftKey.isPressed?-1:1);
            if(k?.enterKey.wasPressedThisFrame==true || p?.buttonSouth.wasPressedThisFrame==true)controls.Find(c=>c.Id==FocusId&&c.Enabled)?.Action();
            if(k?.eKey.wasPressedThisFrame==true || p?.buttonWest.wasPressedThisFrame==true){if(Selling)ConfirmSellSelected();else EquipSelected();}
            if(p?.buttonEast.wasPressedThisFrame==true){if(MoreOpen)MoreOpen=false;else game.Menus.Resume();}
        }
        void CycleFocus(int delta)
        {
            var enabled=controls.Where(c=>c.Enabled).ToList();if(enabled.Count==0)return;
            int i=enabled.FindIndex(c=>c.Id==FocusId);FocusId=enabled[(i+delta+enabled.Count)%enabled.Count].Id;
        }
        void MoveFocus(Vector2 direction)
        {
            var current=controls.Find(c=>c.Id==FocusId);if(current==null){CycleFocus(1);return;}
            Control best=null;float score=float.PositiveInfinity;
            foreach(var c in controls)
            {
                if(!c.Enabled || c==current)continue;Vector2 offset=c.Rect.center-current.Rect.center;
                float forward=Vector2.Dot(offset,direction.normalized);if(forward<1)continue;
                float side=Mathf.Abs(offset.x*direction.y-offset.y*direction.x);float rank=forward+side*3;
                if(rank<score){score=rank;best=c;}
            }
            if(best!=null)FocusId=best.Id;
        }
        static void Fill(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
        void Outline(Rect r,Color c,float width=1){Fill(new Rect(r.x,r.y,r.width,width),c);Fill(new Rect(r.x,r.yMax-width,r.width,width),c);Fill(new Rect(r.x,r.y,width,r.height),c);Fill(new Rect(r.xMax-width,r.y,width,r.height),c);}
        void Text(Rect r,string text,GUIStyle style,Color? color=null){GUI.contentColor=color??Color.white;GUI.Label(r,text,style);GUI.contentColor=Color.white;}
        bool Button(string id,Rect rect,string text,Action action,bool enabled=true,bool active=false,bool pointer=true)
        {
            enabled=enabled&&(!MoreOpen||drawingMore||id=="more"||id=="close");
            controls.Add(new Control{Id=id,Rect=rect,Action=action,Enabled=enabled});
            bool focused=FocusId==id;
            Fill(rect,active?new Color(.045f,.24f,.28f):new Color(.035f,.065f,.085f));Outline(rect,focused||active?cyan:new Color(.17f,.24f,.28f),focused?2:1);
            Text(new Rect(rect.x+10,rect.y+8,rect.width-18,rect.height-8),text,label,enabled?Color.white:muted*.65f);
            if(enabled && Event.current.type==EventType.MouseDown && rect.Contains(Event.current.mousePosition))FocusId=id;
            if(!enabled)return false;
            if(pointer && GUI.Button(rect,GUIContent.none,GUIStyle.none)){FocusId=id;action?.Invoke();return true;}return false;
        }
        void Bar(Rect r,float fraction,Color c){Fill(r,new Color(.1f,.15f,.18f));Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(fraction),r.height),c);}
        void Icon(Rect r,GearKind kind)
        {
            if(icons!=null)GUI.DrawTextureWithTexCoords(r,icons,IconUV(kind));else Text(r,kind.ToString(),small,cyan);
        }
        void Styles()
        {
            label=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=false};
            small=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            title=new GUIStyle(label){fontSize=28,fontStyle=FontStyle.Bold};
            large=new GUIStyle(title){fontSize=48};icons=Resources.Load<Texture2D>("UI/GearIcons");
        }
        void OnGUI()
        {
            if(!Open)return;if(label==null)Styles();
            controls.Clear();
            var matrix=GUI.matrix;var oldDepth=GUI.depth;GUI.depth=-110;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2),Quaternion.identity,Vector3.one*scale);
            try
            {
                Fill(new Rect(0,0,1600,900),new Color(.008f,.015f,.024f,.98f));
                Fill(new Rect(0,0,1600,65),ink);Outline(new Rect(0,64,1600,1),new Color(.15f,.25f,.3f));
                Text(new Rect(28,12,250,45),"OPERATIVE",title);
                Button("tab-character",new Rect(305,10,160,43),"CHARACTER",()=>SetStorage(CharacterStorage.Inventory),true,Storage!=CharacterStorage.Attributes);
                Button("tab-attributes",new Rect(475,10,165,43),"ATTRIBUTES",()=>SetStorage(CharacterStorage.Attributes),true,Storage==CharacterStorage.Attributes);
                Text(new Rect(760,24,390,30),(game.InMission?"MISSION":"NEXUS")+" / GAME PAUSED",small,cyan);
                Text(new Rect(1310,18,220,35),Profile.Credits.ToString("N0")+" CR",label);
                Button("close",new Rect(1540,10,42,43),"X",game.Menus.Resume);
                DrawStats();DrawCharacter();
                if(Storage==CharacterStorage.Attributes)DrawAttributes();else{DrawInventory();DrawDetails();}
                Text(new Rect(30,865,1540,28),Selling?"ARROWS / D-PAD  NAVIGATE     ENTER / A  SELECT     E / X  SELL     I / B / ESC  CLOSE":"ARROWS / D-PAD  NAVIGATE     ENTER / A  SELECT     E / X  EQUIP     I / B / ESC  CLOSE     DRAG ITEMS TO EQUIP / MOVE",small,muted);
                if(dragging && dragItem!=null){var r=new Rect(Event.current.mousePosition.x+12,Event.current.mousePosition.y+12,80,80);Fill(r,ink);Icon(r,dragItem.Design.Kind);Outline(r,EquipmentSystem.QualityColor(dragItem.Quality),2);}
            }
            finally{GUI.matrix=matrix;GUI.depth=oldDepth;GUI.color=GUI.contentColor=Color.white;}
        }
        void DrawStats()
        {
            Fill(new Rect(22,85,280,760),ink);Outline(new Rect(22,85,280,760),new Color(.16f,.25f,.28f));
            Text(new Rect(46,105,220,65),"LEVEL  "+Profile.Level,large);
            Bar(new Rect(46,180,230,7),(float)Profile.Experience/Profile.NextLevelXP,cyan);
            Text(new Rect(46,195,230,28),Profile.Experience+" / "+Profile.NextLevelXP+" XP",small,muted);
            Button("points",new Rect(44,232,235,48),Profile.Points+" ATTRIBUTE POINTS  >",()=>SetStorage(CharacterStorage.Attributes));
            Stat(320,"Health",Mathf.CeilToInt(game.Player.Health)+" / "+Profile.MaxHealth.ToString("0"));
            Stat(370,"Protection",(Mathf.Clamp(Profile.Bonus(BonusKind.Resistance),0,.75f)*100).ToString("0")+"%");
            Stat(420,"Movement",(Profile.Speed*100).ToString("0")+"%");
            Stat(470,"Accuracy","+"+(Profile.Accuracy*100).ToString("0")+"%");
            Stat(520,"Critical chance",(Mathf.Clamp01(.05f+Profile.Bonus(BonusKind.CriticalChance))*100).ToString("0")+"%");
            Stat(570,"Shield recharge","+"+(Profile.Bonus(BonusKind.ShieldRecharge)*100).ToString("0")+"%");
            Text(new Rect(46,645,230,28),"POWER CAPACITY",small,cyan);
            Text(new Rect(46,673,240,55),Profile.UsedPower+" / "+Profile.Capacity,title);
            Bar(new Rect(46,730,230,10),(float)Profile.UsedPower/Profile.Capacity,cyan);
            Text(new Rect(46,770,240,50),"INVENTORY CAPACITY\n"+Profile.Bag.Count+" / "+Profile.BagCapacity+" SLOTS",small,muted);
        }
        void Stat(float y,string name,string value){Text(new Rect(46,y,170,30),name,label,muted);Text(new Rect(175,y,105,30),value,new GUIStyle(label){alignment=TextAnchor.UpperRight});Fill(new Rect(46,y+36,230,1),new Color(.1f,.18f,.22f));}
        void DrawCharacter()
        {
            var rect=new Rect(382,105,495,715);
            if(icons!=null)GUI.DrawTextureWithTexCoords(new Rect(310,85,640,760),icons,new Rect(2/3f,0,1/3f,1/3f));
            if(Preview.Texture!=null)GUI.DrawTexture(rect,Preview.Texture,ScaleMode.ScaleToFit,true);
            Text(new Rect(452,809,360,27),"CHARACTER PREVIEW / DRAG TO ROTATE",small,muted);
            Button("rotate-left",new Rect(404,807,40,35),"<",()=>Preview.Rotate(-25));Button("rotate-right",new Rect(850,807,40,35),">",()=>Preview.Rotate(25));
            if(dragItem==null && Event.current.type==EventType.MouseDrag && rect.Contains(Event.current.mousePosition)){Preview.Rotate(-Event.current.delta.x*.7f);Event.current.Use();}
            foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot)))Slot(slot,SlotRect(slot));
        }
        void Slot(GearSlot slot,Rect r)
        {
            var item=Profile.Equipped[(int)slot];string name=slot==GearSlot.Armor?"BODY ARMOR":slot==GearSlot.MainHand?"MAIN HAND":slot==GearSlot.OffHand?"OFF HAND":slot.ToString().ToUpperInvariant();
            Text(new Rect(r.x,r.y-25,150,24),name,small,muted);
            bool reserved=slot==GearSlot.OffHand && Profile.Equipped[3]?.Design.TwoHanded==true;
            Button("slot-"+slot,r,"",()=>{if(SelectedItem!=null && selectedEquipped<0 && GearCatalog.Weapon(SelectedItem.Design.Kind)&&slot>=GearSlot.MainHand)ChooseHand(slot);else{SelectedItem=item;selectedEquipped=(int)slot;CompareSlot=slot;MoreOpen=false;}},!reserved,CompareSlot==slot&&SelectedItem!=null,false);
            if(item!=null){Icon(new Rect(r.x+5,r.y+5,r.width-10,r.height-10),item.Design.Kind);Outline(r,EquipmentSystem.QualityColor(item.Quality),2);}
            else Text(new Rect(r.x+10,r.y+35,r.width-15,70),reserved?"RESERVED\nBY RIFLE":"EMPTY",small,muted);
            Text(new Rect(r.x,r.yMax+5,145,42),item?.Design.Name??"",small);
            if(!reserved)Target(r,item?.Id,(int)slot);
        }
        void DrawInventory()
        {
            Text(new Rect(975,85,360,40),Selling?"GEAR EXCHANGE / SELL":Storage==CharacterStorage.Inventory?"INVENTORY":Storage==CharacterStorage.Stash?"NEXUS STASH":"EQUIPMENT VENDOR",title);
            Text(new Rect(1400,95,180,28),Storage==CharacterStorage.Inventory?Profile.Bag.Count+" / "+Profile.BagCapacity+" SLOTS":VisibleItems().Count+" ITEMS",small,muted);
            Button("storage-bag",new Rect(975,135,155,36),"INVENTORY",()=>SetStorage(CharacterStorage.Inventory),true,Storage==CharacterStorage.Inventory);
            Button("storage-stash",new Rect(1140,135,135,36),"STASH",()=>SetStorage(CharacterStorage.Stash),!game.InMission,Storage==CharacterStorage.Stash);
            Button("storage-vendor",new Rect(1285,135,135,36),"VENDOR",()=>SetStorage(CharacterStorage.Vendor),!game.InMission,Storage==CharacterStorage.Vendor);
            Button("sort",new Rect(1430,135,145,36),sortMode==0?"ORDER: CUSTOM":sortMode==1?"SORT: QUALITY":"SORT: NAME",()=>{CancelDrag();sortMode=(sortMode+1)%3;Page=0;});
            int filter=0;foreach(CharacterFilter f in Enum.GetValues(typeof(CharacterFilter))){var selected=f;Button("filter-"+f,new Rect(975+filter*135,180,125,35),f.ToString().ToUpperInvariant(),()=>SetFilter(selected),true,Filter==f);filter++;}
            var items=VisibleItems();int pages=Math.Max(1,((CustomGrid?Profile.BagCapacity:items.Count)+19)/20);Page=Math.Min(Page,pages-1);
            for(int n=0;n<20;n++)
            {
                int index=Page*20+n;var r=CellRect(n);
                var item=ItemAt(items,index);
                bool locked=Storage==CharacterStorage.Inventory && index>=Profile.BagCapacity;
                if(item==null){Fill(r,new Color(.018f,.031f,.045f));Outline(r,new Color(.1f,.17f,.2f));if(locked)Text(new Rect(r.x+6,r.y+24,r.width-10,45),"LOCKED",small,muted*.6f);else if(Storage==CharacterStorage.Inventory)Target(r,null,bag:true);continue;}
                var selected=item;
                Button("item-"+item.Id,r,"",()=>SelectItem(selected.Id),true,SelectedItem?.Id==item.Id,Storage!=CharacterStorage.Inventory);
                Icon(new Rect(r.x+18,r.y+2,74,66),item.Design.Kind);Outline(r,EquipmentSystem.QualityColor(item.Quality),SelectedItem?.Id==item.Id?3:1);
                Text(new Rect(r.x+65,r.y+4,46,22),"Lv "+item.Level,small);
                Bar(new Rect(r.x+7,r.yMax-7,r.width-14,3),item.Condition/100f,item.Condition<=25?Color.red:EquipmentSystem.QualityColor(item.Quality));
                if(Storage==CharacterStorage.Inventory)Target(r,item.Id,bag:true);
            }
            if(items.Count==0)Text(new Rect(1000,350,550,60),"No items match this view.",label,muted);
            if(pages>1){Button("prev",new Rect(975,552,70,30),"<",()=>Page=(Page+pages-1)%pages);Text(new Rect(1060,558,280,25),"PAGE "+(Page+1)+" / "+pages,small,muted);Button("next",new Rect(1505,552,70,30),">",()=>Page=(Page+1)%pages);}
        }
        void DrawDetails()
        {
            var r=new Rect(965,565,620,280);Fill(r,ink);Outline(r,new Color(.17f,.27f,.31f));
            var item=SelectedItem;
            if(item==null){Text(new Rect(995,625,550,42),"SELECT AN ITEM TO INSPECT",title,muted);Text(new Rect(995,690,550,95),Status+"\n"+(game.InMission?"Equipment changes, stash and vendor services unlock at the Nexus.":"Choose a hand slot to compare one-handed weapons."),small,muted);return;}
            Icon(new Rect(980,578,90,90),item.Design.Kind);Outline(new Rect(980,578,90,90),EquipmentSystem.QualityColor(item.Quality));
            Text(new Rect(1085,579,470,32),item.Design.Name.ToUpperInvariant(),label,EquipmentSystem.QualityColor(item.Quality));
            Text(new Rect(1085,610,490,24),item.Quality.ToString().ToUpperInvariant()+" / LEVEL "+item.Level+" / CONDITION "+item.Condition+"%",small,muted);
            Bar(new Rect(1085,641,245,5),item.Condition/100f,item.Condition<=25?Color.red:cyan);
            if(Storage==CharacterStorage.Inventory && selectedEquipped<0 && GearCatalog.Weapon(item.Design.Kind) && !item.Design.TwoHanded && item.Design.Kind!=GearKind.Shield)
            {
                Button("choose-main",new Rect(1340,642,108,30),"MAIN HAND",()=>ChooseHand(GearSlot.MainHand),true,CompareSlot==GearSlot.MainHand);
                Button("choose-off",new Rect(1455,642,108,30),"OFF HAND",()=>ChooseHand(GearSlot.OffHand),true,CompareSlot==GearSlot.OffHand);
            }
            if(Storage==CharacterStorage.Inventory && selectedEquipped<0)
            {
                var equipped=Profile.Equipped[(int)CompareSlot];
                Text(new Rect(980,677,600,25),"Compared with "+(equipped?.Design.Name??"empty slot")+" / "+CompareSlot,small,muted);
                int row=0;
                if(GearCatalog.Weapon(item.Design.Kind) && item.Design.Kind!=GearKind.Shield)
                {Text(new Rect(980,704,580,24),"Base hit "+BaseDamage(item).ToString("0.#")+" / equipped "+BaseDamage(equipped).ToString("0.#"),small,cyan);row++;}
                foreach(BonusKind kind in new[]{item.Design.Identity}.Concat(item.Bonuses.Select(b=>b.Kind)).Concat(equipped?.Bonuses.Select(b=>b.Kind)??Enumerable.Empty<BonusKind>()).Concat(equipped==null?Enumerable.Empty<BonusKind>():new[]{equipped.Design.Identity}).Distinct())
                {
                    float delta=CompareBonus(kind);if(Mathf.Abs(delta)<.001f || row>=2)continue;
                    Text(new Rect(980,704+row*23,580,24),kind+" "+(delta>0?"+":"")+(delta*(kind==BonusKind.Health?1:100)).ToString("0.#")+(kind==BonusKind.Health?"":"%"),small,delta<0?new Color(1,.5f,.45f):new Color(.35f,1,.6f));row++;
                }
                Text(new Rect(1290,727,280,24),"Power "+PreviewPower+" / "+Profile.Capacity,small,PreviewPower>Profile.Capacity?new Color(1,.6f,.3f):cyan);
                string error=EquipRestriction;
                if(game.InMission)
                    Button("drop-primary",new Rect(980,793,380,38),"DROP ITEM INTO MISSION",()=>DropSelected());
                else if(Selling)
                    Button("sell-primary",new Rect(980,793,380,38),"SELL / "+item.Sale+" CR",()=>ConfirmSellSelected());
                else
                {
                    Button("equip",new Rect(980,793,185,38),"EQUIP "+CompareSlot.ToString().ToUpperInvariant(),()=>EquipSelected(),error==null);
                    Button("stash",new Rect(1175,793,185,38),"MOVE TO STASH",()=>Transaction(Profile.Transfer(item.Id,true,game.InMission),"Stored at the Nexus."));
                }
                Text(new Rect(980,763,585,27),error??Status,small,error!=null?new Color(1,.7f,.35f):muted);
            }
            else if(Storage==CharacterStorage.Vendor)
            {
                Text(new Rect(980,710,580,55),Describe(item),small,muted);
                Button("buy",new Rect(980,793,245,38),"BUY / "+item.Price+" CR",()=>Transaction(Profile.Buy(item.Id,game.InMission),"Purchased."),!game.InMission&&Profile.Credits>=item.Price&&Profile.Bag.Count<Profile.BagCapacity);
            }
            else if(Storage==CharacterStorage.Stash)
            {
                Text(new Rect(980,710,580,55),Describe(item),small,muted);
                Button("withdraw",new Rect(980,793,300,38),"MOVE TO INVENTORY",()=>Transaction(Profile.Transfer(item.Id,false,game.InMission),"Moved to inventory."),!game.InMission&&Profile.Bag.Count<Profile.BagCapacity);
            }
            else
            {
                Text(new Rect(980,710,580,55),Describe(item),small,muted);
                Button("unequip",new Rect(980,793,300,38),"UNEQUIP TO INVENTORY",()=>UnequipSlot((GearSlot)selectedEquipped),!game.InMission&&Profile.Bag.Count<Profile.BagCapacity);
            }
            Button("more",new Rect(1370,793,190,38),MoreOpen?"CLOSE ACTIONS":"MORE / DETAILS",()=>{MoreOpen=!MoreOpen;FocusId=MoreOpen?"close-more":"more";});
            if(MoreOpen)DrawMore(item);
        }
        static float BaseDamage(GearItem item)=>item==null || !GearCatalog.Weapon(item.Design.Kind)||item.Design.Kind==GearKind.Shield?0:(item.Design.Kind==GearKind.Sword?34:20)*(item.Condition/100f)*(1+(item.Level-1)*.08f);
        static string Describe(GearItem item)
        {
            string stats=GearCatalog.Weapon(item.Design.Kind)&&item.Design.Kind!=GearKind.Shield?"Base hit "+BaseDamage(item).ToString("0.#"):item.Design.Identity+" "+(item.Design.Value*item.Scale*(item.Design.Identity==BonusKind.Health?1:100)).ToString("0.#")+(item.Design.Identity==BonusKind.Health?"":"%");
            if(item.Bonuses.Count>0)stats+=" / "+string.Join(" / ",item.Bonuses.Select(b=>b.Kind+" +"+(b.Value*(b.Kind==BonusKind.Health?1:100)).ToString("0.#")+(b.Kind==BonusKind.Health?"":"%")));
            return "Power "+item.Power+" / Sell "+item.Sale+" CR\n"+stats+(item.SignatureActive?"\n"+item.Design.Signature:"");
        }
        void DrawMore(GearItem item)
        {
            drawingMore=true;
            var r=new Rect(975,330,600,257);Fill(r,new Color(.025f,.055f,.075f));Outline(r,cyan);
            Text(new Rect(995,343,490,105),Describe(item),small);
            Button("close-more",new Rect(1515,342,40,35),"X",()=>CloseActions());
            bool bag=selectedEquipped<0&&Storage==CharacterStorage.Inventory;
            Button("repair",new Rect(995,468,180,40),"REPAIR / "+item.RepairCost+" CR",()=>Transaction(Profile.Repair(item.Id,game.InMission),"Condition restored."),bag&&!game.InMission&&item.Condition<100&&Profile.Credits>=item.RepairCost);
            Button("sell",new Rect(1185,468,180,40),"SELL / "+item.Sale+" CR",()=>game.Menus.ConfirmEquipment("Sell "+item.Design.Name+" for "+item.Sale+" credits?",()=>Transaction(Profile.Sell(item.Id,game.InMission),"Sold.")),bag&&!game.InMission);
            Button("drop",new Rect(1375,468,180,40),"DROP ITEM",()=>DropSelected(),bag&&game.InMission);
            Text(new Rect(995,523,550,50),game.InMission?"Field gear cannot be changed or repaired. Dropped items remain in this mission.":"Repairs restore condition, never lost rarity. Equipped items must be unequipped before selling.",small,muted);
            drawingMore=false;
        }
        void DrawAttributes()
        {
            Text(new Rect(975,100,580,45),"ATTRIBUTE ALLOCATION",title);
            Text(new Rect(975,155,580,55),"One point per level. Allocate at the Nexus. Changes apply immediately.",small,muted);
            string[] descriptions={"+10 maximum health","+2 inventory slots","+2.5% movement speed","+2.5% accuracy","+3 equipment power"};
            for(int n=0;n<5;n++)
            {
                var kind=(AttributeKind)n;float y=230+n*93;
                Text(new Rect(980,y,440,30),kind.ToString().ToUpperInvariant()+" / "+Profile.Attributes[n],label);
                Text(new Rect(980,y+33,420,28),descriptions[n],small,muted);
                Button("spend-"+kind,new Rect(1445,y,110,48),"+ POINT",()=>Transaction(Profile.Spend(kind),"Attribute increased."),!game.InMission&&Profile.Points>0&&Profile.Attributes[n]<50);
            }
            Button("reset",new Rect(980,745,575,48),"RESET ALLOCATION / "+Profile.ResetPrice+" CR",()=>game.Menus.ConfirmEquipment("Reset all allocated points for "+Profile.ResetPrice+" credits? Equipment must fit 20 power and inventory must fit 10 slots.",()=>Transaction(Profile.ResetAttributes(),"Points refunded.")),!game.InMission&&Profile.Credits>=Profile.ResetPrice);
            Text(new Rect(980,805,575,40),game.InMission?"Attribute allocation is available at the Nexus.":Status,small,muted);
        }
    }
}
