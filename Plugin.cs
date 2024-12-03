using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using MonoMod.RuntimeDetour;
using BepInEx.Configuration;
using System.Runtime.CompilerServices;
using System;
using System.CodeDom;
using System.Globalization;

namespace DisplayHPStamMPHudDigits;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    // variable used for if you're cursed or not
    public static Vector3 hpInfoFontText_initial = new(-1, -1, -1);
    // was going to use flagForDangerSound if i made it so you could customize the % hp at which your hp flashed / had an alarm. that feature is too much work right now until i can find a better way to do it
    // public static bool flagForDangerSound = false;
    public static bool setupMpFont = false;
    public static bool setupUILabel = false;
    public static bool setupNGUILabelSetTextFromTextId = false;
    public static bool findDiabloBar = false;
    public static bool setupInitialColor = false;
    internal static new ManualLogSource Logger;

    // create config settings
    public static ConfigEntry<bool> configFixHpMpFullness;
    public static ConfigEntry<float> configHpTextR;
    public static ConfigEntry<float> configHpTextG;
    public static ConfigEntry<float> configHpTextB;

    // i wanted to make it so you could change what % hp the game flashes your HP + gives you the danger sound but it ended up being more
    // trouble than it works, sadly. i'll go back and try again at some point

    // public static ConfigEntry<int> configHpForDanger;

    // these are used for what colour your hp flashes to when less than 30% hp
    public static ConfigEntry<float> configHpDangerColourR;
    public static ConfigEntry<float> configHpDangerColourG;
    public static ConfigEntry<float> configHpDangerColourB;

    // mp text color
    public static ConfigEntry<float> configMpTextR;
    public static ConfigEntry<float> configMpTextG;
    public static ConfigEntry<float> configMpTextB;

    // public static ConfigEntry<float> findIconSize;


    // test var
    public static bool testVar = false;

    private void Awake()
    {
        // bind settings
        configFixHpMpFullness = Config.Bind("General", "FixHpMpFullness", true, "Makes it so when you're full HP or MP it actually displays that properly");

        configHpTextR = Config.Bind("HpAsText.Color", "R", (float)255, new ConfigDescription("R", new AcceptableValueRange<float>(0f, 255f))); 
        configHpTextG = Config.Bind("HpAsText.Color", "G", (float)234.6, new ConfigDescription("G", new AcceptableValueRange<float>(0f, 255f)));
        configHpTextB = Config.Bind("HpAsText.Color", "B", (float)4.08, new ConfigDescription("B", new AcceptableValueRange<float>(0f, 255f)));

        // configHpForDanger = Config.Bind("HpAsText.Danger", "Hp % For Danger", 30, new ConfigDescription("What percent should your HP text start flashing. 0 for never flash, 100 always flash", new AcceptableValueRange<int>(0, 100)));

        configHpDangerColourR = Config.Bind("HpAsText.Danger", "R", (float)255, new ConfigDescription("R", new AcceptableValueRange<float>(0f, 255f)));
        configHpDangerColourG = Config.Bind("HpAsText.Danger", "G", (float)0, new ConfigDescription("G", new AcceptableValueRange<float>(0f, 255f)));
        configHpDangerColourB = Config.Bind("HpAsText.Danger", "B", (float)0, new ConfigDescription("B", new AcceptableValueRange<float>(0f, 255f)));

        configMpTextR = Config.Bind("MpAsText.Color", "R", (float)255, new ConfigDescription("R", new AcceptableValueRange<float>(0f, 255f))); 
        configMpTextG = Config.Bind("MpAsText.Color", "G", (float)234.6, new ConfigDescription("G", new AcceptableValueRange<float>(0f, 255f)));
        configMpTextB = Config.Bind("MpAsText.Color", "B", (float)4.08, new ConfigDescription("B", new AcceptableValueRange<float>(0f, 255f)));

        // findIconSize = Config.Bind("iconsize", "what's the icon size dude", (float)0, new ConfigDescription("B", new AcceptableValueRange<float>(0f, 100f)));

        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony("com.Zeffies.DisplayHpStamMPHudDigits");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(GUIEnergyBarConnector), "Start")]
public class GUIEnergyBarConnector_Start
{
    public static GameObject diabloSphereBarMp;
    public static UILabel mpBarLabel;
    public static NGUILabelSetTextFromTextId mpInfoFontText;
    public static GameObject mpInfoFont;
    public static RegistResolutionFitInfoList mpTextResolutionFix;

