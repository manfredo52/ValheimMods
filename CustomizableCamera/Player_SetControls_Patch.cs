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

            // Zoom in with the bow if enabled.
            if (bowZoomEnabled.Value)
            {
                // Check to make sure if the user is drawing their bow.
                if (__instance.IsHoldingAttack() && characterEquippedBow)
                {
                    // Automatically zoom in on draw.
                    if (bowZoomOnDraw.Value)
                    {
                        if (block || blockHold)
                            characterAiming = false;
                        else
                            characterAiming = true;
                    }
                    else
                    {
                        block = false;
                        blockHold = false;

                        // Cancel draw key, toggle zoom, or hold zoom
                        if (Input.GetKey(bowCancelDrawKey.Value.MainKey))
                        {
                            characterAiming = false;
                            block = true;
                            blockHold = true;
                        }
                        else if (bowZoomKeyToggle.Value)
                        {
                            
                            if (Input.GetKeyDown(bowZoomKey.Value.MainKey))
                                characterAiming = !characterAiming;
                        }
                        else
                        {
                            if (Input.GetKey(bowZoomKey.Value.MainKey))
                                characterAiming = true;
                            else
                                characterAiming = false;
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