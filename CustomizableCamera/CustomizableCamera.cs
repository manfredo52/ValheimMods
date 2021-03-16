using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;

// To-Do:
//  Linear interpolation for switching camera zoom distance.
namespace CustomizableCamera
{
    [BepInPlugin("manfredo52.CustomizableCamera", "Customizable Camera Mod", "1.1.3")]
    [BepInProcess("valheim.exe")]
    public class CustomizableCamera : BaseUnityPlugin
    {
        // Main Settings
        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<int> nexusID;

        // Default Values
        public static int defaultCameraDistance = 4;
        public static int defaultCameraMaxDistance = 8;
        public static int defaultCameraMaxDistanceBoat = 16;
        public static float defaultSmoothness = 0.1f;
        public static float defaultFOV = 65.0f;
        public static float defaultFPFOV = 65.0f;
        public static float defaultBowZoomFOV = 55.0f;
        public static float defaultBowZoomFPFOV = 55.0f;
        public static float defaultTimeDuration = 5.0f;
        public static float defaultBowZoomTimeDuration = 3.0f;
        public static Vector3 defaultPosition = new Vector3(0.25f, 0.25f, 0.00f);

        // Normal Camera Settings
        public static ConfigEntry<float> cameraFOV;
        public static ConfigEntry<float> cameraX;
        public static ConfigEntry<float> cameraY;
        public static ConfigEntry<float> cameraZ;

        // First Person Camera Mod Settings
        public static ConfigEntry<bool> bowZoomFirstPersonEnabled;
        public static ConfigEntry<float> cameraFirstPersonFOV;
        public static ConfigEntry<float> cameraBowZoomFirstPersonFOV;

        // Sneak Camera Settings
        public static ConfigEntry<float> cameraSneakFOV;
        public static ConfigEntry<float> cameraSneakX;
        public static ConfigEntry<float> cameraSneakY;
        public static ConfigEntry<float> cameraSneakZ;

        // Boat Camera Settings
        public static ConfigEntry<float> cameraBoatFOV;
        public static ConfigEntry<float> cameraBoatX;
        public static ConfigEntry<float> cameraBoatY;
        public static ConfigEntry<float> cameraBoatZ;

        // Bow Camera Setting
        public static ConfigEntry<bool> cameraBowSettingsEnabled;
        public static ConfigEntry<float> cameraBowX;
        public static ConfigEntry<float> cameraBowY;
        public static ConfigEntry<float> cameraBowZ;

        // Bow Zoom Camera Settings
        public static ConfigEntry<bool> bowZoomEnabled;
        public static ConfigEntry<bool> bowZoomOnDraw;
        public static ConfigEntry<KeyboardShortcut> bowZoomKey;
        public static ConfigEntry<KeyboardShortcut> bowCancelDrawKey;
        public static ConfigEntry<float> cameraBowZoomFOV;

        // Other Camera Settings
        public static ConfigEntry<float> cameraSmoothness;
        public static ConfigEntry<float> cameraDistance;
        public static ConfigEntry<float> cameraMaxDistance;
        public static ConfigEntry<float> cameraMaxDistanceBoat;

        // Linear Interpolation Settings
        public static ConfigEntry<float> timeFOVDuration;
        public static ConfigEntry<float> timeBowZoomFOVDuration;
        public static ConfigEntry<interpolationTypes> timeBowZoomInterpolationType;
        public static ConfigEntry<float> timeCameraPosDuration;

        // Variables for FOV linear interpolation
        public static float timeFOV = 0;
        public static float targetFOV;
        public static float lastSetFOV;
        public static bool targetFOVHasBeenReached;

        // Variables for camera position linear interpolation
        public static float timeCameraPos = 0;
        public static Vector3 targetPos;
        public static Vector3 lastSetPos;
        public static bool targetPosHasBeenReached;

        // State changing
        public static bool characterStateChanged;
        public static bool characterControlledShip;
        public static bool characterCrouched;
        public static bool characterAiming;
        public static bool characterEquippedBow;

        public static bool isFirstPerson;