    [HarmonyPostfix]
    static void initializeMpBar(GUIEnergyBarConnector __instance)
    {
        Plugin.Logger.LogInfo("initializing mp bar text...");
        diabloSphereBarMp = GameObject.Find("DiabloSphereBarMp");
        if (diabloSphereBarMp != null)
        {
            mpInfoFont = new GameObject("mpInfoFont");
            mpInfoFont.transform.SetParent(diabloSphereBarMp.transform);
            // add uilabel to mp bar
            mpBarLabel = mpInfoFont.GetComponent<UILabel>();
            if (mpBarLabel == null)
            {
                Plugin.Logger.LogInfo("Adding UILabel to mpInfoFont.");
                mpBarLabel = mpInfoFont.AddComponent<UILabel>();
            }
            // add nguilabel to mp bar
            mpInfoFontText = mpInfoFont.GetComponent<NGUILabelSetTextFromTextId>();
            if (mpInfoFontText == null)
            {
                Plugin.Logger.LogInfo("Adding NGUILabelSetTextFromTextId to mpInfoFont.");
                mpInfoFontText = mpInfoFont.AddComponent<NGUILabelSetTextFromTextId>();
            }
            mpTextResolutionFix = mpInfoFont.GetComponent<RegistResolutionFitInfoList>();
            if (mpTextResolutionFix == null)
            {
                Plugin.Logger.LogInfo("Adding RegistResolutionFitInfoList to mpInfoFont");
                mpTextResolutionFix = mpInfoFont.AddComponent<RegistResolutionFitInfoList>();
            }
            if (!Plugin.setupMpFont)
            {
                UILabel sourceLabel = (UILabel)__instance.m_hpInfoFontText.GetComponent("UILabel");
                UILabel targetLabel = (UILabel)mpInfoFont.GetComponent("UILabel");

                targetLabel.bitmapFont = sourceLabel.bitmapFont;
                targetLabel.fontSize = sourceLabel.fontSize;
                targetLabel.effectStyle = sourceLabel.effectStyle;
                targetLabel.effectColor = sourceLabel.effectColor;
                targetLabel.effectDistance = sourceLabel.effectDistance;
                targetLabel.alignment = sourceLabel.alignment;
                targetLabel.overflowMethod = sourceLabel.overflowMethod;
                targetLabel.width = sourceLabel.width;
                targetLabel.spacingX = sourceLabel.spacingX;
                targetLabel.spacingY = sourceLabel.spacingY;
                targetLabel.supportEncoding = sourceLabel.supportEncoding;
                targetLabel.symbolStyle = sourceLabel.symbolStyle;
                Debug.Log("setting up font");
                Plugin.setupMpFont = true;
            }
        }
    }
}

