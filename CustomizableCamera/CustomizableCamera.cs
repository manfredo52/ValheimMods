using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [BepInPlugin("manfredo52.CustomizableCamera", "Customizable Camera Mod", "1.0.3")]
    [BepInProcess("valheim.exe")]
    public class CustomizableCamera : BaseUnityPlugin
    {
        public static ConfigEntry<bool> isEnabled;
        public static ConfigEntry<int> nexusID;

        public static float defaultFOV = 65.0f;
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

        public static float timeFOVDuration = 5.0f;
        public static float timeFOV = 0;
        public static float remainingTimeFOV = 0;
        public static float targetFOV;
        public static float lastSetFOV;
        public static bool targetFOVHasBeenReached;
        public static bool characterStateChanged;
        public static bool isFirstPerson;

        public enum characterState {
            standing, 
            crouching
        };

        public static characterState __characterState;
        public static characterState __characterStatePrev;

        private void Awake()
        {
            isEnabled = Config.Bind<bool>("- General -", "Enable Mod", true, "Enable or disable the mod");
            nexusID = Config.Bind<int>("- General -", "NexusID", 396, "Nexus mod ID for updates");

            cameraFOV = Config.Bind<float>("Camera Settings", "FOV", defaultFOV, "The camera fov.");
            cameraX = Config.Bind<float>("Camera Settings", "CameraX", defaultPosition.x, "The third person camera x position.");
            cameraY = Config.Bind<float>("Camera Settings", "CameraY", defaultPosition.y, "The third person camera y position.");
            cameraZ = Config.Bind<float>("Camera Settings", "CameraZ", defaultPosition.z, "The third person camera z position.");

            cameraSneakFOV = Config.Bind<float>("Camera Settings - Sneak", "SneakFOV", defaultFOV, "Camera fov when sneaking.");
            cameraSneakX = Config.Bind<float>("Camera Settings - Sneak", "CameraSneakX", defaultPosition.x, "Camera X position when sneaking.");
            cameraSneakY = Config.Bind<float>("Camera Settings - Sneak", "CameraSneakY", defaultPosition.y, "Camera Y position when sneaking.");
            cameraSneakZ = Config.Bind<float>("Camera Settings - Sneak", "CameraSneakZ", defaultPosition.z, "Camera Z position when sneaking.");

            DoPatching();
        }

        public static void DoPatching() => new Harmony("CustomizableCamera").PatchAll();

        [HarmonyPatch(typeof(Player), "LateUpdate")]
        public static class Player_FOV_LateUpdate_Patch
        {

            private static void resetCameraSettings(GameCamera __instance)
            {
                __instance.m_fov = defaultFOV;
                __instance.m_3rdOffset = defaultPosition;
            }

            private static void moveToNewCameraPosition(GameCamera __instance, Vector3 targetVector)
            {
                Vector3 currentVector = __instance.m_3rdOffset;
                __instance.m_3rdOffset = targetVector;
            }

            private static void moveToNewCameraFOV(GameCamera __instance, float targetFOV, float time)
            {
                __instance.m_fov = Mathf.Lerp(lastSetFOV, targetFOV, time / timeFOVDuration);
                lastSetFOV = __instance.m_fov;
            }

            public static bool checkFOVLerpDuration(GameCamera __instance, float timeElapsed)
            {
                if (lastSetFOV == targetFOV)
                {
                    __instance.m_fov = targetFOV;
                    return true;
                } 
                else if (timeElapsed >= timeFOVDuration)
                {
                    timeFOV = 0;                   
                    return true;
                } 
                else
                {
                    return false;
                }
            }

            private static void setValuesBasedOnCharacterState(Player __instance)
            {
                __characterStatePrev = __characterState;

                if (__instance.IsCrouching())
                {
                    targetFOV = cameraSneakFOV.Value;
                    __characterState = characterState.crouching;
                }
                else
                {
                    targetFOV = cameraFOV.Value;
                    __characterState = characterState.standing;
                }

                if (__characterState != __characterStatePrev)
                {
                    characterStateChanged = true;
                    __characterStatePrev = __characterState;
                }
            }


            private static void Postfix(Player __instance)
            {
                GameCamera camInstance = GameCamera.instance;

                if (!camInstance)
                    return;

                if (!isEnabled.Value)
                    resetCameraSettings(camInstance);

                setValuesBasedOnCharacterState(__instance);
                targetFOVHasBeenReached = checkFOVLerpDuration(camInstance, timeFOV);

                if (targetFOVHasBeenReached == false)
                {
                    if (characterStateChanged == true)
                    {
                        timeFOV = 0;
                        characterStateChanged = false;
                    } 
                    else 
                    { 
                        timeFOV += Time.deltaTime; 
                    }

                    if (__characterState == characterState.crouching)
                    {
                        moveToNewCameraFOV(camInstance, targetFOV, timeFOV);
                    }
                    else
                    {
                        moveToNewCameraFOV(camInstance, targetFOV, timeFOV); 
                    }
                }

                if (__characterState == characterState.crouching)
                {
                    moveToNewCameraPosition(camInstance, new Vector3(cameraSneakX.Value, cameraSneakY.Value, cameraSneakZ.Value));
                }
                else
                {
                    moveToNewCameraPosition(camInstance, new Vector3(cameraX.Value, cameraY.Value, cameraZ.Value));
                }
            }
        }
    }
}