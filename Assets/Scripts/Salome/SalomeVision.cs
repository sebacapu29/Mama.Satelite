using UnityEngine;
using Game.Audio;

/// <summary>
/// Detecta al jugador en un radio. Cuando entra, Salomé gira suavemente para mirarlo
/// y se activa la respiración de pánico. Si el jugador permanece 'timeToGameOver'
/// segundos dentro del radio, se activa el GameOver.
/// </summary>
public class SalomeVision : MonoBehaviour
{
    [Header("Detección")]
    [Tooltip("Radio en el que Salomé detecta al jugador.")]
    [SerializeField] float detectionRadius = 8f;
    [Tooltip("Tag del objeto jugador.")]
    [SerializeField] string playerTag = "Player";

    [Header("Rotación")]
    [Tooltip("Velocidad a la que Salomé gira para mirar al jugador.")]
    [SerializeField] float rotationSpeed = 2.5f;

    [Header("Game Over")]
    [Tooltip("Segundos que el jugador debe permanecer en el rango antes de perder.")]
    [SerializeField] float timeToGameOver = 5f;
    [Tooltip("Referencia al componente GameOverUI de la escena.")]
    [SerializeField] GameOverUI gameOverUI;

    Transform _player;
    PlayerAudio _playerAudio;
    bool _gameOverTriggered;
    bool _playerInRange;
    float _stareTimer;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerAudio = playerObj.GetComponent<PlayerAudio>();
        }
        else
        {
            Debug.LogWarning("[SalomeVision] No se encontró un GameObject con tag '" + playerTag + "'.");
        }
    }

    void Update()
    {
        if (_gameOverTriggered || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool inRange = dist <= detectionRadius;

        if (inRange != _playerInRange)
        {
            _playerInRange = inRange;
            OnRangeChanged(inRange);
        }

        if (_playerInRange)
        {
            RotateTowardsPlayer();

            _stareTimer += Time.deltaTime;
            if (_stareTimer >= timeToGameOver)
                TriggerGameOver();
        }
        else
        {
            _stareTimer = 0f;
        }
    }

    void OnRangeChanged(bool entered)
    {
        if (_playerAudio != null)
            _playerAudio.SetBreathingPanic(entered);
    }

    void RotateTowardsPlayer()
    {
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    void TriggerGameOver()
    {
        _gameOverTriggered = true;
        if (gameOverUI != null)
            gameOverUI.Show();
        else
            Debug.LogWarning("[SalomeVision] GameOverUI no asignado en el Inspector.");
    }

    /// <summary>Porcentaje del timer de stare (0–1) para mostrar en UI de progreso si se desea.</summary>
    public float StareProgress => Mathf.Clamp01(_stareTimer / timeToGameOver);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
