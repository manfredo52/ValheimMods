using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [HarmonyPatch(typeof(Player), "SetControls")]
    public class Player_SetControls_Patch : CustomizableCamera
    {
        public static bool isPlayerAbleToCrouch;

        private static void Prefix(Player __instance, ref bool block, ref bool blockHold)
        {
            if (!__instance) return;

            ItemDrop.ItemData playerItemEquipped = __instance.GetLeftItem();

            if (playerItemEquipped != null && (playerItemEquipped.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow))
                characterEquippedBow = true;
            else
                characterEquippedBow = false;

            if (bowZoomEnabled.Value)
            {
                if (__instance.IsHoldingAttack() && characterEquippedBow)
                {
                    if (bowZoomOnDraw.Value)
                    {
                        if (block || blockHold)
                            characterAiming = false;
                        else
                            characterAiming = true;
                    }
                    else // Fix when player doesn't use zoom on draw.
                    {
                        block = false;
                        blockHold = false;

                        if (Input.GetKey(bowZoomKey.Value.MainKey))
                            characterAiming = true;

                        if (Input.GetKey(bowCancelDrawKey.Value.MainKey))
                        {
                            characterAiming = false;
                            block = true;
                            blockHold = true;
                        }
                    }                 
                }
                else
                {
                    characterAiming = false;
                }   
            }
        }

        private static void Postfix(Player __instance, bool blockHold, bool crouch, bool run, bool autoRun)
        {
            if (!__instance) return;

            if (characterCrouched && crouch)
            {
                characterCrouched = false;
                return;
            }

            if (run || autoRun || blockHold)
                isPlayerAbleToCrouch = false;
            else
                isPlayerAbleToCrouch = true;

            if (isPlayerAbleToCrouch && crouch)
                characterCrouched = true;
        }
    }
}