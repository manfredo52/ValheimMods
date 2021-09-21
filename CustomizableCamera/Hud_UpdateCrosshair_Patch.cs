using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

// Fix crosshair state when logging out with bow equipped.
namespace CustomizableCamera
{   
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    public class Hud_UpdateCrosshair_Patch : CustomizableCamera
    {
        // Lerp Variables
        public static bool targetCrosshairHasBeenReached;
        public static float timeDuration = timeCameraPosDuration.Value;
        public static float timePos = 0; 

        public static bool crosshairStateChanged;
        public static characterState crosshairStatePrev = characterState.standing;
        public static characterState crosshairState = characterState.standing;

        private static bool checkLerpDuration(float timeElapsed)
        {
            if (lastSetCrosshairPos == targetCrosshairPos || timeElapsed >= timeDuration)
            {
                timePos = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void moveToNewCrosshairPosition(Hud __instance, float time)
        {
            __instance.m_crosshair.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);
            __instance.m_crosshairBow.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);

            __instance.m_hidden.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);
            __instance.m_targeted.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);
            __instance.m_targetedAlert.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);

            __instance.m_stealthBar.transform.position = Vector3.Lerp(lastSetStealthBarPos, targetStealthBarPos, time);

            lastSetCrosshairPos = __instance.m_crosshair.transform.position;
            lastSetStealthBarPos = __instance.m_crosshairBow.transform.position;
        }

        private static void setTargetPositions()
        {
            if (crosshairState == characterState.bowequipped)
            {
                targetCrosshairPos = new Vector3(playerInitialCrosshairX + playerBowCrosshairX.Value, playerInitialCrosshairY + playerBowCrosshairY.Value, 0);
                targetStealthBarPos = new Vector3(playerInitialStealthBarX + playerBowCrosshairX.Value, playerInitialStealthBarY + playerBowCrosshairY.Value * 3, 0);
            }
            else
            {
                targetCrosshairPos = new Vector3(playerInitialCrosshairX, playerInitialCrosshairY, 0);
                targetStealthBarPos = new Vector3(playerInitialStealthBarX, playerInitialStealthBarY, 0);
            }
        }

        private static void setCrosshairState()
        {
            crosshairStatePrev = crosshairState;

            if ((characterAiming || characterEquippedBow) && !isFirstPerson)
                crosshairState = characterState.bowequipped;
            else
                crosshairState = characterState.standing;

            if (crosshairState != crosshairStatePrev)
            {
                timePos = 0;
                crosshairStateChanged = true;
                crosshairStatePrev = crosshairState;
            }
            else
            {
                crosshairStateChanged = false;
            }
        }

        public static void Postfix(Hud __instance)
        {
            if (!isEnabled.Value || !__instance)
                return;

            if (playerBowCrosshairEditsEnabled.Value)
            {
                setCrosshairState();
                setTargetPositions();
                targetCrosshairHasBeenReached = checkLerpDuration(timePos);

                if (!targetCrosshairHasBeenReached)
                {
                    timePos += Time.deltaTime;
                    moveToNewCrosshairPosition(__instance, timePos / timeCameraPosDuration.Value);
                }
            }          
        }
    }
}