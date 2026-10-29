using System;
using System.Linq;
using NUnit.Framework;
using VideoGame.Core;

public sealed class EquipmentTests
{
    static GearItem Item(string id,GearQuality quality=GearQuality.White,int level=1) => GearCatalog.Roll(new Random(91),level,quality,id);
    [Test] public void StarterAndAllRollsHaveStableValidIdentityAndSuitableBonuses()
    {
        var p=GearProfile.Starter();Assert.IsTrue(p.Valid());
        var rng=new Random(17);
        foreach(var d in GearCatalog.All)foreach(GearQuality q in Enum.GetValues(typeof(GearQuality)))
        {
            var item=GearCatalog.Roll(rng,4,q,d.Id);Assert.IsTrue(Guid.TryParse(item.Id,out _));
            Assert.AreEqual(Math.Min(3,(int)item.Quality),item.Bonuses.Count);
            Assert.IsTrue(item.Bonuses.All(b=>GearCatalog.Suitable(d.Kind,b.Kind)));
            if(item.Quality==GearQuality.Gold)Assert.IsNotEmpty(d.Signature);
        }
    }
    [Test] public void TwoHandedReplacementIsAtomicAtFullBagAndCapacityLimits()
    {
        var p=GearProfile.Starter();var rifle=p.Equipped[3];
        Assert.IsNull(p.Equip(p.Bag.First(i=>i.Definition=="pistol").Id,GearSlot.MainHand,false));
        Assert.IsNull(p.Equip(p.Bag.First(i=>i.Definition=="pistol").Id,GearSlot.OffHand,false));
        while(p.Bag.Count<p.BagCapacity)p.Bag.Add(Item("sword"));
        Assert.IsNotNull(p.Equip(rifle.Id,GearSlot.MainHand,false));Assert.AreEqual("pistol",p.Equipped[3].Definition);Assert.IsNotNull(p.Equipped[4]);
        p.Bag.RemoveAt(p.Bag.Count-1);Assert.IsNull(p.Equip(rifle.Id,GearSlot.MainHand,false));Assert.IsNull(p.Equipped[4]);Assert.AreEqual(10,p.Bag.Count);
        Assert.IsNotNull(p.Equip(p.Bag[0].Id,GearSlot.OffHand,false));
    }
    [Test] public void HandednessLevelPowerAndMissionRestrictionsAreEnforced()
    {
        var p=GearProfile.Starter();var shield=p.Bag.First(i=>i.Definition=="shield");
        Assert.IsNotNull(p.Equip(shield.Id,GearSlot.MainHand,false));
        Assert.IsNull(p.Equip(p.Bag.First(i=>i.Definition=="pistol").Id,GearSlot.MainHand,false));
        Assert.IsNull(p.Equip(shield.Id,GearSlot.OffHand,false));
        Assert.IsNotNull(p.Equip(p.Bag.First(i=>i.Definition=="sword").Id,GearSlot.Helmet,false));
        var high=Item("heavy",GearQuality.Gold,2);p.Bag.Add(high);
        Assert.IsNotNull(p.Equip(high.Id,GearSlot.Armor,false));
        high.Level=1;Assert.IsNull(p.Equip(high.Id,GearSlot.Armor,false));
        var gold=Item("executioner",GearQuality.Gold);p.Bag.Add(gold);
        Assert.IsNotNull(p.Equip(gold.Id,GearSlot.Helmet,false));
        Assert.IsNotNull(p.Equip(p.Bag[0].Id,GearSlot.MainHand,true));Assert.IsFalse(p.Unequip(GearSlot.MainHand,true));
    }
    [Test] public void InventoryOverflowDuplicatePickupAndTransfersPreserveOwnership()
    {
        var p=GearProfile.Starter();var item=Item("pistol");Assert.IsTrue(p.Pickup(item));Assert.IsFalse(p.Pickup(item));
        Assert.IsFalse(p.Transfer(item.Id,true,true));Assert.IsTrue(p.Transfer(item.Id,true,false));Assert.IsFalse(p.Transfer(item.Id,true,false));
        while(p.Bag.Count<p.BagCapacity)p.Bag.Add(Item("pistol"));
        Assert.IsFalse(p.Transfer(item.Id,false,false));Assert.IsTrue(p.Stash.Contains(item));Assert.IsFalse(p.Pickup(Item("sword")));
    }
    [Test] public void IdenticalWearableGearAlwaysOccupiesSeparateCapacitySlots()
    {
        var p=GearProfile.Starter();p.Bag.Clear();
        var shields=Enumerable.Range(0,3).Select(_=>Item("shield",GearQuality.Purple,3)).ToArray();
        foreach(var shield in shields)Assert.IsTrue(p.Pickup(shield));
        Assert.AreEqual(3,p.Bag.Count);Assert.AreEqual(3,p.Bag.Select(i=>i.Id).Distinct().Count());
        while(p.Bag.Count<p.BagCapacity)Assert.IsTrue(p.Pickup(Item("shield",GearQuality.Purple,3)));
        Assert.IsFalse(p.Pickup(Item("shield",GearQuality.Purple,3)));Assert.AreEqual(10,p.Bag.Count);
    }
    [Test] public void LegacyMainHandShieldSwapsWithoutChangingOwnershipAndAllSlotsCanBeUnequipped()
    {
        var p=GearProfile.Starter();var shield=p.Bag.First(i=>i.Definition=="shield");var pistol=p.Bag.First(i=>i.Definition=="pistol");
        p.Equip(pistol.Id,GearSlot.MainHand,false);p.Equip(shield.Id,GearSlot.OffHand,false);
        p.Equipped[3]=shield;p.Equipped[4]=pistol;Assert.IsFalse(p.Valid());
        p.NormalizeLegacyHandedness();Assert.IsTrue(p.Valid());Assert.AreSame(shield,p.Equipped[4]);Assert.AreSame(pistol,p.Equipped[3]);
        p.Bag.Clear();
        foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot))){Assert.IsTrue(p.Unequip(slot,false));Assert.IsNull(p.Equipped[(int)slot]);}
        Assert.AreEqual(5,p.Bag.Count);Assert.IsTrue(p.Valid());
    }
    [Test] public void DeathDegradesOnlyFreshRarityButAllCarriedConditionAndOnlyFieldCredits()
    {
        var p=GearProfile.Starter(1000);var old=p.Bag[0];var fresh=Item("sword",GearQuality.Gold);fresh.Fresh=true;p.Bag.Add(fresh);
        var stashed=Item("heavy",GearQuality.Gold);p.Stash.Add(stashed);
        var m=new GearMission(p,1);m.EarnCredits(101);p.AddXP(30);Assert.IsTrue(m.Return(false));Assert.IsFalse(m.Return(false));
        Assert.AreEqual(1070,p.Credits);Assert.AreEqual(30,p.Experience);Assert.AreEqual(75,old.Condition);Assert.AreEqual(GearQuality.White,old.Quality);
        Assert.AreEqual(GearQuality.Purple,fresh.Quality);Assert.AreEqual(3,fresh.Bonuses.Count);Assert.IsFalse(fresh.SignatureActive);Assert.IsFalse(fresh.Fresh);
        Assert.AreEqual(100,stashed.Condition);Assert.AreEqual(100,p.Equipped[0].Condition);Assert.IsTrue(p.Valid());
        for(int i=0;i<3;i++)new GearMission(p,1).Return(false);
        Assert.IsFalse(p.Bag.Contains(old));Assert.IsFalse(p.Bag.Contains(fresh));Assert.IsTrue(p.Stash.Contains(stashed));
    }
    [Test] public void ExtractionPreservesLootAndSecuresAllFieldCreditsExactlyOnce()
    {
        var p=GearProfile.Starter();var fresh=Item("shield",GearQuality.Gold);fresh.Fresh=true;p.Pickup(fresh);
        var m=new GearMission(p,2);m.EarnCredits(100);Assert.IsTrue(m.Return(true));m.EarnCredits(500);Assert.IsFalse(m.Return(true));
        Assert.AreEqual(100,p.Credits);Assert.AreEqual(100,fresh.Condition);Assert.AreEqual(GearQuality.Gold,fresh.Quality);Assert.IsFalse(fresh.Fresh);
    }
    [Test] public void RepairsNeverRestoreLostRarityAndSalesCannotReplay()
    {
        var p=GearProfile.Starter(2000);var i=p.Bag[0];i.Condition=25;int before=p.Credits,cost=i.RepairCost;
        Assert.IsFalse(p.Repair(i.Id,true));Assert.IsTrue(p.Repair(i.Id,false));Assert.AreEqual(before-cost,p.Credits);Assert.AreEqual(100,i.Condition);
        Assert.IsFalse(p.Repair(i.Id,false));Assert.AreEqual(GearQuality.White,i.Quality);
        int sale=i.Sale;before=p.Credits;Assert.IsTrue(p.Sell(i.Id,false));Assert.IsFalse(p.Sell(i.Id,false));Assert.AreEqual(before+sale,p.Credits);
    }
    [Test] public void VendorStockIsDeterministicAndPurchaseIsExactlyOnce()
    {
        var p=GearProfile.Starter(5000);p.Restock(11);Assert.AreEqual(17,p.Shop.Count);
        var item=p.Shop[0];int price=item.Price;Assert.IsTrue(p.Buy(item.Id,false));Assert.IsFalse(p.Buy(item.Id,false));Assert.AreEqual(5000-price,p.Credits);
        Assert.IsTrue(p.Bag.Contains(item));Assert.IsFalse(p.Shop.Contains(item));
    }
    [Test] public void LevelPointsResetAndEnemyRangesRespectMissionEntrySnapshot()
    {
        var p=GearProfile.Starter(10000);p.AddXP(300);Assert.AreEqual(3,p.Level);Assert.AreEqual(2,p.Points);
        Assert.IsTrue(p.Spend(AttributeKind.Inventory));Assert.IsTrue(p.Spend(AttributeKind.Capacity));Assert.IsFalse(p.Spend(AttributeKind.Health));
        Assert.AreEqual(12,p.BagCapacity);Assert.AreEqual(23,p.Capacity);
        var m=new GearMission(p,1);p.AddXP(300);var rng=new Random(44);
        var levels=Enumerable.Range(0,500).Select(_=>m.EnemyLevel(rng)).Distinct().OrderBy(v=>v).ToArray();CollectionAssert.AreEqual(new[]{2,3,4,5,6},levels);
        Assert.IsTrue(p.ResetAttributes());Assert.AreEqual(3,p.Points);Assert.AreEqual(8000,p.Credits);Assert.IsTrue(p.Valid());
    }
    [Test] public void ResetCannotStrandInventoryOrEquippedPower()
    {
        var p=GearProfile.Starter(10000);p.AddXP(100);p.Spend(AttributeKind.Inventory);
        while(p.Bag.Count<11)p.Bag.Add(Item("pistol"));int before=p.Credits;Assert.IsFalse(p.ResetAttributes());Assert.AreEqual(before,p.Credits);
    }
    [Test] public void ShieldDepletionRequiresReleaseAndRespectfulRechargeDelay()
    {
        var shield=new ShieldCharge();for(int i=0;i<181;i++)shield.Tick(true,1/60f);
        Assert.IsFalse(shield.Guarding);Assert.LessOrEqual(shield.Charge,.001f);
        shield.Tick(true,1);Assert.AreEqual(0,shield.Charge,.001f);
        shield.Tick(true,3);Assert.Greater(shield.Charge,0);Assert.IsFalse(shield.Guarding);
        shield.Tick(false,.01f);shield.Tick(true,.01f);Assert.IsTrue(shield.Guarding);
    }
    [Test] public void CorruptGearAndImpossibleLoadoutsAreRejected()
    {
        var p=GearProfile.Starter();p.Bag[0].Id=p.Equipped[0].Id;Assert.IsFalse(p.Valid());
        p=GearProfile.Starter();p.Equipped[4]=Item("pistol");Assert.IsFalse(p.Valid());
        p=GearProfile.Starter();p.Bag[0].Bonuses.Add(new GearBonus{Kind=BonusKind.Damage,Value=float.NaN});Assert.IsFalse(p.Valid());
    }
}
