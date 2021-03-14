using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [HarmonyPatch(typeof(Player), "SetControls")]
    public static class Player_SetControls_Patch
    {
        // Fix crouch bug when player is drawing a bow.
        public static bool isPlayerAbleToCrouch;

        private static void Postfix(Player __instance, bool attack, bool attackHold, bool secondaryAttack, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun)
        {
            if (CustomizableCamera.characterCrouched && crouch)
            {
                CustomizableCamera.characterCrouched = false;
                return;
            }

            if (run || autoRun || blockHold)
                isPlayerAbleToCrouch = false;
            else
                isPlayerAbleToCrouch = true;

            if (isPlayerAbleToCrouch && crouch)
                CustomizableCamera.characterCrouched = true;
        }
    }
}