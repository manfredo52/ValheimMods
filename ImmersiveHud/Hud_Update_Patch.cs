using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
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
            if (pressedKey)
            {
                hudHidden = !hudHidden;

                foreach (string name in hudElementNames)
                    hudElements[name].timeFade = 0;
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
                if (displayHealthAlways.Value)
                {
                    hudElements["healthpanel"].targetAlpha = 1;
                }
                else
                {
                    hudElements["healthpanel"].targetAlpha = 0;
                }

                // Forsaken Power Display
                if (displayForsakenPowerAlways.Value)
                {
                    hudElements["GuardianPower"].targetAlpha = 1;
                }
                else
                {
                    hudElements["GuardianPower"].targetAlpha = 0;
                }

                // Hot Key Bar Display
                if (displayHotbarAlways.Value)
                {
                    hudElements["HotKeyBar"].targetAlpha = 1;
                }
                else
                {
                    hudElements["HotKeyBar"].targetAlpha = 0;
                }

                // Status Effects Display
                if (displayStatusEffectsAlways.Value)
                {
                    hudElements["StatusEffects"].targetAlpha = 1;
                }
                else
                {
                    hudElements["StatusEffects"].targetAlpha = 0;
                }

                // MiniMap Display
                if (displayMiniMapAlways.Value)
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
                    if (displayQuickSlotsAlways.Value)
                    {
                        hudElements["QuickSlotsHotkeyBar"].targetAlpha = 1;
                    }
                    else
                    {
                        hudElements["QuickSlotsHotkeyBar"].targetAlpha = 0;
                    }
                }
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
            }
        }
    }
}
