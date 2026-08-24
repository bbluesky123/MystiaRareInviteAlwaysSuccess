using BepInEx;
using BepInEx.Unity.IL2CPP;
using DayScene.UI;
using GameData.Core.Collections.NightSceneUtility;
using GameData.Profile;
using HarmonyLib;
using System.Linq;

namespace MystiaRareInviteAlwaysSuccess;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BasePlugin
{
    internal static Plugin Instance { get; private set; }
    private Harmony _harmony;

    public override void Load()
    {
        Instance = this;
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        var inviteTarget = AccessTools.Method(
            typeof(DaySceneChatSelectionPannel),
            nameof(DaySceneChatSelectionPannel.Invite),
            new[]
            {
                typeof(string),
                typeof(int),
                typeof(Il2CppSystem.Action<Il2CppSystem.Action>)
            });

        if (inviteTarget == null)
        {
            Log.LogError("Invite target method was not found; the mod is inactive.");
            return;
        }

        var invitePrefix = AccessTools.Method(
            typeof(Patches.RareInvitePatch),
            nameof(Patches.RareInvitePatch.InvitePrefix));

        _harmony.Patch(inviteTarget, prefix: new HarmonyMethod(invitePrefix));

        var invitePatchInfo = Harmony.GetPatchInfo(inviteTarget);
        var isInvitePatched = invitePatchInfo?.Owners?.Contains(MyPluginInfo.PLUGIN_GUID) == true;
        if (!isInvitePatched)
        {
            Log.LogError("Invite patch registration could not be verified; the mod is inactive.");
            return;
        }

        // Keep the lower-level patch as a second line of defence. Some game builds
        // call this helper normally, while others inline it into Invite during IL2CPP
        // native compilation.
        var helperTarget = AccessTools.Method(
            typeof(DaySceneChatSelectionPannel),
            nameof(DaySceneChatSelectionPannel.InviteSpecGuest),
            new[]
            {
                typeof(SpecialGuest),
                typeof(int),
                typeof(DialogPackage).MakeByRefType()
            });

        if (helperTarget == null)
        {
            Log.LogWarning("InviteSpecGuest helper was not found; the primary Invite patch remains active.");
        }
        else
        {
            var helperPrefix = AccessTools.Method(
                typeof(Patches.RareInvitePatch),
                nameof(Patches.RareInvitePatch.InviteSpecGuestPrefix));

            _harmony.Patch(helperTarget, prefix: new HarmonyMethod(helperPrefix));

            var helperPatchInfo = Harmony.GetPatchInfo(helperTarget);
            var isHelperPatched = helperPatchInfo?.Owners?.Contains(MyPluginInfo.PLUGIN_GUID) == true;
            if (!isHelperPatched)
            {
                Log.LogWarning(
                    "InviteSpecGuest helper patch registration could not be verified; " +
                    "the primary Invite patch remains active.");
            }
        }

        Log.LogInfo(
            $"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded; " +
            $"patched {inviteTarget.DeclaringType?.FullName}.{inviteTarget.Name}.");
    }
}
