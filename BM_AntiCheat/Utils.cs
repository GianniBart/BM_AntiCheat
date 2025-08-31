
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using static BM_AntiCheat.Translator;

namespace BM_AntiCheat;
public static class Utils
{
    public static bool Scientist(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Scientist;
    }
    public static bool Angel(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.GuardianAngel;
    }
    public static bool Engineer(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Engineer;
    }
    public static bool Tracker(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Tracker;
    }
    public static bool Impostor(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Impostor;
    }
    public static bool Shapeshifter(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Shapeshifter;
    }
    public static bool Phantom(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Phantom;
    }
    public static bool IsCanMove => PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.CanMove;
    public static bool InGame => AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started;
    public static bool IsInTask => InGame && !MeetingHud.Instance;
    public static bool IsLobby => AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined;

    public static ClientData GetClient(this PlayerControl player)
    {
        try
        {
            return AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
        }
        catch
        {
            return null;
        }
    }
    public static string GetFriendCode(this PlayerControl player)
    {
        if (player == null) return null;
        var client = player.GetClient();
        return client?.FriendCode;
    }

}
[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionShower_Start
{
    private static void Postfix(VersionShower __instance)
    {
        main.credentialsText = $"<b><size=80%><color={main.ModColor}>{main.ModName}</color>\n";
        var credentials = UnityEngine.Object.Instantiate(__instance.text);
        credentials.text = main.credentialsText;
        credentials.alignment = TextAlignmentOptions.Left;
        credentials.transform.position = new Vector3(2f, 2.51f, -2f);
        credentials.fontSize = credentials.fontSizeMax = credentials.fontSizeMin = 2f;
    }
}
[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();

    }
}
