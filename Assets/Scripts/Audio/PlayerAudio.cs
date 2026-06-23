using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Sonidos del jugador: pasos (con detección de superficie por tag),
    /// respiración, fósforo, esconderse. Cada efecto es un SoundEvent asignable.
    /// </summary>
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Pasos")]
        [SerializeField] SoundEvent footstepDefault;
        [SerializeField] SoundEvent footstepWood;
        [SerializeField] SoundEvent footstepCarpet;
        [SerializeField] SoundEvent footstepTile;
        [SerializeField] SoundEvent footstepGrass;

        [Tooltip("Velocidad mínima (m/s) para considerar que el jugador se está moviendo.")]
        [SerializeField] float minMoveSpeed = 0.5f;
        [Tooltip("Velocidad a partir de la cual se considera 'corriendo'.")]
        [SerializeField] float runThreshold = 3f;
        [SerializeField] float walkStepInterval = 0.55f;
        [SerializeField] float runStepInterval = 0.32f;

        [Header("Detección de superficie")]
        [SerializeField] float groundCheckDistance = 1.3f;
        [SerializeField] LayerMask groundMask = ~0;
        [Tooltip("Velocidad por encima de la cual se asume teletransporte (entrar/salir de escondite) y se ignora el frame.")]
        [SerializeField] float teleportThreshold = 20f;

        [Header("Eventos de gameplay")]
        public SoundEvent breathingCalm;
        public SoundEvent breathingPanic;
        [Tooltip("Si está activo, arranca la respiración calmada apenas comienza la escena.")]
        public bool autoStartBreathing = true;

        public SoundEvent hideEnter;
        public SoundEvent hideExit;
        public SoundEvent matchStrike;
        public SoundEvent matchExtinguish;
        public SoundEvent doorOpen;
        public SoundEvent doorClose;

        Rigidbody _rb;
        CharacterController _cc;
        AudioSource _breathingSource;
        float _stepTimer;
        Vector3 _lastPosition;
        float _sampledSpeed; // muestreado en FixedUpdate

        // ---- Estado público (debug / introspección) ----
        public float SampledSpeed       => _sampledSpeed;
        public float StepTimer          => _stepTimer;
        public float MinMoveSpeed       => minMoveSpeed;
        public float RunThreshold       => runThreshold;
        public float GroundCheckDistance=> groundCheckDistance;
        public bool  IsGroundedNow      { get; private set; }
        public bool  RaycastHit         { get; private set; }
        public string LastGroundObject  { get; private set; } = "(none)";
        public string LastGroundTag     { get; private set; } = "(none)";
        public float  LastGroundDistance{ get; private set; } = -1f;
        public string LastFootstepEvent { get; private set; } = "(none)";
        public bool   BreathingPlaying  => _breathingSource != null && _breathingSource.isPlaying;
        public string BreathingClipName => _breathingSource != null && _breathingSource.clip != null ? _breathingSource.clip.name : "(none)";

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _cc = GetComponent<CharacterController>();
            _lastPosition = transform.position;

            _breathingSource = gameObject.AddComponent<AudioSource>();
            _breathingSource.loop = true;
            _breathingSource.spatialBlend = 0f;
            _breathingSource.playOnAwake = false;
            _breathingSource.volume = 0.7f;
        }

        void Start()
        {
            // Rutear al Voice group acá (no en Awake) para asegurar que AudioManager ya inicializó.
            if (AudioManager.Instance != null)
                _breathingSource.outputAudioMixerGroup = AudioManager.Instance.voiceGroup;

            if (autoStartBreathing) SetBreathingPanic(false);
        }

        void FixedUpdate()
        {
            // Muestreamos la velocidad real por delta de posición en el paso de física,
            // donde Rigidbody.position se actualiza de verdad. Update() lee este cache.
            Vector3 currentPos = transform.position;
            Vector3 horizontalDelta = currentPos - _lastPosition;
            horizontalDelta.y = 0f;
            _lastPosition = currentPos;

            float speed = horizontalDelta.magnitude / Time.fixedDeltaTime;
            // Anti-teletransporte: esconderse, recargar escena, dash, etc.
            _sampledSpeed = speed > teleportThreshold ? 0f : speed;
        }

        void Update()
        {
            // Un solo raycast por frame, cacheado para reuso (PlayFootstep) + debug.
            RaycastHit hit;
            RaycastHit = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundMask);
            if (RaycastHit)
            {
                LastGroundObject   = hit.collider.name;
                LastGroundTag      = hit.collider.tag;
                LastGroundDistance = hit.distance;
            }
            else
            {
                LastGroundObject   = "(no hit)";
                LastGroundTag      = "(no hit)";
                LastGroundDistance = -1f;
            }
            IsGroundedNow = _cc != null ? _cc.isGrounded : RaycastHit;

            float actualSpeed = _sampledSpeed;
            bool moving = actualSpeed > minMoveSpeed && IsGroundedNow;
            if (!moving)
            {
                _stepTimer = walkStepInterval;
                return;
            }

            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                PlayFootstep(hit);
                _stepTimer = actualSpeed > runThreshold ? runStepInterval : walkStepInterval;
            }
        }

        void PlayFootstep(RaycastHit groundHit)
        {
            SoundEvent step = footstepDefault;
            if (groundHit.collider != null)
            {
                step = groundHit.collider.tag switch
                {
                    "Wood"   => footstepWood   ?? footstepDefault,
                    "Carpet" => footstepCarpet ?? footstepDefault,
                    "Tile"   => footstepTile   ?? footstepDefault,
                    "Grass"  => footstepGrass  ?? footstepDefault,
                    _        => footstepDefault
                };
            }
            if (step == null || AudioManager.Instance == null)
            {
                LastFootstepEvent = step == null ? "(SoundEvent nulo)" : "(AudioManager nulo)";
                return;
            }
            LastFootstepEvent = step.name;
            AudioManager.Instance.PlaySFXAtPoint(step, transform.position);
        }

        // ----- API pública para enganchar desde PlayerMovement / FireMatchController -----

        public void OnHideEnter() { if (AudioManager.Instance) AudioManager.Instance.PlaySFX(hideEnter); }
        public void OnHideExit()  { if (AudioManager.Instance) AudioManager.Instance.PlaySFX(hideExit); }
        public void OnMatchStrike()     { if (AudioManager.Instance) AudioManager.Instance.PlaySFX(matchStrike); }
        public void OnMatchExtinguish() { if (AudioManager.Instance) AudioManager.Instance.PlaySFX(matchExtinguish); }

        public void SetBreathingPanic(bool panic)
        {
            var target = panic ? breathingPanic : breathingCalm;
            if (target == null || !target.HasClip) { _breathingSource.Stop(); return; }

            var clip = target.PickClip();
            if (_breathingSource.clip == clip && _breathingSource.isPlaying) return;
            _breathingSource.clip   = clip;
            _breathingSource.volume = target.PickVolume();
            _breathingSource.pitch  = target.PickPitch();
            _breathingSource.Play();
        }
    }
}
