using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Altars;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AltarComponent : Component
{
    /// <summary>
    /// The current altar prototype being displayed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AltarPrototype>? Current;
}

[Serializable, NetSerializable]
public enum AltarUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SetAltarMessage(ProtoId<AltarPrototype> altar) : BoundUserInterfaceMessage
{
    public ProtoId<AltarPrototype> Altar = altar;
}
