# Mamá Satélite — Documento de Trabajos Realizados

> Análisis técnico del estado actual del proyecto Unity: pipeline de render, escenas, iluminación, materiales, shaders, texturizado y post-procesado.

---

## 1. Información general del proyecto

| Item | Valor |
|---|---|
| Motor | Unity 6 (6000.70f1) |
| Render Pipeline | **Universal Render Pipeline (URP) 17.0.4** |
| Input | Unity Input System 1.19.0 |
| Navegación IA | Unity AI Navigation 2.0.12 |
| Importación de modelos | glTFast 6.19.0 + UnityGLTF (Khronos) |
| Plataforma destino | PC |
| Género | Terror psicológico en primera persona |

Escenas principales presentes en `Assets/Scenes/`:
- [Floor1.unity](Assets/Scenes/Floor1.unity) — planta baja de la casa
- [Floor2.unity](Assets/Scenes/Floor2.unity) — primer piso
- [Floor3.unity](Assets/Scenes/Floor3.unity) — segundo piso / altillo
- [Outdoor.unity](Assets/Scenes/Outdoor.unity) — patio exterior
- Auxiliares: `MainMenu.unity`, `House_Floor_1.unity`, `TestRoom.unity`, `SampleScene.unity`

---

## 2. Universal Render Pipeline (URP)

El proyecto está configurado completamente sobre URP, con dos perfiles de calidad pensados para distintos targets:

- [PC_RPAsset.asset](Assets/Settings/PC_RPAsset.asset) + [PC_Renderer.asset](Assets/Settings/PC_Renderer.asset)
- [Mobile_RPAsset.asset](Assets/Settings/Mobile_RPAsset.asset) + [Mobile_Renderer.asset](Assets/Settings/Mobile_Renderer.asset)
- Configuración global: [UniversalRenderPipelineGlobalSettings.asset](Assets/UniversalRenderPipelineGlobalSettings.asset)

### Configuración del Render Pipeline de PC

Parámetros relevantes leídos directamente del asset:

| Parámetro | Valor | Comentario |
|---|---|---|
| HDR | ✅ habilitado | necesario para Bloom y tonemapping ACES |
| MSAA | 2x | balance calidad/rendimiento |
| Depth Texture | ✅ | requerida para shaders custom (vidrio, agua) |
| Opaque Texture | ✅ | requerida para shaders de refracción (agua/vidrio reactivo) |
| Render Scale | 1.0 | render a resolución nativa |
| Main Light Shadows | 2048 | sombras de la directional/sol con buena resolución |
| Additional Light Shadows | 4096 (atlas) | varias luces puntuales/spot con sombras (linternas, lámparas, fuego) |
| Shadow Distance | 50 m | corte de sombras para optimización |
| Shadow Cascades | 4 | cobertura del exterior |
| Soft Shadows | ✅ (calidad alta) | suaviza penumbras de fósforo/lámparas |
| Reflection Probe Blending + Box Projection | ✅ | mejora la integración de reflejos por habitación |
| SRP Batcher | ✅ | rendimiento |
| Light Probe System | APV (Adaptive Probe Volumes) | iluminación indirecta moderna en URP 17 |
| Volume Framework Update | Every Frame | el post-procesado puede animarse (vignette dinámica) |
| Volume Profile global | [DefaultVolumeProfile.asset](Assets/Settings/DefaultVolumeProfile.asset) | fallback global de post-procesado |

---

## 3. Iluminación

La estrategia de iluminación combina **lightmaps horneados (Progressive GPU) + Shadowmask** para los interiores y exterior, y **luces en tiempo real** para fuentes interactivas (fósforo, lámparas que el jugador puede apagar/prender).

### Settings de bake por escena

