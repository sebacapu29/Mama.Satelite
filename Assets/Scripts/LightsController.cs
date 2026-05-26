using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Gestor centralizado de luces para el juego de terror.
/// Utiliza el patrón Singleton para acceso global.
/// </summary>
public class LightsController : MonoBehaviour
{
    private static LightsController instance;
    
    // Diccionario para almacenar luces por nombre/ID
    private Dictionary<string, Light> lights = new Dictionary<string, Light>();
    
    // Control de corrutinas activas de tintineos
    private Dictionary<string, Coroutine> activeFlashes = new Dictionary<string, Coroutine>();

    public static LightsController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LightsController>();
                if (instance == null)
                {
                    GameObject controller = new GameObject("LightsController");
                    instance = controller.AddComponent<LightsController>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // Implementar Singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        // Cargar todas las luces de la escena automáticamente
        RegisterAllLights();
    }

    private void Update()
    {
        // Mock: Apagar todas las luces con tecla O
        if (Keyboard.current[Key.O].wasPressedThisFrame)
        {
            TurnOffAllLights();
        }
        if (Keyboard.current[Key.P].wasPressedThisFrame)
        {
            TurnOnAllLights();
        }
    }

    /// <summary>
    /// Registra todas las luces de la escena actual en el diccionario.
    /// </summary>

    private void RegisterAllLights()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in allLights)
        {
            string lightName = light.gameObject.name;
            if(lightName == "TV Light" || lightName == "FireLight")
            {
                continue; // Omitir la luz de la TV si no queremos controlarla
            }
            if (!lights.ContainsKey(lightName))
            {
                lights[lightName] = light;
            }
        }

        Debug.Log($"[LightsController] Se registraron {lights.Count} luces.");
    }

    /// <summary>
    /// Registra una luz manualmente en el controlador.
    /// </summary>
    public void RegisterLight(string identifier, Light light)
    {
        if (light != null)
        {
            lights[identifier] = light;
            Debug.Log($"[LightsController] Luz '{identifier}' registrada.");
        }
        else
        {
            Debug.LogWarning($"[LightsController] Intento de registrar una luz nula con ID '{identifier}'.");
        }
    }

    /// <summary>
    /// Prende una luz específica.
    /// </summary>
    public void TurnOnLight(string lightID)
    {
        if (lights.TryGetValue(lightID, out Light light))
        {
            // Detener tintineos si estaban activos
            StopFlashing(lightID);
            
            light.enabled = true;
            light.intensity = 1f;
            Debug.Log($"[LightsController] Luz '{lightID}' encendida.");
        }
        else
        {
            Debug.LogWarning($"[LightsController] Luz '{lightID}' no encontrada.");
        }
    }

    /// <summary>
    /// Apaga una luz definitivamente.
    /// </summary>
    public void TurnOffLight(string lightID)
    {
        if (lights.TryGetValue(lightID, out Light light))
        {
            // Detener tintineos si estaban activos
            StopFlashing(lightID);
            
            light.enabled = false;
            Debug.Log($"[LightsController] Luz '{lightID}' apagada.");
        }
        else
        {
            Debug.LogWarning($"[LightsController] Luz '{lightID}' no encontrada.");
        }
    }

    /// <summary>
    /// Cambia el color de una luz.
    /// </summary>
    public void ChangeColor(string lightID, Color newColor)
    {
        if (lights.TryGetValue(lightID, out Light light))
        {
            light.color = newColor;
            Debug.Log($"[LightsController] Color de luz '{lightID}' cambiado a {newColor}.");
        }
        else
        {
            Debug.LogWarning($"[LightsController] Luz '{lightID}' no encontrada.");
        }
    }

    /// <summary>
    /// Hace que una luz parpadee (tintinee) durante un tiempo.
    /// </summary>
    public bool StartFlashing(string lightID, float flashDuration = 2f, float flashSpeed = 0.1f)
    {
        if (!lights.TryGetValue(lightID, out Light light))
        {
            // Debug.LogWarning($"[LightsController] Luz '{lightID}' no encontrada.");
            return false;
        }

        // Detener parpadeo anterior si existe
        StopFlashing(lightID);

        // Iniciar nueva corrutina de parpadeo
        Coroutine flashCoroutine = StartCoroutine(FlashCoroutine(light, flashDuration, flashSpeed));
        activeFlashes[lightID] = flashCoroutine;

        // Debug.Log($"[LightsController] Luz '{lightID}' iniciando parpadeo.");
        return true;
    }

    /// <summary>
    /// Detiene el parpadeo de una luz.
    /// </summary>
    public void StopFlashing(string lightID)
    {
        if (activeFlashes.TryGetValue(lightID, out Coroutine flashCoroutine))
        {
            StopCoroutine(flashCoroutine);
            activeFlashes.Remove(lightID);

            if (lights.TryGetValue(lightID, out Light light))
            {
                light.enabled = true; // Asegurar que la luz esté encendida al detener
            }

            Debug.Log($"[LightsController] Parpadeo de luz '{lightID}' detenido.");
        }
    }

    /// <summary>
    /// Corrutina que maneja el parpadeo de una luz.
    /// </summary>
    private IEnumerator FlashCoroutine(Light light, float duration, float speed)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            light.enabled = !light.enabled;
            yield return new WaitForSeconds(speed);
            elapsedTime += speed;
        }

        light.enabled = true; // Dejar la luz encendida al finalizar
    }

    /// <summary>
    /// Apaga todas las luces de la escena.
    /// </summary>
    public void TurnOffAllLights()
    {
        foreach (var light in lights.Values)
        {
            light.enabled = false;
        }

        // Limpiar parpadeos activos
        foreach (var coroutine in activeFlashes.Values)
        {
            StopCoroutine(coroutine);
        }
        activeFlashes.Clear();

        Debug.Log("[LightsController] Todas las luces apagadas.");
    }

    /// <summary>
    /// Enciende todas las luces de la escena.
    /// </summary>
    public void TurnOnAllLights()
    {
        foreach (var light in lights.Values)
        {
            light.enabled = true;
            light.intensity = 1f;
        }

        Debug.Log("[LightsController] Todas las luces encendidas.");
    }

    /// <summary>
    /// Obtiene una luz específica del diccionario.
    /// </summary>
    public Light GetLight(string lightID)
    {
        lights.TryGetValue(lightID, out Light light);
        return light;
    }

    /// <summary>
    /// Retorna la cantidad de luces registradas.
    /// </summary>
    public int GetLightCount()
    {
        return lights.Count;
    }
}
