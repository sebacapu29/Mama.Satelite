using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VisionEffect : MonoBehaviour
{
    public Volume volume;

    private Vignette vignette;

    void Start()
    {
        volume.profile.TryGet(out vignette);
    }

    public void SetHideEffect(bool active)
    {
        if (vignette == null) return;

        vignette.intensity.value = active ? 0.5f : 0f;
    }
}