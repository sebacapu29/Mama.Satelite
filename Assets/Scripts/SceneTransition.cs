using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeOutAndLoad(sceneName);
        else
            SceneManager.LoadScene(sceneName); // fallback defensivo
    }
}
