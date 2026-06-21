using UnityEngine;

public class HoverOutline : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private string hoverMessage = "Click para interactuar";

    private Renderer[] renderers;
    private Material outlineInstance;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        outlineInstance = new Material(outlineMaterial);
    }

    private void OnMouseEnter()
    {
        SetOutline(true);
    }

    private void OnMouseExit()
    {
        SetOutline(false);
    }

    public void SetOutline(bool enabled)
    {
        UpdateOutlineMaterials(enabled);
        UpdateTooltip(enabled);
    }

    private void UpdateOutlineMaterials(bool enabled)
    {
        foreach (var rend in renderers)
        {
            var mats = rend.materials;

            if (enabled)
            {
                if (mats[mats.Length - 1] == outlineInstance)
                    continue;

                Material[] newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);
                newMats[newMats.Length - 1] = outlineInstance;
                rend.materials = newMats;
            }
            else
            {
                if (mats.Length <= 1)
                    continue;

                Material[] newMats = new Material[mats.Length - 1];
                for (int i = 0; i < newMats.Length; i++)
                    newMats[i] = mats[i];

                rend.materials = newMats;
            }
        }
    }

    private void UpdateTooltip(bool enabled)
    {
        if (TooltipUI.Instance == null) return;

        if (enabled)
            TooltipUI.Instance.Show(hoverMessage);
        else
            TooltipUI.Instance.Hide();
    }
}
