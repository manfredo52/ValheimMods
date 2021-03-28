using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace ImmersiveHud
{
    [BepInPlugin("manfredo52.ImmersiveHud", "Immersive Hud", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class ImmersiveHud : BaseUnityPlugin
    {
        // General
        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<int> nexusID;

        // Main Settings
        public static ConfigEntry<KeyboardShortcut> hideHudKey;
        public static ConfigEntry<float> hudFadeDuration;

        public static bool hudHidden;
        public static float timeFade = 0;
        public static float targetAlpha;
        public static float lastSetAlpha;
        public static bool targetAlphaHasBeenReached;

        public static bool isMiniMapActive;
        public static float targetMapAlpha;
        public static float lastSetMapAlpha;
        public static float timeMapFade = 0;
        public static bool targetMapAlphaHasBeenReached;

        // Compatibility
        public static bool hasQuickSlotsEnabled;
        public static bool hasCanvasQuickSlots;

        // List of hud elements
        // Removing "MiniMap" from this will result in a null reference error
        public static string[] hudElements =
        {
            "HotKeyBar",
            "healthpanel",
            "StatusEffects",
            "GuardianPower",
            "MiniMap"
        };

        public static string[] hudElementsOther =
        {
            "QuickSlotsHotkeyBar"
        };

        private void Awake()
        {
            // General
            isEnabled  = Config.Bind<bool>("- General -", "Enable Mod", true, "Enable or disable the mod");
            nexusID    = Config.Bind<int>("- General -", "NexusID", 0, "Nexus mod ID for updates");

            // Main Settings
            hideHudKey = Config.Bind<KeyboardShortcut>("- Main Settings -", "hideHudKey", new KeyboardShortcut(KeyCode.H), "Keyboard shortcut or mouse button to hide the hud.");
            hudFadeDuration = Config.Bind<float>("- Main Settings -", "hudFadeDuration", 1, "hud fade duration.");

            DoPatching();
        }

        public static void DoPatching() => new Harmony("ImmersiveHud").PatchAll();

        [HarmonyPatch(typeof(Hud), "Awake")]
        public class Hud_Awake_Patch
        {
            // Can't seem to check for existence of quick slots object on awake for some reason.
            private static void Postfix(Hud __instance)
            {
                if (!isEnabled.Value)
                    return;

                Transform hudRoot = __instance.transform.Find("hudroot");

                // Add CanvasGroup to each hud element on awake.
                foreach (string hudElement in hudElements)
                    hudRoot.Find(hudElement).GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
            }
        }

        [HarmonyPatch(typeof(Hud), "Update")]
        public class Hud_Update_Patch
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
}
