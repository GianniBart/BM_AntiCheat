using AmongUs.GameOptions;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils;
using BM_AntiCheat;
using HarmonyLib;
using Hazel;
using InnerNet;
using Internal.Threading.Tasks.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.ProBuilder;
using static BM_AntiCheat.AntiCheat;
using static BM_AntiCheat.Translator;
using static BM_AntiCheat.Utils;

namespace BM_AntiCheat
{
    public class AntiCheat
    {
        private static readonly Dictionary<byte, int> taskRpcCount = new();
        private static readonly Dictionary<byte, System.DateTime> taskRpcLastTime = new();

        public static class VentAntiCheat
        {
            private static readonly Dictionary<byte, int> ventRemovalsThisTick = new();
            private static float lastCheckTime = 0;

            public static readonly HashSet<byte> warnedPlayersVent = new();

            public static void RegisterVentBoot(PlayerControl player)
            {
                if (player == null || !AmongUsClient.Instance.AmHost) return;

                if (Time.time - lastCheckTime > Time.fixedDeltaTime)
                {
                    ventRemovalsThisTick.Clear();
                    lastCheckTime = Time.time;
                }

                byte pid = player.PlayerId;
                if (!ventRemovalsThisTick.ContainsKey(pid))
                    ventRemovalsThisTick[pid] = 0;

                ventRemovalsThisTick[pid]++;

                if (ventRemovalsThisTick[pid] > 1)
                {
                    if (!warnedPlayersVent.Contains(pid))
                    {
                        warnedPlayersVent.Add(pid);
                        string msg = string.Format((player.Data?.PlayerName) + GetAuto("VentCheat"));
                        HudManager.Instance.Notifier.AddDisconnectMessage(msg);
                        SendGlobalHackWarning();
                        main.Logger.LogWarning(GetAuto(msg));
                        ClientData client = Utils.GetClient(player);
                        if (client != null)
                        {
                            CheaterManager.AddPlayer(client);
                        }
                    }
                }
            }

        }
        public static void SendGlobalHackWarning(ulong friendCode = 0)
        {
            if (main.hasSentHackWarning) return;
            if (!main.SentWarning) return;
            main.hasSentHackWarning = true;
            string msg = GetAuto("PrincipalMessage");
            PlayerControl.LocalPlayer.RpcSendChat(msg);
        }
        public static void PlayerControlReceiveRpc(PlayerControl pc, byte callId, MessageReader reader)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (pc == PlayerControl.LocalPlayer) return;
            if (pc == null || reader == null) return;