- Floor 1 — [01_First_Floor.lighting](Assets/Lighting/01_First_Floor.lighting)
  - Backend: Progressive GPU (`m_BakeBackend: 2`)
  - Lightmap max size: **1024**
  - Mixed Bake Mode: **Shadowmask** (`m_MixedBakeMode: 2`)
  - Sampling directo: 32, indirecto: 512, ambiente: 256
  - Bounces: 2
  - Denoiser: Optix (directo/indirecto/AO)
  - Realtime Environment Lighting: ON, Realtime Lightmaps: OFF
- Outdoor — [Outdoor.lighting](Assets/Lighting/Outdoor.lighting)
  - Misma config que Floor 1 pero con **Lightmap max size 2048** (superficie mucho mayor) y sample counts más bajos (8/16/32) — preset rápido pensado para iteración.

### Artefactos generados por escena

| Escena | Lightmaps | Shadowmask | Reflection Probes | APV (Probe Volumes) |
|---|---|---|---|---|
| Floor 1 | `Lightmap-0_comp_*` | ✅ | 4 (`ReflectionProbe-0..3`) | — |
| Floor 2 | `Lightmap-0..5_comp_*` (6) | — | 8 (`ReflectionProbe-0..7`) | ✅ (Floor2 Baking Set + CellData / CellSharedData / CellSupportData / CellOptionalData / CellProbeOcclusionData / CellBricksData) |
| Floor 3 | `Lightmap-0_comp_*` | — | 2 | — |
| Outdoor | `Lightmap-0_comp_*` | ✅ | 1 | — |

Floor 2 es la escena con la solución de iluminación más rica: emplea **Adaptive Probe Volumes (APV)** — el sistema moderno de probes de URP que reemplaza a las Light Probes clásicas — además de **8 reflection probes**, lo que da reflejos por habitación y luz indirecta de alta calidad para los pasillos del piso.

### Luces dinámicas

- Controlador centralizado: [LightsController.cs](Assets/Scripts/LightsController.cs)
  - Singleton (`DontDestroyOnLoad`) que registra automáticamente todas las luces de cada escena.
  - API: `TurnOnLight`, `TurnOffLight`, `ChangeColor`, `StartFlashing` (tintineo con corrutinas), `TurnOffAllLights`.
  - Excluye explícitamente `TV Light` y `FireLight` del control global — son luces que responden a otra lógica (TV ambiental, fuego del fósforo).
- Fósforo del jugador: [FireMatchController.cs](Assets/Scripts/FireMatchController.cs) — luz puntual que se enciende/apaga y se consume.
- Ciclo día/noche dinámico: [WeatherDayNightController.cs](Assets/Scripts/Weather/WeatherDayNightController.cs)
  - Consulta una **API REST (WeatherAPI vía RapidAPI)** para obtener `is_day` real de Lanús.
  - Intercambia los skyboxes `Skybox_midday` / `Skybox_night`, ajusta `RenderSettings.ambientLight`, `DynamicGI.UpdateEnvironment()` y la intensidad de la directional.
  - Asset de skyboxes: [Skybox_midday.mat](Assets/Skybox/Skybox_midday.mat), [Skybox_night.mat](Assets/Skybox/Skybox_night.mat).

---

## 4. Materiales

El proyecto trabaja con **cientos de materiales** organizados por escena en `Assets/Models/<Floor>/Materials/` y materiales compartidos en `Assets/Materials/`.

### Convención de nombres

Los materiales importados desde packs externos siguen el patrón `XX_Category-Object-Descriptor_0.1_0_0.mat` (p.ej. `40_Hom-Bathroom-Cabinets-White_...`, `25_End-Indoor-Plant-...`). Materiales propios usan nombres descriptivos en castellano/inglés simple: `Cabeza-Fosforo.mat`, `Palo-Fosforo.mat`, `Fire.mat`, `Kitchen-Bench_0.1.mat`, etc.

### Shader detrás de los materiales

Un sample sobre `Assets/Materials/` (56 .mat) muestra:
- **55 materiales** usan el shader **URP/Lit** (GUID `933532a4fcc9baf4fa0491de14d08ed7`).
- 1 material (`Fire.mat`) usa Standard legacy — probablemente leftover para una partícula.

