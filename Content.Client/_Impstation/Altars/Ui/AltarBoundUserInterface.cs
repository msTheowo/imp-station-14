using System.Linq;
using Content.Client._Impstation.Altars.Ui;
using Content.Shared._Impstation.Altars;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.Altars;

public sealed class AltarBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private AltarMenu? _menu;

    protected override void Open()
    {
        base.Open();

        var altar = EntMan.GetComponentOrNull<AltarComponent>(Owner)?.Current is { } current
            ? _prototype.Index(current)
            : null;
        var allAltars = Shared._Impstation.Altars.AltarSystem.GetAllAltars(_prototype)
            .OrderBy(p => Loc.GetString(p.Name))
            .ToList();
        _menu = new(altar, allAltars);

        _menu.OnAltarSelected += id =>
        {
            SendMessage(new SetAltarMessage(id));
        };

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    public void Update(ProtoId<AltarPrototype>? altar)
    {
        if (_prototype.TryIndex(altar, out var altarPrototype))
            _menu?.UpdateState(altarPrototype);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
