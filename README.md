# 🎮 Mamá Satélite

> Juego de terror psicológico en primera persona desarrollado en Unity.

---

## 📖 Descripción

**Mamá Satélite** es un videojuego de terror psicológico realista en primera persona, donde el jugador encarna a un niño de 8 años que intenta escapar de su hogar mientras evita a su madre, cuyo comportamiento se ha vuelto inestable tras la pérdida del padre.

El juego no presenta elementos sobrenaturales; el horror se construye a partir del entorno doméstico, la tensión emocional y el deterioro psicológico de los personajes.

---

## 🎯 Objetivo del Proyecto

- **Narrativo:** transmitir ansiedad, encierro y desesperación desde la perspectiva infantil  
- **Jugable:** resolver puzzles y escapar sin ser detectado  
- **Conceptual:** explorar el terror realista dentro del núcleo familiar  

---

## 🕹️ Mecánicas Principales

- Exploración en primera persona  
- Sistema de sigilo (detección por sonido y movimiento)  
- Resolución de puzzles (llaves, códigos, objetos interactivos)  
- Gestión de recursos:
  - Luz (fósforos limitados)
  - Estamina  
- Interacción con el entorno (esconderse, observar, recolectar)  

> ❗ No existe combate directo. El enfoque está en la evasión y la tensión constante.

---

## 🏠 Mundo del Juego

- Escenario único: casa familiar de tres pisos + patio  
- Espacios conectados con progresión bloqueada  
- Rutas alternativas y puertas cerradas  
- Deterioro visual dinámico según la narrativa  

La casa funciona como una extensión del estado psicológico de la madre, volviéndose cada vez más hostil.

---

## 🎭 Personajes

- **Niño (Jugador):** 8 años, vulnerable pero ingenioso  
- **Madre (Antagonista):** comportamiento errático, alterna entre afecto y violencia  

---

## 🎨 Estilo Artístico

- Estética realista / semi-realista  
- Iluminación tenue y atmosférica  
- Uso de distorsión visual, ruido y filtros  
- Inspiración en entornos domésticos realistas  

---

## 🔊 Audio

- Sonido ambiental opresivo basado en elementos del hogar  
- Voz de la madre con tono ambiguo (afectivo/inquietante)  
- Efectos: pasos, puertas, golpes, respiración, susurros  

---

## ⚙️ Configuración Técnica

| Componente        | Detalle          |
|------------------|-----------------|
| Motor            | Unity 6         |
| Versión          | 6000.70f1       |
| Render Pipeline  | Universal Render Pipeline (URP) |
| Plataforma       | PC              |

---

## 📦 Dependencias

### GLTFast (Importación de modelos glTF)

Este proyecto utiliza **glTFast** para importar modelos `.gltf` y `.glb`.

### Instalación

1. Abrir el proyecto en Unity  
2. Ir a `Window → Package Manager`  
3. Seleccionar `+ → Add package from git URL`  
4. Ingresar:

```bash
com.unity.cloud.gltfast
```

También podés seguir la documentación oficial:

https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.10/manual/sources.html


## 🎨 Assets y Texturas
Las texturas del proyecto están disponibles en:

https://drive.google.com/drive/folders/***

### Instrucciones

Descargar la carpeta
Importarla en Assets/Textures
Configurar materiales con shaders URP (Lit / Simple Lit)

## 🚧 Estado Actual
Prototipo inicial en desarrollo:

Nivel base (Piso 1 y exterior)
Sistema básico de sigilo
Transición entre escenas
Activacion de fosforo

## ▶️ Ejecución

Abrir el proyecto en Unity 6000.70f1
Cargar la escena principal desde la carpeta /Scenes
Presionar Play en el Editor
