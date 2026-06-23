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
    [SerializeField] private GameObject fosforo;

    [Header("Esconderse")]
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private VisionEffect visionEffect;

    [Header("Head Bob — Niño 8 años")]
    [SerializeField] private bool enableHeadBob = true;
    [Tooltip("Velocidad con la que el bob se intensifica/atenúa al cambiar de estado.")]
    [SerializeField] private float bobSmoothing = 8f;

    [Header("Bob al caminar")]
    [SerializeField] private float walkBobFrequency = 8.5f;
    [Tooltip("Bob vertical pequeño: piernas cortas → cabeza oscila menos en Y.")]
    [SerializeField] private float walkBobAmplitudeY = 0.045f;
    [Tooltip("Sway lateral marcado: el niño tambalea de lado a lado.")]
    [SerializeField] private float walkBobAmplitudeX = 0.05f;
    [Tooltip("Roll/tilt en grados sobre el eje Z, sincronizado al sway.")]
    [SerializeField] private float walkBobTiltZ = 2.2f;

    [Header("Bob al correr")]
    [SerializeField] private float runBobFrequency = 12f;
    [SerializeField] private float runBobAmplitudeY = 0.085f;
    [SerializeField] private float runBobAmplitudeX = 0.08f;
    [SerializeField] private float runBobTiltZ = 3.5f;

    [Header("Idle — siempre activo (respiración + fidget)")]
    [Tooltip("Respiración: pequeño bob vertical continuo, también suena cuando se mueve.")]
    [SerializeField] private float idleBreathFrequency = 0.9f;
    [SerializeField] private float idleBreathAmplitude = 0.008f;
    [Tooltip("Grados de cabeza moviéndose con Perlin noise (mirar alrededor inquieto).")]
    [SerializeField] private float idleFidgetAmplitude = 0.45f;
    [SerializeField] private float idleFidgetSpeed = 0.4f;

    private Rigidbody rb;
    private Transform cameraHolder;
    private Transform cameraTransform;
    private Vector3 velocidad = Vector3.zero;
    private float velocidadCaida = 0f;
    // private bool estaEnSuelo = true;
    private float rotacionX = 0f;

    private Vector3 cameraBaseLocalPos;
    private float bobTimer;
    private float bobBlend; // 0 = idle, 1 = caminar, 2 = correr (interpolado)
    private float fidgetSeedX;
    private float fidgetSeedY;
    
    private bool isHiding;
    public bool IsHiding => isHiding;

    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float savedRotacionX;

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
        cameraBaseLocalPos = cameraTransform.localPosition;

        // Seeds distintos para que pitch y yaw del fidget no se muevan en fase
        fidgetSeedX = Random.value * 100f;
        fidgetSeedY = Random.value * 100f;

        // Configurar Rigidbody
        rb.freezeRotation = true;
        rb.linearDamping = 0f; // Sin damping del Rigidbody, controlamos la fricción manualmente

        // Bloquear el cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (IntroUI.IsActive || OutroCreditsUI.IsActive) return;

        // Input del ratón para rotar cámara
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * sensibilidadMouse;
        float mouseY = mouseDelta.y * sensibilidadMouse;

        // Rotación horizontal (cuerpo del jugador) — siempre activa
        transform.Rotate(Vector3.up * mouseX);

        // Rotación vertical — bloqueada mientras el jugador está escondido
        if (!isHiding)
        {
            rotacionX -= mouseY;
            rotacionX = Mathf.Clamp(rotacionX, -rotacionMaximaArriba, rotacionMaximaArriba);
        }

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);

        // Bloquear/desbloquear cursor con ESC
        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.Confined : CursorLockMode.Locked;
        }

        // Toggle fosforo con F
        if (Keyboard.current[Key.F].wasPressedThisFrame && fosforo != null)
        {
            if (!fosforo.activeSelf && LevelController.Instance != null && LevelController.Instance.FireMatchCount <= 0)
            {
                Debug.LogWarning("No hay fósforos disponibles para encender.");
                return;
            }            
            fosforo.SetActive(true);
        }

        if (Keyboard.current[Key.E].wasPressedThisFrame)
        {
            if (isHiding)
                ExitHiding();
            else if (objetoAgarrado != null)
                SoltarObjeto();
        }

        if (isHiding) return;

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

    public void EnterHiding(Transform hidePoint)
    {
        if (isHiding) return;
        isHiding = true;

        savedPosition = transform.position;
        savedRotation = transform.rotation;
        savedRotacionX = rotacionX;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        float hideYaw = hidePoint.eulerAngles.y;
        transform.SetPositionAndRotation(hidePoint.position, Quaternion.Euler(0f, hideYaw, 0f));
        rotacionX = 0f;
        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.identity;

        if (playerRenderer != null)
            playerRenderer.enabled = false;

        if (visionEffect != null) visionEffect.SetHideEffect(true);

        var audio = GetComponent<Game.Audio.PlayerAudio>();
        if (audio != null)
        {
            audio.OnHideEnter();
            audio.SetBreathingPanic(true);
        }
    }

    public void ExitHiding()
    {
        if (!isHiding) return;
        isHiding = false;

        rb.isKinematic = false;
        transform.SetPositionAndRotation(savedPosition, savedRotation);
        rotacionX = savedRotacionX;

        if (playerRenderer != null)
            playerRenderer.enabled = true;

        if (visionEffect != null) visionEffect.SetHideEffect(false);

        var audio = GetComponent<Game.Audio.PlayerAudio>();
        if (audio != null)
        {
            audio.OnHideExit();
            audio.SetBreathingPanic(false);
        }
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

    // ----------------- HEAD BOB -----------------
    // Se aplica a cameraTransform (hijo del CameraHolder). El holder sigue manejando
    // el pitch del mouse; acá sólo se suma micro-offset de posición y un tilt sutil.
    void LateUpdate()
    {
        if (!enableHeadBob || cameraTransform == null) return;

        if (isHiding)
        {
            cameraTransform.localPosition = cameraBaseLocalPos;
            cameraTransform.localRotation = Quaternion.identity;
            bobTimer = 0f;
            bobBlend = 0f;
            return;
        }

        float horizontalSpeed = rb != null
            ? new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude
            : 0f;

        float walkRunMidpoint = (velocidadCaminar + velocidadCorrer) * 0.5f;
        float targetBlend = horizontalSpeed < 0.15f ? 0f
                          : horizontalSpeed < walkRunMidpoint ? 1f
                          : 2f;
        bobBlend = Mathf.MoveTowards(bobBlend, targetBlend, bobSmoothing * Time.deltaTime);

        // Interpolar parámetros del bob entre idle(0) → walk(1) → run(2)
        float freq, ampY, ampX, tiltZ;
        if (bobBlend <= 1f)
        {
            float k = bobBlend;
            freq  = Mathf.Lerp(0f, walkBobFrequency, k);
            ampY  = Mathf.Lerp(0f, walkBobAmplitudeY, k);
            ampX  = Mathf.Lerp(0f, walkBobAmplitudeX, k);
            tiltZ = Mathf.Lerp(0f, walkBobTiltZ, k);
        }
        else
        {
            float k = bobBlend - 1f;
            freq  = Mathf.Lerp(walkBobFrequency, runBobFrequency, k);
            ampY  = Mathf.Lerp(walkBobAmplitudeY, runBobAmplitudeY, k);
            ampX  = Mathf.Lerp(walkBobAmplitudeX, runBobAmplitudeX, k);
            tiltZ = Mathf.Lerp(walkBobTiltZ, runBobTiltZ, k);
        }

        // Sólo avanza el timer cuando hay bob real → la fase no se desincroniza al detenerse
        bobTimer += Time.deltaTime * freq;

        // Y oscila a la frecuencia del paso; X y tilt a la mitad (un sway por cada 2 pasos)
        float yBob  = Mathf.Sin(bobTimer * 2f * Mathf.PI) * ampY;
        float xBob  = Mathf.Sin(bobTimer * Mathf.PI)      * ampX;
        float zRoll = Mathf.Sin(bobTimer * Mathf.PI)      * tiltZ;

        // Respiración: bob vertical continuo, también con el jugador quieto
        float breath = Mathf.Sin(Time.time * idleBreathFrequency * 2f * Mathf.PI) * idleBreathAmplitude;

        // Fidget: Perlin noise en pitch/yaw → cabeza de niño mirando alrededor.
        // Se atenúa cuando se está moviendo (el bob ya da movimiento sobrado).
        float idleWeight = Mathf.Clamp01(1f - bobBlend * 0.6f);
        float fidgetPitch = (Mathf.PerlinNoise(Time.time * idleFidgetSpeed, fidgetSeedX) - 0.5f) * 2f
                            * idleFidgetAmplitude * idleWeight;
        float fidgetYaw   = (Mathf.PerlinNoise(fidgetSeedY, Time.time * idleFidgetSpeed) - 0.5f) * 2f
                            * idleFidgetAmplitude * idleWeight;

        cameraTransform.localPosition = cameraBaseLocalPos + new Vector3(xBob, yBob + breath, 0f);
        cameraTransform.localRotation = Quaternion.Euler(fidgetPitch, fidgetYaw, zRoll);
    }

    }

