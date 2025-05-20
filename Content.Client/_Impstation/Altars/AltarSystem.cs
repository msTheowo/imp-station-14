using System.ComponentModel;
using Content.Shared._Impstation.Altars;
using Microsoft.Win32;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.Altars;

public sealed class AltarSystem : VisualizerSystem<AltarComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AltarComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(EntityUid uid, AltarComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<AltarBoundUserInterface>(uid, AltarUiKey.Key, out var bui))
            bui.Update(component.Current);

        UpdateAppearance(uid, component);
    }

    protected override void OnAppearanceChange(EntityUid uid, AltarComponent component, ref AppearanceChangeEvent args)
    {
        UpdateAppearance(uid, component, args.Component, args.Sprite);
    }

    private void UpdateAppearance(EntityUid uid, AltarComponent altar, AppearanceComponent? appearance = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref appearance, ref sprite))
        {
            return;
        }

        if (altar.Current != null
            && _prototypeManager.TryIndex(altar.Current, out var proto))
        {
            sprite.LayerSetSprite(0, proto.Icon);
            if (proto.Lighting != null)
            {
                if (!sprite.TryGetLayer(1, out var spriteLayer))
                {
                    sprite.AddLayer(proto.Lighting);
                }
                else
                {
                    sprite.LayerSetSprite(1, proto.Lighting);
                }
                sprite.LayerSetShader(1, "unshaded");
            }
            else
            {
                if (sprite.TryGetLayer(1, out var spriteLayer))
                {
                    sprite.RemoveLayer(1);
                }
            }
        }
    }
}
