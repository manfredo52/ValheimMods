using UnityEngine;
using HarmonyLib;

namespace ImmersiveHud
{
    [HarmonyPatch(typeof(Hud), "Update")]
    public class Hud_Update_Patch : ImmersiveHud
    {
        public static void setCompatibility(Transform hud)
        {
            // Compatibility check for Quick Slots mod.
            if (quickSlotsEnabled.Value && hudElements["QuickSlotsHotkeyBar"].element == null)
            {
                if (hud.Find("QuickSlotsHotkeyBar"))
                {
                    hudElements["QuickSlotsHotkeyBar"].element = hud.Find("QuickSlotsHotkeyBar");
                    hudElements["QuickSlotsHotkeyBar"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                    hasQuickSlotsEnabled = true;
                } 
                else
                {
                    hasQuickSlotsEnabled = false;
                }
            }
        }

        public static void updateHudElementTransparency(HudElement hudElement)
        {
            float lerpedAlpha;
            lerpedAlpha = Mathf.Lerp(hudElement.lastSetAlpha, hudElement.targetAlpha, hudElement.timeFade / hudFadeDuration.Value);

            if (hudElement.elementName == "MiniMap")
            {
                hudElement.element.GetComponent<Minimap>().m_mapImageSmall.CrossFadeAlpha(hudElement.targetAlpha, hudElement.timeFade, false);
            }

            hudElement.lastSetAlpha = lerpedAlpha;
            hudElement.element.GetComponent<CanvasGroup>().alpha = lerpedAlpha;
        }

        public static bool checkHudLerpDuration(float timeElapsed)
        {
            if (timeElapsed >= hudFadeDuration.Value)
                return true;
            else
                return false;
        }

        public static void getPlayerData(Transform hud)
        {
            Minimap playerMap = hud.Find("MiniMap").GetComponent<Minimap>();
            bool prevState = isMiniMapActive;

            isMiniMapActive = playerMap.m_smallRoot.activeSelf;

            // Reset timer when changing map modes.
            if (prevState != isMiniMapActive)
                hudElements["MiniMap"].timeFade = 0;
        }

        public static void setValuesBasedOnHud(bool pressedKey)
        {
            Player localPlayer = Player.m_localPlayer;

            // Store previous target alpha for timer reset.
            foreach (string name in hudElementNames)
            {
                if (hudElements[name].element == null)
                    continue;

                hudElements[name].targetAlphaPrev = hudElements[name].targetAlpha;
            }         

            if (pressedKey)
            {
                hudHidden = !hudHidden;

                if (hudHiddenNotification.Value)
                {
                    if (hudHidden)
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Hud is hidden.");
                    else
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Hud is visible.");
                }

                foreach (string name in hudElementNames)
                {
                    hudElements[name].timeFade = 0;
                    hudElements[name].timeDisplay = 0;
                }
            }

            if (hudHidden)
            {
                foreach (string name in hudElementNames)
                    hudElements[name].targetAlpha = 0;

                if (isMiniMapActive)
                    hudElements["MiniMap"].targetAlpha = 0;
                else
                    hudElements["MiniMap"].targetAlpha = 1;
            }
            else
            {
                // Health Display
                if (displayHealthAlways.Value || (displayHealthInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["healthpanel"].targetAlpha = 1;
                }
                else
                {
                    // Display health panel when below a given percentage.
                    if (displayHealthWhenBelowPercentage.Value && localPlayer.GetHealthPercentage() <= healthPercentage.Value)
                        hudElements["healthpanel"].targetAlpha = 1;
                    else
                        hudElements["healthpanel"].targetAlpha = 0;
                }

                // Forsaken Power Display
                if (displayForsakenPowerAlways.Value || (displayPowerInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["GuardianPower"].targetAlpha = 1;
                }
                else
                {             
                    // Show the forsaken power for a duration when the key is pressed.
                    if (displayPowerOnActivation.Value)
                    {
                        if (ZInput.GetButtonDown("GPower") || ZInput.GetButtonDown("JoyGPower"))
                        {
                            hudElements["GuardianPower"].targetAlpha = 1;
                            hudElements["GuardianPower"].timeDisplay = 0;
                        } 
                        else if (hudElements["GuardianPower"].timeDisplay >= hudDisplayDuration.Value)
                        {
                            hudElements["GuardianPower"].targetAlpha = 0;
                        }
                    } 
                    else
                        hudElements["GuardianPower"].targetAlpha = 0;
                }

                // HotKeyBar Display
                if (displayHotKeyBarAlways.Value || (displayHotKeyBarInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["HotKeyBar"].targetAlpha = 1;
                }
                else
                {
                    hudElements["HotKeyBar"].targetAlpha = 0;
                }

                // Status Effects Display
                if (displayStatusEffectsAlways.Value || (displayStatusEffectsInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["StatusEffects"].targetAlpha = 1;
                }
                else
                {
                    hudElements["StatusEffects"].targetAlpha = 0;
                }

                // MiniMap Display
                if (displayMiniMapAlways.Value || (displayMiniMapInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["MiniMap"].targetAlpha = 1;
                }
                else
                {
                    hudElements["MiniMap"].targetAlpha = 0;
                }

                // QuickSlots Display
                if (quickSlotsEnabled.Value && hasQuickSlotsEnabled)
                {
                    if (displayQuickSlotsAlways.Value || (displayQuickSlotsInInventory.Value && InventoryGui.IsVisible()))
                    {
                        hudElements["QuickSlotsHotkeyBar"].targetAlpha = 1;
                    }
                    else
                    {
                        hudElements["QuickSlotsHotkeyBar"].targetAlpha = 0;
                    }
                }
            }

            // Reset timer when the target alpha changed.
            foreach (string name in hudElementNames)
            {
                // Reset fade timers if they go on for too long.
                if (hudElements[name].timeFade > 240f)
                    hudElements[name].timeFade = 0;

                // Reset display timers if they go on for too long.
                if (hudElements[name].timeDisplay > 240f)
                    hudElements[name].timeDisplay = 0;

                if (hudElements[name].element == null)
                    continue;

                if (hudElements[name].targetAlphaPrev != hudElements[name].targetAlpha)
                    hudElements[name].timeFade = 0;               
            }
        }

        private static void Postfix(Hud __instance)
        {
            Player localPlayer = Player.m_localPlayer;

            if (!isEnabled.Value || !localPlayer)
                return;

            Transform hudRoot = __instance.transform.Find("hudroot");

            getPlayerData(hudRoot);
            setCompatibility(hudRoot);
            setValuesBasedOnHud(Input.GetKeyDown(hideHudKey.Value.MainKey));

            foreach (string name in hudElementNames) 
            {
                if (hudElements[name].element == null)
                    continue;

                hudElements[name].targetAlphaReached = checkHudLerpDuration(hudElements[name].timeFade);

                if (!hudElements[name].targetAlphaReached)
                {
                    hudElements[name].timeFade += Time.deltaTime;
                    updateHudElementTransparency(hudElements[name]);
                } 
                else
                {
                    hudElements[name].timeDisplay += Time.deltaTime;
                }
            }
        }
    }
}
