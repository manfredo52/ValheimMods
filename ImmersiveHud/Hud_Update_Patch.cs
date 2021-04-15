using UnityEngine;
using HarmonyLib;

namespace ImmersiveHud
{
    [HarmonyPatch(typeof(Hud), "Update")]
    public class Hud_Update_Patch : ImmersiveHud
    {
        public static void setCompatibility(Transform hud)
        {
            // Compatibility check for Quick Slots.
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

            // Compatibility check for BetterUI HP Bar
            if (betterUIHPEnabled.Value && hudElements["BetterUI_HPBar"].element == null)
            {
                if (hud.Find("BetterUI_HPBar"))
                {
                    hudElements["BetterUI_HPBar"].element = hud.Find("BetterUI_HPBar");
                    hudElements["BetterUI_HPBar"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                    hasBetterUIHPEnabled = true;
                }
                else
                {
                    hasBetterUIHPEnabled = false;
                }
            }

            // Compatibility check for BetterUI Food Bar
            if (betterUIFoodEnabled.Value && hudElements["BetterUI_FoodBar"].element == null)
            {
                if (hud.Find("BetterUI_FoodBar"))
                {
                    hudElements["BetterUI_FoodBar"].element = hud.Find("BetterUI_FoodBar");
                    hudElements["BetterUI_FoodBar"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                    hasBetterUIFoodEnabled = true;
                }
                else
                {
                    hasBetterUIFoodEnabled = false;
                }
            }

            // Compatibility check for BetterUI Stam Bar
            if (betterUIStamEnabled.Value && hudElements["BetterUI_StaminaBar"].element == null)
            {
                if (hud.Find("BetterUI_StaminaBar"))
                {
                    hudElements["BetterUI_StaminaBar"].element = hud.Find("BetterUI_StaminaBar");
                    hudElements["BetterUI_StaminaBar"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                    hasBetterUIStamEnabled = true;
                }
                else
                {
                    hasBetterUIStamEnabled = false;
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

        public static void getPlayerData(Transform hud)
        {
            Minimap playerMap = hud.Find("MiniMap").GetComponent<Minimap>();
            bool prevState = isMiniMapActive;

            isMiniMapActive = playerMap.m_smallRoot.activeSelf;

            // Reset timer when changing map modes.
            if (prevState != isMiniMapActive)
                hudElements["MiniMap"].timeFade = 0;
        }

        public static void setValuesBasedOnHud(bool pressedHideKey, bool pressedShowKey)
        {
            Player localPlayer = Player.m_localPlayer;

            // Store previous target alpha for timer reset.
            foreach (string name in hudElementNames)
            {
                if (hudElements[name].element != null)
                    hudElements[name].targetAlphaPrev = hudElements[name].targetAlpha;
            }

            if (pressedHideKey)
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
                    hudElements[name].resetTimers();
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

                    if (betterUIHPEnabled.Value && hasBetterUIHPEnabled)
                        hudElements["BetterUI_HPBar"].targetAlpha = 1;
                }
                else
                {
                    // Display when key pressed
                    if (pressedShowKey && showHealthOnKeyPressed.Value)
                    {
                        hudElements["healthpanel"].showHudForDuration();

                        if (betterUIHPEnabled.Value && hasBetterUIHPEnabled)
                            hudElements["BetterUI_HPBar"].showHudForDuration();
                    }
                        

                    // Display health panel when below a given percentage.
                    if (displayHealthWhenBelowPercentage.Value && localPlayer.GetHealthPercentage() <= healthPercentage.Value)
                    {
                        hudElements["healthpanel"].hudSetTargetAlpha(1);

                        if (betterUIHPEnabled.Value && hasBetterUIHPEnabled)
                            hudElements["BetterUI_HPBar"].hudSetTargetAlpha(1);
                    }                
                    else
                    {
                        hudElements["healthpanel"].hudSetTargetAlpha(0);

                        if (betterUIHPEnabled.Value && hasBetterUIHPEnabled)
                            hudElements["BetterUI_HPBar"].hudSetTargetAlpha(0);
                    }
                        
                }

                // Food Bar Display
                if (displayBetterUIFoodAlways.Value || (displayFoodBarInInventory.Value && InventoryGui.IsVisible()))
                {
                    if (betterUIFoodEnabled.Value && hasBetterUIFoodEnabled)
                        hudElements["BetterUI_FoodBar"].targetAlpha = 1;
                }
                else
                {
                    // Display when key pressed
                    if (pressedShowKey && showFoodBarOnKeyPressed.Value)
                    {
                        if (betterUIFoodEnabled.Value && hasBetterUIFoodEnabled)
                            hudElements["BetterUI_FoodBar"].showHudForDuration();
                    } 
                    else
                    {
                        if (betterUIFoodEnabled.Value && hasBetterUIFoodEnabled)
                            hudElements["BetterUI_FoodBar"].hudSetTargetAlpha(0);
                    }
                }

                // Stamina Bar Display
                if (displayStaminaBarAlways.Value || (displayStaminaBarInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["staminapanel"].targetAlpha = 1;

                    if (betterUIStamEnabled.Value && hasBetterUIStamEnabled)
                        hudElements["BetterUI_StaminaBar"].targetAlpha = 1;
                }
                else
                {
                    // Display when key pressed
                    if (pressedShowKey && showStaminaBarOnKeyPressed.Value)
                    {
                        hudElements["staminapanel"].showHudForDuration();

                        if (betterUIStamEnabled.Value && hasBetterUIStamEnabled)
                            hudElements["BetterUI_StaminaBar"].showHudForDuration();
                    }

                    // Display stamina bar when stamina is used
                    if (displayStaminaBarOnUse.Value && playerUsedStamina)
                    {
                        hudElements["staminapanel"].showHudForDuration();

                        if (betterUIStamEnabled.Value && hasBetterUIStamEnabled)
                            hudElements["BetterUI_StaminaBar"].showHudForDuration();

                        playerUsedStamina = false;
                    }

                    // Display health panel when below a given percentage.
                    if (displayStaminaBarWhenBelowPercentage.Value && localPlayer.GetStaminaPercentage() <= staminaPercentage.Value)
                    {
                        hudElements["staminapanel"].hudSetTargetAlpha(1);

                        if (betterUIStamEnabled.Value && hasBetterUIStamEnabled)
                            hudElements["BetterUI_StaminaBar"].hudSetTargetAlpha(1);
                    }
                    else
                    {
                        hudElements["staminapanel"].hudSetTargetAlpha(0);

                        if (betterUIStamEnabled.Value && hasBetterUIStamEnabled)
                            hudElements["BetterUI_StaminaBar"].hudSetTargetAlpha(0);
                    }
                }

                // Forsaken Power Display
                if (displayForsakenPowerAlways.Value || (displayPowerInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["GuardianPower"].targetAlpha = 1;
                }
                else
                {
                    // Display when key pressed
                    if (pressedShowKey && showPowerOnKeyPressed.Value)
                        hudElements["GuardianPower"].showHudForDuration();

                    // Show the forsaken power for a duration when the key is pressed.
                    if (displayPowerOnActivation.Value && (ZInput.GetButtonDown("GPower") || ZInput.GetButtonDown("JoyGPower")))
                        hudElements["GuardianPower"].showHudForDuration();

                    hudElements["GuardianPower"].hudSetTargetAlpha(0);
                }

                // HotKeyBar Display
                if (displayHotKeyBarAlways.Value || (displayHotKeyBarInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["HotKeyBar"].targetAlpha = 1;
                }
                else
                {
                    // Display when key pressed
                    if (pressedShowKey && showHotKeyBarOnKeyPressed.Value)
                        hudElements["HotKeyBar"].showHudForDuration();

                    // Display on item switch/use
                    if (displayHotKeyBarOnItemSwitch.Value && playerUsedHotBarItem)
                    {
                        hudElements["HotKeyBar"].showHudForDuration();
                        playerUsedHotBarItem = false;
                    }

                    hudElements["HotKeyBar"].hudSetTargetAlpha(0);
                }

                // Status Effects Display
                if (displayStatusEffectsAlways.Value || (displayStatusEffectsInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["StatusEffects"].targetAlpha = 1;
                }
                else
                {
                    // Display when key pressed
                    if (pressedShowKey && showStatusEffectsOnKeyPressed.Value)
                        hudElements["StatusEffects"].showHudForDuration();

                    hudElements["StatusEffects"].hudSetTargetAlpha(0);
                }

                // MiniMap Display
                if (displayMiniMapAlways.Value || (displayMiniMapInInventory.Value && InventoryGui.IsVisible()) || !isMiniMapActive)
                {
                    hudElements["MiniMap"].targetAlpha = 1;
                }
                else
                {
                    // Display when key pressed
                    if (pressedShowKey && showMiniMapOnKeyPressed.Value)
                        hudElements["MiniMap"].showHudForDuration();

                    hudElements["MiniMap"].hudSetTargetAlpha(0);
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
                        // Display when key pressed
                        if (pressedShowKey && showQuickSlotsOnKeyPressed.Value)
                            hudElements["QuickSlotsHotkeyBar"].showHudForDuration();

                        hudElements["QuickSlotsHotkeyBar"].hudSetTargetAlpha(0);
                    }
                }
            }

            // Reset timer when the target alpha changed.
            foreach (string name in hudElementNames)
            {
                hudElements[name].hudCheckDisplayTimer();

                if (hudElements[name].element == null)
                    continue;

                if (hudElements[name].targetAlphaPrev != hudElements[name].targetAlpha)
                    hudElements[name].timeFade = 0;               
            }
        }

        private static void Postfix(Hud __instance)
        {
            Player localPlayer = Player.m_localPlayer;

            if (!isEnabled.Value || !localPlayer || !__instance)
                return;

            Transform hudRoot = __instance.transform.Find("hudroot");

            getPlayerData(hudRoot);
            setCompatibility(hudRoot);
            setValuesBasedOnHud(Input.GetKeyDown(hideHudKey.Value.MainKey), Input.GetKey(showHudKey.Value.MainKey));

            foreach (string name in hudElementNames) 
            {
                if (hudElements[name].element == null)
                    continue;

                hudElements[name].hudCheckLerpDuration();

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
