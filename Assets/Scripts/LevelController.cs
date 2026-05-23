using System;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    float randomValueForFlash = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartRandomFlashValue();
        StartFlashingLight();        
    }
    void StartRandomFlashValue()
    {
        InvokeRepeating(nameof(UpdateRandomFlashValue), 0f, 0.5f);
    }
    void UpdateRandomFlashValue()
    {
        randomValueForFlash = UnityEngine.Random.Range(0f, 3f);
    }
    // Update is called once per frame
    void Update()
    {
    }
    //Metodo para iniciar el parpadeo de una luz por su ID despues de 2 segundos
    public void StartFlashingLight()
    {
        InvokeRepeating(nameof(TriggerFlash), 1.5f, 3f);
    }
    void TriggerFlash()
    {
            bool success = LightsController.Instance.StartFlashing("Point Light Wall Lamp 1a");
            if (!success)
            {
                Debug.LogWarning($"[LevelController] No se pudo iniciar el parpadeo para la luz '{"Point Light Wall Lamp 1a"}'.");
            }
    }
}