            try
            {
                var rpc = (RpcCalls)callId;

                switch (rpc)
                {
                    case RpcCalls.CompleteTask:
                        var nowTask = System.DateTime.UtcNow;
                        byte taskId = pc.PlayerId;

                        if (!taskRpcLastTime.ContainsKey(taskId))
                        {
                            taskRpcCount[taskId] = 0;
                            taskRpcLastTime[taskId] = nowTask;
                        }

                        if ((nowTask - taskRpcLastTime[taskId]).TotalSeconds < 0.5)
                        {
                            taskRpcCount[taskId]++;
                            if (taskRpcCount[taskId] > 1)
                            {
                                string msg = string.Format((pc.Data.PlayerName) + GetAuto("TaskCheat"));
                                HudManager.Instance.Notifier.AddDisconnectMessage(msg);
                                SendGlobalHackWarning();
                                main.Logger.LogWarning(msg);
                                taskRpcCount[taskId] = 0;
                                ClientData client = Utils.GetClient(pc);
                                if (client != null)
                                {
                                    CheaterManager.AddPlayer(client);
                                }
                            }
                        }
                        else
                        {
                            taskRpcCount[taskId] = 1;
                        }

                        taskRpcLastTime[taskId] = nowTask;
                        break;

                    default:
                        break;
                }
            }
            catch (System.Exception ex)
            {
                HudManager.Instance.Notifier.AddDisconnectMessage(
                    $"Error AntiCheat: {ex.Message}");
                SendGlobalHackWarning();
                main.Logger.LogWarning("BM_AntiCheat Error");
            }
        }

    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class MovementDetectionPatch
{
    private static readonly Dictionary<byte, Vector2> lastPosition = new();
    private static readonly Dictionary<byte, bool> lastInVentState = new(); // Nuovo stato per le ventole

    public static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (__instance == null || __instance.Data == null) return;
        if (__instance == PlayerControl.LocalPlayer) return;
        if (__instance.Data.IsDead || __instance.Data.Disconnected) return;

        byte pid = __instance.PlayerId;
        Vector2 currentPos = __instance.GetTruePosition();

        lastInVentState.TryGetValue(pid, out bool prevInVent);

        if (__instance.inVent && !prevInVent)
        {
            bool isAuthorized = Utils.Impostor(__instance) || Utils.Phantom(__instance) || Utils.Shapeshifter(__instance) || Utils.Engineer(__instance);

            if (!isAuthorized)
            {
                string msg = string.Format((__instance.Data.PlayerName) + GetAuto("VentCheat1"));
                HudManager.Instance.Notifier.AddDisconnectMessage(msg);
                SendGlobalHackWarning();
                main.Logger.LogWarning(msg);
                ClientData client = Utils.GetClient(__instance);
                if (client != null)
                {
                    CheaterManager.AddPlayer(client);
                }

            }
        }

        // Aggiorna l'ultimo stato delle ventole
        lastInVentState[pid] = __instance.inVent;

        if (lastPosition.TryGetValue(pid, out Vector2 prevPos))
        {
            float dist = Vector2.Distance(prevPos, currentPos);
            bool isAuthorized = Utils.Impostor(__instance) || Utils.Phantom(__instance) || Utils.Shapeshifter(__instance) || Utils.Engineer(__instance);

            if (!isAuthorized && Utils.IsInTask && Utils.IsCanMove && dist > 1.5f)
            {
                string msg = string.Format((__instance.Data.PlayerName) + GetAuto("SnapToCheat"));
                HudManager.Instance.Notifier.AddDisconnectMessage(msg);
                SendGlobalHackWarning();
                main.Logger.LogWarning(msg);
                ClientData client = Utils.GetClient(__instance);
                if (client != null)
                {
                    CheaterManager.AddPlayer(client);
                }
            }
        }

        lastPosition[pid] = currentPos;
    }
}


[HarmonyPatch]
public static class ShipStatus_UpdateSystem_Patch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(ShipStatus), "UpdateSystem", new Type[] {
            typeof(SystemTypes),
            typeof(PlayerControl),
            typeof(Hazel.MessageReader)
        });
    }

    public static void Prefix(
        ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] Hazel.MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost || player == null || reader == null) return;

        SystemTypes[] monitoredSystems = new SystemTypes[]
        {
        SystemTypes.Electrical,
        SystemTypes.LifeSupp,
        SystemTypes.Comms,
        SystemTypes.Doors,
        SystemTypes.Sabotage,
        SystemTypes.Laboratory,
        SystemTypes.HeliSabotage,
        SystemTypes.MushroomMixupSabotage,
        SystemTypes.Reactor,
        SystemTypes.Ventilation // controlliamo anche ventilazione
        };

        if (Array.IndexOf(monitoredSystems, systemType) == -1) return;

        string playerName = player.Data?.PlayerName ?? "Unknown";
        RoleTypes playerRole = player.Data?.Role?.Role ?? RoleTypes.Crewmate;
        bool isAuthorized = Utils.Impostor(player) || Utils.Phantom(player) || Utils.Shapeshifter(player);

        if (systemType == SystemTypes.Ventilation)
        {
            VentAntiCheat.RegisterVentBoot(player);
        }
        else
        {
            int originalPos = reader.Position;
            byte amount = reader.ReadByte();
            reader.Position = originalPos;

            if ((amount == 128 || amount == 69) && !isAuthorized)
            {
                string msg = string.Format((playerName) + GetAuto("SabotageCheat"));
                HudManager.Instance.Notifier.AddDisconnectMessage(msg);
                SendGlobalHackWarning();
                main.Logger.LogWarning(msg);
                ClientData client = Utils.GetClient(player);
                if (client != null)
                {
                    CheaterManager.AddPlayer(client);
                }

            }
        }
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnDestroy))]
public static class GameEndPatchBM_AntiCheat
{
    private static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        main.hasSentHackWarning = false;
        VentAntiCheat.warnedPlayersVent.Clear();
    }
}

