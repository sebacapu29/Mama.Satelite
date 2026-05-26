using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadCaminar = 1f;
    [SerializeField] private float velocidadCorrer = 5f;
    [SerializeField] private float friccion = 0.9f; // 0.9 = pequeña reducción cada frame sin input

    [Header("Rotación Cámara")]
    [SerializeField] private float sensibilidadMouse = 2f;
    [SerializeField] private float rotacionMaximaArriba = 90f;

    [Header("Agarrar Objetos")]
    [SerializeField] private float distanciaAgarrar = 2f; // Distancia adelante de la cámara

    [Header("Fosforo")]
    [SerializeField] private GameObject fosforo; // Referencia al GameObject del fosforo

    private Rigidbody rb;
    private Transform cameraHolder;
    private Transform cameraTransform;
    private Vector3 velocidad = Vector3.zero;
    private float velocidadCaida = 0f;
    // private bool estaEnSuelo = true;
    private float rotacionX = 0f;
    
    // Variables para agarrar objetos
    private GameObject objetoAgarrado;
    private Rigidbody rbObjeto;
    private Collider colliderObjeto;

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

        // Guardar referencia a la transformada de la cámara
        cameraTransform = fpsCamera.transform;

        // Asegurar que la cámara esté en el centro del CameraHolder
        cameraTransform.localPosition = Vector3.zero;
        cameraTransform.localRotation = Quaternion.identity;

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

        // Toggle fosforo con F
        if (Keyboard.current[Key.F].wasPressedThisFrame && fosforo != null)
        {
            fosforo.SetActive(!fosforo.activeSelf);
        }

        // Input para agarrar/soltar con E
        if (Keyboard.current[Key.E].wasPressedThisFrame)
        {
            if (objetoAgarrado != null)
            {
                SoltarObjeto();
            }
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

        // Implementar correr al presionar tecla shift
        float velocidadActual = velocidadCaminar;
        if(Keyboard.current[Key.LeftShift].isPressed)
        {
            velocidadActual = velocidadCorrer;
        }

        // Aplicar movimiento
        AplicarMovimiento(direccionMovimiento, velocidadActual);

        // Actualizar posición del objeto agarrado
        if (objetoAgarrado != null)
        {
            ActualizarPosicionObjeto();
        }

        // Aplicar gravedad
        // AplicarGravedad();
    }

    void AplicarMovimiento(Vector3 direccion, float velocidadActual)
    {
        if (rb == null) return;

        // Si hay input, aplicar velocidad directa
        if (direccion.magnitude > 0.01f)
        {
            velocidad.x = direccion.x * velocidadActual;
            velocidad.z = direccion.z * velocidadActual;
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

    // Sistema de agarrar objetos
    void OnTriggerEnter(Collider other)
    {
        // Solo permitir agarrar si no hay objeto agarrado
        if (objetoAgarrado == null && other.CompareTag("Agarra"))
        {
            // Cambiar color para indicar que puede ser agarrado (opcional)
            Debug.Log("Objeto cerca - Presiona E para agarrar: " + other.gameObject.name);
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Mantener la referencia del objeto cuando está en el trigger
        if (objetoAgarrado == null && Keyboard.current[Key.E].wasPressedThisFrame && other.CompareTag("Agarra"))
        {
            AgarrarObjeto(other.gameObject);
        }
    }

    void AgarrarObjeto(GameObject objeto)
    {
        objetoAgarrado = objeto;
        rbObjeto = objeto.GetComponent<Rigidbody>();
        colliderObjeto = objeto.GetComponent<Collider>();

        if (rbObjeto != null)
        {
            rbObjeto.isKinematic = true; // Hacer kinematic para que se mueva con la cámara
        }

        if (colliderObjeto != null)
        {
            colliderObjeto.enabled = false; // Desactivar colisión mientras está agarrado
        }

        Debug.Log("Objeto agarrado: " + objeto.name);
    }

    void SoltarObjeto()
    {
        if (objetoAgarrado == null) return;

        if (rbObjeto != null)
        {
            rbObjeto.isKinematic = false;
        }

        if (colliderObjeto != null)
        {
            colliderObjeto.enabled = true;
        }

        Debug.Log("Objeto soltado: " + objetoAgarrado.name);
        objetoAgarrado = null;
        rbObjeto = null;
        colliderObjeto = null;
    }

    void ActualizarPosicionObjeto()
    {
        if (objetoAgarrado == null || cameraTransform == null) return;

        // Posicionar el objeto adelante de la cámara
        Vector3 posicionCamara = gameObject.transform.position;
        Vector3 adelanteCamara = gameObject.transform.forward * distanciaAgarrar;
        
        objetoAgarrado.transform.position = posicionCamara + adelanteCamara;
        objetoAgarrado.transform.rotation = cameraTransform.rotation;
    }
    
    }

