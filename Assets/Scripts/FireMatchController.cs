using UnityEngine;
using Game.Audio;

public class FireMatchController : MonoBehaviour
{
    [SerializeField] private int timeToLive = 15; // segundos antes de que el fósforo se apague

    private float timer = 0f;
    private bool consumedThisActivation;
    private PlayerAudio playerAudio;

    void Awake()
    {
        playerAudio = FindFirstObjectByType<PlayerAudio>();
    }

    void OnEnable()
    {
        // Intentamos consumir un fósforo del inventario. Si no quedan,
        // desactivamos sin sonido (el fósforo no llegó a prenderse).
        consumedThisActivation = TryConsumeMatch();
        if (!consumedThisActivation)
        {
            gameObject.SetActive(false);
            return;
        }

        timer = 0f;
        if (playerAudio != null) playerAudio.OnMatchStrike();
    }

    void OnDisable()
    {
        // Sólo sonido de apagado si realmente se llegó a prender.
        if (!consumedThisActivation) return;
        if (playerAudio != null) playerAudio.OnMatchExtinguish();
        consumedThisActivation = false;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= timeToLive)
        {
            Debug.Log("[FireMatchController] Match has burned out.");
            gameObject.SetActive(false);
        }
    }

    bool TryConsumeMatch()
    {
        if (LevelController.Instance == null)
        {
            Debug.LogError("[FireMatchController] LevelController instance not found!");
            return false;
        }

        if (LevelController.Instance.FireMatchCount <= 0)
        {
            Debug.LogWarning("[FireMatchController] No more matches left to light!");
            return false;
        }

        LevelController.Instance.FireMatchCount--;
        Debug.Log($"[FireMatchController] Match lit! Remaining matches: {LevelController.Instance.FireMatchCount}");
        return true;
    }
}