//[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
//class ChatReceiveLengthPatch
//{
//    static void Prefix(PlayerControl sourcePlayer, string chatText)
//    {
//        if (sourcePlayer == PlayerControl.LocalPlayer) return;
//        if (chatText.Length > 100)
//        {
//            string msg = string.Format((sourcePlayer.Data.PlayerName) + GetAuto("ChatCheat"));
//            HudManager.Instance.Notifier.AddDisconnectMessage(msg);
//            SendGlobalHackWarning();
//            main.Logger.LogWarning(msg);
//        }
//    }
//}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
    {
        AntiCheat.PlayerControlReceiveRpc(__instance, callId, reader);
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class MovementDuringMeetingPatch
{
    public static readonly Dictionary<byte, Vector2> lastPosition = new();

    public static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (__instance == null || __instance.Data == null) return;
        if (__instance.Data.IsDead || __instance == PlayerControl.LocalPlayer || __instance.Data.Disconnected) return;


        if (MeetingHud.Instance)
        {
            Vector2 currentPos = __instance.GetTruePosition();

            if (lastPosition.TryGetValue(__instance.PlayerId, out Vector2 prevPos))
            {
                float dist = Vector2.Distance(prevPos, currentPos);

                if (dist > 0.1f)
                {
                    string msg = $"{__instance.Data.PlayerName} {GetAuto("MoveDuringMeetingCheat")}";
                    HudManager.Instance.Notifier.AddDisconnectMessage(msg);
                    SendGlobalHackWarning();
                    main.Logger.LogWarning(msg);
                    ClientData client = Utils.GetClient(__instance);
                    if (client != null)
                    {
                        CheaterManager.AddPlayer(client);
                    }
                }
            }

            lastPosition[__instance.PlayerId] = currentPos;
        }
        else
        {
            lastPosition.Remove(__instance.PlayerId);
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
public static class CloseDoorsOfTypePatch
{
    private static int closeDoorsCount = 0;
    private static float firstCallTime = 0f;
    private static bool warningSent = false;

    public static void Prefix(ShipStatus __instance, SystemTypes room)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        float now = Time.time;

        if (firstCallTime == 0f || now - firstCallTime > 1f)
        {
            // Reset counter ogni 1 secondo
            closeDoorsCount = 0;
            firstCallTime = now;
            warningSent = false;
        }

        closeDoorsCount++;

        // Se vengono chiuse più di 3 porte in meno di 1 secondo e non abbiamo ancora avvisato
        if (closeDoorsCount > 3 && !warningSent)
        {
            string msg = GetAuto("AnonimousCheat");
            HudManager.Instance.Notifier.AddDisconnectMessage(msg);
            main.Logger.LogWarning(msg);

            // Puoi chiamare anche qui un metodo di alert globale se vuoi
            AntiCheat.SendGlobalHackWarning();


            warningSent = true;
        }
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class MovementDuringMeetingPatch1
{
    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        // Avvia la coroutine solo sull'host
        AmongUsClient.Instance.StartCoroutine(WaitAndCapturePositions());
    }

    private static IEnumerator WaitAndCapturePositions()
    {
        yield return new WaitForSeconds(1f); // Attesa di 2 secondi

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Data == null || player.Data.IsDead || player.Data.Disconnected)
                continue;

            Vector2 pos = player.GetTruePosition();

            MovementDuringMeetingPatch.lastPosition[player.PlayerId] = pos;

        }
    }
}