using System.Collections;
using UnityEngine;
using TMPro;

public class OutroCreditsUI : MonoBehaviour
{
    public static OutroCreditsUI Instance { get; private set; }
    public static bool IsActive { get; private set; } = false;

    [Header("Panel")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Scroll")]
    [SerializeField] private RectTransform creditsContainer;
    [SerializeField] private TextMeshProUGUI creditsText;

    [Header("Configuración")]
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float scrollSpeed = 70f;
    [SerializeField] private float holdBeforeScroll = 1.5f;
    [SerializeField] private float holdAfterScroll = 2f;

    [Header("Texto de créditos")]
    [SerializeField, TextArea(10, 20)]
    private string creditsContent =
        "Gracias por jugar\n\nMamá Satélite\n\n\n\nIntegrantes\n\n" +
        "Sebastian Capurro\n\nJose Luis Ressia\n\nJesus Arias\n\nGerman Heuer\n\n\n\n";

    private bool triggered = false;

    void Awake()
    {
        Instance = this;
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void Show()
    {
        if (triggered) return;
        triggered = true;
        IsActive = true;

        if (creditsText != null)
            creditsText.text = creditsContent;

        creditsPanel.SetActive(true);
        canvasGroup.alpha = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        StartCoroutine(CreditsSequence());
    }

    IEnumerator CreditsSequence()
    {
        // Esperar un frame para que TMPro calcule el layout
        yield return new WaitForEndOfFrame();

        float panelHalfHeight = ((RectTransform)creditsPanel.transform).rect.height / 2f;
        float contentHeight = creditsContainer.rect.height;

        float startY = -panelHalfHeight - contentHeight / 2f;
        float endY = panelHalfHeight + contentHeight / 2f;

        creditsContainer.anchoredPosition = new Vector2(0f, startY);

        // Fade in
        yield return StartCoroutine(Fade(0f, 1f));

        yield return new WaitForSeconds(holdBeforeScroll);

        // Scroll hacia arriba
        float totalDistance = endY - startY;
        float duration = totalDistance / scrollSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            creditsContainer.anchoredPosition = new Vector2(
                0f,
                Mathf.Lerp(startY, endY, elapsed / duration)
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        creditsContainer.anchoredPosition = new Vector2(0f, endY);

        yield return new WaitForSeconds(holdAfterScroll);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f));

        creditsPanel.SetActive(false);
        IsActive = false;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