Los materiales URP/Lit típicamente activan los keywords:
- `_METALLICSPECGLOSSMAP` — mapa metallic+smoothness empaquetado
- `_NORMALMAP` — normal map activo
- `_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A` — smoothness en el canal alpha del albedo

Esto es coherente con la naming convention de las texturas (`*_C` Color/Albedo, `*_N` Normal, `*_R` Roughness/Smoothness, `*_H` Height, `*_M` Metallic).

### Materiales específicos por escena (cantidad aproximada)

| Escena | Carpeta | Volumen |
|---|---|---|
| Floor 1 | `Models/Floor1/Materials/` | gran cantidad (cocina, baño, comedor, decoración) |
| Floor 2 | `Models/Floor2/Materials/` | el más extenso — dormitorios, ropa, juguetes, cortinas, alfombras |
| Floor 3 | `Models/Floor3/Materials/` | altillo: cajas, bicicletas, herramientas, laptops vintage |
| Outdoor | `Models/Outdoor/Materials/` | suelo, piscina, casa exterior, vegetación |

Notables en Outdoor:
- `Swimming_Pool_Water-1.mat` — usa **shader custom** (Shader Graph) en lugar de URP/Lit.
- `M_VientoArboles_Shader.mat` — material que aplica el shader de viento a los árboles.
- `house_04_diff_Mat.00X.mat` — set de materiales del modelo exterior de la casa.

---

## 5. Shaders custom

Carpeta dedicada: [Assets/Shader/](Assets/Shader)

### 5.1 Outline (selección/hover de objetos interactivos)

- [OutlineShaderURP.shader](Assets/Shader/OutlineShaderURP.shader) — shader HLSL escrito a mano, técnica de **"inverted hull"**:
  - `Cull Front` — culling de caras frontales para ver solo la "cáscara" trasera.
  - El vértice se expande a lo largo de la normal (`positionOS + normal * _OutlineWidth`).
  - Color y ancho configurables (`_OutlineColor`, `_OutlineWidth` 0..0.1).
  - Tag `RenderPipeline = UniversalPipeline` + `LightMode = UniversalForward`, integrado a URP.
- Materiales: `OutlineWhite.mat`, `OutlineWhite2.mat`.
- Variante adicional en Shader Graph: [OutloneShader.shadergraph](Assets/Shader/OutloneShader.shadergraph).
- Driver en runtime: [HoverOutline.cs](Assets/Scripts/HoverOutline.cs) — instancia el material outline y lo añade/quita como último material de cada `Renderer` al entrar/salir del mouse, además de disparar el tooltip de interacción.

### 5.2 Hover Emission

- [ColorHover.shadergraph](Assets/Shader/ColorHover.shadergraph) + `HoverEmission.mat` — feedback visual emisivo para objetos que se pueden recoger.

### 5.3 Vidrio reactivo

- [Shader_Vidrio_Reactivo.shadergraph](Assets/Shader/Shader_Vidrio_Reactivo.shadergraph) + `Shader_Vidrio_Reactivo.mat` — shader de vidrio que reacciona al entorno (probablemente refracción usando la Opaque Texture habilitada en el URP asset).

### 5.4 Agua

- [WaterShader.shadergraph](Assets/Shader/WaterShader.shadergraph) y [WaterShader2.shadergraph](Assets/Shader/WaterShader2.shadergraph) — dos variantes de shader de agua.
- Materiales: `WaterMaterial.mat`, `WaterMaterial2.mat`.
- Texturas asociadas: `Water Normal 1.png`, `Water Normal 2.png` — normales scrolleadas para olas.
- El de Outdoor se aplica al material `Swimming_Pool_Water-1.mat`.

### 5.5 Viento en árboles (Outdoor)

- [VientoArboles_Shader.shadergraph](Assets/Models/Outdoor/Materials/VientoArboles_Shader.shadergraph) — vértices animados para simular movimiento de hojas con el viento, aplicado a la vegetación del patio.