        public enum interpolationTypes
        {
            Linear,
            SmoothStep
        }

        public enum characterState {
            standing,
            sprinting,
            crouching,
            sailing,
            bowequipped,
            bowaiming
        };

        public static characterState __characterState;
        public static characterState __characterStatePrev;

        private void Awake()
        {
            // All settings start off with the game's original values.
            isEnabled   = Config.Bind<bool>("- General -", "Enable Mod", true, "Enable or disable the mod");
            nexusID     = Config.Bind<int>("- General -", "NexusID", 396, "Nexus mod ID for updates");

            // These misc options will only take effect on game camera awake and when you save the game settings in the menu.
            cameraSmoothness        = Config.Bind<float>("- Misc -", "cameraSmoothness", defaultSmoothness, new ConfigDescription("Camera smoothing. Determines how smoothly/quickly the camera will follow your player.", new AcceptableValueRange<float>(0, 20f)));
            cameraDistance          = Config.Bind<float>("- Misc -", "cameraDistance", defaultCameraDistance, new ConfigDescription("Camera distance that should be set when starting the game.", new AcceptableValueRange<float>(0, 100)));
            cameraMaxDistance       = Config.Bind<float>("- Misc -", "cameraMaxDistance", defaultCameraMaxDistance, new ConfigDescription("Maximum distance you can zoom out.", new AcceptableValueRange<float>(1, 100)));
            cameraMaxDistanceBoat   = Config.Bind<float>("- Misc -", "cameraMaxDistanceBoat", defaultCameraMaxDistanceBoat, new ConfigDescription("Maximum distance you can zoom out when on a boat.", new AcceptableValueRange<float>(1, 100)));

            timeFOVDuration              = Config.Bind<float>("- Misc Time Values -", "timeFOVDuration", defaultTimeDuration, new ConfigDescription("How quickly the fov changes.", new AcceptableValueRange<float>(0.001f, 50f)));
            timeBowZoomFOVDuration       = Config.Bind<float>("- Misc Time Values -", "timeBowZoomFOVDuration", defaultBowZoomTimeDuration, new ConfigDescription("How quickly the bow zooms in.", new AcceptableValueRange<float>(0.001f, 50f)));
            timeBowZoomInterpolationType = Config.Bind<interpolationTypes>("- Misc Time Values -", "timeBowZoomInterpolationType", interpolationTypes.Linear, new ConfigDescription("Interpolation method for the bow zoom."));
            timeCameraPosDuration        = Config.Bind<float>("- Misc Time Values -", "timeCameraPosDuration", defaultTimeDuration, new ConfigDescription("How quickly the camera moves to the new camera position", new AcceptableValueRange<float>(0.001f, 50f)));

            cameraFOV = Config.Bind<float>("Camera Settings", "cameraFOV", defaultFOV, "The camera fov.");
            cameraX   = Config.Bind<float>("Camera Settings", "cameraX", defaultPosition.x, "The third person camera x position.");
            cameraY   = Config.Bind<float>("Camera Settings", "cameraY", defaultPosition.y, "The third person camera y position.");
            cameraZ   = Config.Bind<float>("Camera Settings", "cameraZ", defaultPosition.z, "The third person camera z position.");

            cameraSneakFOV  = Config.Bind<float>("Camera Settings - Sneak", "cameraSneakFOV", defaultFOV, "Camera fov when sneaking.");
            cameraSneakX    = Config.Bind<float>("Camera Settings - Sneak", "cameraSneakX", defaultPosition.x, "Camera X position when sneaking.");
            cameraSneakY    = Config.Bind<float>("Camera Settings - Sneak", "cameraSneakY", defaultPosition.y, "Camera Y position when sneaking.");
            cameraSneakZ    = Config.Bind<float>("Camera Settings - Sneak", "cameraSneakZ", defaultPosition.z, "Camera Z position when sneaking.");

            cameraBoatFOV   = Config.Bind<float>("Camera Settings - Boat", "cameraBoatFOV", defaultFOV, "Camera fov when sailing.");
            cameraBoatX     = Config.Bind<float>("Camera Settings - Boat", "cameraBoatX", defaultPosition.x, "Camera X position when sailing.");
            cameraBoatY     = Config.Bind<float>("Camera Settings - Boat", "cameraBoatY", defaultPosition.y, "Camera Y position when sailing.");
            cameraBoatZ     = Config.Bind<float>("Camera Settings - Boat", "cameraBoatZ", defaultPosition.z, "Camera Z position when sailing.");

            cameraBowSettingsEnabled = Config.Bind<bool>("Camera Settings - Bow", "bowSettingsEnable", false, "Enable or disable if there should be separate camera settings when holding a bow.");
            cameraBowX               = Config.Bind<float>("Camera Settings - Bow", "cameraBowX", defaultPosition.x, "Camera X position when holding a bow.");
            cameraBowY               = Config.Bind<float>("Camera Settings - Bow", "cameraBowY", defaultPosition.y, "Camera Y position when holding a bow.");
            cameraBowZ               = Config.Bind<float>("Camera Settings - Bow", "cameraBowZ", defaultPosition.z, "Camera Z position when holding a bow.");

            bowZoomEnabled      = Config.Bind<bool>("Camera Settings - Bow Zoom", "bowZoomEnable", false, "Enable or disable bow zoom");
            bowZoomOnDraw       = Config.Bind<bool>("Camera Settings - Bow Zoom", "bowZoomOnDraw", true, "Zoom in automatically when drawing the bow.");
            bowZoomKey          = Config.Bind<KeyboardShortcut>("Camera Settings - Bow Zoom", "bowZoomKey", new KeyboardShortcut(KeyCode.Mouse1), "Keyboard shortcut or mouse button for zooming in with the bow.");
            bowCancelDrawKey    = Config.Bind<KeyboardShortcut>("Camera Settings - Bow Zoom", "bowCancelDrawKey", new KeyboardShortcut(KeyCode.Mouse4), "Keyboard shortcut or mouse button to cancel bow draw. This is only necessary when your zoom key interferes with the block key.");
            cameraBowZoomFOV    = Config.Bind<float>("Camera Settings - Bow Zoom", "cameraBowZoomFOV", defaultBowZoomFOV, "FOV when zooming in with the bow.");

            bowZoomFirstPersonEnabled   = Config.Bind<bool>("Camera Settings - First Person Mod Compatibility", "bowZoomFirstPersonEnable", false, "Enable or disable bow zoom when in first person. Ensures compatibility with first person mods.");
            cameraFirstPersonFOV        = Config.Bind<float>("Camera Settings - First Person Mod Compatibility", "cameraFirstPersonFOV", defaultFPFOV, "The camera fov when you are in first person. This is only used to ensure compatibility for first person mods and first person bow zoom.");
            cameraBowZoomFirstPersonFOV = Config.Bind<float>("Camera Settings - First Person Mod Compatibility", "cameraBowZoomFirstPersonFOV", defaultBowZoomFPFOV, "FOV when zooming in with the bow when in first person.");

            DoPatching();
        }

        // Set default m_distance on game as a new option.
        private static void setMiscCameraSettings(GameCamera __instance)
        {   
            __instance.m_smoothness = cameraSmoothness.Value;  
            __instance.m_maxDistance = cameraMaxDistance.Value;
            __instance.m_maxDistanceBoat = cameraMaxDistanceBoat.Value;
        }

        public static void DoPatching() => new Harmony("CustomizableCamera").PatchAll();

        [HarmonyPatch(typeof(GameCamera), "Awake")]
        public static class GameCamera_Awake_Patch
        {
            private static void Postfix(GameCamera __instance, ref float ___m_distance)
            {
                setMiscCameraSettings(__instance);
                ___m_distance = cameraDistance.Value;
            }
        }

        [HarmonyPatch(typeof(GameCamera), "ApplySettings")]
        private static class GameCamera_ApplySettings_Patch
        {
            private static void Postfix(GameCamera __instance)
            {
                setMiscCameraSettings(__instance);
            }
        }
    }
}