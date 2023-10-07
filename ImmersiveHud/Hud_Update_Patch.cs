using UnityEngine;
using HarmonyLib;
using System;
using System.Reflection;

namespace ImmersiveHud
{
    [HarmonyPatch(typeof(Hud), "Update")]
    public class Hud_Update_Patch : ImmersiveHud
    {
        private static MethodInfo _GetRightItemMethod = AccessTools.Method(typeof(Humanoid), "GetRightItem");
        private static MethodInfo _GetLeftItemMethod = AccessTools.Method(typeof(Humanoid), "GetLeftItem");
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

        public static void getPlayerData(Transform hud, Player player)
        {
            Minimap playerMap = hud.Find("MiniMap").GetComponent<Minimap>();
            bool prevState = isMiniMapActive;

            isMiniMapActive = playerMap.m_smallRoot.activeSelf;

            // Reset timer when changing map modes.
            if (prevState != isMiniMapActive)
                hudElements["MiniMap"].timeFade = 0;

            getPlayerTotalFoodValue(player);

            ItemDrop.ItemData playerItemEquippedLeft = _GetLeftItemMethod.Invoke(player, Array.Empty<object>()) as ItemDrop.ItemData;
            ItemDrop.ItemData playerItemEquippedRight = _GetRightItemMethod.Invoke(player, Array.Empty<object>()) as ItemDrop.ItemData;

            if (playerItemEquippedLeft != null || playerItemEquippedRight != null)
                playerHasItemEquipped = true;
            else
                playerHasItemEquipped = false;
        }

