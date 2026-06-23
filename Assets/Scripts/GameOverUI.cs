using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Muestra el canvas de derrota ("PERDISTE") y recarga la escena inicial tras un delay.
/// Asociar al GameObject raíz del Canvas de Game Over en la escena.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Canvas")]
    [Tooltip("El Canvas (o panel raíz) con el mensaje PERDISTE. Debe estar desactivado al inicio.")]
    [SerializeField] GameObject panelRoot;

    [Header("Barra de progreso (opcional)")]
    [Tooltip("Image de tipo Filled que muestra cuánto falta para el game over. Puede ser null.")]
    [SerializeField] Image stareProgressBar;
    [Tooltip("Referencia a SalomeVision para leer el progreso del timer.")]
    [SerializeField] SalomeVision salomeVision;

    [Header("Recarga")]
    [Tooltip("Nombre exacto de la escena a cargar al reiniciar.")]
    [SerializeField] string sceneToReload = "Floor3";
    [Tooltip("Segundos que el panel permanece visible antes de recargar la escena.")]
    [SerializeField] float delayBeforeReload = 3f;

    bool _shown;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (stareProgressBar != null)
            stareProgressBar.fillAmount = 0f;
    }

    void Update()
    {
        if (_shown || salomeVision == null || stareProgressBar == null) return;
        stareProgressBar.fillAmount = salomeVision.StareProgress;
    }

    public void Show()
    {
        if (_shown) return;
        _shown = true;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ReloadAfterDelay());
    }

    IEnumerator ReloadAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeReload);
        SceneManager.LoadScene(sceneToReload);
    }

    /// <summary>Llamado desde el botón "Reintentar" del Canvas (opcional).</summary>
    public void OnRetryButtonPressed()
    {
        SceneManager.LoadScene(sceneToReload);
    }
}
