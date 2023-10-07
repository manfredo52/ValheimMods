using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System;

namespace ImmersiveHud
{
    [HarmonyPatch(typeof(Player), "Update")]
    public class Player_Update_Patch : ImmersiveHud
    {
        private static MethodInfo _GetRightItemMethod = AccessTools.Method(typeof(Humanoid), "GetRightItem");
        private static MethodInfo _GetLeftItemMethod = AccessTools.Method(typeof(Humanoid), "GetLeftItem");
        private static void Prefix(Player __instance)
        {
            if (!__instance) return;

            ItemDrop.ItemData playerItemEquippedLeft = _GetLeftItemMethod.Invoke(__instance, Array.Empty<object>()) as ItemDrop.ItemData;
            ItemDrop.ItemData playerItemEquippedRight = _GetRightItemMethod.Invoke(__instance, Array.Empty<object>()) as ItemDrop.ItemData;

            if (playerItemEquippedLeft != null && (playerItemEquippedLeft.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow))
            {
                characterEquippedBow = true;
                characterEquippedItem = false;
            } 
            else if (playerItemEquippedLeft != null || playerItemEquippedRight != null)
            {
                characterEquippedBow = false;
                characterEquippedItem = true;
            }                
            else
            {
                characterEquippedItem = false;
                characterEquippedBow = false;
            }         
        }
    }

    [HarmonyPatch(typeof(Player), "UseStamina")]
    public class Player_UseStamina_Patch : ImmersiveHud
    {
        private static void Postfix(ref float v)
        {
            if (v == 0.0)
                return;

            playerUsedStamina = true;
        }
    }

    [HarmonyPatch(typeof(Player), "UseHotbarItem")]
    public class Player_UseHotbarItem_Patch : ImmersiveHud
    {
        private static void Postfix(int index)
        {
            Player localPlayer = Player.m_localPlayer;

            ItemDrop.ItemData itemAt = localPlayer.GetInventory().GetItemAt(index - 1, 0);

            if (itemAt == null)
                return;

            playerUsedHotBarItem = true;
        }
    }

    [HarmonyPatch]
    public class Player_UpdateFood_Patch : ImmersiveHud
    {
        private static MethodBase TargetMethod()
        {
            return typeof(Player).GetMethod("UpdateFood", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void Postfix(ref float dt, ref bool forceUpdate)
        {
            if (dt == 0 && forceUpdate)
                playerAteFood = true;
            else
                playerAteFood = false;
        }
    }
}