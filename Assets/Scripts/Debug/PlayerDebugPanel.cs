using UnityEngine;
using UnityEngine.InputSystem;
using Game.Audio;

/// <summary>
/// Panel de debug del jugador. Se togglea con una tecla (F3 por defecto) y muestra
/// FPS, transform, Rigidbody, estado de PlayerAudio (pasos, raycast al suelo,
/// respiración) y estado del AudioManager. Pensado para diagnosticar por qué un
/// sonido no suena, qué tag de suelo está detectando, etc.
/// Pegalo en el GameObject del Player.
/// </summary>
public class PlayerDebugPanel : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] Key toggleKey = Key.F3;
    [SerializeField] bool startVisible = true;

    [Header("Display")]
    [SerializeField] int fontSize = 13;
    [SerializeField] float panelWidth = 480f;
    [SerializeField] Vector2 panelOffset = new Vector2(10f, 10f);

    bool _visible;

    PlayerMovement _movement;
    PlayerAudio    _audio;
    Rigidbody      _rb;

    GUIStyle _styleLabel;
    GUIStyle _styleHeader;
    Texture2D _bgTex;

    // FPS
    float _fpsAccum;
    int   _fpsFrames;
    float _fpsTimer;
    float _displayFps;

    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _audio    = GetComponent<PlayerAudio>();
        _rb       = GetComponent<Rigidbody>();
        _visible  = startVisible;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            _visible = !_visible;

        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _fpsAccum  += 1f / dt;
        _fpsFrames += 1;
        _fpsTimer  += dt;
        if (_fpsTimer >= 0.25f)
        {
            _displayFps = _fpsAccum / Mathf.Max(_fpsFrames, 1);
            _fpsAccum = 0f; _fpsFrames = 0; _fpsTimer = 0f;
        }
    }

    void EnsureStyle()
    {
        if (_styleLabel != null) return;

        _styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            richText = true,
            wordWrap = false
        };
        _styleLabel.normal.textColor = Color.white;

        _styleHeader = new GUIStyle(_styleLabel)
        {
            fontStyle = FontStyle.Bold
        };
        _styleHeader.normal.textColor = new Color(1f, 0.85f, 0.4f);

        _bgTex = new Texture2D(1, 1);
        _bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.78f));
        _bgTex.Apply();
    }

    void OnGUI()
    {
        if (!_visible) return;
        EnsureStyle();

        float height = _audio != null ? 590f : 250f;
        var bg = new Rect(panelOffset.x, panelOffset.y, panelWidth, height);
        GUI.DrawTexture(bg, _bgTex);

        var inner = new Rect(bg.x + 12f, bg.y + 10f, bg.width - 24f, bg.height - 20f);
        GUILayout.BeginArea(inner);

        GUILayout.Label($"== PLAYER DEBUG  (toggle: {toggleKey})", _styleHeader);
        GUILayout.Label($"FPS: {_displayFps,5:F0}   Δt: {Time.deltaTime*1000f:F1}ms   Fixed Δt: {Time.fixedDeltaTime*1000f:F1}ms", _styleLabel);
        GUILayout.Space(6);

        GUILayout.Label("-- Transform --", _styleHeader);
        GUILayout.Label($"Pos: {V(transform.position)}", _styleLabel);
        GUILayout.Label($"Yaw: {transform.eulerAngles.y:F1}°", _styleLabel);
        GUILayout.Space(6);

        if (_rb != null)
        {
            var v = _rb.linearVelocity;
            float hSpeed = new Vector2(v.x, v.z).magnitude;
            GUILayout.Label("-- Rigidbody --", _styleHeader);
            GUILayout.Label($"linearVelocity: {V(v)}", _styleLabel);
            GUILayout.Label($"|horizontal|:    {hSpeed:F2} m/s", _styleLabel);
            GUILayout.Label($"isKinematic:     {Bool(_rb.isKinematic)}", _styleLabel);
            GUILayout.Space(6);
        }

        if (_movement != null)
        {
            GUILayout.Label("-- PlayerMovement --", _styleHeader);
            GUILayout.Label($"Hiding: {Bool(_movement.IsHiding)}", _styleLabel);
            GUILayout.Space(6);
        }

        if (_audio != null)
        {
            GUILayout.Label("-- PlayerAudio: footsteps --", _styleHeader);
            float sampled = _audio.SampledSpeed;
            float minMv   = _audio.MinMoveSpeed;
            bool grounded = _audio.IsGroundedNow;
            bool moving   = sampled > minMv && grounded;

            GUILayout.Label($"Sampled speed: {Colored($"{sampled:F2} m/s", sampled > minMv ? Color.green : new Color(1f,0.6f,0.3f))}   (umbral {minMv:F2})", _styleLabel);
            GUILayout.Label($"Grounded:      {Bool(grounded)}", _styleLabel);
            GUILayout.Label($"Step timer:    {_audio.StepTimer:F2} s", _styleLabel);
            GUILayout.Label($"¿Moviendo?:    {Bool(moving)}", _styleLabel);
            GUILayout.Space(4);

            GUILayout.Label($"Raycast suelo (range {_audio.GroundCheckDistance:F1} m):", _styleLabel);
            GUILayout.Label($"  hit:      {Colored(_audio.LastGroundObject, _audio.RaycastHit ? Color.white : new Color(1f,0.5f,0.5f))}", _styleLabel);
            GUILayout.Label($"  tag:      {_audio.LastGroundTag}", _styleLabel);
            GUILayout.Label($"  distance: {_audio.LastGroundDistance:F2} m", _styleLabel);
            GUILayout.Space(4);

            GUILayout.Label($"Último SoundEvent disparado:", _styleLabel);
            GUILayout.Label($"  {_audio.LastFootstepEvent}", _styleLabel);
            GUILayout.Space(6);

            GUILayout.Label("-- PlayerAudio: respiración --", _styleHeader);
            GUILayout.Label($"Sonando:  {Bool(_audio.BreathingPlaying)}", _styleLabel);
            GUILayout.Label($"Clip:     {_audio.BreathingClipName}", _styleLabel);
            GUILayout.Space(6);
        }

        bool managerOk = AudioManager.Instance != null;
        GUILayout.Label("-- AudioManager --", _styleHeader);
        GUILayout.Label($"Instance: {(managerOk ? Colored("OK", Color.green) : Colored("NULL", Color.red))}", _styleLabel);

        GUILayout.EndArea();
    }

    static string V(Vector3 v) => $"({v.x,7:F2}, {v.y,7:F2}, {v.z,7:F2})";
    static string Bool(bool b) => b ? Colored("true",  new Color(0.55f, 1f, 0.55f))
                                    : Colored("false", new Color(1f, 0.5f, 0.5f));
    static string Colored(string s, Color c) => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{s}</color>";
}
