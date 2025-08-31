using BM_AntiCheat;
using HarmonyLib;
using InnerNet;
using JetBrains.Annotations;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static BM_AntiCheat.Translator;
using static Il2CppSystem.Globalization.CultureInfo;

namespace BM_AntiCheat
{
    public static class CheaterManager
    {
        private const string CheaterListPath = "./BM_AntiCheat_data/Cheater.txt";

        public static void Initialize()
        {
            try
            {
                Directory.CreateDirectory("BM_AntiCheat_data");

                if (!File.Exists(CheaterListPath))
                    File.Create(CheaterListPath).Close();
            }
            catch (Exception e)
            {
                Debug.LogError("[BM_AntiCheat] Failed to initialize: " + e);
            }
        }

        public static string GetResourcesTxt(string path)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            if (stream == null) return string.Empty;
            stream.Position = 0;
            using StreamReader reader = new(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public static void AddPlayer(ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost || player == null) return;

            string friendCode = player.FriendCode?.Trim();
            string playerName = player.PlayerName?.Trim();

            if (string.IsNullOrWhiteSpace(friendCode)) return;

            if (!CheckList(friendCode))
            {
                File.AppendAllText(CheaterListPath, $"{friendCode},{playerName}\n");
                if (main.kickCheater)
                {
                    AmongUsClient.Instance.KickPlayer(player.Id, false);
                }
                else if (main.banCheater)
                {
                    AmongUsClient.Instance.KickPlayer(player.Id, true);
                }
                else return;
            }
        }


        public static bool CheckList(string friendCode)
        {
            if (string.IsNullOrWhiteSpace(friendCode))
                return false;

            try
            {
                if (!File.Exists(CheaterListPath))
                    File.Create(CheaterListPath).Close();

                var lines = File.ReadAllLines(CheaterListPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length > 0 && parts[0].Trim().Equals(friendCode.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BM_AntiCheat] Error checking list: {e}");
            }

            return false;
        }

    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
static class OnPlayerJoinedPatch
{

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    static class OnPlayerJoined_ClientData_Patch
    {
        public static void Postfix([HarmonyArgument(0)] ClientData client)
        {

            if (CheaterManager.CheckList(client.FriendCode))
            {
                string msg = GetAuto(("PlayerSuspect"), client.PlayerName);
                HudManager.Instance.Notifier.AddDisconnectMessage(msg);
            }
            
        }
    }
}


