using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace ImmersiveHud
{
    [BepInPlugin("manfredo52.ImmersiveHud", "Immersive Hud", "1.0.1")]
    [BepInProcess("valheim.exe")]
    public class ImmersiveHud : BaseUnityPlugin
    {
        // General
        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<int> nexusID;

        // Main Settings
        public static ConfigEntry<KeyboardShortcut> hideHudKey;
        public static ConfigEntry<bool> hudHiddenOnStart;
        public static ConfigEntry<float> hudFadeDuration;

        public static bool hudHidden;
        public static bool isMiniMapActive;

        public static float timeFade = 0;
        public static float targetAlpha;
        public static float lastSetAlpha;

        public static float timeMapFade = 0;
        public static float targetMapAlpha;
        public static float lastSetMapAlpha;  

        public static bool targetAlphaHasBeenReached;
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
            nexusID    = Config.Bind<int>("- General -", "NexusID", 790, "Nexus mod ID for updates");

            // Main Settings
            hideHudKey = Config.Bind<KeyboardShortcut>("- Main Settings -", "hideHudKey", new KeyboardShortcut(KeyCode.H), "Keyboard shortcut or mouse button to hide the hud.");
            hudHiddenOnStart = Config.Bind<bool>("- Main Settings -", "hudHiddenOnStart", false, "Hide the hud when the game is started.");
            hudFadeDuration = Config.Bind<float>("- Main Settings -", "hudFadeDuration", 1, "How quickly the hud fades in or out.");       

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

                hudHidden = hudHiddenOnStart.Value;
            }
        }
    }
}