[HarmonyPatch(typeof(GUIEnergyBarConnector), "Update")]
public class GUIEnergyBarConnector_Update
{
    [HarmonyPostfix]
    static void DisplayHPStamMPHudDigits(GUIEnergyBarConnector __instance)
    {
        // float hpInfoFontText_Alpha = __instance.m_hpInfoFontText.GetLabelAlpha();
        bool flag2 = __instance.m_slideAnimationMode == GUIEnergyBarConnector.SlideAnimationMode.FinishSlideIn || __instance.m_slideAnimationMode == GUIEnergyBarConnector.SlideAnimationMode.FinishSlideOut;

        if (GUIEnergyBarConnector_Start.diabloSphereBarMp != null)
        {

            GUIEnergyBarConnector_Start.mpInfoFontText.SetText(((int)__instance.m_playerGos.GetMp()).ToString() + "/" + __instance.m_playerGos.m_max_mp.ToString());
            

            // copy hp text active state and do the stuff it does when active
            if (__instance.m_hpInfoFontText.gameObject.activeSelf)
            {
                if (!GUIEnergyBarConnector_Start.mpInfoFont.gameObject.activeSelf)
                {
                    Debug.Log("setting mpinfofont active");
                }
                GUIEnergyBarConnector_Start.mpInfoFont.gameObject.SetActive(true);
                if (Plugin.setupInitialColor == false)
                {
                    // set initial colour
                    Debug.Log("setting up mp text initial color");
                    GUIEnergyBarConnector_Start.mpInfoFontText.SetLabelColor(new Color (Plugin.configMpTextR.Value / 255, Plugin.configMpTextG.Value / 255, Plugin.configMpTextB.Value / 255));
                    Plugin.setupInitialColor = true;
                }

                // copy hp text alpha
                if (GUIEnergyBarConnector_Start.mpInfoFontText.GetLabelAlpha() != __instance.m_hpInfoFontText.GetLabelAlpha())
                {
                    if (__instance.m_hpInfoFontText.GetLabelAlpha() >= .01f)                                                        // todo move alpha logic to where hpinfotext processes it
                    {
                        // Debug.Log("mpBarLabelAlpha = " + GUIEnergyBarConnector_Start.mpInfoFontText.GetLabelAlpha());
                        // Debug.Log("hpbar alpha= " + __instance.m_hpInfoFontText.GetLabelAlpha());
                        GUIEnergyBarConnector_Start.mpInfoFontText.SetLabelAlpha(__instance.m_hpInfoFontText.GetLabelAlpha());
                    }
                    else
                    {
                        GUIEnergyBarConnector_Start.mpInfoFontText.SetLabelAlpha(0f);
                    }
                }
                
                // set position
                float num = __instance.m_playerGos.GetMaxMp() / 100f;                                                                                                   //TODO move localposition logic to SetupHUDUsingPlayerGos
                int num2 = Mathf.CeilToInt(num);
                if (GUIEnergyBarConnector_Start.mpInfoFont.transform.localPosition != new Vector3(-874f, 385f, 1000f) + new Vector3(47.3f * num2, 0f, 0f))
                {
                    Debug.Log("repeat count = " + num2);
                    GUIEnergyBarConnector_Start.mpInfoFont.transform.localPosition = new Vector3(-874f, 385f, 1000f) + new Vector3(47.3f * num2, 0f, 0f);
                }
                GUIEnergyBarConnector_Start.mpInfoFontText.transform.localScale = Vector3.one;
                var currentColor = GUIEnergyBarConnector_Start.mpInfoFontText.GetLabelColor();
                if (Math.Truncate(currentColor.r * 255) != Math.Truncate(Plugin.configMpTextR.Value) || 
                    Math.Truncate(currentColor.g * 255) != Math.Truncate(Plugin.configMpTextG.Value) || 
                    Math.Truncate(currentColor.b * 255) != Math.Truncate(Plugin.configMpTextB.Value)
                    )
                {
                    // Debug.Log("\n current color r = " + Math.Truncate(currentColor.r * 255) + " || configmptext r =" + Math.Truncate(Plugin.configMpTextR.Value) + "\n"
                    //         + "current color g = " + Math.Truncate(currentColor.g * 255) + " || configmptext g =" + Math.Truncate(Plugin.configMpTextG.Value)     + "\n"
                    //         + "current color b = " + Math.Truncate(currentColor.b * 255) + " || configmptext b =" + Math.Truncate(Plugin.configMpTextB.Value));
                    GUIEnergyBarConnector_Start.mpInfoFontText.SetLabelColor(new Color (Plugin.configMpTextR.Value / 255, Plugin.configMpTextG.Value / 255, Plugin.configMpTextB.Value / 255));
                }
            }
            else if (GUIEnergyBarConnector_Start.mpInfoFont.gameObject.activeSelf)
            {
                Debug.Log("setting mpinfofont inactive");
                GUIEnergyBarConnector_Start.mpInfoFont.gameObject.SetActive(false);
            }

            // if (__instance.m_hpInfoFontText.GetLabelAlpha() >= .95f && flag2)
            // {
            //     if (!GUIEnergyBarConnector_Start.mpInfoFont.gameObject.activeSelf)
            //     {
            //         Debug.Log("setting mptext as active");
            //         GUIEnergyBarConnector_Start.mpInfoFont.gameObject.SetActive(true);
            //     }
            //     float num = __instance.m_playerGos.GetMaxMp() / 100f;
            //     int num2 = Mathf.CeilToInt(num);
            //     if (GUIEnergyBarConnector_Start.mpInfoFont.transform.localPosition != new Vector3(-864f, 385f, 1000f) + new Vector3(44f * (float)num2, 0f, 0f))
            //     {
            //         GUIEnergyBarConnector_Start.mpInfoFont.transform.localPosition = new Vector3(-864f, 385f, 1000f) + new Vector3(44f * (float)num2, 0f, 0f);
            //     }
            //     GUIEnergyBarConnector_Start.mpInfoFontText.transform.localScale = Vector3.one;
            //     GUIEnergyBarConnector_Start.mpInfoFontText.SetLabelColor(new Color (Plugin.configMpTextR.Value / 255, Plugin.configMpTextG.Value / 255, Plugin.configMpTextB.Value / 255));
            // }
            // if (GUIEnergyBarConnector_Start.mpInfoFontText.GetLabelAlpha() <= 0f && GUIEnergyBarConnector_Start.mpInfoFont.gameObject.activeSelf)
            // {
            //     Debug.Log("setting mptext as inactive");
            //     GUIEnergyBarConnector_Start.mpInfoFont.gameObject.SetActive(false);
            // }
            // if (GUIEnergyBarConnector_Start.mpInfoFont.activeSelf)
            // {
            //     if (GUIEnergyBarConnector_Start.mpInfoFontText.GetLabelAlpha() != __instance.m_hpInfoFontText.GetLabelAlpha())
            //     {
            //         if (__instance.m_hpInfoFontText.GetLabelAlpha() >= .01f)
            //         {
            //             Debug.Log("mpBarLabelAlpha = " + GUIEnergyBarConnector_Start.mpInfoFontText.GetLabelAlpha());
            //             GUIEnergyBarConnector_Start.mpInfoFontText.SetLabelAlpha(__instance.m_hpInfoFontText.GetLabelAlpha());
            //         }
            //         else
            //         {
            //             GUIEnergyBarConnector_Start.mpInfoFontText.SetLabelAlpha(0f);
            //         }
            //     }
            // }
        }
        
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

        // same for mp
        // mystuff.mpAsText.SetText(((int)__instance.m_playerGos.GetMp()).ToString() + "/" + __instance.m_playerGos.m_max_mp.ToString());

        // set tween colors
        if (__instance.m_hpInfoFontTextTweenColor.from != new Color(Plugin.configHpDangerColourB.Value / 255, Plugin.configHpDangerColourG.Value / 255, Plugin.configHpDangerColourB.Value / 255) || 
            __instance.m_hpInfoFontTextTweenColor.to != new Color(Plugin.configHpTextR.Value / 255, Plugin.configHpTextG.Value / 255, Plugin.configHpTextB.Value / 255))
        {
            __instance.m_hpInfoFontTextTweenColor.from = new Color(Plugin.configHpDangerColourR.Value / 255, Plugin.configHpDangerColourG.Value / 255, Plugin.configHpDangerColourB.Value / 255);
            __instance.m_hpInfoFontTextTweenColor.to = new Color(Plugin.configHpTextR.Value / 255, Plugin.configHpTextG.Value / 255, Plugin.configHpTextB.Value / 255);
        }

        // IC has a really weird way of handling whether or not to render the hpInfoFontText where it will count from 0 to 1 in decimal
        // and when it reaches 1, it changes whether you can see it or not. however, whatever function that is counting from 0 to 1
        // doesn't always work properly and can get stuck at like .999, which is why sometimes FULL doesn't display right or is always on
        // when it's not supposed to. rather than fix this function, i just said if it's over .95 go ahead and display it, since it always hits
        // at least that much

        if (__instance.m_hpInfoFontText.GetLabelAlpha() >= .95f && flag2)
        {
            // if (__instance.m_playerGos.GetHpRateIgnoreBadStatus() > (float)Plugin.configHpForDanger.Value / 100)
            if (__instance.m_playerGos.GetHpRateIgnoreBadStatus() > .3f)
            {
                __instance.m_hpInfoFontText.gameObject.SetActive(true);
                __instance.m_hpInfoFontText.transform.localScale = Vector3.one;
                __instance.m_hpInfoFontText.SetLabelColor(new Color (Plugin.configHpTextR.Value / 255, Plugin.configHpTextG.Value / 255, Plugin.configHpTextB.Value / 255)); 
                __instance.m_hpInfoFontTextTweenColor.enabled = false;
                __instance.m_hpInfoFontTextTweenScale.enabled = false;
            }
                                                                                                                                                                                // TODO: fix the issue where tweencolor only works at .3f hp
            // else if (__instance.m_playerGos.GetHpRateIgnoreBadStatus() <= (float)Plugin.configHpForDanger.Value /100)
            // {
            //     __instance.m_hpInfoFontText.gameObject.SetActive(true);
            //     __instance.m_hpInfoFontTextTweenColor.enabled = true;
            //     __instance.m_hpInfoFontTextTweenScale.enabled = true;
            //     if (__instance.m_hpBar.gameObject.activeSelf)
            //     {
            //         Plugin.flagForDangerSound = true;
            //     }
            // }
            // else
            // { 
            //     __instance.m_hpInfoFontText.gameObject.SetActive(false);
            // }

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