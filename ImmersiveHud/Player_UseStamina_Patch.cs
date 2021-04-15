using UnityEngine;
using HarmonyLib;

namespace ImmersiveHud
{
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
}