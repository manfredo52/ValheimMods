using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [HarmonyPatch(typeof(Player), "StartShipControl")]
    public class Player_StartShipControl_Patch : CustomizableCamera
    {
        private static void Postfix(Player __instance)
        {
            characterControlledShip = true;
        }
    }

    [HarmonyPatch(typeof(Player), "StopShipControl")]
    public class Player_StopShipControl_Patch : CustomizableCamera
    {
        private static void Postfix(Player __instance)
        {
            characterControlledShip = false;
        }
    }
}