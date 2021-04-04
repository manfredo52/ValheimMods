using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace ImmersiveHud
{
    // Use updateCrosshair function?
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    public class Hud_UpdateCrosshair_Patch : ImmersiveHud
    {
        public static void updateCrosshairHudElement(float bowDrawPercentage)
        {
            playerCrosshair.CrossFadeAlpha(targetCrosshairAlpha, fadeDuration, false);

            if (bowDrawPercentage > 0.0)
            {
                if (useCustomBowCrosshair.Value)
                {
                    float num = Mathf.Lerp(0.75f, 0.25f, bowDrawPercentage);
                    playerBowCrosshair.transform.localScale = new Vector3(num, num, num);
                }

                if (displayBowDrawCrosshair.Value)
                    playerBowCrosshair.color = Color.Lerp(new Color(1f, 1f, 1f, 0.0f), crosshairBowDrawColor.Value, bowDrawPercentage);         
                else
                    playerBowCrosshair.color = new Color(0, 0, 0, 0);
            }
        }

        public static void setValuesBasedOnHud(Player player)
        {
            GameObject hoverObject = player.GetHoverObject();
            Hoverable hoverable = hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null;
            if (hoverable != null && !TextViewer.instance.IsVisible())
                isLookingAtActivatable = true;
            else
                isLookingAtActivatable = false;

            if (displayCrosshairAlways.Value)
                targetCrosshairAlpha = crosshairColor.Value.a;
            else if (displayCrosshairWhenBuilding.Value && player.InPlaceMode())
                targetCrosshairAlpha = crosshairColor.Value.a;
            else if (displayCrosshairOnActivation.Value && isLookingAtActivatable)
                targetCrosshairAlpha = crosshairColor.Value.a;
            else if (displayCrosshairOnEquipped.Value && characterEquippedItem)
                targetCrosshairAlpha = crosshairColor.Value.a;
            else if (displayCrosshairOnBowEquipped.Value && characterEquippedBow)
                targetCrosshairAlpha = crosshairColor.Value.a;
            else
                targetCrosshairAlpha = 0;
        }

        private static void Postfix(Hud __instance, Player player, float bowDrawPercentage)
        {
            playerCrosshair = __instance.m_crosshair;
            playerBowCrosshair = __instance.m_crosshairBow;

            setValuesBasedOnHud(player);
            updateCrosshairHudElement(bowDrawPercentage);
        }
    }
}
