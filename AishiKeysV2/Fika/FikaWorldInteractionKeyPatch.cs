using AishiKeysPro;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Linq;

namespace AishiKeys.Fika
{
    [HarmonyPatch(typeof(WorldInteractionPacket), nameof(WorldInteractionPacket.Execute))]
    internal static class FikaWorldInteractionKeyPatch
    {
        private sealed class DoorKeyState
        {
            internal WorldInteractiveObject Door;
            internal string OriginalKeyId;
        }

        [HarmonyPrefix]
        private static void Prefix(
            WorldInteractionPacket __instance,
            FikaPlayer player,
            out DoorKeyState __state)
        {
            __state = null;

            if (__instance == null ||
                player == null ||
                __instance.InteractionType != EInteractionType.Unlock ||
                __instance.InteractionStage != EInteractionStage.Start ||
                string.IsNullOrEmpty(__instance.ItemId) ||
                string.IsNullOrEmpty(__instance.InteractiveId))
            {
                return;
            }

            Item keyItem = player.Profile != null && player.Profile.Inventory != null
                ? player.Profile.Inventory
                    .GetPlayerItems((EPlayerItems)63)
                    .FirstOrDefault(item =>
                        item != null &&
                        string.Equals(item.Id, __instance.ItemId, StringComparison.Ordinal))
                : null;

            if (keyItem == null ||
                !AishiKeysMod.IsApprovedMasterKeyTemplate(keyItem.TemplateId))
            {
                return;
            }

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            WorldInteractiveObject door = gameWorld != null
                ? gameWorld.FindDoor(__instance.InteractiveId)
                : null;

            if (door == null)
                return;

            __state = new DoorKeyState
            {
                Door = door,
                OriginalKeyId = door.KeyId
            };

            door.KeyId = keyItem.TemplateId;
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(
            Exception __exception,
            DoorKeyState __state)
        {
            if (__state != null && __state.Door != null)
                __state.Door.KeyId = __state.OriginalKeyId;

            return __exception;
        }
    }
}
