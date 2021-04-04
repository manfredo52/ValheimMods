using UnityEngine;
using HarmonyLib;

namespace ImmersiveHud
{
    [HarmonyPatch(typeof(Player), "Update")]
    public class Player_Update_Patch : ImmersiveHud
    {
        private static void Prefix(Player __instance)
        {
            if (!__instance) return;

            ItemDrop.ItemData playerItemEquippedLeft = __instance.GetLeftItem();
            ItemDrop.ItemData playerItemEquippedRight = __instance.GetRightItem();

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
}