### 5.6 Cortinas (Floor 2)

- [S_Cortinas.shadergraph](Assets/Scenes/Floor2/S_Cortinas.shadergraph) — shader específico para el comportamiento visual de las cortinas del dormitorio.

---

## 6. Texturizado

### Estructura

Las texturas están organizadas en:
- `Assets/Textures/` — banco principal, con subcarpetas por escena (`Floor1`, etc.) y un `Floor1.zip` que indica que el set fue distribuido externamente (ver README — link a Google Drive).
- `Assets/Models/Floor*/Materials/` — texturas embebidas junto a los materiales del FBX.
- `Assets/Skybox/Skybox_Textures/` — cielos día/noche.

### Convención de canales

Cada superficie suele tener su set completo PBR:

| Sufijo | Significado |
|---|---|
| `_C` | Color / Albedo |
| `_N` | Normal map |
| `_R` | Roughness (canal de smoothness invertido) |
| `_H` | Height / Displacement |
| `_M` | Metallic |

Ejemplos: `Apple_C/H/N/R.jpg`, `Ground_Floor_C/H/M/N/R.jpg`, `Curtain_C/H/N/R.jpg`, `Inner_Wall_C/H/N.jpg`.

Esto le permite a URP/Lit producir el look semi-realista que pide la dirección de arte (ver README, sección "Estilo Artístico").

### Texturas particulares

- `Assets/Shader/Water Normal 1.png` / `Water Normal 2.png` — mapas de normales para el shader de agua.
- `Assets/Particles/WaterSplash.png` — sprite para sistema de partículas de agua.
- `Assets/Models/Outdoor/Props/Textures/Tree1_Leaf_C.png` — alpha-tested para hojas de árboles del patio.

---

## 7. Post-procesado

El proyecto usa el **Volume Framework de URP** con perfiles por escena, no sólo un volumen global.

### 7.1 Floor 1 — [PostProcessing_Volume Profile.asset](Assets/Scenes/Floor1/PostProcessing_Volume%20Profile.asset)

Tres efectos configurados:

| Efecto | Parámetro | Valor |
|---|---|---|
| **Vignette** | Center (0.5, 0.5), Intensity 0.5, Smoothness 0.6 | acentúa el túnel visual del personaje |
| **Bloom** | Threshold 0.9, Intensity 1.2 | resalta fuentes de luz puntuales |
| **Color Adjustments** | Post Exposure −0.2, Contrast +16, Saturation −19 | escena más oscura, contrastada y desaturada — coherente con el tono opresivo |

### 7.2 Outdoor — [Global Volume Profile.asset](Assets/Scenes/Outdoor/Global%20Volume%20Profile.asset)

Pipeline más completo para exteriores:

| Efecto | Parámetro relevante | Valor |
|---|---|---|
| **Tonemapping** | Mode = ACES | grading filmico estándar de HDR |
| **Vignette** | Color negro, Intensity 0.192, Smoothness 1, Rounded ON | sutil, redondeada |
| **Bloom** | Threshold 0.9, Intensity **3.97** | mucho más agresivo — sol y reflejos exteriores |
| **Color Adjustments** | Post Exposure **−2.19** | escena oscurecida fuerte (probablemente para forzar noche al inicio) |
| **Lift Gamma Gain** | Gamma W −0.13, Gain W −0.21 | curva tonal hacia abajo en medios y altas luces |
| **Shadows / Midtones / Highlights** | trims de color cyan/verde-azulado en sombras (−0.047), midtones −0.137, highlights −0.076 | tinta el exterior con un teñido frío |

### 7.3 Volúmenes locales

- [Bathrrom Tent Volumen.asset](Assets/Lighting/Bathrrom%20Tent%20Volumen.asset) — volume local en el baño de Floor 1, override por habitación.

### 7.4 Post-procesado interactivo desde gameplay

