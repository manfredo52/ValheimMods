using System;
using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
    public class GameCamera_UpdateCamera_Patch : CustomizableCamera
    {
        // Reimplement camera settings reset
        private static void resetCameraSettings(GameCamera __instance)
        {
            __instance.m_fov = defaultFOV;
            __instance.m_3rdOffset = defaultPosition;
        }

        private static void moveToNewCameraPosition(GameCamera __instance, Vector3 targetVector, float time)
        {
            __instance.m_3rdOffset = Vector3.Lerp(__instance.m_3rdOffset, targetVector, time / timeCameraPosDuration.Value);
            lastSetPos = __instance.m_3rdOffset;
        }

        private static void moveToNewCameraFOV(GameCamera __instance, float targetFOV, float time)
        {
            __instance.m_fov = Mathf.Lerp(lastSetFOV, targetFOV, time / timeFOVDuration.Value);
            lastSetFOV = __instance.m_fov;
        }

        private static void moveToNewCameraFOVBowZoom(GameCamera __instance, float targetFOV, float time, interpolationTypes interpType)
        {
            if (interpType == interpolationTypes.SmoothStep)
                __instance.m_fov = Mathf.SmoothStep(lastSetFOV, targetFOV, time / timeBowZoomFOVDuration.Value);
            else
                __instance.m_fov = Mathf.Lerp(lastSetFOV, targetFOV, time / timeBowZoomFOVDuration.Value);

            lastSetFOV = __instance.m_fov;
        }

        public static bool checkFOVLerpDuration(GameCamera __instance, float timeElapsed)
        {
            if (lastSetFOV == targetFOV)
            {
                __instance.m_fov = targetFOV;
                return true;
            }
            else if (timeElapsed >= timeFOVDuration.Value)
            {
                timeFOV = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool checkCameraLerpDuration(GameCamera __instance, float timeElapsed)
        {
            if (lastSetPos == targetPos)
            {
                __instance.m_3rdOffset = targetPos;
                return true;
            }
            else if (timeElapsed >= timeCameraPosDuration.Value)
            {
                timeCameraPos = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void setValuesBasedOnCharacterState(Player __instance)
        {
            __characterStatePrev = __characterState;

            if (characterAiming && bowZoomEnabled.Value)
            {
                targetFOV = cameraBowZoomFOV.Value;
                if (characterEquippedBow && cameraBowSettingsEnabled.Value)
                    targetPos = new Vector3(cameraBowX.Value, cameraBowY.Value, cameraBowZ.Value);
                else
                    targetPos = new Vector3(cameraX.Value, cameraY.Value, cameraZ.Value);
                __characterState = characterState.bowaiming;
            }
            else if (characterControlledShip)
            {
                targetFOV = cameraBoatFOV.Value;
                targetPos = new Vector3(cameraBoatX.Value, cameraBoatY.Value, cameraBoatZ.Value);
                __characterState = characterState.sailing;
            }
            else if (characterEquippedBow && cameraBowSettingsEnabled.Value)
            {
                targetFOV = cameraFOV.Value;
                targetPos = new Vector3(cameraBowX.Value, cameraBowY.Value, cameraBowZ.Value);
                __characterState = characterState.bowequipped;

            }
            else if (characterCrouched)
            {
                targetFOV = cameraSneakFOV.Value;
                targetPos = new Vector3(cameraSneakX.Value, cameraSneakY.Value, cameraSneakZ.Value);
                __characterState = characterState.crouching;
            }
            else
            {
                targetFOV = cameraFOV.Value;
                targetPos = new Vector3(cameraX.Value, cameraY.Value, cameraZ.Value);
                __characterState = characterState.standing;
            }

            if (__characterState != __characterStatePrev)
            {
                characterStateChanged = true;
                __characterStatePrev = __characterState;
            }
            else
            {
                characterStateChanged = false;
            }
        }

        // Implement compatability for first person bow zoom.
        private static bool checkIfFirstPerson(GameCamera __instance, float ___m_distance)
        {
            if (___m_distance <= 0.0)
                return true;

            return false;
        }

        // FOV flicker when bow is zoomed in for too long.
        private static void Postfix(GameCamera __instance, ref float ___m_distance)
        {
            Player localPlayer = Player.m_localPlayer;

            if (!__instance || !localPlayer)
                return;

            if (!isEnabled.Value)
                resetCameraSettings(__instance);

            isFirstPerson = checkIfFirstPerson(__instance, ___m_distance);

            if (!isFirstPerson)
            {
                setValuesBasedOnCharacterState(localPlayer);
                targetFOVHasBeenReached = checkFOVLerpDuration(__instance, timeFOV);

                if (!targetFOVHasBeenReached)
                {
                    if (characterStateChanged)
                        timeFOV = 0;
                    else
                        timeFOV += Time.deltaTime;

                    if (characterAiming)
                        moveToNewCameraFOVBowZoom(__instance, targetFOV, localPlayer.GetAttackDrawPercentage(), timeBowZoomInterpolationType.Value);
                    else
                        moveToNewCameraFOV(__instance, targetFOV, timeFOV);
                }

                targetPosHasBeenReached = checkCameraLerpDuration(__instance, timeCameraPos);

                if (!targetPosHasBeenReached)
                {
                    if (characterStateChanged)
                        timeCameraPos = 0;
                    else
                        timeCameraPos += Time.deltaTime;

                    moveToNewCameraPosition(__instance, targetPos, timeCameraPos);
                }
            }
        }
    }
}