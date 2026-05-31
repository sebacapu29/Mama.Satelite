using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelController : MonoBehaviour
{
    float randomValueForFlash = 0f;
    int fireMatchCount = 5;
    

    public int FireMatchCount
    {
        get => fireMatchCount;
        set
        {
            fireMatchCount = value;
            Debug.Log($"[LevelController] FireMatchCount updated: {fireMatchCount}");
        }
    }
    [SerializeField]
    private GameObject stalkerObject;
    public static LevelController Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartRandomFlashValue();
        StartFlashingLight();    
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LevelController] Multiple instances detected. Destroying the new one.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }    
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
        MockedStalkerBehavior();
    }

    private void MockedStalkerBehavior()
    {
        if (Keyboard.current[Key.I].wasPressedThisFrame)
        {
            stalkerObject.SetActive(true);
        }        
    }

    //Metodo para iniciar el parpadeo de una luz por su ID despues de 2 segundos
    public void StartFlashingLight()
    {
        InvokeRepeating(nameof(TriggerFlash), 1.5f, 3f);
    }
    void TriggerFlash()
    {
            bool success = LightsController.Instance.StartFlashing("Point Light Wall Lamp 1a - entrada");
            if (!success)
            {
                Debug.LogWarning($"[LevelController] No se pudo iniciar el parpadeo para la luz '{"PPoint Light Wall Lamp 1a - entrada"}'.");
            }
    }
}
