using UnityEngine;
using TMPro;

/// <summary>
/// Singleton que controla el tooltip de instruccion en pantalla.
/// Asignarlo al GameObject raiz del panel de tooltip en la escena.
/// </summary>
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI label;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(string message)
    {
        label.text = message;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
