using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Impstation.Altars;

public sealed class AltarSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<AltarComponent>(AltarUiKey.Key,
            subs =>
            {
                subs.Event<SetAltarMessage>(OnSetAltarMessage);
            });
    }

    private void OnSetAltarMessage(Entity<AltarComponent> ent, ref SetAltarMessage args)
    {
        if (!_prototypeManager.TryIndex(args.Altar, out var altarPrototype))
            return;

        SetAltar(ent, altarPrototype);
    }

    public void SetAltar(Entity<AltarComponent> ent, AltarPrototype newPrototype)
    {
        var meta = MetaData(ent);
        var name = Loc.GetString(newPrototype.Name);
        _metaData.SetEntityName(ent, name, meta);
        _metaData.SetEntityDescription(ent, Loc.GetString(newPrototype.Description), meta);

        ent.Comp.Current = newPrototype.ID;
        Dirty(ent);

        if (ent.Comp.Current != null
            && _prototypeManager.TryIndex(ent.Comp.Current, out var proto))
        {
            EnsureComp<PointLightComponent>(ent);
            if (proto.PointLight.Enabled)
            {
                _lights.SetColor(ent, proto.PointLight.Color);
                _lights.SetRadius(ent, proto.PointLight.Radius);
                _lights.SetEnergy(ent, proto.PointLight.Energy);
            }
            else
            {
                _lights.SetRadius(ent, 0);
            }
        }
    }

    public static List<AltarPrototype> GetAllAltars(IPrototypeManager prototypeManager)
    {
        return prototypeManager
            .EnumeratePrototypes<AltarPrototype>()
            .Where(p => !p.Hidden)
            .ToList();
    }
}
