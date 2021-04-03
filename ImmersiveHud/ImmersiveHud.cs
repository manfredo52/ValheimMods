using System.IO;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace ImmersiveHud
{
    [BepInPlugin("manfredo52.ImmersiveHud", "Immersive Hud", "1.0.4")]
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

        // Hud Element Settings
        public static ConfigEntry<bool> displayHealthAlways;
        public static ConfigEntry<bool> displayHotbarAlways;
        public static ConfigEntry<bool> displayForsakenPowerAlways;
        public static ConfigEntry<bool> displayStatusEffectsAlways;
        public static ConfigEntry<bool> displayMiniMapAlways;
        public static ConfigEntry<bool> displayQuickSlotsAlways;

        // Hud Element - All
        public static bool hudHidden;

        // Hud Element - Health

        // Hud Element - Forsaken

        // Hud Element - HotKeyBar

        // Hud Element - MiniMap
        public static bool isMiniMapActive;

        // Hud Element - QuickSlots

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
        public static string[] hudElementNames =
        {
            "healthpanel",
            "GuardianPower",
            "HotKeyBar",
            "StatusEffects",
            "MiniMap"
        };

        public static string[] hudElementsOther =
        {
            "QuickSlotsHotkeyBar"
        };

        public class HudElement
        {
            public Transform element;
            public string elementName;
            public bool targetAlphaReached;
            public float targetAlpha;
            public float lastSetAlpha;
            public float timeFade = 0;

            public HudElement(string name)
            {
                elementName = name;
            }
        }

        public static Dictionary<string, HudElement> hudElements = new Dictionary<string, HudElement>();

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

            // Display Elements Settings
            displayHealthAlways             = Config.Bind<bool>("Display Settings", "displayHealthAlways", false, "Always display the health panel.");
            displayHotbarAlways             = Config.Bind<bool>("Display Settings", "displayHotbarAlways", false, "Always display the hotbar.");
            displayForsakenPowerAlways      = Config.Bind<bool>("Display Settings", "displayForsakenPowerAlways", false, "Always display the forsaken power.");
            displayStatusEffectsAlways      = Config.Bind<bool>("Display Settings", "displayStatusEffectsAlways", false, "Always display status effects.");
            displayMiniMapAlways            = Config.Bind<bool>("Display Settings", "displayMiniMapAlways", false, "Always display the minimap.");
            displayQuickSlotsAlways         = Config.Bind<bool>("Display Settings", "displayQuickSlotsAlways", false, "Always display the quick slots (Requires quick slots mod).");

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
                foreach (string name in hudElementNames)
                {
                    hudElements.Add(name, new HudElement(name));
                    hudElements[name].element = hudRoot.Find(name);
                    hudElements[name].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                }  

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
