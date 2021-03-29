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
            if (hud.Find("QuickSlotsHotkeyBar"))
            {
                hasQuickSlotsEnabled = true;

                if (!hasCanvasQuickSlots)
                {
                    hud.Find("QuickSlotsHotkeyBar").GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                    hasCanvasQuickSlots = true;
                }
            }
            else
            {
                hasQuickSlotsEnabled = false;
            }
        }

        public static void updateHudElementTransparency(string hudElement, float targetAlpha, float time)
        {
            if (hudElement == "QuickSlotsHotkeyBar" && !hasQuickSlotsEnabled)
                return;

            Transform hudRoot = Hud.instance.transform.Find("hudroot");
            float lerpedAlpha;

            if (hudElement == "MiniMap")
            {
                lerpedAlpha = Mathf.Lerp(lastSetMapAlpha, targetAlpha, time / hudFadeDuration.Value);
                hudRoot.Find(hudElement).GetComponent<Minimap>().m_mapImageSmall.CrossFadeAlpha(targetAlpha, time, false);
                lastSetMapAlpha = hudRoot.Find(hudElement).GetComponent<CanvasGroup>().alpha;
            }
            else
            {
                lerpedAlpha = Mathf.Lerp(lastSetAlpha, targetAlpha, time / hudFadeDuration.Value);
                lastSetAlpha = hudRoot.Find(hudElement).GetComponent<CanvasGroup>().alpha;
            }

            hudRoot.Find(hudElement).GetComponent<CanvasGroup>().alpha = lerpedAlpha;
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
                timeMapFade = 0;
        }

        public static void setValuesBasedOnHud(bool pressedKey)
        {
            if (pressedKey)
            {
                hudHidden = !hudHidden;
                timeFade = 0;
                timeMapFade = 0;
            }

            if (hudHidden)
            {
                targetAlpha = 0;

                if (isMiniMapActive)
                    targetMapAlpha = 0;
                else
                    targetMapAlpha = 1;
            }
            else
            {
                targetAlpha = 1;
                targetMapAlpha = 1;
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

            // Hud elements
            targetAlphaHasBeenReached = checkHudLerpDuration(timeFade);

            if (!targetAlphaHasBeenReached)
            {
                timeFade += Time.deltaTime;

                foreach (string hudElement in hudElements)
                    updateHudElementTransparency(hudElement, targetAlpha, timeFade);

                updateHudElementTransparency("QuickSlotsHotkeyBar", targetAlpha, timeFade);
            }

            // Minimap
            targetMapAlphaHasBeenReached = checkHudLerpDuration(timeMapFade);

            if (!targetMapAlphaHasBeenReached)
            {
                timeMapFade += Time.deltaTime;
                updateHudElementTransparency("MiniMap", targetMapAlpha, timeMapFade);
            }
        }
    }
}
