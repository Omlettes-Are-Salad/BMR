﻿using ImGuiNET;
using Dalamud.Interface.Utility.Raii;
using System.Globalization;

namespace BossMod.AI;

sealed class AIManagementWindow : UIWindow
{
    private readonly AIConfig _config;
    private readonly AIManager _manager;
    private readonly EventSubscriptions _subscriptions;
    private const string _title = $"AI: off{_windowID}";
    private const string _windowID = "###AI debug window";
    DateTime? saveConfigIn;

    public AIManagementWindow(AIManager manager) : base(_windowID, false, new(100, 100))
    {
        WindowName = _title;
        _config = Service.Config.Get<AIConfig>();
        _manager = manager;
        _subscriptions = new
        (
            _config.Modified.ExecuteAndSubscribe(() => IsOpen = _config.DrawUI)
        );
        RespectCloseHotkey = false;
    }

    protected override void Dispose(bool disposing)
    {
        _subscriptions.Dispose();
        base.Dispose(disposing);
    }

    public void SetVisible(bool vis)
    {
        if (_config.DrawUI != vis)
        {
            _config.DrawUI = vis;
            _config.Modified.Fire();
        }
    }

    public override void Draw()
    {
        var configModified = false;

        ImGui.TextUnformatted($"Navi={_manager.Controller.NaviTargetPos}");
        _manager.Beh?.DrawDebug();

        configModified |= ImGui.Checkbox("Forbid actions", ref _config.ForbidActions);
        ImGui.SameLine();
        configModified |= ImGui.Checkbox("Forbid movement", ref _config.ForbidMovement);
        ImGui.SameLine();
        configModified |= ImGui.Checkbox("Forbid mounted movement", ref _config.ForbidAIMovementMounted);
        ImGui.SameLine();
        configModified |= ImGui.Checkbox("Follow during combat", ref _config.FollowDuringCombat);
        ImGui.Spacing();
        configModified |= ImGui.Checkbox("Follow during active boss module", ref _config.FollowDuringActiveBossModule);
        ImGui.SameLine();
        configModified |= ImGui.Checkbox("Follow out of combat", ref _config.FollowOutOfCombat);
        ImGui.SameLine();
        configModified |= ImGui.Checkbox("Follow target", ref _config.FollowTarget);
        ImGui.Spacing();
        configModified |= ImGui.Checkbox("Manual targeting", ref _config.ManualTarget);
        ImGui.SameLine();
        configModified |= ImGui.Checkbox("Override autorotation values", ref _config.OverrideAutorotation);
        ImGui.SameLine();
        configModified |= ImGui.Checkbox("Allow outside bounds", ref _config.AllowAIToBeOutsideBounds);

        ImGui.Text("Follow party slot");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(250);
        ImGui.SetNextWindowSizeConstraints(new Vector2(0, 0), new Vector2(float.MaxValue, ImGui.GetTextLineHeightWithSpacing() * 50));
        if (ImRaii.Combo("##Leader", _manager.Beh == null ? "<idle>" : _manager.WorldState.Party[_manager.MasterSlot]?.Name ?? "<unknown>"))
        {
            if (ImGui.Selectable("<idle>", _manager.Beh == null))
                _manager.SwitchToIdle();
            foreach (var (i, p) in _manager.WorldState.Party.WithSlot(true))
            {
                if (ImGui.Selectable(p.Name, _manager.MasterSlot == i))
                {
                    var cfg = _config.FollowSlot;
                    _manager.SwitchToFollow(i);
                    _config.FollowSlot = i;
                    if (cfg != _config.FollowSlot)
                        configModified = true;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.Separator();
        ImGui.Text("Desired positional");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        var positionalOptions = Enum.GetNames(typeof(Positional));
        var positionalIndex = (int)_config.DesiredPositional;
        if (ImGui.Combo("##DesiredPositional", ref positionalIndex, positionalOptions, positionalOptions.Length))
        {
            var cfg = _config.DesiredPositional;
            _config.DesiredPositional = (Positional)positionalIndex;
            if (cfg != _config.DesiredPositional)
                configModified = true;
        }
        ImGui.SameLine();
        ImGui.Text("Max distance - to targets");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        var maxDistanceTargetStr = _config.MaxDistanceToTarget.ToString(CultureInfo.InvariantCulture);
        if (ImGui.InputText("##MaxDistanceToTarget", ref maxDistanceTargetStr, 64))
        {
            maxDistanceTargetStr = maxDistanceTargetStr.Replace(',', '.');
            if (float.TryParse(maxDistanceTargetStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var maxDistance))
            {
                var cfg = _config.MaxDistanceToTarget;
                _config.MaxDistanceToTarget = maxDistance;
                if (cfg != _config.MaxDistanceToTarget)
                    configModified = true;
            }
        }
        ImGui.SameLine();
        ImGui.Text("- to slots");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        var maxDistanceSlotStr = _config.MaxDistanceToSlot.ToString(CultureInfo.InvariantCulture);
        if (ImGui.InputText("##MaxDistanceToSlot", ref maxDistanceSlotStr, 64))
        {
            maxDistanceSlotStr = maxDistanceSlotStr.Replace(',', '.');
            if (float.TryParse(maxDistanceSlotStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var maxDistance))
            {
                var cfg = _config.MaxDistanceToSlot;
                _config.MaxDistanceToSlot = maxDistance;
                if (cfg != _config.MaxDistanceToTarget)
                    configModified = true;
            }
        }

        ImGui.Text("Movement decision delay");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        var movementDelayStr = _config.MoveDelay.ToString(CultureInfo.InvariantCulture);
        if (ImGui.InputText("##MovementDelay", ref movementDelayStr, 64))
        {
            movementDelayStr = movementDelayStr.Replace(',', '.');
            if (float.TryParse(movementDelayStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay))
            {
                var cfg = _config.MoveDelay;
                _config.MoveDelay = delay;
                if (cfg != _config.MoveDelay)
                    configModified = true;
            }
        }
        ImGui.SameLine();
        ImGui.Text("Autorotation AI preset");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(250);
        ImGui.SetNextWindowSizeConstraints(default, new Vector2(float.MaxValue, ImGui.GetTextLineHeightWithSpacing() * 50));
        var aipreset = _config.AIAutorotPresetName;
        var presets = _manager.Autorot.Database.Presets.VisiblePresets;
        var presetNames = presets.Select(p => p.Name).ToList();
        if (aipreset != null)
            presetNames.Add("Deactivate");

        var selectedIndex = presetNames.IndexOf(aipreset ?? "");
        if (selectedIndex == -1 && _manager.AiPreset == null)
            selectedIndex = -1;
        if (ImGui.Combo("##AI preset", ref selectedIndex, [.. presetNames], presetNames.Count))
        {
            var cfg = _config.AIAutorotPresetName;
            if (selectedIndex == presetNames.Count - 1 && aipreset != null)
            {
                _manager.SetAIPreset(null);
                _config.AIAutorotPresetName = null;
                selectedIndex = -1;
            }
            else if (selectedIndex >= 0 && selectedIndex < presets.Count())
            {
                var selectedPreset = presets.ElementAt(selectedIndex);
                _manager.SetAIPreset(selectedPreset);
                _config.AIAutorotPresetName = selectedPreset.Name;
            }
            if (cfg != _config.AIAutorotPresetName)
                configModified = true;
        }
        if (configModified)
            saveConfigIn = DateTime.Now.AddSeconds(10); // delay config saving to potentially save multiple setting changes in a batch
        if (saveConfigIn <= DateTime.Now)
        {
            _config.Modified.Fire();
            saveConfigIn = null;
        }
    }

    public override void OnClose() => SetVisible(false);

    public void UpdateTitle()
    {
        var masterSlot = _manager?.MasterSlot ?? -1;
        var masterName = _manager?.Autorot?.WorldState?.Party[masterSlot]?.Name ?? "unknown";
        var masterSlotNumber = masterSlot != -1 ? (masterSlot + 1).ToString() : "N/A";

        WindowName = $"AI: {(_manager?.Beh != null ? "on" : "off")}, master={masterName}[{masterSlotNumber}]{_windowID}";
    }
}
