using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static BM_AntiCheat.Translator;
using static BM_AntiCheat.Utils;

namespace BM_AntiCheat;

[BepInPlugin(PluginGuid, "BM_AntiCheat", PluginVersion)]


[BepInProcess("Among Us.exe")]
public partial class main : BasePlugin
{
    public Harmony Harmony { get; } = new(PluginGuid);
    public static string modVersion = "1.0.0";
    public const string PluginGuid = "com.GianniBart.BM_AntiCheat";
    public const string PluginVersion = "1.0.0";
    public static List<string> supportedAU = new List<string> { "2025.5.23" };
    public static readonly string ModName = "BM_AntiCheat";
    public static bool hasSentHackWarning = false;
    public static ManualLogSource Logger;
    public static string credentialsText;
    public const string ModColor = "#FFA500";
    public static main Instance;
    public static Settings Settings;
    //bool
    public static bool kickCheater;
    public static bool banCheater;
    public static bool SentWarning;




    public override void Load()
    {
        Instance = this;

        Logger = Log;
        Translator.Initialize();
        CheaterManager.Initialize();
        Harmony.PatchAll();
        Settings = AddComponent<Settings>();
    }

}

