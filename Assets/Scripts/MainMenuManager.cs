/* using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] Button buttonNewGame;
    [SerializeField] Button creditsButton;
    [SerializeField] Button buttonQuitGame;
    AsyncOperation asyncLoad;

    void Start()
    {
        buttonNewGame.onClick.AddListener(ActivateScene);
        creditsButton.onClick.AddListener(ActivateScene);
        buttonQuitGame.onClick.AddListener(QuitGame);
        StartCoroutine(PreloadScene("Level_1"));
    }

    IEnumerator PreloadScene(string sceneName)
    {
        asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    void ActivateScene()
    {
        asyncLoad.allowSceneActivation = true;
    }
   
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
} */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Botones de Menú")]
    [SerializeField] Button buttonNewGame;
    [SerializeField] Button creditsButton;
    [SerializeField] Button buttonQuitGame;

    private AsyncOperation asyncLoad;

    void Start()
    {
        // Asignar eventos a los botones
        buttonNewGame.onClick.AddListener(ActivateScene);
        creditsButton.onClick.AddListener(() => LoadLevel("Credits"));
        buttonQuitGame.onClick.AddListener(QuitGame);

        // Comenzar precarga de la escena del juego
        StartCoroutine(PreloadScene("Level_1"));
    }

    IEnumerator PreloadScene(string sceneName)
    {
        asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        // Esperar hasta que la escena esté lista para activarse
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
    }

    // Activar la escena pre-cargada (Level_1)
    void ActivateScene()
    {
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = true;
        }
    }

    // Cargar escena directamente (para créditos, sin precarga)
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    // Salir del juego
    public void QuitGame()
    {
        Application.Quit();
    }
}
