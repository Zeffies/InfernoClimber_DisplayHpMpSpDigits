using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using MonoMod.RuntimeDetour;
using BepInEx.Configuration;
using System.Runtime.CompilerServices;

namespace DisplayHPStamMPHudDigits;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    // variable used for if you're cursed or not
    public static Vector3 hpInfoFontText_initial = new(-1, -1, -1);
    public static bool flagForDangerSound = false;
    internal static new ManualLogSource Logger;

    // create config settings
    public static ConfigEntry<bool> configFixHpMpFullness;
    public static ConfigEntry<float> configHpTextR;
    public static ConfigEntry<float> configHpTextG;
    public static ConfigEntry<float> configHpTextB;
    // public static ConfigEntry<int> configHpForDanger;
    // public static ConfigEntry<float> configHpDangerColourR;
    // public static ConfigEntry<float> configHpDangerColourG;
    // public static ConfigEntry<float> configHpDangerColourB;
    private void Awake()
    {
        // bind settings
        configFixHpMpFullness = Config.Bind("General", "FixHpMpFullness", true, "Makes it so when you're full HP or MP it actually displays that properly");

        configHpTextR = Config.Bind("HpAsText.Color", "R", (float)255, new ConfigDescription("R", new AcceptableValueRange<float>(1f, 255f))); 
        configHpTextG = Config.Bind("HpAsText.Color", "G", (float)234.6, new ConfigDescription("G", new AcceptableValueRange<float>(1f, 255f)));
        configHpTextB = Config.Bind("HpAsText.Color", "B", (float)4.08, new ConfigDescription("B", new AcceptableValueRange<float>(1f, 255f)));

        // configHpForDanger = Config.Bind("HpAsText.Danger", "Hp % For Danger", 30, new ConfigDescription("What percent should your HP text start flashing. 0 for never flash, 100 always flash", new AcceptableValueRange<int>(0, 100)));

        // configHpDangerColourR = Config.Bind("HpAsText.Danger", "R", (float)255, new ConfigDescription("R", new AcceptableValueRange<float>(1f, 255f)));
        // configHpDangerColourG = Config.Bind("HpAsText.Danger", "G", (float)0, new ConfigDescription("G", new AcceptableValueRange<float>(1f, 255f)));
        // configHpDangerColourB = Config.Bind("HpAsText.Danger", "B", (float)0, new ConfigDescription("B", new AcceptableValueRange<float>(1f, 255f)));

        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony("com.Zeffies.DisplayHpStamMPHudDigits");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(GUIEnergyBarConnector), "Update")]
