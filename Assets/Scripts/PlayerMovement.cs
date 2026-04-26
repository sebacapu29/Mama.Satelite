using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadMovimiento = 5f;
    [SerializeField] private float friccion = 0.9f; // 0.9 = pequeña reducción cada frame sin input

    [Header("Rotación Cámara")]
    [SerializeField] private float sensibilidadMouse = 2f;
    [SerializeField] private float rotacionMaximaArriba = 90f;

    [Header("Gravedad")]
    [SerializeField] private float gravedad = 9.81f;

    private Rigidbody rb;
    private Transform cameraHolder;
    private Vector3 velocidad = Vector3.zero;
    private float velocidadCaida = 0f;
    private bool estaEnSuelo = true;
    private float rotacionX = 0f;

    void Start()
    {
        // Obtener el Rigidbody del Player
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement: No se encontró Rigidbody en el jugador.");
            return;
        }

        // Obtener el CameraHolder (hijo directo del Player)
        cameraHolder = transform.Find("CameraHolder");
        if (cameraHolder == null)
        {
            Debug.LogError("PlayerMovement: No se encontró el objeto 'CameraHolder' como hijo del Player.");
            return;
        }

        // Buscar la cámara dentro del CameraHolder
        Camera fpsCamera = cameraHolder.GetComponentInChildren<Camera>();
        if (fpsCamera == null)
        {
            Debug.LogError("PlayerMovement: No se encontró una Camera dentro de CameraHolder.");
            return;
        }

        // Asegurar que la cámara esté en el centro del CameraHolder
        fpsCamera.transform.localPosition = Vector3.zero;
        fpsCamera.transform.localRotation = Quaternion.identity;

        // Configurar Rigidbody
        rb.freezeRotation = true;
        rb.linearDamping = 0f; // Sin damping del Rigidbody, controlamos la fricción manualmente

        // Bloquear el cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Input del ratón para rotar cámara
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * sensibilidadMouse;
        float mouseY = mouseDelta.y * sensibilidadMouse;

        // Rotación horizontal (cuerpo del jugador)
        transform.Rotate(Vector3.up * mouseX);

        // Rotación vertical (cámara)
        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -rotacionMaximaArriba, rotacionMaximaArriba);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
        }

        // Bloquear/desbloquear cursor con ESC
        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.Confined : CursorLockMode.Locked;
        }

        // Obtener input de movimiento
        float inputX = 0f;
        float inputZ = 0f;

        if (Keyboard.current[Key.W].isPressed) inputZ += 1f;
        if (Keyboard.current[Key.S].isPressed) inputZ -= 1f;
        if (Keyboard.current[Key.D].isPressed) inputX += 1f;
        if (Keyboard.current[Key.A].isPressed) inputX -= 1f;

        Vector3 direccionMovimiento = new Vector3(inputX, 0f, inputZ).normalized;

        // Rotar movimiento relativo al jugador
        if (direccionMovimiento.magnitude > 0)
        {
            Vector3 adelante = transform.forward;
            Vector3 derecha = transform.right;

            direccionMovimiento = (adelante * direccionMovimiento.z + derecha * direccionMovimiento.x).normalized;
        }

        // Aplicar movimiento
        AplicarMovimiento(direccionMovimiento);

        // Aplicar gravedad
        // AplicarGravedad();
    }

    void AplicarMovimiento(Vector3 direccion)
    {
        if (rb == null) return;

        // Si hay input, aplicar velocidad directa
        if (direccion.magnitude > 0.01f)
        {
            velocidad.x = direccion.x * velocidadMovimiento;
            velocidad.z = direccion.z * velocidadMovimiento;
        }
        else
        {
            // Sin input: aplicar fricción simple
            velocidad.x *= friccion;
            velocidad.z *= friccion;
        }

        // Mantener la velocidad de caída (gravedad)
        velocidad.y = velocidadCaida;

        // Aplicar la velocidad al Rigidbody
        rb.linearVelocity = velocidad;
    }

    // void AplicarGravedad()
    // {
    //     if (rb == null) return;

    //     // Verificar si está en el suelo usando raycast
    //     estaEnSuelo = Physics.Raycast(transform.position, Vector3.down, 0.1f);

    //     if (estaEnSuelo)
    //     {
    //         velocidadCaida = 0f; // En suelo, sin caída
    //     }
    //     else
    //     {
    //         velocidadCaida -= gravedad * Time.deltaTime;
    //     }
    // }
}

