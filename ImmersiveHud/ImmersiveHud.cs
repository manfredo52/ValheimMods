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
    [BepInPlugin("manfredo52.ImmersiveHud", "Immersive Hud", "1.0.9")]
    [BepInProcess("valheim.exe")]
    public class ImmersiveHud : BaseUnityPlugin
    {
        // General
        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<int> nexusID;

        // Main Settings
        public static ConfigEntry<KeyboardShortcut> hideHudKey;
        public static ConfigEntry<KeyboardShortcut> showHudKey;
        public static ConfigEntry<bool> hudHiddenNotification;
        public static ConfigEntry<bool> hudHiddenOnStart;
        public static ConfigEntry<float> hudFadeDuration;
        public static ConfigEntry<float> showHudDuration;

        // Crosshair Settings
        public static ConfigEntry<bool> useCustomCrosshair;
        public static ConfigEntry<bool> useCustomBowCrosshair;
        public static ConfigEntry<Color> crosshairColor;
        public static ConfigEntry<Color> crosshairBowDrawColor;
        public static ConfigEntry<bool> displayCrosshairAlways;
        public static ConfigEntry<bool> displayCrosshairWhenBuilding;
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
        public static ConfigEntry<bool> displayHotKeyBarAlways;
        public static ConfigEntry<bool> displayForsakenPowerAlways;
        public static ConfigEntry<bool> displayStatusEffectsAlways;
        public static ConfigEntry<bool> displayMiniMapAlways;
        public static ConfigEntry<bool> displayQuickSlotsAlways;

        // Compatibility Settings
        public static ConfigEntry<bool> quickSlotsEnabled;

        public static bool hasQuickSlotsEnabled;

        // Hud Element - All
        public static bool hudHidden;

        // Hud Element - Health      
        public static ConfigEntry<bool> displayHealthInInventory;
        //public static ConfigEntry<bool> displayHealthDuringRegen;
        //public static ConfigEntry<bool> displayHealthWhenDamaged;
        //public static ConfigEntry<bool> displayHealthWhenFoodConsumed;
        // public static ConfigEntry<bool> displayHealthWhenHungry; //"You could eat another bite"
        public static ConfigEntry<bool> displayHealthWhenBelowPercentage;
        public static ConfigEntry<float> healthPercentage;
        public static ConfigEntry<bool> showHealthOnKeyPressed;

        // Hud Element - Forsaken Power
        public static ConfigEntry<bool> displayPowerInInventory;
        public static ConfigEntry<bool> displayPowerOnActivation;
        public static ConfigEntry<bool> displayPowerWhenTimeChanges;
        public static ConfigEntry<bool> displayPowerOnReady;
        public static ConfigEntry<float> powerTimeChangeInterval;
        public static ConfigEntry<bool> showPowerOnKeyPressed;

        // Hud Element - HotKeyBar
        public static ConfigEntry<bool> displayHotKeyBarInInventory;
        public static ConfigEntry<bool> displayHotKeyBarOnItemSwitch;
        public static ConfigEntry<bool> showHotKeyBarOnKeyPressed;

        // Hud Element - Status Effects    
        public static ConfigEntry<bool> displayStatusEffectsInInventory;
        public static ConfigEntry<bool> showStatusEffectsOnKeyPressed;

        // Hud Element - MiniMap      
        public static ConfigEntry<bool> displayMiniMapInInventory;
        public static ConfigEntry<bool> showMiniMapOnKeyPressed;
        public static bool isMiniMapActive;

        // Hud Element - QuickSlots
        public static ConfigEntry<bool> displayQuickSlotsInInventory;
        //public static ConfigEntry<bool> displayQuickSlotsOnItemSwitch;
        public static ConfigEntry<bool> showQuickSlotsOnKeyPressed;

        // Character States
        public static bool characterEquippedItem;
        public static bool characterEquippedBow;
        public static bool isLookingAtActivatable;
        public static bool playerUsedHotBarItem;
        public static bool playerUsedQuickSlotsItem;

        // Other
        public static float fadeDuration = 0.5f;

        // List of hud elements
        public static string[] hudElementNames =
        {
            "healthpanel",
            "GuardianPower",
            "HotKeyBar",
            "StatusEffects",
            "MiniMap",
            "QuickSlotsHotkeyBar"
        };

        public class HudElement
        {
            public Transform element;
            public string elementName;
            public bool targetAlphaReached;
            public float targetAlphaPrev;
            public float targetAlpha;
            public float lastSetAlpha;
            public float timeFade = 0;
            public float timeDisplay = 0;
            public bool isDisplaying;

            public HudElement(string name)
            {
                element = null;
                elementName = name;
            }

            public void hudSetTargetAlpha(float alpha)
            {
                if (!isDisplaying)
                    targetAlpha = alpha;
            }

            public void hudCheckDisplayTimer()
            {
                if (timeDisplay >= showHudDuration.Value && isDisplaying)
                {
                    targetAlpha = 0;
                    isDisplaying = false;
                }                         
            }

            public  void hudCheckLerpDuration()
            {
                if (timeFade >= hudFadeDuration.Value)
                    targetAlphaReached = true;
                else
                    targetAlphaReached = false;
            }

            public void showHudForDuration()
            {
                targetAlpha = 1;
                timeDisplay = 0;
                isDisplaying = true;
            }

            public void resetTimers()
            {
                timeFade = 0;
                timeDisplay = 0;
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
            hideHudKey              = Config.Bind<KeyboardShortcut>("- Main Settings -", "hideHudKey", new KeyboardShortcut(KeyCode.H), "Keyboard shortcut or mouse button to hide the hud.");
            hudHiddenOnStart        = Config.Bind<bool>("- Main Settings -", "hudHiddenOnStart", false, "Hide the hud when the game is started.");
            hudHiddenNotification   = Config.Bind<bool>("- Main Settings -", "hudHiddenNotification", false, "Enable notifications in the top left corner for hiding the hud.");
            hudFadeDuration         = Config.Bind<float>("- Main Settings -", "hudFadeDuration", 1f, "How quickly the hud fades in or out.");
            showHudDuration         = Config.Bind<float>("- Main Settings -", "showHudDuration", 1f, "How long a hud element should stay up for when it is activated for certain conditions.");
            showHudKey              = Config.Bind<KeyboardShortcut>("- Main Settings -", "showHudKey", new KeyboardShortcut(KeyCode.G), "Keyboard shortcut or mouse button to display the hud for a duration.");

            // Compatibility
            quickSlotsEnabled   = Config.Bind<bool>("- Mod Compatibility -", "quickSlotsEnabled", false, "Enable compatibility for quickslots mod.");

            // Crosshair Settings
            useCustomCrosshair              = Config.Bind<bool>("- Settings: Crosshair -", "useCustomCrosshair", false, new ConfigDescription("Enable or disable the new crosshair.", null, new ConfigurationManagerAttributes { Order = 1 }));
            useCustomBowCrosshair           = Config.Bind<bool>("- Settings: Crosshair -", "useCustomBowCrosshair", false, new ConfigDescription("Enable or disable the new crosshair for the bow draw.", null, new ConfigurationManagerAttributes { Order = 2 }));
            crosshairColor                  = Config.Bind<Color>("- Settings: Crosshair -", "crosshairColor", Color.white, "Color and transparency of the crosshair.");
            crosshairBowDrawColor           = Config.Bind<Color>("- Settings: Crosshair -", "crosshairBowDrawColor", Color.yellow, "Color and transparency of the bow draw crosshair.");
            displayCrosshairAlways          = Config.Bind<bool>("- Settings: Crosshair -", "displayCrosshairAlways", true, "Always display the crosshair, overriding other display crosshair settings.");
            displayBowDrawCrosshair         = Config.Bind<bool>("- Settings: Crosshair -", "displayBowDrawCrosshair", true, "Display the bow draw crosshair.");
            displayCrosshairWhenBuilding    = Config.Bind<bool>("- Settings: Crosshair -", "displayCrosshairWhenBuilding", true, "Display the crosshair when you have the hammer equipped.");
            displayCrosshairOnActivation    = Config.Bind<bool>("- Settings: Crosshair -", "displayCrosshairOnActivation", false, "Display crosshair when hovering over an activatable object.");
            displayCrosshairOnEquipped      = Config.Bind<bool>("- Settings: Crosshair -", "displayCrosshairOnEquipped", false, "Display crosshair when an item is equipped in either hand.");
            displayCrosshairOnBowEquipped   = Config.Bind<bool>("- Settings: Crosshair -", "displayCrosshairOnBowEquipped", false, "Display crosshair when the bow is equipped.");

            // Display Elements Settings
            displayHealthAlways         = Config.Bind<bool>("- Settings: Display -", "displayHealthAlways", false, "Always display the health panel.");
            displayHotKeyBarAlways      = Config.Bind<bool>("- Settings: Display -", "displayHotbarAlways", false, "Always display the hotbar.");
            displayForsakenPowerAlways  = Config.Bind<bool>("- Settings: Display -", "displayForsakenPowerAlways", false, "Always display the forsaken power.");
            displayStatusEffectsAlways  = Config.Bind<bool>("- Settings: Display -", "displayStatusEffectsAlways", false, "Always display status effects.");
            displayMiniMapAlways        = Config.Bind<bool>("- Settings: Display -", "displayMiniMapAlways", false, "Always display the minimap.");
            displayQuickSlotsAlways     = Config.Bind<bool>("- Settings: Display -", "displayQuickSlotsAlways", false, "Always display the quick slots (Requires quick slots mod).");

            // Display Scenario Settings - Health          
            displayHealthInInventory            = Config.Bind<bool>("Display - Health", "displayHealthInInventory", true, "Display your health when in the inventory.");
            //displayHealthDuringRegen          = Config.Bind<bool>("Display - Health", "displayDuringRegen", false, "During health regen, the health panel will display.");
            //displayHealthWhenDamaged          = Config.Bind<bool>("Display - Health", "displayWhenDamaged", false, "Display the health panel when damaged.");
            //displayHealthWhenFoodConsumed     = Config.Bind<bool>("Display - Health", "displayWhenFoodConsumed", false, "Display the health panel when you consume food.");
            displayHealthWhenBelowPercentage    = Config.Bind<bool>("Display - Health", "displayWhenBelowPercentage", false, "When you are at or below a certain health percentage, display the health panel.");
            healthPercentage                    = Config.Bind<float>("Display - Health", "healthPercentage", 0.75f, new ConfigDescription("How quickly the bow zooms in.", new AcceptableValueRange<float>(0f, 1f)));
            showHealthOnKeyPressed              = Config.Bind<bool>("Display - Health", "showHealthOnKeyPressed", true, "Show the health panel when the display key is pressed.");

            // Display Scenario Settings - Forsaken Power           
            displayPowerInInventory         = Config.Bind<bool>("Display - Forsaken Power", "displayPowerInInventory", true, "Display the forsaken power when in the inventory.");
            displayPowerOnActivation        = Config.Bind<bool>("Display - Forsaken Power", "displayPowerOnActivation", false, "Display the forsaken power when the key to use it is pressed.");
            //displayPowerWhenTimeChanges
            //displayPowerOnReady
            showPowerOnKeyPressed           = Config.Bind<bool>("Display - Forsaken Power", "showPowerOnKeyPressed", true, "Show the health panel when the display key is pressed.");

            // Display Scenario Settings - Hot Key Bar
            displayHotKeyBarInInventory     = Config.Bind<bool>("Display - Hot Key Bar", "displayHotKeyBarInInventory", true, "Display the hot key bar when in the inventory.");
            displayHotKeyBarOnItemSwitch    = Config.Bind<bool>("Display - Hot Key Bar", "displayHotKeyBarOnItemSwitch", false, "Display the hot key bar when you press any key for your hot bar items.");
            showHotKeyBarOnKeyPressed       = Config.Bind<bool>("Display - Hot Key Bar", "showHotKeyBarOnKeyPressed", true, "Show the health panel when the display key is pressed.");

            // Display Scenario Settings - Status Effects 
            displayStatusEffectsInInventory     = Config.Bind<bool>("Display - Status Effects", "displayStatusEffectsInInventory", true, "Display status effects when in the inventory.");
            showStatusEffectsOnKeyPressed       = Config.Bind<bool>("Display - Status Effects", "showStatusEffectsOnKeyPressed", true, "Show the health panel when the display key is pressed.");

            // Display Scenario Settings - MiniMap
            displayMiniMapInInventory   = Config.Bind<bool>("Display - MiniMap", "displayMiniMapInInventory", true, "Display the minimap when in the inventory.");
            showMiniMapOnKeyPressed     = Config.Bind<bool>("Display - MiniMap", "showMiniMapOnKeyPressed", true, "Show the health panel when the display key is pressed.");

            // Display Scenario Settings - Quick Slots     
            displayQuickSlotsInInventory    = Config.Bind<bool>("Display - Quick Slots", "displayQuickSlotsInInventory", true, "Display quick slots when in the inventory.");
            //displayQuickSlotsOnItemSwitch = Config.Bind<bool>("Display - Quick Slots", "displayQuickSlotsOnItemSwitch", false, "Display the quick slots when you press any key for your quick slot items.");
            showQuickSlotsOnKeyPressed      = Config.Bind<bool>("Display - Quick Slots", "showQuickSlotsOnKeyPressed", true, "Show the health panel when the display key is pressed.");

            // Crosshair Sprites
            crosshairSprite                 = LoadCrosshairTexture("ImmersiveHud/crosshair.png");
            crosshairBowSprite              = LoadCrosshairTexture("ImmersiveHud/bowcrosshair.png");

            DoPatching();
        }

        public static void DoPatching() => new Harmony("ImmersiveHud").PatchAll();

        [HarmonyPatch(typeof(Hud), "Awake")]
        public class Hud_Awake_Patch
        {
            private static void Postfix(Hud __instance)
            {
                if (!isEnabled.Value)
                    return;

                Transform hudRoot = __instance.transform.Find("hudroot");

                // Add CanvasGroup to each hud element on awake.
                foreach (string name in hudElementNames)
                {
                    hudElements.Add(name, new HudElement(name));

                    if (hudRoot.Find(name) == null)
                        continue;
   
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
