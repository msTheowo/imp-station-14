using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Altars;

[Prototype]
public sealed partial class AltarPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Icon { get; private set; } = default!;

    [DataField]
    public SpriteSpecifier Lighting { get; private set; } = default!;

    [DataField]
    public PointLightComponent PointLight { get; private set; } = default!;

    [DataField]
    public LocId Name { get; private set; } = "altar-component-name";

    [DataField]
    public LocId Description { get; private set; }

    [DataField]
    public bool Hidden { get; private set; }
}
