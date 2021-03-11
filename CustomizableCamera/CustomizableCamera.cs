using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CustomizableCamera
{
    [BepInPlugin("manfredo52.CustomizableCamera", "Customizable Camera Mod", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class CustomizableCamera : BaseUnityPlugin
    {
        public static ConfigEntry<bool> isEnabled;
        public static float defaultFOV = 65f;
        public static Vector3 defaultPosition = new Vector3(0.25f, 0.25f, 0.0f);
        public static ConfigEntry<float> fov;
        public static ConfigEntry<float> cameraX;
        public static ConfigEntry<float> cameraY;
        public static ConfigEntry<float> cameraZ;

        private void Awake()
        {
            isEnabled = Config.Bind<bool>("- General -", "Enable Mod", true, "Enable or disable the mod");
            fov = Config.Bind<float>("Camera Settings", "FOV", defaultFOV, "The camera fov.");
            cameraX = Config.Bind<float>("Camera Settings", "CameraX", defaultPosition.x, "The third person camera x position.");
            cameraY = Config.Bind<float>("Camera Settings", "CameraY", defaultPosition.y, "The third person camera y position.");
            cameraZ = Config.Bind<float>("Camera Settings", "CameraZ", defaultPosition.z, "The third person camera z position.");
            DoPatching();
        }

        public static void DoPatching() => new Harmony(nameof(CustomizableCamera)).PatchAll();


        [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
        public static class UpdateCameraPosition_Patch
        {
            private static void resetCameraSettings(GameCamera __instance)
            {
                __instance.m_fov = defaultFOV;
                __instance.m_3rdOffset = defaultPosition;
            }

            private static void Postfix(GameCamera __instance)
            {
                if (!isEnabled.Value)
                {
                    resetCameraSettings(__instance);
                }
                else
                {
                    __instance.m_fov = fov.Value;
                    __instance.m_3rdOffset = new Vector3(cameraX.Value, cameraY.Value, cameraZ.Value);
                }
            }
        }
    }
}
