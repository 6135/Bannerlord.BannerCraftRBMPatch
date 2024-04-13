using Bannerlord.BannerCraft.Mixins;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.BannercraftRBMPatch
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly string Namespace = typeof(SubModule).Namespace;

        private readonly Harmony _harmony = new Harmony(Namespace);

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            //patch the GetRandomModifierWithTargetRBMPostfix method from CraftingMixin in bannercraft namespace
            //check if Bannerlord.BannerCraft is loaded
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name.Contains("Bannerlord.BannerCraft")))
                _harmony.Patch(AccessTools.Method(typeof(CraftingMixin), "GetRandomModifierWithTarget"),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(SubModule),
                    "GetRandomModifierWithTargetRBMPostfix"))
                    );
        }

        private delegate T GetItemFieldDelegate<out T>(ItemModifier item, string _fieldName);

#if v116 || v115 || v114 || v113 || v112 || v111 || v110 || v103 || v102 || v101 || v100

        private static string MountHitPoints = "_mountHitPoints";
        private static string ChargeDamage = "_chargeDamage";
        private static string Maneuver = "_maneuver";
        private static string MountSpeed = "_mountSpeed";
        private static string StackCount = "_stackCount";
        private static string HitPoints = "_hitPoints";
        private static string Armor = "_armor";
        private static string MissileSpeed = "_missileSpeed";
        private static string Speed = "_speed";
        private static string Damage = "_damage";
        private static string PriceMultiplier = "_priceMultiplier";

        private static int GetItemFieldInt(ItemModifier item, string _fieldName)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var value = item.GetType()?.GetField(_fieldName, bindingFlags)?.GetValue(item);
            if (value is not null)
                return (int)value;
            else return 0;
        }

        private static short GetItemFieldShort(ItemModifier item, string _fieldName)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var value = item.GetType()?.GetField(_fieldName, bindingFlags)?.GetValue(item);
            if (value is not null)
                return (short)value;
            else return 0;
        }

        //float
        private static float GetItemFieldFloat(ItemModifier item, string _fieldName)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var value = item.GetType()?.GetField(_fieldName, bindingFlags)?.GetValue(item);
            if (value is not null)
                return (float)value;
            else return 0;
        }

#else
        private static readonly string MountHitPoints = "MountHitPoints";
        private static readonly string ChargeDamage = "ChargeDamage";
        private static readonly string Maneuver = "Maneuver";
        private static readonly string MountSpeed = "MountSpeed";
        private static readonly string StackCount = "StackCount";
        private static readonly string HitPoints = "HitPoints";
        private static readonly string Armor = "Armor";
        private static readonly string MissileSpeed = "MissileSpeed";
        private static readonly string Speed = "Speed";
        private static readonly string Damage = "Damage";
        private static readonly string PriceMultiplier = "PriceMultiplier";

        //they were into properties.
        private static int GetItemFieldInt(ItemModifier item, string _fieldName)
        {
            var value = item.GetType()?.GetProperty(_fieldName)?.GetValue(item);
            if (value is not null)
                return (int)value;
            else return 0;
        }

        private static short GetItemFieldShort(ItemModifier item, string _fieldName)
        {
            var value = item.GetType()?.GetProperty(_fieldName)?.GetValue(item);
            if (value is not null)
                return (short)value;
            else return 0;
        }

        //float
        private static float GetItemFieldFloat(ItemModifier item, string _fieldName)
        {
            var value = item.GetType()?.GetProperty(_fieldName)?.GetValue(item);
            if (value is not null)
                return (float)value;
            else return 0;
        }

#endif

        private static float getModifierSum(ItemModifier im)
        {
            /*
             *           string MountHitPoints; //float
                        string ChargeDamage; //float
                        string Maneuver; //float
                        string MountSpeed; //float
                        string StackCount; //short
                        string HitPoints; //short
                        string Armor; //int
                        string MissileSpeed; //int
                        string Speed;   //int
                        string Damage; //int
                        string PriceMultiplier; //float*/
            //sorts by the sum of all the modifiers, then by price multiplier

            GetItemFieldDelegate<int>? getItemFieldDelegateInstanceInt = GetItemFieldInt;
            GetItemFieldDelegate<short>? getItemFieldDelegateInstanceShort = GetItemFieldShort;
            GetItemFieldDelegate<float>? getItemFieldDelegateInstanceFloat = GetItemFieldFloat;
            //sum of all the modifiers and multiply by the price multiplier, using /getItemFieldDelegateInstanceInt /and getItemFieldDelegateInstanceShort
            float sum =
                getItemFieldDelegateInstanceFloat(im, MountHitPoints) +
                getItemFieldDelegateInstanceFloat(im, ChargeDamage) +
                getItemFieldDelegateInstanceFloat(im, Maneuver) +
                getItemFieldDelegateInstanceFloat(im, MountSpeed) +
                getItemFieldDelegateInstanceShort(im, StackCount) +
                getItemFieldDelegateInstanceShort(im, HitPoints) +
                getItemFieldDelegateInstanceInt(im, Armor) +
                getItemFieldDelegateInstanceInt(im, MissileSpeed) +
                getItemFieldDelegateInstanceInt(im, Speed) +
                getItemFieldDelegateInstanceInt(im, Damage);
            //if sum is negative, abs it and multiply by the price multiplier and return it negative using branchless code

            if (sum < 0)
                return sum / getItemFieldDelegateInstanceFloat(im, PriceMultiplier);
            else
                return sum * getItemFieldDelegateInstanceFloat(im, PriceMultiplier);
        }

        public static void GetRandomModifierWithTargetRBMPostfix(ref ItemModifier __result, ItemModifierGroup modifierGroup, int modifierTier)
        {
            //check if there are more than 6 modifiers
            if (modifierGroup.ItemModifiers.Count() > 6)
            {
                var modifiers = modifierGroup.ItemModifiers;
                //remove duplicate itemModifiers and order them by the sum of the modifiers
                var filteredOrdered = modifiers.Distinct().OrderBy(mod => getModifierSum(mod));
                //create a new list with the same order, but with the sums of the modifiers as the key, there can be duplicate k
                var resultsWithSum = filteredOrdered.Select(mod => new KeyValuePair<float, ItemModifier>(getModifierSum(mod), mod));

                //get the 3 highest sums
                var highest = resultsWithSum.Skip(resultsWithSum.Count() - 3).Take(3);
                //get all the negative sums
                var negative = resultsWithSum.Where(mod => mod.Key < 0);
                //order the negative sums by the lowest first
                var negativeOrdered = negative.OrderBy(mod => mod.Key);
                //get the 2 highest sums of the negative sums without TakeLast
                var negativeHighest = negativeOrdered.Skip(negativeOrdered.Count() - 2).Take(2);
                //join the 3 highest sums and the 3 highest negative sums
                var joined = negativeHighest.Concat(highest);
                //return the modifier at modifierTier
                __result = joined.ElementAt(Math.Min(joined.Count() - 1, modifierTier)).Value;
            }
        }
    }
}