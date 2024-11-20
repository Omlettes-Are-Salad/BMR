﻿namespace BossMod.Dawntrail.Dungeon.D09YuweyawataFieldStation.D091LindblumZaghnal;

public enum OID : uint
{
    Boss = 0x4641, // R9.0
    RawElectrope = 0x4642, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    Teleport = 40622, // Boss->location, no cast, single-target

    ElectricalOverload = 40635, // Boss->self, 5.0s cast, range 40 circle

    Gore1 = 40630, // Boss->self, 3.0s cast, single-target
    Gore2 = 41266, // Boss->self, 3.0s cast, single-target
    CaberToss = 40624, // Boss->self, 19.0s cast, single-target
    LineVoltageWide1 = 41121, // Helper->self, 3.3s cast, range 50 width 10 rect
    LineVoltageWide2 = 40627, // Helper->self, 3.5s cast, range 50 width 10 rect
    LineVoltageNarrow1 = 41122, // Helper->self, 3.0s cast, range 50 width 5 rect
    LineVoltageNarrow2 = 40625, // Helper->self, 4.0s cast, range 50 width 5 rect
    CellShock = 40626, // Helper->self, 2.0s cast, range 26 circle

    LightningStormVisual = 40636, // Boss->self, 4.5s cast, single-target
    LightningStorm = 40637, // Helper->player, 5.0s cast, range 5 circle, spread

    SparkingFissureVisual = 40632, // Boss->self, 13.0s cast, single-target
    SparkingFissure = 41258, // Helper->self, 13.7s cast, range 40 circle
    SparkingFissureFirst = 41267, // Helper->self, 5.2s cast, range 40 circle
    SparkingFissureRepeat = 40631, // Helper->self, no cast, range 40 circle

    LightningBolt = 40638, // Helper->location, 5.0s cast, range 6 circle
    Electrify = 40634, // RawElectrope->self, 16.0s cast, range 40 circle
}

abstract class LineVoltage(BossModule module, AID aid, float halfWidth) : Components.SelfTargetedAOEs(module, ActionID.MakeSpell(aid), new AOEShapeRect(50, halfWidth));
class LineVoltageWide1(BossModule module) : LineVoltage(module, AID.LineVoltageWide1, 5);
class LineVoltageWide2(BossModule module) : LineVoltage(module, AID.LineVoltageWide2, 5);
class LineVoltageNarrow1(BossModule module) : LineVoltage(module, AID.LineVoltageNarrow1, 2.5f);
class LineVoltageNarrow2(BossModule module) : LineVoltage(module, AID.LineVoltageNarrow2, 2.5f);

class LightningBolt(BossModule module) : Components.LocationTargetedAOEs(module, ActionID.MakeSpell(AID.LightningBolt), 6);
class LightningStorm(BossModule module) : Components.SpreadFromCastTargets(module, ActionID.MakeSpell(AID.LightningStorm), 5);
class ElectricalOverload(BossModule module) : Components.RaidwideCast(module, ActionID.MakeSpell(AID.ElectricalOverload));
class SparkingFissure(BossModule module) : Components.RaidwideCast(module, ActionID.MakeSpell(AID.SparkingFissure));
class SparkingFissureFirst(BossModule module) : Components.RaidwideCast(module, ActionID.MakeSpell(AID.SparkingFissureFirst));

class CellShock(BossModule module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle Circle = new(26);
    private AOEInstance? _aoe;

    private static readonly Dictionary<byte, WPos> initialPositions = new()
    {
        { 0x0D, new(81.132f, 268.868f) }, { 0x0E, new(81.132f, 285.132f) },
        { 0x0F, new(64.868f, 268.868f) }, { 0x10, new(64.868f, 285.132f) }
    };

    private static readonly Dictionary<byte, byte> pairsWithSamePositions = new()
    {
        { 0x0D, 0x10 }, { 0x0E, 0x0F }, { 0x0F, 0x0E }, { 0x10, 0x0D }
    };

    public override IEnumerable<AOEInstance> ActiveAOEs(int slot, Actor actor) => Utils.ZeroOrOne(_aoe);

    public override void OnEventEnvControl(byte index, uint state)
    {
        if (state is 0x00020001 or 0x00200010)
        {
            if (state == 0x00200010 && pairsWithSamePositions.TryGetValue(index, out var remappedIndex))
                index = remappedIndex;

            if (initialPositions.TryGetValue(index, out var position))
                _aoe = new(Circle, position, default, WorldState.FutureTime(8));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.CellShock)
            _aoe = null;
    }
}

class D091LindblumZaghnalStates : StateMachineBuilder
{
    public D091LindblumZaghnalStates(BossModule module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CellShock>()
            .ActivateOnEnter<LineVoltageNarrow1>()
            .ActivateOnEnter<LineVoltageNarrow2>()
            .ActivateOnEnter<LineVoltageWide1>()
            .ActivateOnEnter<LineVoltageWide2>()
            .ActivateOnEnter<LightningBolt>()
            .ActivateOnEnter<LightningStorm>()
            .ActivateOnEnter<ElectricalOverload>()
            .ActivateOnEnter<SparkingFissure>()
            .ActivateOnEnter<SparkingFissureFirst>();
    }
}

[ModuleInfo(BossModuleInfo.Maturity.Verified, Contributors = "The Combat Reborn Team (Malediktus)", GroupType = BossModuleInfo.GroupType.CFC, GroupID = 1008, NameID = 13623)]
public class D091LindblumZaghnal(WorldState ws, Actor primary) : BossModule(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsComplex arena = new([new Polygon(new(73, 277), 19.5f * CosPI.Pi40th, 40)], [new Rectangle(new(72, 297), 20, 1.1f),
    new Rectangle(new(72, 257), 20, 1.05f)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies(OID.RawElectrope).Concat([PrimaryActor]));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        for (var i = 0; i < hints.PotentialTargets.Count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = (OID)e.Actor.OID switch
            {
                OID.RawElectrope => 1,
                _ => 0
            };
        }
    }
}