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
    [BepInPlugin("manfredo52.ImmersiveHud", "Immersive Hud", "1.2.0")]
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
        public static ConfigEntry<bool> disableStealthHud;
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

        public static float targetStealthHudAlpha;
        public static GuiBar playerStealthBar;
        public static GameObject playerStealthIndicator;
        public static GameObject playerStealthIndicatorTargeted;
        public static GameObject playerStealthIndicatorAlert;

        // Hud Element Settings
        public static ConfigEntry<bool> displayHealthAlways;
        public static ConfigEntry<bool> displayHotKeyBarAlways;
        public static ConfigEntry<bool> displayForsakenPowerAlways;
        public static ConfigEntry<bool> displayStatusEffectsAlways;
        public static ConfigEntry<bool> displayStaminaBarAlways;
        public static ConfigEntry<bool> displayMiniMapAlways;
        public static ConfigEntry<bool> displayQuickSlotsAlways;
        public static ConfigEntry<bool> displayBetterUIFoodAlways;
        public static ConfigEntry<bool> displayCompassAlways;

        // Compatibility Settings
        public static ConfigEntry<bool> quickSlotsEnabled;
        public static ConfigEntry<bool> betterUIHPEnabled;
        public static ConfigEntry<bool> betterUIFoodEnabled;
        public static ConfigEntry<bool> betterUIStamEnabled;
        public static ConfigEntry<bool> compassEnabled;

        // Hud Element - All
        public static bool hudHidden;

        // Hud Element - Health      
        public static ConfigEntry<bool> displayHealthInInventory;
        //public static ConfigEntry<bool> displayHealthDuringRegen;
        //public static ConfigEntry<bool> displayHealthWhenDamaged;
        //public static ConfigEntry<bool> displayHealthWhenHungry;
        public static ConfigEntry<bool> displayHealthWhenEating;
        public static ConfigEntry<bool> displayHealthWhenBelowPercentage;
        public static ConfigEntry<float> healthPercentage;
        public static ConfigEntry<bool> showHealthOnKeyPressed;

        // Hud Element - Food Bar (Better UI)
        public static ConfigEntry<bool> displayFoodBarInInventory;
        public static ConfigEntry<bool> displayFoodBarWhenEating;
        public static ConfigEntry<bool> showFoodBarOnKeyPressed;

        // Hud Element - Stamina Bar
        public static ConfigEntry<bool> displayStaminaBarInInventory;
        public static ConfigEntry<bool> displayStaminaBarOnUse;
        public static ConfigEntry<bool> displayStaminaBarWhenEating;
        public static ConfigEntry<bool> displayStaminaBarWhenBelowPercentage;
        public static ConfigEntry<float> staminaPercentage;
        public static ConfigEntry<bool> showStaminaBarOnKeyPressed;

        // Hud Element - Forsaken Power
        public static ConfigEntry<bool> displayPowerInInventory;
        public static ConfigEntry<bool> displayPowerOnActivation;
        public static ConfigEntry<bool> displayPowerWhenTimeChanges;
        public static ConfigEntry<bool> displayPowerOnReady;
        public static ConfigEntry<float> powerTimeChangeInterval;
        public static ConfigEntry<bool> showPowerOnKeyPressed;

        // Hud Element - Hot Key Bar
        public static ConfigEntry<bool> displayHotKeyBarInInventory;
        public static ConfigEntry<bool> displayHotKeyBarOnItemSwitch;
        public static ConfigEntry<bool> showHotKeyBarOnKeyPressed;

        // Hud Element - Status Effects    
        public static ConfigEntry<bool> displayStatusEffectsInInventory;
        public static ConfigEntry<bool> showStatusEffectsOnKeyPressed;

        // Hud Element - Mini Map      
        public static ConfigEntry<bool> displayMiniMapInInventory;
        public static ConfigEntry<bool> showMiniMapOnKeyPressed;
        public static bool isMiniMapActive;

        // Hud Element - Compass
        public static ConfigEntry<bool> displayCompassInInventory;
        public static ConfigEntry<bool> showCompassOnKeyPressed;

        // Hud Element - Quick Slots
        public static ConfigEntry<bool> displayQuickSlotsInInventory;
        //public static ConfigEntry<bool> displayQuickSlotsOnItemSwitch;
        public static ConfigEntry<bool> showQuickSlotsOnKeyPressed;

        // Character States
        public static bool characterEquippedItem;
        public static bool characterEquippedBow;
        public static bool isLookingAtActivatable;
        public static bool playerUsedStamina;
        public static bool playerAteFood;
        public static bool playerUsedHotBarItem;
        public static bool playerUsedQuickSlotsItem;

        // Other
        public static float fadeDuration = 0.5f;

        // List of hud elements
        public static string[] hudElementNames =
        {
            "healthpanel",
            "staminapanel",
            "GuardianPower",
            "HotKeyBar",
            "StatusEffects",
            "MiniMap",
            "QuickSlotsHotkeyBar",
            "BetterUI_HPBar",
            "BetterUI_FoodBar",
            "BetterUI_StaminaBar",
            "Compass"
        };

        public class HudElement
        {
            public Transform element;
            public string elementName;
            
            public float targetAlphaPrev;
            public float targetAlpha;
            public float lastSetAlpha;
            public float timeFade = 0;
            public float timeDisplay = 0;

            public bool targetAlphaReached;
            public bool isDisplaying;
            public bool doesExist;

            public HudElement(string name)
            {
                element = null;
                elementName = name;
            }

            public void setElement(Transform e)
            {
                if (e != null)
                {
                    element = e;
                    doesExist = true;
                }
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
                if (!doesExist)
                    return;

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

        public static Dictionary<string, HudElement> hudElements;

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
            hideHudKey              = Config.Bind<KeyboardShortcut>("- Main Settings -", "hideHudKey", new KeyboardShortcut(KeyCode.H), "Keyboard shortcut or mouse button to hide the hud permanently.");
            hudHiddenOnStart        = Config.Bind<bool>("- Main Settings -", "hudHiddenOnStart", false, "Hide the hud when the game is started.");
            hudHiddenNotification   = Config.Bind<bool>("- Main Settings -", "hudHiddenNotification", true, "Enable notifications in the top left corner for hiding the hud.");
            hudFadeDuration         = Config.Bind<float>("- Main Settings -", "hudFadeDuration", 1f, "How quickly the hud fades in or out.");
            showHudDuration         = Config.Bind<float>("- Main Settings -", "showHudDuration", 1f, "How long a hud element should stay up for when it is activated for certain conditions.");
            showHudKey              = Config.Bind<KeyboardShortcut>("- Main Settings -", "showHudKey", new KeyboardShortcut(KeyCode.G), "Keyboard shortcut or mouse button to display the hud for a duration.");

            // Compatibility
            quickSlotsEnabled       = Config.Bind<bool>("- Mod Compatibility -", "quickSlotsEnabled", false, "Enable compatibility for quickslots mod.");
            betterUIHPEnabled       = Config.Bind<bool>("- Mod Compatibility -", "betterUIHPEnabled", false, "Enable compatibility for Better UI's custom HP bar.");
            betterUIFoodEnabled     = Config.Bind<bool>("- Mod Compatibility -", "betterUIFoodEnabled", false, "Enable compatibility for Better UI's custom food bar.");
            betterUIStamEnabled     = Config.Bind<bool>("- Mod Compatibility -", "betterUIStamEnabled", false, "Enable compatibility for Better UI's custom stamina bar.");
            compassEnabled          = Config.Bind<bool>("- Mod Compatibility -", "compassEnabled", false, "Enable compatibility for aedenthorn's compass mod.");

            // Crosshair Settings
            useCustomCrosshair              = Config.Bind<bool>("- Settings: Crosshair -", "useCustomCrosshair", false, new ConfigDescription("Enable or disable the new crosshair.", null, new ConfigurationManagerAttributes { Order = 1 }));
            useCustomBowCrosshair           = Config.Bind<bool>("- Settings: Crosshair -", "useCustomBowCrosshair", false, new ConfigDescription("Enable or disable the new crosshair for the bow draw.", null, new ConfigurationManagerAttributes { Order = 2 }));
            crosshairColor                  = Config.Bind<Color>("- Settings: Crosshair -", "crosshairColor", Color.white, "Color and transparency of the crosshair.");
            crosshairBowDrawColor           = Config.Bind<Color>("- Settings: Crosshair -", "crosshairBowDrawColor", Color.yellow, "Color and transparency of the bow draw crosshair.");
            disableStealthHud               = Config.Bind<bool>("- Settings: Crosshair -", "disableStealthHud", false, "Disable the stealth bar and indicator so it doesn't display.");
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
            displayStaminaBarAlways     = Config.Bind<bool>("- Settings: Display -", "displayStaminaBarAlways", false, "Always display the stamina bar.");
            displayMiniMapAlways        = Config.Bind<bool>("- Settings: Display -", "displayMiniMapAlways", false, "Always display the minimap.");
            displayQuickSlotsAlways     = Config.Bind<bool>("- Settings: Display -", "displayQuickSlotsAlways", false, "Always display the quick slots (Requires quick slots mod).");
            displayBetterUIFoodAlways   = Config.Bind<bool>("- Settings: Display -", "displayBetterUIFoodAlways", false, "Always display the food bar (Requires Better UI).");
            displayCompassAlways        = Config.Bind<bool>("- Settings: Display -", "displayCompassAlways", false, "Always display the compass (Required aedenthorns compass).");

            // Display Scenario Settings - Health          
            displayHealthInInventory            = Config.Bind<bool>("Display - Health", "displayHealthInInventory", true, "Display your health when in the inventory.");
            //displayHealthDuringRegen          = Config.Bind<bool>("Display - Health", "displayDuringRegen", false, "During health regen, the health panel will display.");
            //displayHealthWhenDamaged          = Config.Bind<bool>("Display - Health", "displayWhenDamaged", false, "Display the health panel when damaged.");
            displayHealthWhenEating             = Config.Bind<bool>("Display - Health", "displayHealthWhenEating", false, "Display the health panel when you eat food.");
            displayHealthWhenBelowPercentage    = Config.Bind<bool>("Display - Health", "displayWhenBelowPercentage", false, "When you are at or below a certain health percentage, display the health panel.");
            healthPercentage                    = Config.Bind<float>("Display - Health", "healthPercentage", 0.75f, new ConfigDescription("Health percentage at which the health panel should be displayed", new AcceptableValueRange<float>(0f, 1f)));
            showHealthOnKeyPressed              = Config.Bind<bool>("Display - Health", "showHealthOnKeyPressed", true, "Show the health panel when the show hud key is pressed.");

            // Display Scenario Settings - Food Bar (Better UI)
            displayFoodBarInInventory   = Config.Bind<bool>("Display - Food Bar (Better UI)", "displayBetterUIFoodBarInInventory", true, "Display the food bar when in the inventory.");
            displayFoodBarWhenEating    = Config.Bind<bool>("Display - Food Bar (Better UI)", "displayFoodBarWhenEating", true, "Display the food bar when you eat food.");
            showFoodBarOnKeyPressed     = Config.Bind<bool>("Display - Food Bar (Better UI)", "showFoodBarOnKeyPressed", true, "Display the food bar when the show hud key is pressed.");

            // Display Scenario Settings - Forsaken Power           
            displayPowerInInventory         = Config.Bind<bool>("Display - Forsaken Power", "displayPowerInInventory", true, "Display the forsaken power when in the inventory.");
            displayPowerOnActivation        = Config.Bind<bool>("Display - Forsaken Power", "displayPowerOnActivation", false, "Display the forsaken power when the key to use it is pressed.");
            //displayPowerWhenTimeChanges
            //displayPowerOnReady
            showPowerOnKeyPressed           = Config.Bind<bool>("Display - Forsaken Power", "showPowerOnKeyPressed", true, "Show the forsaken power when the show hud key is pressed.");

            // Display Scenario Settings - Hot Key Bar
            displayHotKeyBarInInventory     = Config.Bind<bool>("Display - Hot Key Bar", "displayHotKeyBarInInventory", true, "Display the hot key bar when in the inventory.");
            displayHotKeyBarOnItemSwitch    = Config.Bind<bool>("Display - Hot Key Bar", "displayHotKeyBarOnItemSwitch", false, "Display the hot key bar when you press any key for your hot bar items.");
            showHotKeyBarOnKeyPressed       = Config.Bind<bool>("Display - Hot Key Bar", "showHotKeyBarOnKeyPressed", true, "Show the hot key bar when the show hud key is pressed.");

            // Display Scenario Settings - Status Effects 
            displayStatusEffectsInInventory     = Config.Bind<bool>("Display - Status Effects", "displayStatusEffectsInInventory", true, "Display status effects when in the inventory.");
            showStatusEffectsOnKeyPressed       = Config.Bind<bool>("Display - Status Effects", "showStatusEffectsOnKeyPressed", true, "Show the status effects when the show hud key is pressed.");

            // Display Scenario Settings - Stamina
            displayStaminaBarInInventory    = Config.Bind<bool>("Display - Stamina Bar", "displayStaminaBarInInventory", true, "Display the stamina bar when in the inventory.");
            displayStaminaBarOnUse          = Config.Bind<bool>("Display - Stamina Bar", "displayStaminaBarOnUse", true, "Display the stamina bar when stamina is used.");
            displayStaminaBarWhenEating     = Config.Bind<bool>("Display - Stamina Bar", "displayStaminaBarWhenEating", true, "Display the stamina bar when you eat food.");
            displayStaminaBarWhenBelowPercentage = Config.Bind<bool>("Display - Stamina Bar", "displayStaminaBarWhenBelowPercentage", false, "When you are at or below a certain stamina percentage, display the stamina bar.");
            staminaPercentage               = Config.Bind<float>("Display - Stamina Bar", "staminaPercentage", 0.99f, new ConfigDescription("Stamina percentage at which the stamina bar should be displayed", new AcceptableValueRange<float>(0f, 1f)));
            showStaminaBarOnKeyPressed      = Config.Bind<bool>("Display - Stamina Bar", "showStaminaBarOnKeyPressed", true, "Show the stamina bar when the show hud key is pressed.");

            // Display Scenario Settings - MiniMap
            displayMiniMapInInventory   = Config.Bind<bool>("Display - MiniMap", "displayMiniMapInInventory", true, "Display the minimap when in the inventory.");
            showMiniMapOnKeyPressed     = Config.Bind<bool>("Display - MiniMap", "showMiniMapOnKeyPressed", true, "Show the minimap when the show hud key is pressed.");

            // Display Scenario Settings - Compass
            displayCompassInInventory   = Config.Bind<bool>("Display - Compass", "displayCompassInInventory", false, "Display the compass when in the inventory.");
            showCompassOnKeyPressed     = Config.Bind<bool>("Display - Compass", "showCompassOnKeyPressed", false, "Show the compass when the show hud key is pressed.");

            // Display Scenario Settings - Quick Slots
            displayQuickSlotsInInventory    = Config.Bind<bool>("Display - Quick Slots", "displayQuickSlotsInInventory", false, "Display quick slots when in the inventory.");
            //displayQuickSlotsOnItemSwitch = Config.Bind<bool>("Display - Quick Slots", "displayQuickSlotsOnItemSwitch", false, "Display the quick slots when you press any key for your quick slot items.");
            showQuickSlotsOnKeyPressed      = Config.Bind<bool>("Display - Quick Slots", "showQuickSlotsOnKeyPressed", false, "Show the quick slots when the show hud key is pressed.");

            // Crosshair Sprites
            crosshairSprite                 = LoadCrosshairTexture("ImmersiveHud/crosshair.png");
            crosshairBowSprite              = LoadCrosshairTexture("ImmersiveHud/bowcrosshair.png");

            DoPatching();
        }

        public static void DoPatching() => new Harmony("ImmersiveHud").PatchAll();

        public static void DebugListOfHudElements(Transform hud)
        {
            foreach (Transform t in hud.GetComponentsInChildren<Transform>(true))
                Debug.Log(t.name);
        }

        [HarmonyPatch(typeof(Hud), "Awake")]
        public class Hud_Awake_Patch
        {
            private static void Postfix(Hud __instance)
            {
                if (!isEnabled.Value)
                    return;

                Transform hudRoot = __instance.transform.Find("hudroot");
                hudElements = new Dictionary<string, HudElement>();

                // Add CanvasGroup to each hud element on awake.
                foreach (string name in hudElementNames)
                {
                    hudElements.Add(name, new HudElement(name));

                    if (hudRoot.Find(name) == null)
                        continue;

                    hudElements[name].setElement(hudRoot.Find(name));
                    hudElements[name].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                }

                hudHidden = hudHiddenOnStart.Value;

                playerCrosshair = __instance.m_crosshair;
                playerBowCrosshair = __instance.m_crosshairBow;

                playerStealthBar = __instance.m_stealthBar;
                playerStealthIndicator = __instance.m_hidden;
                playerStealthIndicatorTargeted = __instance.m_targeted;
                playerStealthIndicatorAlert = __instance.m_targetedAlert;

                playerStealthBar.transform.gameObject.AddComponent<CanvasGroup>();
                playerStealthIndicator.transform.gameObject.AddComponent<CanvasGroup>();
                playerStealthIndicatorTargeted.transform.gameObject.AddComponent<CanvasGroup>();
                playerStealthIndicatorAlert.transform.gameObject.AddComponent<CanvasGroup>();

                crosshairSpriteOriginal = playerCrosshair.sprite;
                crosshairBowSpriteOriginal = playerBowCrosshair.sprite;

                if (useCustomCrosshair.Value && crosshairSprite != null)
                    playerCrosshair.sprite = crosshairSprite;

                if (useCustomBowCrosshair.Value && crosshairBowSprite != null)
                    playerBowCrosshair.sprite = crosshairBowSprite;

                setCompatibilityInit();
                setCompatibility(hudRoot);
            }
        }

        public static void setCompatibilityInit()
        {
            hudElements["BetterUI_HPBar"].doesExist = false;
            hudElements["BetterUI_FoodBar"].doesExist = false;
            hudElements["BetterUI_StaminaBar"].doesExist = false;
            hudElements["Compass"].doesExist = false;
            hudElements["QuickSlotsHotkeyBar"].doesExist = false;
        }

        public static void setCompatibility(Transform hud)
        {
            // Compatibility check for BetterUI HP Bar
            if (betterUIHPEnabled.Value && !hudElements["BetterUI_HPBar"].doesExist)
            {
                if (hud.Find("BetterUI_HPBar"))
                {
                    hudElements["BetterUI_HPBar"].setElement(hud.Find("BetterUI_HPBar"));
                    hudElements["BetterUI_HPBar"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Compatibility check for BetterUI Food Bar
            if (betterUIFoodEnabled.Value && !hudElements["BetterUI_FoodBar"].doesExist)
            {
                if (hud.Find("BetterUI_FoodBar"))
                {
                    hudElements["BetterUI_FoodBar"].setElement(hud.Find("BetterUI_FoodBar"));
                    hudElements["BetterUI_FoodBar"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Compatibility check for BetterUI Stam Bar
            if (betterUIStamEnabled.Value && !hudElements["BetterUI_StaminaBar"].doesExist)
            {
                if (hud.Find("BetterUI_StaminaBar"))
                {
                    hudElements["BetterUI_StaminaBar"].setElement(hud.Find("BetterUI_StaminaBar"));
                    hudElements["BetterUI_StaminaBar"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Compatibility check for Compass
            if (compassEnabled.Value && !hudElements["Compass"].doesExist)
            {
                if (hud.Find("Compass"))
                {
                    hudElements["Compass"].setElement(hud.Find("Compass"));
                    hudElements["Compass"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Compatibility check for Quick Slots.
            if (quickSlotsEnabled.Value && !hudElements["QuickSlotsHotkeyBar"].doesExist)
            {
                if (hud.Find("QuickSlotsHotkeyBar"))
                {
                    hudElements["QuickSlotsHotkeyBar"].setElement(hud.Find("QuickSlotsHotkeyBar"));
                    hudElements["QuickSlotsHotkeyBar"].element.GetComponent<RectTransform>().gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
    }
}
