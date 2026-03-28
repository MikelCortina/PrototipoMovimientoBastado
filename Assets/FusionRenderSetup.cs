using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

public class FusionRenderSetup : NetworkBehaviour
{
    [Header("Renderers SOLO para Mí (Brazos FPS)")]
    public Renderer[] localRenderers;

    [Header("Renderers SOLO para los Demás (Cuerpo entero)")]
    public Renderer[] remoteRenderers;

    public bool keepShadowsForLocalPlayer = true;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            SetRenderersState(localRenderers, true, ShadowCastingMode.On);
            SetRenderersState(remoteRenderers, keepShadowsForLocalPlayer, keepShadowsForLocalPlayer ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On);
            if (keepShadowsForLocalPlayer) foreach (var r in remoteRenderers) r.enabled = true;
        }
        else
        {
            SetRenderersState(localRenderers, false, ShadowCastingMode.On);
            SetRenderersState(remoteRenderers, true, ShadowCastingMode.On);
        }
    }

    private void SetRenderersState(Renderer[] renderers, bool isEnabled, ShadowCastingMode shadowMode)
    {
        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                r.enabled = isEnabled;
                if (isEnabled) r.shadowCastingMode = shadowMode;
            }
        }
    }
}