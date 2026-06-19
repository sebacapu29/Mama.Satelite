using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class IntroUI : MonoBehaviour
{
    public static bool IsActive { get; private set; } = false;

    // Persiste entre cargas de escena; se resetea solo al cerrar el juego
    private static bool alreadyShown = false;

    [Header("Panel")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Texto")]
    [SerializeField] private TextMeshProUGUI skipHintText;
    [SerializeField] private string skipHintMessage = "Presioná cualquier tecla para continuar...";

    [Header("Configuración")]
    [SerializeField] private float displayDuration = 12f;
    [SerializeField] private float fadeDuration = 0.6f;

    private Coroutine runningCoroutine;
    private bool dismissed;

    void Awake()
    {
        if (alreadyShown)
        {
            // Ya se mostró en una escena anterior — desactivar el panel y este componente
            if (introPanel != null)
                introPanel.SetActive(false);
            enabled = false;
            return;
        }

        alreadyShown = true;
        IsActive = true;
    }

    void Start()
    {
        if (!enabled) return;

        if (introPanel != null)
            introPanel.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (skipHintText != null)
            skipHintText.text = skipHintMessage;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        runningCoroutine = StartCoroutine(IntroSequence());
    }

    void Update()
    {
        if (!dismissed && introPanel != null && introPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                Dismiss();
        }
    }

    IEnumerator IntroSequence()
    {
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f));

        // Esperar el timer (el Update maneja el skip anticipado)
        yield return new WaitForSeconds(displayDuration);

        Dismiss();
    }

    void Dismiss()
    {
        if (dismissed) return;
        dismissed = true;

        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }

        StartCoroutine(FadeOutAndClose());
    }

    IEnumerator FadeOutAndClose()
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        yield return StartCoroutine(Fade(startAlpha, 0f));

        if (introPanel != null)
            introPanel.SetActive(false);

        IsActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;

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
