using UnityEngine;
using HarmonyLib;

namespace CustomizableCamera
{
    // Shouldn't take effece in third person
    [HarmonyPatch(typeof(Hud), "Update")]
    public class Hud_CrosshairUpdate_Patch : CustomizableCamera
    {
        private static void Postfix(Hud __instance)
        {
            if (playerBowCrosshairEditsEnabled.Value)
            { 
                UnityEngine.UI.Image playerCrosshair = __instance.m_crosshair;
                UnityEngine.UI.Image playerBowCrosshair = __instance.m_crosshairBow;
                GuiBar playerStealthBar = __instance.m_stealthBar;
                GameObject playerHidden = __instance.m_hidden;

                Transform transform = playerCrosshair.transform;
                Transform transformBow = playerBowCrosshair.transform;
                Transform transformStealthBar = playerStealthBar.transform;
                Transform transformPlayerHidden = playerHidden.transform;

                if ((characterAiming || characterEquippedBow) && !isFirstPerson) 
                {
                    Vector3 newLocation = new Vector3(playerInitialCrosshairX + playerBowCrosshairX.Value, playerInitialCrosshairY + playerBowCrosshairY.Value, 0);
                    Vector3 newLocationS = new Vector3(playerInitialStealthbarX + playerBowCrosshairX.Value, playerInitialStealthbarY + playerBowCrosshairY.Value * 3, 0);

                    transform.position = transformBow.position = transformPlayerHidden.position = newLocation;
                    transformStealthBar.position = newLocationS;
                } 
                else
                {
                    Vector3 newLocation = new Vector3(playerInitialCrosshairX, playerInitialCrosshairY, 0);
                    Vector3 newLocationS = new Vector3(playerInitialStealthbarX, playerInitialStealthbarY, 0);

                    transform.position = transformBow.position = transformPlayerHidden.position = newLocation;
                    transformStealthBar.position = newLocationS;
                }
            }
        }
    }
}