using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;

// To-Do:
//  Linear interpolation for switching camera zoom distance.
//  Setup all settings on awake.
namespace CustomizableCamera
{
    [BepInPlugin("manfredo52.CustomizableCamera", "Customizable Camera Mod", "1.0.7")]
    [BepInProcess("valheim.exe")]
    public class CustomizableCamera : BaseUnityPlugin
    {
        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<int> nexusID;

        public static float defaultCameraMaxDistance = 8;
        public static float defaultCameraMaxDistanceBoat = 16;
        public static float defaultSmoothness = 0.1f;
        public static float defaultFOV = 65.0f;
        public static float defaultTimeDuration = 4.0f;
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

            cameraSmoothness        = Config.Bind<float>("- Misc -", "cameraSmoothness", defaultSmoothness, "Camera smoothing. Determines how smoothly/quickly the camera will follow your player.");
            cameraMaxDistance       = Config.Bind<float>("- Misc -", "cameraMaxDistance", defaultCameraMaxDistance, "Maximum distance you can zoom out.");
            cameraMaxDistanceBoat   = Config.Bind<float>("- Misc -", "cameraMaxDistanceBoat", defaultCameraMaxDistanceBoat, "Maximum distance you can zoom out when on a boat.");

            timeFOVDuration         = Config.Bind<float>("- Misc -", "timeFOVDuration", defaultTimeDuration, "How quickly the fov changes.");
            timeCameraPosDuration   = Config.Bind<float>("- Misc -", "timeCameraPosDuration", defaultTimeDuration, "How quickly the camera moves to the new camera position");

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

        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        public static class GameCamera_UpdateCamera_Patch
        {

            // Reimplement camera settings reset
            private static void resetCameraSettings(GameCamera __instance)
            {
                __instance.m_fov = defaultFOV;
                __instance.m_3rdOffset = defaultPosition;
            }

            // Needed compatability for first person mod? Doesn't seem like it.
            private static void setMiscCameraSettings(GameCamera __instance)
            {
                __instance.m_smoothness = cameraSmoothness.Value;
                __instance.m_maxDistance = cameraMaxDistance.Value;
                __instance.m_maxDistanceBoat = cameraMaxDistanceBoat.Value;
            }

            private static void moveToNewCameraPosition(GameCamera __instance, Vector3 targetVector, float time)
            {
                __instance.m_3rdOffset = Vector3.Lerp(__instance.m_3rdOffset, targetVector, time / timeCameraPosDuration.Value);
                lastSetPos = __instance.m_3rdOffset;
            }

            private static void moveToNewCameraFOV(GameCamera __instance, float targetFOV, float time)
            {
                __instance.m_fov = Mathf.Lerp(lastSetFOV, targetFOV, time / timeFOVDuration.Value);
                lastSetFOV = __instance.m_fov;
            }

            public static bool checkFOVLerpDuration(GameCamera __instance, float timeElapsed)
            {
                if (lastSetFOV == targetFOV)
                {
                    __instance.m_fov = targetFOV;
                    return true;
                } 
                else if (timeElapsed >= timeFOVDuration.Value)
                {
                    timeFOV = 0;                   
                    return true;
                } 
                else
                {
                    return false;
                }
            }

            public static bool checkCameraLerpDuration(GameCamera __instance, float timeElapsed)
            {
                if (lastSetPos == targetPos)
                {
                    __instance.m_3rdOffset = targetPos;
                    return true;
                }
                else if (timeElapsed >= timeCameraPosDuration.Value)
                {
                    timeCameraPos = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }

 
            // Change implementation for character crouching state. Target fov and state should be set when the user presses their crouch button (onCrouch?)
            private static void setValuesBasedOnCharacterState(Player __instance)
            {
                __characterStatePrev = __characterState;

                if (characterControlledShip)
                {
                    targetFOV = cameraBoatFOV.Value;
                    targetPos = new Vector3(cameraBoatX.Value, cameraBoatY.Value, cameraBoatZ.Value);
                    __characterState = characterState.sailing;
                }
                else if (characterCrouched)
                {
                    targetFOV = cameraSneakFOV.Value;
                    targetPos = new Vector3(cameraSneakX.Value, cameraSneakY.Value, cameraSneakZ.Value);
                    __characterState = characterState.crouching;
                }
                else
                {
                    targetFOV = cameraFOV.Value;
                    targetPos = new Vector3(cameraX.Value, cameraY.Value, cameraZ.Value);
                    __characterState = characterState.standing;
                }

                if (__characterState != __characterStatePrev)
                {
                    characterStateChanged = true;
                    __characterStatePrev = __characterState;
                }
            }

            // Will have to change this implementation when adding in bow zoom.
            // When changing back to third person, there is an fov change going on that may be irritating. Not important.
            private static bool checkIfFirstPerson(GameCamera __instance, float ___m_distance)
            {
                if (___m_distance <= 0.0)
                    return true;

                return false;
            }

            private static void Postfix(GameCamera __instance, ref float ___m_distance)
            {
                Player localPlayer = Player.m_localPlayer;

                if (!__instance || !localPlayer)
                    return;

                if (!isEnabled.Value)
                    resetCameraSettings(__instance);

                setMiscCameraSettings(__instance);// remove
                isFirstPerson = checkIfFirstPerson(__instance, ___m_distance);

                if (!isFirstPerson) {
                    setValuesBasedOnCharacterState(localPlayer);
                    targetFOVHasBeenReached = checkFOVLerpDuration(__instance, timeFOV);

                    if (!targetFOVHasBeenReached)
                    {
                        if (characterStateChanged)
                            timeFOV = 0;
                        else
                            timeFOV += Time.deltaTime;

                        moveToNewCameraFOV(__instance, targetFOV, timeFOV);
                    }

                    targetPosHasBeenReached = checkCameraLerpDuration(__instance, timeCameraPos);

                    if (!targetPosHasBeenReached) 
                    {
                        if (characterStateChanged)
                        {
                            timeCameraPos = 0;
                            characterStateChanged = false;
                        }
                        else
                        {
                            timeCameraPos += Time.deltaTime;
                        }

                        moveToNewCameraPosition(__instance, targetPos, timeCameraPos);
                    }
                }
            }
        }
    }
}