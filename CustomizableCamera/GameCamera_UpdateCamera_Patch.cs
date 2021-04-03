using System;
using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
    public class GameCamera_UpdateCamera_Patch : CustomizableCamera
    {
        // Lerp Variables
        public static bool targetDistanceHasBeenReached;
        public static float timeDuration = smoothZoomSpeed;
        public static float timePos = 0;

        // Distance Variables
        public static float targetDistance;
        public static float lastSetDistance;
        public static float camDistance;
        public static float zoomSens;

        public static bool checkLerpDuration(float timeElapsed)
        {
            if (lastSetDistance == targetDistance || timeElapsed >= timeDuration)
            {
                timePos = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void moveToNewCameraDistance(float time, ref float ___m_distance)
        {
            // Move FP check into here
            ___m_distance = Mathf.Lerp(lastSetDistance, targetDistance, time);
            lastSetDistance = ___m_distance;
        }

        private static void Prefix(GameCamera __instance, ref float ___m_distance, ref float ___m_zoomSens)
        {
            if (!isEnabled.Value || GameCamera.InFreeFly())
                return;
            
            Player localPlayer = Player.m_localPlayer;

            if (!localPlayer)
                return;

            if (smoothZoomEnabled.Value)
            {
                // Disable the games default zooming in and out. Otherwise, the distance will flicker.
                ___m_zoomSens = 0;

                if ((Chat.instance && Chat.instance.HasFocus() || (Console.IsVisible() || InventoryGui.IsVisible()) || (StoreGui.IsVisible() || Menu.IsVisible() || (Minimap.IsOpen() || localPlayer.InCutscene())) ? 0 : (!localPlayer.InPlaceMode() ? 1 : 0)) != 0)
                {
                    float minDistance = __instance.m_minDistance;
                    float prevTargetDistance = targetDistance;

                    Debug.Log("Distance: " + ___m_distance + "    targetDistance: " + targetDistance);

                    targetDistance -= Input.GetAxis("Mouse ScrollWheel") * cameraZoomSensitivity.Value;
                    float max = localPlayer.GetControlledShip() != null ? __instance.m_maxDistanceBoat : __instance.m_maxDistance;
                    targetDistance = Mathf.Clamp(targetDistance, minDistance, max);

                    // Remove the delay when the player is going into first person.
                    if (___m_distance <= 0.1 && targetDistance == 0)
                    {
                        // TODO: Lerp TP Camera Position when player is going into FP

                        ___m_distance = targetDistance;
                        lastSetDistance = ___m_distance;
                    }

                    // Reset time when player changes zoom distance (scrollwheel)
                    if (prevTargetDistance != targetDistance)
                        timePos = 0;
                }

                targetDistanceHasBeenReached = checkLerpDuration(timePos);

                if (!targetDistanceHasBeenReached)
                {
                    timePos += Time.deltaTime;
                    moveToNewCameraDistance(timePos / timeDuration, ref ___m_distance);
                }
            }
            else
            {
                ___m_zoomSens = cameraZoomSensitivity.Value;
            }
        }
    }
}