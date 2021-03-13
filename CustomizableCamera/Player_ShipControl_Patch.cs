using UnityEngine;
using HarmonyLib;

// To-do:
//  Save player's original camera zoom distance state.
namespace CustomizableCamera
{
    [HarmonyPatch(typeof(Player), "StartShipControl")]
    public static class Player_StartShipControl_Patch
    {
        private static void Postfix(Player __instance)
        {
            CustomizableCamera.characterIsControllingShip = true;
        }
    }

    [HarmonyPatch(typeof(Player), "StopShipControl")]
    public static class Player_StopShipControl_Patch
    {
        private static void Postfix(Player __instance)
        {
            CustomizableCamera.characterIsControllingShip = false;
        }
    }
}