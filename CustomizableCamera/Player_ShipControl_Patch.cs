using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [HarmonyPatch(typeof(Player), "StartDoodadControl")]
    public class Player_StartShipControl_Patch : CustomizableCamera
    {
        public static void Postfix(Player __instance)
        {
            if (!isEnabled.Value || !__instance)
                return;

            characterControlledShip = true;
            characterStoppedShipControl = false;
            canChangeCameraDistance = true;
        }
    }

    [HarmonyPatch(typeof(Player), "StopDoodadControl")]
    public class Player_StopShipControl_Patch : CustomizableCamera
    {
        public static void Postfix(Player __instance)
        {
            if (!isEnabled.Value || !__instance)
                return;

            characterControlledShip = false;
            characterStoppedShipControl = true;
            canChangeCameraDistance = true;
        }
    }
}