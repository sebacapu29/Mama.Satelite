using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WeatherDayNightController : MonoBehaviour
{
    public static WeatherDayNightController I { get; private set; }

    [Header("RapidAPI")]
    [Tooltip("URL completa del endpoint. Ej: https://weatherapi-com.p.rapidapi.com/current.json?q=Lanus")]
    public string endpointUrl;

    [Tooltip("Tu RapidAPI Key")]
    public string rapidApiKey;

    [Tooltip("El host exacto que te muestra RapidAPI (header x-rapidapi-host)")]
    public string rapidApiHost;

    [Header("Update")]
    [Min(10f)] public float refreshSeconds = 300f; // cada 5 min
    public bool fetchOnStart = true;

    [Header("Skyboxes")]
    public Material daySkybox;
    public Material nightSkybox;

    [Header("Lighting (opcional)")]
    public Light mainDirectionalLight;
    [Range(0f, 2f)] public float dayLightIntensity = 1.1f;
    [Range(0f, 2f)] public float nightLightIntensity = 0.25f;
    public Color dayAmbient = new Color(0.75f, 0.75f, 0.80f);
    public Color nightAmbient = new Color(0.08f, 0.08f, 0.12f);

    public bool IsDay { get; private set; }

    /// Evento para enganchar cosas (ej: comportamiento de la madre)
    public event Action<bool> OnDayNightChanged;

    private const int RequestTimeoutSeconds = 10;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (fetchOnStart) StartCoroutine(PollRoutine());
    }

    IEnumerator PollRoutine()
    {
        while (true)
        {
            yield return FetchAndApply();
            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    public IEnumerator FetchAndApply()
    {
        endpointUrl = endpointUrl?.Trim();
        rapidApiKey = rapidApiKey?.Trim();
        rapidApiHost = rapidApiHost?.Trim();

        if (string.IsNullOrEmpty(endpointUrl) || string.IsNullOrEmpty(rapidApiKey))
        {
            Debug.LogWarning("[WeatherDayNightController] Falta configurar endpointUrl / rapidApiKey.");
            yield break;
        }

        Debug.Log("[WeatherDayNightController] Request URL: '" + endpointUrl + "'");

        Uri endpointUri;
        try
        {
            endpointUri = new Uri(endpointUrl);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WeatherDayNightController] URL inválida: " + ex.Message);
            yield break;
        }

        string endpointHost = endpointUri.Host;
        if (string.IsNullOrEmpty(rapidApiHost))
        {
            rapidApiHost = endpointHost;
            Debug.Log("[WeatherDayNightController] x-rapidapi-host vacío; usando host de la URL: '" + rapidApiHost + "'");
        }
        else if (!string.Equals(endpointHost, rapidApiHost, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("[WeatherDayNightController] El host de la URL ('" + endpointHost + "') no coincide con x-rapidapi-host ('" + rapidApiHost + "'). Usando el host de la URL para evitar 404.");
            rapidApiHost = endpointHost;
        }

        Debug.Log("[WeatherDayNightController] Request host: '" + rapidApiHost + "'");

        using (var request = UnityWebRequest.Get(endpointUrl))
        {
            request.SetRequestHeader("x-rapidapi-key", rapidApiKey);
            request.SetRequestHeader("x-rapidapi-host", rapidApiHost);
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = RequestTimeoutSeconds;

            yield return request.SendWebRequest();

            Debug.Log("endpointUrl: " + endpointUrl + " | responseCode: " + request.responseCode);

            if (request.result != UnityWebRequest.Result.Success)
            {
                string body = request.downloadHandler?.text;
                Debug.LogWarning("[WeatherDayNightController] Error HTTP: " + request.error + " | body: " + body);
                yield break;
            }
            var json = request.downloadHandler.text;

            bool newIsDay = TryParseIsDay(json, out var parsedIsDay)
                ? parsedIsDay
                : FallbackLocalDay();

            ApplyDayNight(newIsDay);
        }
    }

    bool TryParseIsDay(string json, out bool isDay)
    {
        // Parse minimalista sin librerías:
        // busca "is_day":1 o "is_day":0
        // (Esto funciona con JSON tipo WeatherAPI.com, que expone is_day en "current".)
        // Ejemplo de respuesta con "is_day": 1 se ve en docs públicas. [6](https://www.weatherapi.com/)

        isDay = false;

        int idx = json.IndexOf("\"is_day\"", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;

        int colon = json.IndexOf(':', idx);
        if (colon < 0) return false;

        // Avanza hasta el primer dígito
        for (int i = colon + 1; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '0') { isDay = false; return true; }
            if (c == '1') { isDay = true; return true; }
        }
        return false;
    }

    bool FallbackLocalDay()
    {
        // Plan B: de 7 a 19 = día (ajustable)
        int h = DateTime.Now.Hour;
        return h >= 7 && h < 19;
    }

    void ApplyDayNight(bool newIsDay)
    {
        if (IsDay == newIsDay) return;

        IsDay = newIsDay;

        // Skybox global
        RenderSettings.skybox =  IsDay ? daySkybox : nightSkybox ;

        // Muy importante para que no se “apague” la escena si usas GI/ambient probe:
        // Unity docs indican que si cambias skybox en playmode, actualices el entorno. [4](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RenderSettings-skybox.html)
        DynamicGI.UpdateEnvironment(); // recomendado también por casos reales de escena oscura [5](https://stackoverflow.com/questions/73170767/changing-the-skybox-via-script-in-unity-makes-all-the-gameobjects-in-the-scene-s)

        // Ajustes opcionales de luz/ambiente
        RenderSettings.ambientLight = IsDay ? dayAmbient : nightAmbient;

        if (mainDirectionalLight != null)
            mainDirectionalLight.intensity = IsDay ? dayLightIntensity : nightLightIntensity;

        OnDayNightChanged?.Invoke(IsDay);

        Debug.Log("[WeatherDayNightController] Cambió a: " + (IsDay ? "DÍA" : "NOCHE"));
    }
}