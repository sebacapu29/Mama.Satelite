using UnityEngine;

/// <summary>
/// Script de TESTING. Mueve a Salomé entre dos puntos en loop para probar
/// la animación idle/walk sin necesidad de tener la IA completa.
/// Cuando llegue a un punto, se queda quieta unos segundos (idle) y va al otro.
/// </summary>
public class SalomePatrolTest : MonoBehaviour
{
    [SerializeField] Transform pointA;
    [SerializeField] Transform pointB;
    [SerializeField] float speed = 0.6f;
    [SerializeField] float idleTime = 2f;
    [SerializeField] float arriveDistance = 0.1f;

    Transform _target;
    float _idleTimer;
    bool _isIdle;

    void Start()
    {
        _target = pointA;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        if (_isIdle)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                _isIdle = false;
                _target = (_target == pointA) ? pointB : pointA;
            }
            return;
        }

        Vector3 toTarget = _target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < arriveDistance)
        {
            _isIdle = true;
            _idleTimer = idleTime;
            return;
        }

        Vector3 dir = toTarget.normalized;
        transform.position += dir * speed * Time.deltaTime;
        // Orientarse hacia donde camina
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir, Vector3.up),
            Time.deltaTime * 5f
        );
    }
}
