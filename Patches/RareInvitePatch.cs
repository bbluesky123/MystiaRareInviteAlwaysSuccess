using GameData.Core.Collections.NightSceneUtility;
using GameData.Profile;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace MystiaRareInviteAlwaysSuccess.Patches;

internal static class RareInvitePatch
{
    private const int GuaranteedInviteKizunaLevel = 5;

    internal static void InvitePrefix(string characterLabel, ref int currentKizunaLevel)
    {
        var originalKizunaLevel = currentKizunaLevel;
        currentKizunaLevel = GuaranteedInviteKizunaLevel;

        Plugin.Instance?.Log.LogInfo(
            $"Intercepted rare guest invite request: {characterLabel}, " +
            $"originalKizuna={originalKizunaLevel}, effectiveKizuna={currentKizunaLevel}.");
    }

    internal static bool InviteSpecGuestPrefix(
        SpecialGuest specialGuest,
        int kizunaLevel,
        ref DialogPackage selectedDialogue,
        ref bool __result)
    {
        if (specialGuest == null)
        {
            Plugin.Instance?.Log.LogWarning(
                "InviteSpecGuest was called without a special guest; using the original method.");
            return true;
        }

        Il2CppReferenceArray<DialogPackage> successDialogues;
        try
        {
            successDialogues = specialGuest.GetInviteDialogPackageAtKizunaLevel(kizunaLevel, true);
        }
        catch (System.Exception exception)
        {
            Plugin.Instance?.Log.LogError(
                $"Could not obtain invite-success dialogue for {specialGuest.StringId}, " +
                $"kizuna={kizunaLevel}: {exception}");
            return true;
        }

        if (successDialogues == null || successDialogues.Length <= 0)
        {
            Plugin.Instance?.Log.LogWarning(
                $"No invite-success dialogue exists for {specialGuest.StringId}, " +
                $"kizuna={kizunaLevel}; using the original method.");
            return true;
        }

        var index = successDialogues.Length == 1 ? 0 : Random.Range(0, successDialogues.Length);
        selectedDialogue = successDialogues[index];
        __result = true;

        Plugin.Instance?.Log.LogInfo(
            $"Forced rare guest invite success before the original roll: " +
            $"{specialGuest.StringId}, kizuna={kizunaLevel}, dialogueIndex={index}.");

        // Skip the original random invite roll. The caller receives a successful
        // result and continues through the game's normal invitation-recording flow.
        return false;
    }
}
