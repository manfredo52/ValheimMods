using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [HarmonyPatch(typeof(Player), "StartShipControl")]
    public static class Player_StartShipControl_Patch
    {
        private static void Postfix(Player __instance)
        {
            CustomizableCamera.characterControlledShip = true;
        }
    }

    [HarmonyPatch(typeof(Player), "StopShipControl")]
    public static class Player_StopShipControl_Patch
    {
        private static void Postfix(Player __instance)
        {
            CustomizableCamera.characterControlledShip = false;
        }
    }
}