using UnityEngine;

/// <summary>
/// Detecta si Salomé se está moviendo y setea el bool IsWalking del Animator.
/// Funciona midiendo el delta de posición frame a frame, así sirve para
/// NavMeshAgent, scripts custom o cualquier mecánica que mueva el transform.
/// </summary>
[RequireComponent(typeof(Animator))]
public class SalomeAnimator : MonoBehaviour
{
    [Tooltip("Velocidad mínima (m/s) para considerar que está caminando.")]
    [SerializeField] float walkThreshold = 0.05f;

    [Tooltip("Cuánto se suaviza el cambio para evitar parpadeos entre idle/walk.")]
    [SerializeField] float speedSmoothing = 6f;

    [Tooltip("Nombre del parámetro bool en el Animator Controller.")]
    [SerializeField] string isWalkingParam = "IsWalking";

    Animator _animator;
    Vector3 _lastPosition;
    float _smoothedSpeed;
    int _isWalkingHash;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _isWalkingHash = Animator.StringToHash(isWalkingParam);
        _lastPosition = transform.position;
    }

    void Update()
    {
        Vector3 delta = transform.position - _lastPosition;
        delta.y = 0f;
        _lastPosition = transform.position;

        float instantSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, instantSpeed, Time.deltaTime * speedSmoothing);

        _animator.SetBool(_isWalkingHash, _smoothedSpeed > walkThreshold);
    }
}
