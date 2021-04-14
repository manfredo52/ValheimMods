using UnityEngine;
using HarmonyLib;

namespace PassTheTime
{
    [HarmonyPatch(typeof(Player), "SetMouseLook")]
    public class Player_SetMouseLook_Patch : PassTheTime
    {
        private static void Prefix(ref Vector2 mouseLook)
        {
            Player localPlayer = Player.m_localPlayer;

            if (!isEnabled.Value || !localPlayer)
                return;

            if (waitDialog.activeSelf)
                mouseLook = Vector2.zero;
        }
    }
}
