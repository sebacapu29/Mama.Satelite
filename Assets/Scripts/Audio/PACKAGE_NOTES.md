# MamaSatelite Audio System — Package Notes

Sistema de audio + cambios al player para *Mamá Satélite*. Exportado como
`.unitypackage` para backup / reimport.

---

## Contenido

### Scripts nuevos (Assets/Scripts/)
- `Audio/AudioCategory.cs` — enum de categorías (Music/SFX/Ambient/UI/Voice)
- `Audio/SoundEvent.cs` — ScriptableObject reutilizable para cualquier efecto
- `Audio/SceneMusicLibrary.cs` — ScriptableObject de mapeo escena→música
- `Audio/AudioManager.cs` — singleton persistente; crossfade entre escenas, SFX 2D/3D
- `Audio/AmbientSoundEmitter.cs` — sonido 3D anclado a un objeto
- `Audio/AmbientZone.cs` — trigger de ambient 2D por habitación
- `Audio/PlayerAudio.cs` — pasos por superficie + respiración + eventos
- `Audio/AUDIO_README.md` — guía de uso
- `Debug/PlayerDebugPanel.cs` — overlay F3 con info del player
- `Transitions/SceneFader.cs` — fade in/out entre escenas (auto-bootstrap)

### Scripts existentes modificados
**Al importar el package, estos archivos se SOBREESCRIBEN. Si tenés cambios locales nuevos, destildalos al importar.**

- `PlayerMovement.cs` — agrega head bob de niño 8 años + llamadas a PlayerAudio.OnHideEnter/Exit + SetBreathingPanic
- `FireMatchController.cs` — bug fixes (consumo en OnEnable, no en burnout) + audio hooks
- `SceneTransition.cs` — usa SceneFader en lugar de SceneManager.LoadScene directo

### Assets de configuración
- SoundEvents (.asset) — los efectos definidos
- SceneMusicLibrary.asset — librería de música por escena
- MainMixer.mixer — AudioMixer con 5 groups (Music/SFX/Ambient/UI/Voice)

### NO incluidos
- AudioClips (.wav/.mp3/.ogg) — gestionarlos por separado por peso y licencia
- Prefabs, escenas — son específicos del proyecto

---

## Dependencias del proyecto

Los scripts modificados referencian clases del proyecto que NO están en el package:
- `PlayerMovement.cs` → `VisionEffect`, `LevelController`
- `FireMatchController.cs` → `LevelController`
- `SceneTransition.cs` → ninguna externa

Si el package se importa en un proyecto que no tiene esas clases, las modificaciones no compilan. En ese caso eliminar esos 3 archivos del import y usar sólo los scripts nuevos del sistema de audio.

---

## Cómo se usa después de importar

Ver `AUDIO_README.md` para el setup completo. Resumen:

1. Crear `SceneMusicLibrary` y asignar clips por escena
2. Crear GameObject `[AudioManager]` en escena inicial, asignarle la library
3. Crear AudioMixer con 5 groups, conectarlos al AudioManager
4. Crear SoundEvents (.asset) para cada efecto del juego
5. Agregar `PlayerAudio` al Player, asignar los SoundEvents
6. Agregar `PlayerDebugPanel` al Player para diagnóstico (F3)
7. `SceneFader` se auto-instancia, no requiere setup

---

## Compatibilidad

- Unity 6 (6000.x) con URP 17+
- Requiere Input System (no Input legacy)
- Sin Addressables — todo es referencia directa
