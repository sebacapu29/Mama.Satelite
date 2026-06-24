using UnityEngine;

public class OutroCreditsTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [Header("Renderers a apagar al contacto")]
    [Tooltip("Arrastrá aquí los Renderers del plano/objeto que tienen el shader/material a desactivar.")]
    [SerializeField] private Renderer[] renderersToDisable;

    void Start()
    {
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        DisableRenderers();

        if (OutroCreditsUI.Instance != null)
            OutroCreditsUI.Instance.Show();

        enabled = false;
    }

    private void DisableRenderers()
    {
        if (renderersToDisable == null) return;
        foreach (var r in renderersToDisable)
        {
            if (r != null)
                r.enabled = false;
        }
    }
}
