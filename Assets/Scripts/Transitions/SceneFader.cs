using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton persistente que dibuja un overlay full-screen y maneja el fade
/// entre escenas. Auto-arranca antes de cargar la primera escena vía
/// RuntimeInitializeOnLoadMethod, así no hay que ponerlo a mano en ninguna escena.
///
/// El load ocurre EN PARALELO al fade-out (LoadSceneAsync) → sin freeze visible.
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [SerializeField] float defaultDuration = 0.3f;
    [SerializeField] Color fadeColor = Color.black;

    Canvas _canvas;
    Image _image;
    bool _transitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[SceneFader]");
        go.AddComponent<SceneFader>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void BuildOverlay()
    {
        var canvasGO = new GameObject("FaderCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32760; // por encima de cualquier UI del juego

        var imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _image = imgGO.AddComponent<Image>();
        _image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        _image.raycastTarget = true; // bloquea clicks sobre UI mientras esté visible

        var rt = _image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si el overlay quedó en negro (cargamos directamente esta escena en Editor o
        // venimos de un FadeOutAndLoad), hacemos fade-in para revelar la escena nueva.
        if (_image != null && _image.color.a > 0.01f)
            StartCoroutine(FadeRoutine(0f, defaultDuration));
    }

    /// <summary>Fade-out → carga async la escena → fade-in.</summary>
    public void FadeOutAndLoad(string sceneName, float duration = -1f)
    {
        if (_transitioning) return;
        if (duration < 0f) duration = defaultDuration;
        StartCoroutine(FadeOutAndLoadRoutine(sceneName, duration));
    }

    IEnumerator FadeOutAndLoadRoutine(string sceneName, float duration)
    {
        _transitioning = true;

        // Empezamos el load EN PARALELO con el fade-out. allowSceneActivation = false
        // para que Unity no active la escena nueva hasta que el overlay esté en negro.
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // Fade-out
        yield return FadeRoutine(1f, duration);

        // Esperar a que el load esté listo (progress llega a 0.9 cuando está todo cargado,
        // luego se queda esperando allowSceneActivation = true).
        while (op.progress < 0.9f) yield return null;

        op.allowSceneActivation = true;
        // OnSceneLoaded se va a disparar solo y va a hacer el fade-in.
        _transitioning = false;
    }

    IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = _image.color.a;
        float t = 0f;
        // unscaledDeltaTime: funciona incluso si Time.timeScale = 0 (pausa).
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            _image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, a);
            yield return null;
        }
        _image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, targetAlpha);
    }
}