- [VisionEffect.cs](Assets/Scripts/VisionEffect.cs) — script que toma un `UnityEngine.Rendering.Volume`, lee la `Vignette` y modifica `intensity` en runtime (`0.5` al esconderse, `0` al salir). Es el mecanismo que enfatiza visualmente el estado de "escondido" del jugador.

---

## 8. Scripts de soporte de la capa visual

Más allá del controlador de luces y del de clima:

| Script | Rol visual |
|---|---|
| [HoverOutline.cs](Assets/Scripts/HoverOutline.cs) | Selección de objetos: añade el material outline dinámicamente |
| [VisionEffect.cs](Assets/Scripts/VisionEffect.cs) | Modula vignette del Volume al esconderse |
| [LightsController.cs](Assets/Scripts/LightsController.cs) | Encendido/apagado/parpadeo de luces de la escena |
| [FireMatchController.cs](Assets/Scripts/FireMatchController.cs) | Luz consumible del fósforo |
| [WeatherDayNightController.cs](Assets/Scripts/Weather/WeatherDayNightController.cs) | Skybox + ambient + intensidad de directional según clima real |
| [SceneTransition.cs](Assets/Scripts/SceneTransition.cs) | Transición entre Floor1 / Floor2 / Floor3 / Outdoor |

---

## 9. Resumen de los trabajos realizados

Punteo final del trabajo técnico evidenciable en el repo:

**Setup de pipeline**
- Migración / configuración completa del proyecto sobre URP 17 con dos quality assets (PC / Mobile).
- HDR, MSAA 2x, Depth y Opaque textures habilitadas para soportar shaders custom de refracción y agua.
- APV (Adaptive Probe Volumes) activado y horneado en Floor 2.

**Iluminación**
- Bakes con Progressive GPU + Shadowmask en Floor 1, Floor 2, Floor 3 y Outdoor, con lightmaps específicos por escena (Floor 2: 6 lightmaps; Outdoor: lightmap 2048).
- Reflection probes propias por escena (Floor 1: 4, Floor 2: 8, Floor 3: 2, Outdoor: 1).
- Sistema de luces dinámicas con singleton `LightsController` (registro automático, parpadeo, color y on/off por ID).
- Ciclo día/noche dinámico controlado por API meteorológica real (RapidAPI / WeatherAPI) con cambio de skybox, ambient y directional intensity.

**Materiales y texturizado**
- Cientos de materiales URP/Lit organizados por escena, con flujo PBR completo (albedo / normal / roughness / height / metallic).
- Skyboxes propios día/noche.
- Convención de naming de texturas (`_C/_N/_R/_H/_M`) coherente con materiales que activan los keywords `_NORMALMAP` y `_METALLICSPECGLOSSMAP`.

**Shaders custom**
- Outline en HLSL (`OutlineShaderURP.shader`) técnica inverted-hull integrado a URP, más variante en Shader Graph.
- Shader Graph de vidrio reactivo (refracción).
- Dos shaders de agua + texturas de normales.
- Shader de viento para vegetación (`VientoArboles_Shader`).
- Shader específico de cortinas en Floor 2 (`S_Cortinas`).
- Shader de hover con emisión.

**Post-procesado**
- Perfil de post-procesado por escena, no global:
  - Floor 1: Vignette + Bloom + Color Adjustments oscuro/contrastado/desaturado.
  - Outdoor: ACES Tonemapping + Bloom intenso + Lift-Gamma-Gain + Shadows/Midtones/Highlights teñidas frías + Vignette redondeada.
  - Volumen local extra en el baño de Floor 1.
- Integración de post-procesado con gameplay (`VisionEffect` modula vignette en runtime al esconderse).

**Soporte de gameplay visual**
- Sistema de hover/tooltip que añade un material outline dinámicamente al renderer apuntado.
- Transición entre las tres plantas y el exterior.

---

*Documento generado a partir de inspección directa de `Packages/manifest.json`, los `*.lighting`, los `Volume Profile`, los `*.mat` y `*.shader*` del proyecto, y los scripts en `Assets/Scripts/`.*
