using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace ImmersiveHud
{
    [BepInPlugin("manfredo52.ImmersiveHud", "Immersive Hud", "1.0.3")]
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

        // Crosshair Settings
        public static ConfigEntry<bool> useCustomCrosshair;
        public static ConfigEntry<bool> useCustomBowCrosshair;
        public static ConfigEntry<Color> crosshairColor;
        public static ConfigEntry<Color> crosshairBowDrawColor;
        public static ConfigEntry<bool> displayCrosshairAlways;
        public static ConfigEntry<bool> displayCrosshairOnActivation;
        public static ConfigEntry<bool> displayCrosshairOnEquipped;
        public static ConfigEntry<bool> displayCrosshairOnBowEquipped;
        public static ConfigEntry<bool> displayBowDrawCrosshair;

        public static float targetCrosshairAlpha;
        public static float targetBowDrawCrosshairAlpha;
        public static Image playerCrosshair;
        public static Image playerBowCrosshair;
        public static Sprite crosshairSprite;
        public static Sprite crosshairBowSprite;
        public static Sprite crosshairSpriteOriginal;
        public static Sprite crosshairBowSpriteOriginal;

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

        // Character States
        public static bool characterEquippedItem;
        public static bool characterEquippedBow;
        public static bool isLookingAtActivatable;

        // Compatibility
        public static bool hasQuickSlotsEnabled;
        public static bool hasCanvasQuickSlots;

        // Other
        public static float fadeDuration = 0.5f;

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

        public static Sprite LoadCrosshairTexture(string filename)
        {
            string filePath = Path.Combine(Paths.PluginPath, filename);

            if (File.Exists(filePath))
            {
                Texture2D texture = new Texture2D(0, 0);
                ImageConversion.LoadImage(texture, File.ReadAllBytes(filePath));
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            } 
            else
            {
                Debug.Log("ImmersiveHud: Error. Couldn't load provided crosshair image. Check if the folder ImmersiveHud in the plugins folder has a file named crosshair.png or bowcrosshair.png");
                return null;
            }
        }

        private void Awake()
        {
            // General
            isEnabled   = Config.Bind<bool>("- General -", "Enable Mod", true, "Enable or disable the mod");
            nexusID     = Config.Bind<int>("- General -", "NexusID", 790, "Nexus mod ID for updates");

            // Main Settings
            hideHudKey          = Config.Bind<KeyboardShortcut>("- Main Settings -", "hideHudKey", new KeyboardShortcut(KeyCode.H), "Keyboard shortcut or mouse button to hide the hud.");
            hudHiddenOnStart    = Config.Bind<bool>("- Main Settings -", "hudHiddenOnStart", false, "Hide the hud when the game is started.");
            hudFadeDuration     = Config.Bind<float>("- Main Settings -", "hudFadeDuration", 1, "How quickly the hud fades in or out.");

            // Crosshair Settings
            useCustomCrosshair              = Config.Bind<bool>("Crosshair Settings", "useCustomCrosshair", false, new ConfigDescription("Enable or disable the new crosshair.", null, new ConfigurationManagerAttributes { Order = 1 }));
            useCustomBowCrosshair           = Config.Bind<bool>("Crosshair Settings", "useCustomBowCrosshair", false, new ConfigDescription("Enable or disable the new crosshair for the bow draw.", null, new ConfigurationManagerAttributes { Order = 2 }));
            crosshairColor                  = Config.Bind<Color>("Crosshair Settings", "crosshairColor", Color.white, "Color and transparency of the crosshair.");
            crosshairBowDrawColor           = Config.Bind<Color>("Crosshair Settings", "crosshairBowDrawColor", Color.yellow, "Color and transparency of the bow draw crosshair.");
            displayCrosshairAlways          = Config.Bind<bool>("Crosshair Settings", "displayCrosshairAlways", true, "Always display the crosshair, overriding other display crosshair settings.");
            displayBowDrawCrosshair         = Config.Bind<bool>("Crosshair Settings", "displayBowDrawCrosshair", true, "Display the bow draw crosshair.");     
            displayCrosshairOnActivation    = Config.Bind<bool>("Crosshair Settings", "displayCrosshairOnActivation", false, "Display crosshair when hovering over an activatable object.");
            displayCrosshairOnEquipped      = Config.Bind<bool>("Crosshair Settings", "displayCrosshairOnEquipped", false, "Display crosshair when an item is equipped in either hand.");
            displayCrosshairOnBowEquipped   = Config.Bind<bool>("Crosshair Settings", "displayCrosshairOnBowEquipped", false, "Display crosshair when the bow is equipped.");

            // Hud Elements Settings

            // Display Scenario Settings

            // Crosshair Sprites
            crosshairSprite                 = LoadCrosshairTexture("ImmersiveHud/crosshair.png");
            crosshairBowSprite              = LoadCrosshairTexture("ImmersiveHud/bowcrosshair.png");

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

                playerCrosshair = __instance.m_crosshair;
                playerBowCrosshair = __instance.m_crosshairBow;

                crosshairSpriteOriginal = playerCrosshair.sprite;
                crosshairBowSpriteOriginal = playerBowCrosshair.sprite;

                if (useCustomCrosshair.Value && crosshairSprite != null)
                    playerCrosshair.sprite = crosshairSprite;

                if (useCustomBowCrosshair.Value && crosshairBowSprite != null)
                    playerBowCrosshair.sprite = crosshairBowSprite;
            }
        }
    }
}
