using UnityEngine;
using HarmonyLib;

namespace PassTheTime
{
    [HarmonyPatch(typeof(Player), "SetControls")]
    public class Player_SetControls_Patch : PassTheTime
    {
        private static void Prefix(ref bool attack, ref bool attackHold, ref bool block, ref bool blockHold)
        {
            if (!isEnabled.Value)
                return;

            if (waitDialog.activeSelf)
            {
                attack = false;
                attackHold = false;
                block = false;
                blockHold = false;
            }
        }
    }
}