public class GUIEnergyBarConnector_Update
{
    [HarmonyPostfix]
    static void DisplayHPStamMPHudDigits(GUIEnergyBarConnector __instance)
    {
        // fix hp and mp bar to actually display as full if you don't have a max hp/mp stat divisible by 100
        if (Plugin.configFixHpMpFullness.Value)
        {
            __instance.m_hpBar.valueMax = (int)AnSingleton<AnGameMgr>.Instance.GetPlayerGos().GetMaxHpIgnoreBadStatus();
            __instance.m_mpBar.valueMax = (int)AnSingleton<AnGameMgr>.Instance.GetPlayerGos().GetMaxMp();
        }
        else
        {
            // if you turn it off for some reason in the settings, this does it. also handles it so it can be changed dynamically with BepInEx Configuration Plugin
            float unFixHpFullness = AnSingleton<AnGameMgr>.Instance.GetPlayerGos().GetMaxHpIgnoreBadStatus() / 100;
            if (unFixHpFullness > (int)unFixHpFullness)
            {
                __instance.m_hpBar.valueMax = ((int)unFixHpFullness + 1) * 100;
            }
            else
            {
                __instance.m_hpBar.valueMax = (int)unFixHpFullness * 100;
            }
            float unFixMpFullness = AnSingleton<AnGameMgr>.Instance.GetPlayerGos().GetMaxMp() / 100;
             if (unFixMpFullness > (int)unFixMpFullness)
            {
                __instance.m_mpBar.valueMax = ((int)unFixMpFullness + 1) * 100;
            }
            else
            {
                __instance.m_mpBar.valueMax = (int)unFixMpFullness * 100;
            }
        }

        // set hpInfoFontText to say current/max hp instead of FULL
        __instance.m_hpInfoFontText.SetText(((int)__instance.m_playerGos.GetHp()).ToString() + "/" + __instance.m_playerGos.m_max_hp.ToString());
        // __instance.m_hpInfoFontTextTweenColor.from = new Color(Plugin.configHpDangerColourR.Value / 255, Plugin.configHpDangerColourG.Value / 255, Plugin.configHpDangerColourB.Value / 255);

        bool flag2 = __instance.m_slideAnimationMode == GUIEnergyBarConnector.SlideAnimationMode.FinishSlideIn || __instance.m_slideAnimationMode == GUIEnergyBarConnector.SlideAnimationMode.FinishSlideOut;

        // IC has a really weird way of handling whether or not to render the hpInfoFontText where it will count from 0 to 1 in decimal
        // and when it reaches 1, it changes whether you can see it or not. however, whatever function that is counting from 0 to 1
        // doesn't always work properly and can get stuck at like .999, which is why sometimes FULL doesn't display right or is always on
        // when it's not supposed to. rather than fix this function, i just said if it's over .95 go ahead and display it, since it always hits
        // at least that much

        if (__instance.m_hpInfoFontText.GetLabelAlpha() >= .95f && flag2)
        {
            __instance.m_hpInfoFontTextTweenColor.to = new Color(Plugin.configHpTextR.Value / 255, Plugin.configHpTextG.Value / 255, Plugin.configHpTextB.Value / 255);
            if (__instance.m_playerGos.GetHpRateIgnoreBadStatus() > .3f)
            {
                __instance.m_hpInfoFontText.gameObject.SetActive(true);
                __instance.m_hpInfoFontText.transform.localScale = Vector3.one;
                __instance.m_hpInfoFontText.SetLabelColor(new Color (Plugin.configHpTextR.Value / 255, Plugin.configHpTextG.Value / 255, Plugin.configHpTextB.Value / 255)); 
                __instance.m_hpInfoFontTextTweenColor.enabled = false;
                __instance.m_hpInfoFontTextTweenScale.enabled = false;
            }
        //     else if (__instance.m_playerGos.GetHpRateIgnoreBadStatus() <= (float)Plugin.configHpForDanger.Value /100)
        //     {
        //         __instance.m_hpInfoFontText.gameObject.SetActive(true);
        //         __instance.m_hpInfoFontTextTweenColor.enabled = true;
        //         __instance.m_hpInfoFontTextTweenScale.enabled = true;
        //         if (__instance.m_hpBar.gameObject.activeSelf)
        //         {
        //             Plugin.flagForDangerSound = true;
        //         }
        //     }
        //     else
        //     { 
        //         __instance.m_hpInfoFontText.gameObject.SetActive(false);
        //     }

        }
        // if (Plugin.flagForDangerSound)
        // {
            
        // }

        // fix location of hp text if you're cursed
        if (__instance.m_sptCurseIcon.gameObject.activeSelf)
        {
            if (Plugin.hpInfoFontText_initial.x == -1)
            {
                Plugin.hpInfoFontText_initial = __instance.m_hpInfoFontText.transform.localPosition;
                __instance.m_hpInfoFontText.transform.localPosition = __instance.m_hpInfoFontText.transform.localPosition + new Vector3(65, 0, 0);
            }
        }
        else
        {
            if (Plugin.hpInfoFontText_initial.x != -1)
            {
                __instance.m_hpInfoFontText.transform.localPosition = Plugin.hpInfoFontText_initial;
                Plugin.hpInfoFontText_initial = new Vector3(-1, -1, -1);
            }
        }

    }
}

// [HarmonyPatch(typeof(GUIEnergyBarConnector), "SetupHUDByUsingPlayerGos")]
// public class GUIEnergyBarConnector_SetupHUDByUsingPlayerGos
// {
//     [HarmonyPrefix]
//     static void FixEmptyStaminaPosition(GUIEnergyBarConnector __instance, ref float ___m_hpInfoFontBlockShiftSize, GameObjectStatus pc_gos)
//     {
//         float mynum3 = pc_gos.GetStaminaMaxIgnoreBadStatus() / 100f;
//         Debug.Log("mynum3 = " + mynum3);
//         Vector3 m_spInfoFontStartLeft = new Vector3(-945f, 429f, 1000f);
// 		int mynum4 = Mathf.CeilToInt(mynum3);
//         __instance.m_spInfoFontText.transform.localPosition = m_spInfoFontStartLeft + new Vector3(___m_hpInfoFontBlockShiftSize * (float)mynum4, 0f, 0f);
//     }
// }

// [HarmonyPatch(typeof(GUIEnergyBarConnector), "SetupHUDByUsingPlayerGos")]
// public class GUIEnergyBarConnector_SetupHUDByUsingPlayerGos
// {
//     [HarmonyPostfix]
//     static void FixEmptyStaminaPosition(GUIEnergyBarConnector __instance, ref Vector3 ___m_hpInfoFontStartLeft, ref float ___m_hpInfoFontBlockShiftSize, GameObjectStatus pc_gos)
//     {
//         float num = pc_gos.GetMaxHpIgnoreBadStatus() / 100f;
// 		int num2 = Mathf.CeilToInt(num);
//         Vector3 fixCursedHealthOffest = new Vector3(50, 0, 0);
//         if (__instance.m_playerGos.IsBadStatus(BAD_STATUS_KIND.CURSE))
//         {
//             __instance.m_hpInfoFontText.transform.localPosition = fixCursedHealthOffest + __instance.m_sptCurseIcon.transform.localPosition;
//         }
//         else
//         {
//             __instance.m_hpInfoFontText.transform.localPosition = ___m_hpInfoFontStartLeft + new Vector3(___m_hpInfoFontBlockShiftSize * (float)num2, 0f, 0f);
//         }
        
//     }
// }