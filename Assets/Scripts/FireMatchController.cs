using UnityEngine;

public class FireMatchController : MonoBehaviour
{
    float timer = 0f;

    [SerializeField]
    private int timeToLive = 15; // Time in seconds before the match is destroyed
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CheckFireMatchCount();
    }

    void CheckFireMatchCount()
    {
        if (LevelController.Instance != null)
        {
            if (LevelController.Instance.FireMatchCount > 0)
            {
                LevelController.Instance.FireMatchCount--;
                Debug.Log($"[FireMatchController] Match lit! Remaining matches: {LevelController.Instance.FireMatchCount}");
            }
            else
            {
                Debug.LogWarning("[FireMatchController] No more matches left to light!");
                gameObject.SetActive(false); // Deactivate the match if no more are available
            }
        }
        else
        {
            Debug.LogError("[FireMatchController] LevelController instance not found!");
        }
    }
    // Update is called once per frame
    void Update()
    {
        HandleMatchLifeTime();
    }
    void HandleMatchLifeTime()
    {
        timer += Time.deltaTime;
        if (timer >= timeToLive)
        {
            gameObject.SetActive(false);
            Debug.Log("[FireMatchController] Match has burned out and is now inactive.");
            timer = 0f; // Reset timer in case the match is reactivated later
            CheckFireMatchCount();
        }
    }
}
