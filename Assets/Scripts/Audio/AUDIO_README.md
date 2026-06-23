# Sistema de Audio — Guía rápida

Sistema modular pensado para asignar archivos de audio desde el Inspector, con crossfade automático entre escenas y atenuación espacial nativa.

---

## Componentes

| Archivo | Tipo | Para qué |
|---|---|---|
| `AudioCategory.cs` | enum | Music / SFX / Ambient / UI / Voice |
| `SoundEvent.cs` | ScriptableObject | **Cualquier efecto**: array de clips, rango de pitch/volumen, distancias 3D |
| `SceneMusicLibrary.cs` | ScriptableObject | Mapa "escena → música" |
| `AudioManager.cs` | Singleton MonoBehaviour | Música con crossfade + reproducción de SFX 2D/3D |
| `AmbientSoundEmitter.cs` | MonoBehaviour 3D | Sonido anclado a un objeto (chimenea, TV) |
| `AmbientZone.cs` | MonoBehaviour trigger | Atmósfera 2D por habitación |
| `PlayerAudio.cs` | MonoBehaviour | Pasos según superficie + eventos del player |

---

## Setup inicial (una sola vez)

### 1. Crear la biblioteca de música

`Click derecho en Project → Create → Mama Satelite → Audio → Scene Music Library`

Asigná los clips de música:

| Scene Name | Clip | Volume | Loop |
|---|---|---|---|
| Floor1 | música_planta_baja.ogg | 0.6 | ✅ |
| Floor2 | música_primer_piso.ogg | 0.55 | ✅ |
| Floor3 | música_altillo.ogg | 0.5 | ✅ |
| Outdoor | música_exterior.ogg | 0.4 | ✅ |
| MainMenu | tema_menu.ogg | 0.7 | ✅ |

> Los `sceneName` deben ser **idénticos** al nombre del archivo `.unity` (sin extensión).

### 2. Crear el AudioManager persistente

- En la escena **MainMenu** (o la que cargues primero):
  - GameObject vacío `[AudioManager]`
  - Add Component → `AudioManager`
  - Asignar `Music Library` al ScriptableObject creado
  - Ajustar `Crossfade Duration` (2s por defecto va bien)

Gracias a `DontDestroyOnLoad`, el manager sobrevive a las transiciones de escena y la música va a ir cambiando sola al cargar Floor1 → Floor2 → etc.

### 3. (Opcional pero recomendado) AudioMixer

`Click derecho en Project → Create → Audio Mixer → "MainMixer"`

Crear los groups: `Music`, `SFX`, `Ambient`, `UI`, `Voice`.

Volver al `AudioManager` y arrastrar cada `AudioMixerGroup` a su slot. Eso te da:
- Un slider de volumen por categoría
- Snapshots para "Pausa" o "Esconderse" (low-pass + ducking)
- Compresión y EQ por grupo

---

## Uso por tipo de sonido

### Música de fondo

**Ya está resuelto** por la biblioteca. Si necesitás cambiarla manualmente (cinemática, jumpscare):

```csharp
AudioManager.Instance.PlayMusic(clip, volume: 0.8f, loop: true);
AudioManager.Instance.StopMusic(); // con fade-out
```

### SFX puntual (UI, voz interna, jumpscare)

1. Crear un asset: `Project → Create → Mama Satelite → Audio → Sound Event`
2. Llamarlo `SE_BotonClick`, asignarle el clip, `Category = UI`
3. Desde código:
   ```csharp
   [SerializeField] SoundEvent clickSound;
   ...
   AudioManager.Instance.PlaySFX(clickSound);
   ```

### SFX 3D (un golpe, una puerta cerrándose lejos)

```csharp
AudioManager.Instance.PlaySFXAtPoint(doorSlam, doorTransform.position);
```

Unity atenúa solo según la distancia al `AudioListener`. Ajustá `Min Distance` / `Max Distance` en el `SoundEvent`.

### Ambient anclado a un objeto (chimenea, TV)

1. Crear GameObject en la escena en la posición de la chimenea
2. Add Component → `AmbientSoundEmitter`
3. Asignar el clip de "fuego crepitando" y ajustar `Max Distance` (ej. 8 m)
4. Listo — el sonido va a atenuarse cuando el jugador se aleje

Los gizmos azules en escena muestran el `minDistance` (intenso) y `maxDistance` (silencio).

### Ambient por habitación (zumbido del baño, viento del altillo)

1. Crear GameObject con un `BoxCollider` que cubra la habitación
2. Add Component → `AmbientZone`
3. Asignar clip y `Target Volume`
4. Asegurarse de que el jugador tenga el tag `Player`

El sonido hace fade-in al entrar y fade-out al salir.

### Sonidos del jugador (pasos, esconderse, fósforo)

1. Agregar `PlayerAudio` al GameObject del Player
2. Crear `SoundEvent` para cada superficie:
   - `SE_FootstepWood` (3-4 clips, variación de pitch 0.95-1.05)
   - `SE_FootstepCarpet`
   - `SE_FootstepTile`
   - `SE_FootstepGrass`
3. Asignar `footstepDefault` como fallback
4. Etiquetar los colliders del suelo con los tags `Wood`, `Carpet`, `Tile`, `Grass`

**Para los eventos discretos** (esconderse, encender fósforo), enganchar desde los scripts existentes:

```csharp
// En PlayerMovement.cs, donde el player se esconde:
GetComponent<PlayerAudio>()?.OnHideEnter();

// En FireMatchController.cs, al encender:
playerAudio.OnMatchStrike();
```

Y para la respiración dinámica:
```csharp
// Cuando la madre está cerca:
playerAudio.SetBreathingPanic(true);
// Cuando se escapó:
playerAudio.SetBreathingPanic(false);
```

---

## Ejemplo de sonidos típicos para esta casa

| Lugar / situación | Tipo | Componente |
|---|---|---|
| Música ambiente de Floor2 | Música | `SceneMusicLibrary` |
| Reloj del living | Ambient 3D | `AmbientSoundEmitter` |
| TV encendida del Floor1 | Ambient 3D | `AmbientSoundEmitter` |
| Goteo en el baño | Ambient 3D | `AmbientSoundEmitter` |
| Viento en el altillo (Floor3) | Ambient zona | `AmbientZone` |
| Zumbido eléctrico del sótano | Ambient zona | `AmbientZone` |
| Pasos del jugador | SFX 3D | `PlayerAudio` + 4× `SoundEvent` |
| Encender fósforo | SFX 2D | `SoundEvent` + `PlayerAudio.OnMatchStrike()` |
| Lluvia del exterior | Ambient zona | `AmbientZone` en Outdoor |
| Puerta cerrándose | SFX 3D | `PlaySFXAtPoint` |
| Pasos de la madre acercándose | SFX 3D | `PlaySFXAtPoint` desde el AI |
| Jumpscare | SFX 2D | `PlaySFX` con volumen alto |

---

## Tips finales

- **Variación = realismo.** Cada `SoundEvent` permite cargar varios clips y un rango de pitch/volumen → el mismo paso nunca suena igual.
- **El `AudioListener` viaja con la cámara.** Asegurate de tener uno solo (en la cámara del player). Unity loguea warning si hay más de uno.
- **Importación.** Para música usá `Compressed in Memory` + `Streaming` (>200 KB). Para SFX cortos usá `Decompress on Load`.
- **Categorías.** Si después querés un menú de opciones con volumen separado, solo tenés que crear el AudioMixer una vez y los sliders mapean a los `AudioMixerGroup` ya rutados.
