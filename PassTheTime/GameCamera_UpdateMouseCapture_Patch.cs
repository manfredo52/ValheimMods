using UnityEngine;
using HarmonyLib;

namespace PassTheTime
{
    [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
    public class GameCamera_UpdateMouseCapture_Patch : PassTheTime
    {
        private static void Postfix()
        {
            if (!isEnabled.Value)
                return;

            if (waitDialog.activeSelf)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = ZInput.IsMouseActive();
            }
        }
    }
}