        public static void getPlayerTotalFoodValue(Player player)
        {
            playerTotalFoodValue = playerCurrentFoodValue = playerHungerCount = 0;

            foreach (Player.Food food in player.GetFoods())
            {
                playerTotalFoodValue += food.m_item.m_shared.m_food;
                playerCurrentFoodValue += food.m_health;

                if (food.CanEatAgain())
                    playerHungerCount++;
            }

            playerFoodPercentage = playerCurrentFoodValue / playerTotalFoodValue;
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

            // Hide hud key
            if (pressedHideKey)
            {
                hudHidden = !hudHidden;

                // Hud hidden notification
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

            // Hunger notification
            if (hungerNotification.Value && ((hungerNotificationOption.Value == hungerNotificationOptions.FoodHungerAmount && playerHungerCount >= foodHungerAmount.Value) || (hungerNotificationOption.Value == hungerNotificationOptions.FoodPercentage && playerFoodPercentage <= foodPercentage.Value)))
            {
                notificationTimer += Time.deltaTime;

                if ((int) notificationTimer % hungerNotificationInterval.Value == 0)
                {
                    switch (hungerNotificationType.Value)
                    {
                        case notificationTypes.SmallTopLeft:
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, hungerNotificationText.Value);                         
                            break;
                        case notificationTypes.LargeCenter:
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, hungerNotificationText.Value);
                            break;
                        default:
                            break;
                    }

                    notificationTimer = 1;
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
            else if (pressedShowKey)
            {
                // Health
                if (showHealthOnKeyPressed.Value)
                {
                    hudElements["healthpanel"].showHudForDuration();
                    hudElements["BetterUI_HPBar"].showHudForDuration();
                }

                // Food Bar
                if (showFoodBarOnKeyPressed.Value)
                    hudElements["BetterUI_FoodBar"].showHudForDuration();

                // Stamina
                if (showStaminaBarOnKeyPressed.Value)
                {
                    hudElements["staminapanel"].showHudForDuration();
                    hudElements["BetterUI_StaminaBar"].showHudForDuration();
                }

                // Forsaken Power
                if (showPowerOnKeyPressed.Value)
                    hudElements["GuardianPower"].showHudForDuration();

                // Hot Key Bar
                if (showHotKeyBarOnKeyPressed.Value)
                    hudElements["HotKeyBar"].showHudForDuration();

                // Status Effects
                if (showStatusEffectsOnKeyPressed.Value)
                    hudElements["StatusEffects"].showHudForDuration();

                // Mini Map
                if (showMiniMapOnKeyPressed.Value)
                    hudElements["MiniMap"].showHudForDuration();

                // Compass
                if (showCompassOnKeyPressed.Value)
                    hudElements["Compass"].showHudForDuration();

                // Day and Time
                if (showTimeOnKeyPressed.Value && oryxenTimeEnabled.Value)
                    hudElements["DayTimePanel"].showHudForDuration();

                // Quick Slots
                if (showQuickSlotsOnKeyPressed.Value)
                    hudElements["QuickSlotsHotkeyBar"].showHudForDuration();
            }
            else
            {
                // Health Display
                if (displayHealthAlways.Value || (displayHealthInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["healthpanel"].targetAlpha = 1;
                    hudElements["BetterUI_HPBar"].targetAlpha = 1;
                        
                    if (!displayHealthInInventory.Value && InventoryGui.IsVisible())
                    {
                        hudElements["healthpanel"].hudSetTargetAlpha(0);
                        hudElements["BetterUI_HPBar"].hudSetTargetAlpha(0);
                    }                 
                }
                else
                {
                    // Display health panel when eating food
                    if (displayHealthWhenEating.Value && playerAteFood)
                    {
                        hudElements["healthpanel"].showHudForDuration();
                        hudElements["BetterUI_HPBar"].showHudForDuration();
                    }

                    // Display health panel when below a given percentage.
                    if (displayHealthWhenBelow.Value && localPlayer.GetHealthPercentage() <= healthPercentage.Value)
                    {
                        hudElements["healthpanel"].hudSetTargetAlpha(1);
                        hudElements["BetterUI_HPBar"].hudSetTargetAlpha(1);
                    }
                    else if (displayHealthWhenFoodBelow.Value && playerFoodPercentage <= foodPercentage.Value)
                    {
                        hudElements["healthpanel"].hudSetTargetAlpha(1);
                        hudElements["BetterUI_HPBar"].hudSetTargetAlpha(1);
                    }
                    else if (displayHealthWhenHungry.Value && playerHungerCount >= foodHungerAmount.Value)
                    {
                        hudElements["healthpanel"].hudSetTargetAlpha(1);
                        hudElements["BetterUI_HPBar"].hudSetTargetAlpha(1);
                    }
                    else
                    {
                        hudElements["healthpanel"].hudSetTargetAlpha(0);
                        hudElements["BetterUI_HPBar"].hudSetTargetAlpha(0);
                    }                        
                }

                // Food Bar Display
                if (displayBetterUIFoodAlways.Value || (displayFoodBarInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["BetterUI_FoodBar"].targetAlpha = 1;

                    if (!displayFoodBarInInventory.Value && InventoryGui.IsVisible())
                        hudElements["BetterUI_FoodBar"].hudSetTargetAlpha(0);
                }
                else
                {
                    if (betterUIFoodEnabled.Value && hudElements["BetterUI_FoodBar"].doesExist)
                    {
                        // Display food bar when eating food
                        if (displayFoodBarWhenEating.Value && playerAteFood)
                            hudElements["BetterUI_FoodBar"].showHudForDuration();

                        // Display food bar when below a given percentage.
                        if (displayFoodBarWhenBelow.Value && playerFoodPercentage <= foodPercentage.Value)
                        {
                            hudElements["BetterUI_FoodBar"].hudSetTargetAlpha(1);
                        }
                        else if (displayFoodBarWhenHungry.Value && playerHungerCount >= foodHungerAmount.Value)
                        {
                            hudElements["BetterUI_FoodBar"].hudSetTargetAlpha(1);
                        }
                        else
                        {
                            hudElements["BetterUI_FoodBar"].hudSetTargetAlpha(0);
                        }
                    }
                }

                // Stamina Bar Display
                if (displayStaminaBarAlways.Value || (displayStaminaBarInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["staminapanel"].targetAlpha = 1;
                    hudElements["BetterUI_StaminaBar"].targetAlpha = 1;

                    if (!displayStaminaBarInInventory.Value && InventoryGui.IsVisible())
                    { 
                        hudElements["staminapanel"].hudSetTargetAlpha(0);
                        hudElements["BetterUI_StaminaBar"].hudSetTargetAlpha(0);
                    }
                }
                else
                {
                    // Display stamina bar when stamina is used
                    if (displayStaminaBarOnUse.Value && playerUsedStamina)
                    {
                        hudElements["staminapanel"].showHudForDuration();
                        hudElements["BetterUI_StaminaBar"].showHudForDuration();
                    }

                    // Display stamina bar when eating food
                    if (displayStaminaBarWhenEating.Value && playerAteFood)
                    {
                        hudElements["staminapanel"].showHudForDuration();
                        hudElements["BetterUI_StaminaBar"].showHudForDuration();
                    }

                    // Display stamina bar when below a given percentage.
                    if (displayStaminaBarWhenBelow.Value && localPlayer.GetStaminaPercentage() <= staminaPercentage.Value)
                    {
                        hudElements["staminapanel"].hudSetTargetAlpha(1);
                        hudElements["BetterUI_StaminaBar"].hudSetTargetAlpha(1);
                    }
                    else if (displayStaminaBarWhenFoodBelow.Value && playerFoodPercentage <= foodPercentage.Value)
                    {
                        hudElements["staminapanel"].hudSetTargetAlpha(1);
                        hudElements["BetterUI_StaminaBar"].hudSetTargetAlpha(1);
                    }
                    else if (displayStaminaBarWhenHungry.Value && playerHungerCount >= foodHungerAmount.Value)
                    {
                        hudElements["staminapanel"].hudSetTargetAlpha(1);
                        hudElements["BetterUI_StaminaBar"].hudSetTargetAlpha(1);
                    }
                    else
                    {
                        hudElements["staminapanel"].hudSetTargetAlpha(0);
                        hudElements["BetterUI_StaminaBar"].hudSetTargetAlpha(0);
                    }
                }

                // Forsaken Power Display
                if (displayForsakenPowerAlways.Value || (displayPowerInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["GuardianPower"].targetAlpha = 1;

                    if (!displayPowerInInventory.Value && InventoryGui.IsVisible())
                        hudElements["GuardianPower"].hudSetTargetAlpha(0);
                }
                else
                {
                    // Show the forsaken power for a duration when the key is pressed.
                    if (displayPowerOnActivation.Value && (ZInput.GetButtonDown("GPower") || ZInput.GetButtonDown("JoyGPower")))
                        hudElements["GuardianPower"].showHudForDuration();

                    hudElements["GuardianPower"].hudSetTargetAlpha(0);
                }

                // Hot Key Bar Display
                if (displayHotKeyBarAlways.Value || (displayHotKeyBarInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["HotKeyBar"].targetAlpha = 1;

                    if (!displayHotKeyBarInInventory.Value && InventoryGui.IsVisible())
                        hudElements["HotKeyBar"].hudSetTargetAlpha(0);
                }
                else
                {
                    // Display on item switch/use
                    if (displayHotKeyBarOnItemSwitch.Value && playerUsedHotBarItem)
                        hudElements["HotKeyBar"].showHudForDuration();
                    else if (displayHotKeyBarWhenItemEquipped.Value && playerHasItemEquipped)
                        hudElements["HotKeyBar"].hudSetTargetAlpha(1);
                    else
                        hudElements["HotKeyBar"].hudSetTargetAlpha(0);
                }

                // Status Effects Display
                if (displayStatusEffectsAlways.Value || (displayStatusEffectsInInventory.Value && InventoryGui.IsVisible()))
                {
                    hudElements["StatusEffects"].targetAlpha = 1;

                    if (!displayStatusEffectsInInventory.Value && InventoryGui.IsVisible())
                        hudElements["StatusEffects"].hudSetTargetAlpha(0);
                }
                else
                {
                    hudElements["StatusEffects"].hudSetTargetAlpha(0);
                }

                // Mini Map Display
                if (displayMiniMapAlways.Value || (displayMiniMapInInventory.Value && InventoryGui.IsVisible()) || !isMiniMapActive)
                {
                    hudElements["MiniMap"].targetAlpha = 1;

                    if (!displayMiniMapInInventory.Value && InventoryGui.IsVisible())
                        hudElements["MiniMap"].hudSetTargetAlpha(0);
                }
                else
                {
                    hudElements["MiniMap"].hudSetTargetAlpha(0);
                }

                // Compass Display
                if (aedenCompassEnabled.Value && hudElements["Compass"].doesExist)
                {
                    if (displayCompassAlways.Value || (displayCompassInInventory.Value && InventoryGui.IsVisible()))
                    {
                        hudElements["Compass"].targetAlpha = 1;

                        if (!displayCompassInInventory.Value && InventoryGui.IsVisible())
                            hudElements["Compass"].hudSetTargetAlpha(0);
                    }
                    else
                    {
                        hudElements["Compass"].hudSetTargetAlpha(0);
                    }
                }

                // Day and Time Display
                if (oryxenTimeEnabled.Value && hudElements["DayTimePanel"].doesExist)
                {
                    if (displayTimeAlways.Value || (displayTimeInInventory.Value && InventoryGui.IsVisible()))
                    {
                        hudElements["DayTimePanel"].targetAlpha = 1;

                        if (!displayTimeInInventory.Value && InventoryGui.IsVisible())
                            hudElements["DayTimePanel"].hudSetTargetAlpha(0);
                    }
                    else
                    {
                        hudElements["DayTimePanel"].hudSetTargetAlpha(0);
                    }
                }

                // QuickSlots Display
                if (quickSlotsEnabled.Value && hudElements["QuickSlotsHotkeyBar"].doesExist)
                {
                    if (displayQuickSlotsAlways.Value || (displayQuickSlotsInInventory.Value && InventoryGui.IsVisible()))
                    {
                        hudElements["QuickSlotsHotkeyBar"].targetAlpha = 1;

                        if (!displayQuickSlotsInInventory.Value && InventoryGui.IsVisible())
                            hudElements["QuickSlotsHotkeyBar"].hudSetTargetAlpha(0);
                    }
                    else
                    {
                        hudElements["QuickSlotsHotkeyBar"].hudSetTargetAlpha(0);
                    }
                }
            }

            // Reset timer when the target alpha changed.
            foreach (string name in hudElementNames)
            {
                hudElements[name].hudCheckDisplayTimer();

                if (!hudElements[name].doesExist || hudElements[name].element == null)
                    continue;

                if (hudElements[name].targetAlphaPrev != hudElements[name].targetAlpha)
                    hudElements[name].timeFade = 0;               
            }

            playerUsedHotBarItem = false;
            playerUsedStamina = false;
            playerAteFood = false;
        }

        private static void Postfix(Hud __instance)
        {
            Player localPlayer = Player.m_localPlayer;

            if (!isEnabled.Value || !localPlayer || !__instance)
                return;

            Transform hudRoot = __instance.transform.Find("hudroot");

            getPlayerData(hudRoot, localPlayer);
            setCompatibility(hudRoot);
            setValuesBasedOnHud(Input.GetKeyDown(hideHudKey.Value.MainKey), Input.GetKey(showHudKey.Value.MainKey));

            // Set vanilla stamina bar to always be active so hiding and showing works properly.
            // FIX: vanilla stam bar when eating food.
            if (!hudElements["BetterUI_StaminaBar"].doesExist)
            {
                hudElements["staminapanel"].element.gameObject.SetActive(true);
                __instance.m_staminaAnimator.SetBool("Visible", true);
            }

            foreach (string name in hudElementNames)
            {
                if (!hudElements[name].doesExist || hudElements[name].element == null)
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
