using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;

// To-Do:
//  Linear interpolation for switching camera zoom distance.
//  Setup all settings on awake.
namespace CustomizableCamera
{
    [BepInPlugin("manfredo52.CustomizableCamera", "Customizable Camera Mod", "1.0.8")]
    [BepInProcess("valheim.exe")]
    public class CustomizableCamera : BaseUnityPlugin
    {
        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<int> nexusID;

        // Default Values
        public static int defaultCameraMaxDistance = 8;
        public static int defaultCameraMaxDistanceBoat = 16;
        public static float defaultSmoothness = 0.1f;
        public static float defaultFOV = 65.0f;
        public static float defaultTimeDuration = 5.0f;
        public static Vector3 defaultPosition = new Vector3(0.25f, 0.25f, 0.00f);

        // Normal Camera Settings
        public static ConfigEntry<float> cameraFOV;
        public static ConfigEntry<float> cameraX;
        public static ConfigEntry<float> cameraY;
        public static ConfigEntry<float> cameraZ;

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

        // TO-DO: Set this on game camera awake and when player goes into settings
        // Other Camera Settings
        public static ConfigEntry<float> cameraSmoothness;
        public static ConfigEntry<float> cameraMaxDistance;
        public static ConfigEntry<float> cameraMaxDistanceBoat;

        // Linear Interpolation Settings
        public static ConfigEntry<float> timeFOVDuration;
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

        public static bool characterStateChanged;
        public static bool characterControlledShip;
        public static bool characterCrouched;
        public static bool isFirstPerson;

        public enum characterState {
            standing,
            sprinting,
            crouching,
            sailing,
            bowdrawn,
            bowaiming
        };

        public static characterState __characterState;
        public static characterState __characterStatePrev;

        private void Awake()
        {
            // All settings start off with the game's original values.
            isEnabled   = Config.Bind<bool>("- General -", "Enable Mod", true, "Enable or disable the mod");
            nexusID     = Config.Bind<int>("- General -", "NexusID", 396, "Nexus mod ID for updates");

            cameraSmoothness        = Config.Bind<float>("- Misc -", "cameraSmoothness", defaultSmoothness, new ConfigDescription("Camera smoothing. Determines how smoothly/quickly the camera will follow your player.", new AcceptableValueRange<float>(0, 20f)));
            cameraMaxDistance       = Config.Bind<float>("- Misc -", "cameraMaxDistance", defaultCameraMaxDistance, new ConfigDescription("Maximum distance you can zoom out.", new AcceptableValueRange<float>(1, 100)));
            cameraMaxDistanceBoat   = Config.Bind<float>("- Misc -", "cameraMaxDistanceBoat", defaultCameraMaxDistanceBoat, new ConfigDescription("Maximum distance you can zoom out when on a boat.", new AcceptableValueRange<float>(1, 100)));

            timeFOVDuration         = Config.Bind<float>("- Misc -", "timeFOVDuration", defaultTimeDuration, new ConfigDescription("How quickly the fov changes.", new AcceptableValueRange<float>(0.001f, 50f)));
            timeCameraPosDuration   = Config.Bind<float>("- Misc -", "timeCameraPosDuration", defaultTimeDuration, new ConfigDescription("How quickly the camera moves to the new camera position", new AcceptableValueRange<float>(0.001f, 50f)));      

            cameraFOV       = Config.Bind<float>("Camera Settings", "cameraFOV", defaultFOV, "The camera fov.");
            cameraX         = Config.Bind<float>("Camera Settings", "cameraX", defaultPosition.x, "The third person camera x position.");
            cameraY         = Config.Bind<float>("Camera Settings", "cameraY", defaultPosition.y, "The third person camera y position.");
            cameraZ         = Config.Bind<float>("Camera Settings", "cameraZ", defaultPosition.z, "The third person camera z position.");

            cameraSneakFOV  = Config.Bind<float>("Camera Settings - Sneak", "cameraSneakFOV", defaultFOV, "Camera fov when sneaking.");
            cameraSneakX    = Config.Bind<float>("Camera Settings - Sneak", "cameraSneakX", defaultPosition.x, "Camera X position when sneaking.");
            cameraSneakY    = Config.Bind<float>("Camera Settings - Sneak", "cameraSneakY", defaultPosition.y, "Camera Y position when sneaking.");
            cameraSneakZ    = Config.Bind<float>("Camera Settings - Sneak", "cameraSneakZ", defaultPosition.z, "Camera Z position when sneaking.");

            cameraBoatFOV   = Config.Bind<float>("Camera Settings - Boat", "cameraBoatFOV", defaultFOV, "Camera fov when sailing.");
            cameraBoatX     = Config.Bind<float>("Camera Settings - Boat", "cameraBoatX", defaultPosition.x, "Camera X position when sailing.");
            cameraBoatY     = Config.Bind<float>("Camera Settings - Boat", "cameraBoatY", defaultPosition.y, "Camera Y position when sailing.");
            cameraBoatZ     = Config.Bind<float>("Camera Settings - Boat", "cameraBoatZ", defaultPosition.z, "Camera Z position when sailing.");

            DoPatching();
        }

        public static void DoPatching() => new Harmony("CustomizableCamera").PatchAll();
    }
}