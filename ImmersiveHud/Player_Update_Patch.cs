using UnityEngine;
using HarmonyLib;

namespace ImmersiveHud
{
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
}