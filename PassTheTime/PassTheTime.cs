using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace PassTheTime
{
    [BepInPlugin("manfredo52.PassTheTime", "Pass The Time", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class PassTheTime : BaseUnityPlugin
    {
        // General
        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<int> nexusID;

        // Main Settings
        public static ConfigEntry<KeyboardShortcut> openMenuKey;

        public static GameObject waitDialog;
        public static GameObject timeDisplay;
        public static GameObject dayDisplay;
        public static GameObject waitTextDisplay;

        public static string displayFont = "Norsebold";

        public static Dictionary<string, Font> fonts;

        private void Awake()
        {
            // General
            isEnabled = Config.Bind<bool>("- General -", "Enable Mod", true, "Enable or disable the mod");
            nexusID = Config.Bind<int>("- General -", "NexusID", 0, "Nexus mod ID for updates");

            // Main Settings
            openMenuKey = Config.Bind<KeyboardShortcut>("- Main Settings -", "openMenuKey", new KeyboardShortcut(KeyCode.T), "Keyboard shortcut or mouse button to open the menu.");

            DoPatching();
        }

        public static void DoPatching() => new Harmony("PassTheTime").PatchAll();

        private void OnDestroy()
        {
            if (waitDialog != null)
                Destroy(waitDialog);
        }

        private void Update()
        {
            if (!waitDialog || !isEnabled.Value)
                return;

            if (Input.GetKeyDown(openMenuKey.Value.MainKey))
                ToggleMenu();
                               
            UpdateMenu();
        }

        public static void Setup()
        {
            fonts = GetFonts();
        }

        public static void HideMenu()
        {
            waitDialog.SetActive(false);
        }

        public static void ShowMenu()
        {
            waitDialog.SetActive(true);
        }

        public static void ToggleMenu() 
        {
            if (waitDialog.activeSelf)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Closed wait menu.");
                HideMenu();
            }         
            else
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Opened wait menu.");
                ShowMenu();
            }
        }

        public static void UpdateMenu()
        {
            UpdateDayText();
            UpdateTimeText();
        }

        public static void UpdateDayText()
        {
            Text text = dayDisplay.GetComponent<Text>();
            text.text = GetCurrentDay();
        }

        public static void UpdateTimeText()
        {
            Text text = timeDisplay.GetComponent<Text>();
            text.text = GetCurrentTime();
        }

        public static void CreateWaitDialog(Hud hud)
        {
            waitDialog = new GameObject("PTTWaitDialog");
            waitDialog.SetActive(false);

            RectTransform rectTransform = waitDialog.AddComponent<RectTransform>();
            waitDialog.transform.SetParent(hud.m_rootObject.transform);
            rectTransform.sizeDelta = new Vector2(450, 200f);
            rectTransform.anchoredPosition = new Vector2(0, 0);

            Image image = waitDialog.AddComponent<Image>();
            Sprite sprite = ((IEnumerable<Sprite>) Resources.FindObjectsOfTypeAll<Sprite>()).FirstOrDefault((s => s.name == "InputFieldBackground"));
            image.enabled = true;
            image.color = new Color(0.0f, 0.0f, 0.0f, 0.5f);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;

            CreateWaitTextDisplay();
            CreateDayDisplay();
            CreateTimeDisplay();
        }

        public static void CreateWaitTextDisplay()
        {
            waitTextDisplay = new GameObject("PTTWaitTextDisplay");

            waitTextDisplay.transform.SetParent(waitDialog.transform);
            RectTransform rectTransform = waitTextDisplay.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, -25f);

            Text text = waitTextDisplay.AddComponent<Text>();
            text.color = Color.white;
            text.text = "Wait how long?";
            text.enabled = true;
            text.font = fonts["Norsebold"];
            text.fontSize = 32;
        }

        public static void CreateDayDisplay()
        {
            dayDisplay = new GameObject("PTTDayDisplay");

            dayDisplay.transform.SetParent(waitDialog.transform);
            RectTransform rectTransform = dayDisplay.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(-50f, 0);

            Text text = dayDisplay.AddComponent<Text>();
            text.color = Color.white;
            text.text = GetCurrentDay();
            text.enabled = true;
            text.font = fonts["Norsebold"];
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleLeft;
        }

        public static void CreateTimeDisplay()
        {
            timeDisplay = new GameObject("PTTimeDisplay");

            timeDisplay.transform.SetParent(waitDialog.transform);
            RectTransform rectTransform = timeDisplay.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(50, 0);

            Text text = timeDisplay.AddComponent<Text>();
            text.color = Color.white;
            text.text = GetCurrentTime();
            text.enabled = true;
            text.font = fonts["Norsebold"];
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleRight;
        }

        public static string GetCurrentDay()
        {
            EnvMan env = EnvMan.instance;

            if (!env)
                return "";

            object[] parameters = null;
            int currentDay = (int) typeof(EnvMan).GetMethod("GetCurrentDay", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(env, parameters);

            return "Day " + currentDay.ToString();
        }

        public static string GetCurrentTime()
        {
            EnvMan env = EnvMan.instance;

            if (!env)
                return "";

            float smoothDayFraction = (float) typeof(EnvMan).GetField("m_smoothDayFraction", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(env);

            int hours = (int) (smoothDayFraction * 24.0);
            int minutes = (int) ((smoothDayFraction * 24.0 - hours) * 60.0);
            int seconds = (int) (((smoothDayFraction * 24.0 - hours) * 60.0 - minutes) * 60.0);

            DateTime time = DateTime.Today;
            DateTime currentGameTime = new DateTime(time.Year, time.Month, time.Day, hours, minutes, seconds);
            //dateTime.ToString("HH:mm" : "hh:mm tt");
            return currentGameTime.ToString("hh:mm tt");
        }

        public static Dictionary<string, Font> GetFonts()
        {
            Font[] fontArray = Resources.FindObjectsOfTypeAll<Font>();
            Dictionary<string, Font> fonts = new Dictionary<string, Font>();
            
            foreach (Font font in fontArray)
                fonts.Add(font.name, font);

            return fonts;
        }

        [HarmonyPatch(typeof(Hud), "Awake")]
        public class Hud_Awake_Patch
        {
            private static void Postfix(Hud __instance) => CreateWaitDialog(__instance);
        }

        [HarmonyPatch(typeof(FejdStartup), "Awake")]
        public class FejdStartup_Awake_Patch
        {
            private static void Postfix() => Setup();
        }
    }
}
