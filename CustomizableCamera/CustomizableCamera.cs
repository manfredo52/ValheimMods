using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [BepInPlugin("manfredo52.CustomizableCamera", "Customizable Camera Mod", "1.0.2")]
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
        public static bool isSneaking;

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

        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        public static class UpdateCameraPosition_Patch
        {

            private static void resetCameraSettings(GameCamera __instance)
            {
                __instance.m_fov = defaultFOV;
                __instance.m_3rdOffset = defaultPosition;
            }

            private static void moveToNewCameraPosition(GameCamera __instance, Vector3 targetVector)
            {
                __instance.m_3rdOffset = targetVector;
            }

            private static void moveToNewCameraFOV(GameCamera __instance, float targetFOV)
            {
                __instance.m_fov = targetFOV;
            }

            private static void Postfix(GameCamera __instance)
            {
                if (!isEnabled.Value)
                {
                    resetCameraSettings(__instance);
                    return;
                }

                Player localPlayer = Player.m_localPlayer;

                if (localPlayer.IsCrouching())
                {
                    moveToNewCameraFOV(__instance, cameraSneakFOV.Value);
                    moveToNewCameraPosition(__instance, new Vector3(cameraSneakX.Value, cameraSneakY.Value, cameraSneakZ.Value));
                }
                else
                {
                    moveToNewCameraFOV(__instance, cameraFOV.Value);
                    moveToNewCameraPosition(__instance, new Vector3(cameraX.Value, cameraY.Value, cameraZ.Value));
                }
            }
        }
    }
}
