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

        // Position Variables
        public static Vector3 lastSetCrosshairPos = playerCrosshair.transform.position;
        public static Vector3 lastSetStealthBarPos = playerStealthBar.transform.position;
        public static Vector3 targetCrosshairPos = playerCrosshair.transform.position;
        public static Vector3 targetStealthBarPos = playerStealthBar.transform.position;

        public static bool crosshairStateChanged;
        public static characterState crosshairStatePrev = characterState.standing;
        public static characterState crosshairState = characterState.standing;

        public static bool checkLerpDuration(float timeElapsed)
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

        public static void moveToNewCrosshairPosition(float time)
        {
            playerCrosshair.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);
            playerBowCrosshair.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);

            playerStealthIndicator.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);
            playerStealthIndicatorTargeted.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);
            playerStealthIndicatorAlert.transform.position = Vector3.Lerp(lastSetCrosshairPos, targetCrosshairPos, time);

            playerStealthBar.transform.position = Vector3.Lerp(lastSetStealthBarPos, targetStealthBarPos, time);

            lastSetCrosshairPos = playerCrosshair.transform.position;
            lastSetStealthBarPos = playerStealthBar.transform.position;
        }

        public static void setTargetPositions()
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

        public static void setCrosshairState()
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

        private static void Postfix(Hud __instance)
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
                    moveToNewCrosshairPosition(timePos / timeCameraPosDuration.Value);
                }
            }
        }
    }
}