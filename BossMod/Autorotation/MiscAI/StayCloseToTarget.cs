﻿using System.Globalization;

namespace BossMod.Autorotation.MiscAI;

public sealed class StayCloseToTarget(RotationModuleManager manager, Actor player) : RotationModule(manager, player)
{

    public enum Tracks
    {
        Range
    }

    public enum RangeDefinition
    {
    }

    public static RotationModuleDefinition Definition()
    {
        RotationModuleDefinition def = new("Misc AI: Stay within range of target", "Module for use by AutoDuty preset.", "Misc", "veyn", RotationModuleQuality.Basic, new(~0ul), 1000);

        var configRef = def.Define(Tracks.Range).As<RangeDefinition>("range");

        for (float f = 1; f <= 30f; f = MathF.Round(f + 0.1f, 1))
        {
            configRef.AddOption((RangeDefinition)(f * 10f - 10f), f.ToString(CultureInfo.InvariantCulture));
        }

        return def;
    }

    public override void Execute(StrategyValues strategy, Actor? primaryTarget, float estimatedAnimLockDelay, bool isMoving)
    {
        if (primaryTarget != null)
            Hints.GoalZones.Add(Hints.GoalSingleTarget(primaryTarget.Position, (strategy.Option(Tracks.Range).Value.Option + 10f) / 10f + primaryTarget.HitboxRadius, 0.5f));
    }
}