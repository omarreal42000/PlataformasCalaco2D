# Videojuego de Plataformas 2D en Unity (`PlataformasCalaco2D`)

* **Alumno:** Omar Real Solano
* **Institución:** Universidad de Guadalajara (UDGVirtual / CUCEI)
* **Motor Gráfico:** Unity (Modo 2D, C#)
* **Repositorio Público:** https://github.com/omarreal42000/PlataformasCalaco2D

---

## Actividad 3.1. La escena principal y la estructura del personaje

### Objetivo
Crear la escena principal y la estructura base de un personaje jugable para un videojuego en 2D dentro de un proyecto versionado en un repositorio público.

### Desarrollo e Implementación
* **Configuración del entorno:** Se estructuró el proyecto 2D con escenas organizadas en `Assets/Scenes/` (`MenuPrincipal.unity`, `Nivel1.unity`, `Nivel2.unity` y `Resultados.unity`), cámara ortográfica (`Main Camera`), superficies con colisionadores 2D e interfaz de usuario (`UIManager.cs`) que muestra el nivel actual, controles, monedas recolectadas y estado de conexión con Firebase.
* **Estructura del personaje (`Player`):**
  * `Sprite Renderer` y `Animator` (`DroneVisual.controller` con clips `Idle.anim`, `Walk.anim` y `Shoot.anim`) para gestionar los estados visuales en función de las teclas presionadas.
  * `Rigidbody2D` y colisionador 2D con material físico `SinFriccion.physicsMaterial2D` para evitar bloqueos laterales contra plataformas.
  * Objeto hijo `FirePoint` posicionado frente al personaje para la salida de proyectiles.

---

## Actividad 3.2. Los prefabs

### Objetivo
Configurar prefabs reutilizables para optimizar la instanciación de elementos dinámicos en un videojuego 2D.

### Desarrollo e Implementación
En `Assets/Prefabs/` se configuraron los siguientes prefabs reutilizables:
* **`Bala.prefab` (`BulletController.cs`):** Proyectil con `Rigidbody2D` y collider tipo *Trigger* que se instancia dinámicamente mediante `Instantiate()` cada vez que el jugador dispara.
* **`Nucleo.prefab` (`CoinPickup.cs`):** Ítem recolectable con collider *Trigger* que incrementa el contador de monedas al entrar en contacto con el jugador y destruye su instancia.
* **`Enemigo.prefab` (`EnemyHealth.cs`) y `EfectoImpacto.prefab`:** Prefabs para enemigos y partículas/efectos visuales de impacto al colisionar con los disparos.
* **Punto de disparo alineado:** En `PlayerController.cs`, la bala se instancia exactamente en la posición de `FirePoint` (y no en el centro del personaje) heredando la dirección hacia la que mira el jugador.

---

## Actividad 3.3. Programación de mecánicas y físicas

### Objetivo
Desarrollar la programación de mecánicas básicas y simulación de físicas (gravedad, salto y colisiones) en un entorno 2D.

### Desarrollo e Implementación
* **Simulación de gravedad:** El componente `Rigidbody2D` del personaje opera bajo la gravedad del motor físico 2D con rotación congelada en el eje Z para mantener estabilidad sobre las plataformas.
* **Detección de suelo y salto:** Mediante verificación de contacto con la capa de suelo (`Ground`), `PlayerController.cs` habilita el salto al presionar la tecla `Espacio` únicamente cuando el personaje está apoyado en una superficie, evitando saltos infinitos en el aire.

---

## Actividad 3.4. Configuración de la cámara: videojuego en 2D

### Objetivo
Programar el comportamiento de la cámara 2D para ofrecer un seguimiento fluido y una proporción adecuada del área de juego.

### Desarrollo e Implementación
* **Seguimiento suavizado (`CameraFollow2D.cs`):** Adjunto a `Main Camera`, sigue la posición del jugador mediante interpolación suavizada (`Lerp`) con un desplazamiento (`offset`) configurado para mantener al personaje centrado sin movimientos bruscos.
* **Proporción del área de juego:** El tamaño ortográfico (`Orthographic Size`) permite visualizar al personaje, las plataformas de salto, los enemigos y los colectables con claridad.

---

## Actividad 3.5. El movimiento del jugador

### Objetivo
Programar el desplazamiento lateral del personaje respetando las físicas y colisiones del escenario 2D.

### Desarrollo e Implementación
* **Controles de desplazamiento:** `PlayerController.cs` lee el eje horizontal (`A`/`D` y flechas de dirección) y aplica la velocidad lateral directamente sobre el `Rigidbody2D`, respetando las colisiones con muros, suelo y plataformas.
* **Orientación visual:** Al cambiar de dirección izquierda/derecha, el personaje invierte su orientación horizontal (`localScale.x`) para apuntar hacia el sentido de avance.

---

## Actividad 3.6. El movimiento de una bala

### Objetivo
Crear simulaciones en 2D de la trayectoria de un proyectil vinculadas correctamente a las acciones y estados del personaje.

### Desarrollo e Implementación
* **Sincronización con estados de animación:** El controlador vincula las transiciones entre `Idle`, `Walk` (izquierda/derecha) y `Shoot` de forma inmediata al detectar las entradas del teclado, sin retrasos entre la pulsación y el disparo.
* **Trayectoria del proyectil (`BulletController.cs`):** Al instanciarse, cada bala toma la dirección actual del personaje y se desplaza de forma continua hasta impactar un objetivo o cumplir su tiempo de vida.

---

## Actividad 3.7. Efectos de acción

### Objetivo
Programar los efectos de acción y comportamiento físico de la trayectoria de un proyectil en un videojuego 2D.

### Desarrollo e Implementación
* **Gravedad en cero:** El `Rigidbody2D` de `Bala.prefab` mantiene `gravityScale = 0` para que el proyectil no caiga en parábola y conserve una trayectoria lineal y horizontal constante.
* **Cadencia de disparo uniforme:** `PlayerController.cs` incorpora un temporizador de enfriamiento (*cooldown*) entre disparos para asegurar que los proyectiles salgan de manera fluida y sin superponerse de forma errática.

---

## Actividad 3.8. La conexión a base de datos

### Objetivo
Implementar una conexión a una base de datos externa para almacenar y recuperar de forma persistente el progreso del jugador (total de monedas recolectadas y nivel alcanzado).

### Desarrollo e Implementación
* **Persistencia en Cloud Firestore (`FirebaseManager.cs`):**
  * Se implementó la comunicación mediante peticiones HTTP (`UnityWebRequest` con `GET` y `PATCH`) hacia Firebase Cloud Firestore para guardar y consultar el documento de progreso del jugador.
  * **Campos sincronizados:** Total de monedas recolectadas y nivel alcanzado (`Nivel 1` y `Nivel 2`, gestionado con `LevelGoal.cs`).
* **Sincronización entre sesiones:** Al iniciar el juego se descargan los valores almacenados desde Firestore para actualizar el HUD (`UIManager.cs`), y cada vez que se recoge una moneda (`CoinPickup.cs`) o se completa un nivel (`LevelGoal.cs`) se actualiza el registro de manera persistente, manteniendo el avance al cerrar y volver a abrir el proyecto.
