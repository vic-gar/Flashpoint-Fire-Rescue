# Auditoría contra rúbrica y reto

Fecha: 10 de septiembre de 2026. Rama `carlos`.
Fuentes consultadas y su ruta exacta al final del documento.

---

## 1. Qué exige la rúbrica, criterio por criterio

La rúbrica existe en dos copias y **son idénticas**: el PDF del proyecto de
Claude y `tc2008b/project-ad2026/Rúbrica AD2026.docx`. No hay contradicción
entre ellas.

| # | Criterio | % | Texto del nivel máximo | Estado |
|---|---|---|---|---|
| 1 | Solución aleatoria | 10 | "sigue correctamente las reglas del juego y permite la interacción con todos los elementos del escenario" | Cumplido |
| 2 | Estrategia mejorada | 20 | "mejora significativa con datos comparativos claros en relación a la estrategia aleatoria" | Cumplido |
| 3 | Rendimiento | 20 | "altamente eficiente, rápida y precisa. Todos los agentes funcionan de manera coordinada" | Cumplido, falta redactarlo |
| 4 | Cliente-Servidor | 15 | "robusta y funcional, permitiendo comunicación sin problemas" | Cumplido con margen |
| 5 | Visualización 3D | 15 | "clara, detallada y permite una buena interacción con el escenario" | En curso |
| 6 | Narrativa | 10 | "detallada, coherente y crea una historia convincente" | **No hecho** |
| 7 | Innovación | 5 | "ideas, técnicas o tecnologías nuevas y creativas" | Hay material, falta redactarlo |
| 8 | Presentación final | 5 | "profesional, clara y bien organizada" | Pendiente |

---

## 2. Qué exige el reto

De `Reto AD 2026.docx`, requerimientos textuales:

- Área de 6x8 celdas descrita por archivo de texto. **Cumplido** (`data/final.txt`).
- Family Game Setup: 6 bomberos, 10 marcadores de víctima, 5 de falsa alarma,
  24 contadores de daño. **Cumplido**.
- "Puede haber más de un agente por celda". **Cumplido** (MultiGrid).
- Fin: 7 rescatadas gana; 4 perdidas o colapso pierde. **Cumplido**.
- "vista superior en 2D utilizando modelos en 3D, permitiendo **desplazar la
  cámara por todo el escenario**". **Cumplido** (`TopDownCameraController`).
  Esto es requisito explícito, no adorno.
- Dos estrategias, la segunda "respaldada por datos comparativos". **Cumplido**.

### Definición de Rendimiento según el propio reto

> "Un buen rendimiento garantiza que la simulación se lleve a cabo sin retrasos
> significativos y que los resultados obtenidos sean **precisos y reproducibles**."

Esta frase es la que decide el asunto de la semilla. Ver sección 8.

---

## 3. Contradicción encontrada y resuelta: 24 o 25 de daño

El reto se contradice a sí mismo dentro del mismo documento:

- Requerimiento 1: "**24 contadores de daño**".
- Requerimiento 2: "el edificio colapsa al acumular **25 puntos de daño o más**".

`Server/model/flashpoint_model.py` línea 73 ya traía el aviso escrito y usa
`MAX_DAMAGE = 24`.

**Resuelto con el reglamento oficial**, que el propio reto manda seguir
("siguiendo las reglas del juego de mesa"). `Flash-Point-Fire-Rescue.pdf`,
sección GAME END, línea 537 del texto extraído:

> "Building Collapse – The game ends immediately as the building collapses when
> **all 24 Damage markers have been placed** on the board."

**Nuestra implementación es la correcta.** No hay bug. Lo que hay es una
imprecisión en el reto, y conviene mencionarlo en el reporte porque demuestra
que se leyó el reglamento y no solo el enunciado.

---

## 4. Qué encontré en el material de clase

Revisados los notebooks de `tc2008b/multi-agents/`.

### Aleatoriedad

El profesor usa el generador único de Mesa:

    MoneyModel (AD2026).ipynb    other_agent = self.random.choice(self.model.agents)
                                 x = self.random.randrange(self.grid.width)
                                 self.agents.shuffle_do("step")
    RobotSweep (Step 2).ipynb    x = self.random.randrange(width)
    GameOfLife (AD2026).ipynb    self.live = np.random.choice([0, 1])

### Semillas explícitas: sí, y en dos notebooks

    MoneyModel (AD2026).ipynb  y  RobotSweep (Step 2).ipynb:

        rng = np.random.default_rng(42)
        rng_values = rng.integers(0, sys.maxsize, size=(5,))
        ...
        rng=rng_values.tolist(),

El profesor fija una semilla maestra (42), genera cinco semillas hijas, y se las
pasa a `batch_run` como lista. Es decir: **semillas explícitas y varias
réplicas**. Es exactamente el patrón que usamos en `run_batch.py` con 100
semillas pareadas.

Nuestro proyecto va más lejos que la clase en un punto: separamos tres
generadores (`fire_random`, `poi_random`, `strategy_random`) en vez de usar uno
solo. Eso es material directo para el criterio 7, Innovación.

### Velocidad de simulación

    GameOfLife (AD2026).ipynb        anim = animation.FuncAnimation(fig, animate, frames=MAX_GENERATIONS)
    SegregationModel (AD2026).ipynb  anim = animation.FuncAnimation(fig, animate, frames=MAX_GENERATIONS)
    RobotSweep (Step 2).ipynb        anim = animation.FuncAnimation(fig, animate, frames=model.steps)

El profesor **corre el modelo completo primero y después reproduce los estados
guardados a una velocidad de despliegue**. La velocidad de la animación es un
parámetro de presentación, separado del modelo. No hay ningún caso en el
material donde la velocidad de visualización toque las decisiones de los
agentes.

Precedente directo para `secondsBetweenSteps`.

### Vecindad

`MoneyModel` usa `moore=True` y `torus=True`. Nosotros usamos `moore=False` y
`torus=False` porque el reglamento dice literalmente "Diagonal spaces are not
Adjacent" y porque un edificio no da la vuelta. Copiar al profesor aquí sería
un error. Vale la pena decirlo en la presentación.

---

## 5. Plantilla cliente-servidor del profesor

`tc2008b/template-project/`, dos archivos:

- `tc2008B_server.py`: 45 líneas. `BaseHTTPRequestHandler` con `do_GET` y
  `do_POST`. El POST devuelve un diccionario fijo `{"x":1,"y":2,"z":3}`.
  Puerto 8585 por argumento de línea de comandos.
- `WebClient.cs`: `UnityWebRequest.Post` a `http://localhost:8585`,
  `JsonUtility.FromJson<Vector3>`.

Ese es el mínimo que pide la materia. Nuestro sistema tiene cinco endpoints
REST, contrato JSON documentado, selección de estrategia y semilla por
`POST /reset`, y 29 pruebas de servidor entre ellas una que lee
`SimulationState.cs` y compara sus campos contra las llaves del JSON.

**Estamos muy por encima del mínimo en el criterio 4.** Conviene enseñar la
plantilla al lado de lo nuestro en la presentación.

El README de la plantilla dice explícitamente que el puerto se puede cambiar,
así que usar 3000 en vez de 8585 no es una desviación.

---

## 6. Qué ya cumple el proyecto y qué falta de verdad

### Cumplido y verificado

- Motor de reglas completo en Mesa, 150 pruebas.
- Tres estrategias registradas por nombre.
- 100 semillas pareadas: aleatoria 0%, sin coordinación 38%, mejorada 44%.
- Cliente-servidor funcionando extremo a extremo.
- Cámara superior desplazable, que es requisito textual del reto.
- Materiales, texturas de piso y pared, HUD, fuego, humo, modelos.

### Falta de verdad

1. **Narrativa (10%). No existe nada escrito.** Es el hueco más grande y el más
   barato de cerrar: son dos o tres párrafos de contexto del escenario.
2. **Presentación final (5%).** Pendiente.
3. **Innovación (5%) redactada.** El material existe (separación de RNG,
   coordinación por mejor respuesta, experimentos pareados, el bug de orden de
   vecinos que resultó ser un artefacto). Falta escribirlo como sección.
4. **Reporte de rendimiento (parte del 20%).** Los números existen en los CSV,
   falta la redacción que los conecte con la palabra "reproducibles" del reto.
5. **Visualización (15%).** En curso, es lo que sigue en esta sesión.

### Cosas que hicimos y no eran obligatorias

- `mejorada_sin_coordinacion`. La rúbrica pide dos estrategias, tenemos tres.
  No es desperdicio: es lo que permite medir cuánto aporta coordinar, y eso
  alimenta el criterio 7.
- La separación en tres generadores. Tampoco la pedían. Es lo que hace que la
  comparación pareada sea válida.
- Las 41 pruebas de MultiGrid.

Ninguna de las tres se debe presentar como requisito cumplido, sino como
decisión propia. Es la diferencia entre criterio 2 y criterio 7.

---

## 7. La contradicción de resultados, resuelta

Tu consola mostró `victoria en 38 turnos, 7 rescatadas, 1 perdida, daño 15`.
La línea base documentada decía `derrota_colapso, 51 turnos, 6, 3, 24`.

Causa: **`MainScene.unity` línea 101682 dice `seed: 1`**, no 42. El valor por
defecto del script es 42, pero el que manda es el del inspector, y alguien lo
cambió a 1.

Verificado corriendo el modelo real:

    semilla  1 mejorada  -> ('victoria', 38, 7, 1, 15)
    semilla 42 mejorada  -> ('derrota_colapso', 51, 6, 3, 24)
    semilla  1 dos veces identica: True
    semilla 42 dos veces identica: True
    semilla  1 aleatoria -> ('derrota_colapso', 21, 1, 0, 24)

La semilla 1 reproduce **exactamente** lo que viste en Unity. La 42 reproduce
exactamente la línea base.

Dato extra que sale gratis: esto se corrió con **Mesa 3.3.1** en el contenedor,
mientras que tu máquina tiene 3.5.1, y los números son idénticos. Es decir, el
resultado no depende de la versión de Mesa, porque el proyecto usa sus propios
`random.Random` y su propio orden `MOVEMENT_DIRECTIONS`. Eso es evidencia fuerte
de reproducibilidad y va directo al criterio 3.

**La reproducibilidad no está rota. Nunca lo estuvo.**

---

## 8. Conclusión sobre semilla y reproducibilidad

El reto define rendimiento incluyendo que los resultados sean "precisos y
**reproducibles**". Eso son 20 puntos. El profesor, en su propio material, fija
`default_rng(42)` y genera semillas explícitas.

Por lo tanto:

- **Opción A (semilla fija siempre):** cumple la rúbrica pero hace que la demo
  se vea siempre igual. Si el profesor pide "córrelo otra vez", parece grabado.
- **Opción B (semilla aleatoria cada Play):** se ve vivo, pero pierdes el
  argumento de reproducibilidad justo donde vale puntos.
- **Opción C (dos modos):** el modo reproducible es el que se defiende ante la
  rúbrica; el aleatorio demuestra que no es una grabación. Cubre las dos cosas.

**Recomendación: C.** Con el modo reproducible como predeterminado, y la semilla
visible en pantalla para que se vea que es la misma.

Esto es un **cambio funcional** (modifica lo que `SimulationClient` manda en
`POST /reset`). No lo implemento. Requiere tu autorización.

---

## 9. Conclusión sobre velocidad de simulación

`MainScene.unity` línea 101683: `secondsBetweenSteps: 0.5`.

`SimulationClient.cs` línea 143: `yield return new WaitForSeconds(secondsBetweenSteps);`
justo antes del siguiente `POST /step`.

Ese valor **solo controla cada cuánto Unity pide el siguiente turno**. No entra
en ninguna decisión, no toca ningún generador y no cambia ningún resultado. El
servidor calcula el turno igual de rápido y devuelve el mismo estado. Es el
mismo patrón que `FuncAnimation` en los notebooks del profesor.

Es ajuste de presentación, categoría A. **Recomendación: 1.2 segundos.** Con
unos 38 a 40 turnos son 45 a 50 segundos de demo, que es la duración correcta
para un video.

Limitación que hay que decir en voz alta: un `/step` es **un turno completo**,
o sea los seis bomberos actúan y después avanza el fuego. Unity recibe un solo
estado agregado, así que los seis se mueven en el mismo instante por mucho que
bajes la velocidad. Que se muevan uno por uno requeriría que el servidor
mandara el estado agente por agente, y eso **sí es un cambio funcional**. Queda
como propuesta, no lo hago.

Mitigación puramente visual que sí haré: escalonar el arranque del suavizado
por bombero, para que el ojo pueda seguirlos.

---

## 10. El Scale del Game View

El `Scale` de la barra del Game View es **zoom del editor**. No cambia la
resolución, ni el Canvas, ni la build. A 0.53 cabe el marco completo de
1920x1080 dentro de tu panel acoplado; a 1.0 estás viendo píxeles 1:1 de un
marco de 1920 dentro de un panel de unos 1000, así que ves el centro y las
esquinas quedan fuera. Nada está roto.

Lo que **sí** es un problema real de diseño: el HUD está dimensionado para
alguien que mira una pantalla completa. En un panel acoplado se vuelve
ilegible. Eso se arregla haciéndolo más grande, y es categoría A.

Para grabar la demo:

1. Menú desplegable `Play Focused` de la barra del Game View, cámbialo a
   **`Play Maximized`**. Al darle Play el Game View ocupa todo el editor.
2. O pon el cursor sobre la pestaña Game y pulsa **Shift + Espacio** para
   maximizar el panel.
3. Con el panel maximizado, pon `Scale` en **1** y el aspecto en
   **Full HD (1920x1080)**.

---

## 11. Clasificación de cambios

### A) VISUAL / UX, autorizados, los hago ahora

1. HUD completo en español.
2. HUD más grande y legible.
3. Quitar la línea roja vertical, identidad visual nueva.
4. Rediseño para que no parezca dashboard.
5. "Sin conectar" como indicador discreto en vez de panel.
6. Pantalla final en español, "Civiles rescatados. Estructura estable."
7. Fuego con base, cuerpo y punta en vez de esfera brillante.
8. Humo diferenciado del fuego.
9. Feedback visual de apagado, comparando estados consecutivos en Unity.
10. `secondsBetweenSteps` 0.5 a 1.2.
11. Escalonado visual por bombero.
12. Revisión de escala de textura, repetición y contraste de paredes.

### B) FUNCIONAL, pendientes de tu autorización

1. **Modo de semilla aleatoria** (opción C). Toca `SimulationClient.cs`, campo
   nuevo y lo que se manda en `POST /reset`. Riesgo bajo. No altera resultados
   anteriores porque el modo reproducible sigue siendo el predeterminado.
2. **Paso por agente** en vez de por turno, para que los bomberos se muevan uno
   por uno. Toca `server.py` y el contrato JSON. Riesgo alto a estas horas.
   Mi recomendación: **no hacerlo hoy**.

### C) BACKEND / MODELO

Ninguno. No encontré ningún bug. La duda del 24 contra 25 quedó resuelta a
favor de lo que ya tenemos.

### D) EXPERIMENTAL / MÉTRICAS

Queda pendiente de sesiones anteriores la decisión de subir el experimento
oficial de 100 a 300 semillas para que el aporte de la coordinación salga
significativo. Con 100 semillas la prueba de signos da p = 0.35. No lo toco.

### E) DOCUMENTACIÓN

Narrativa, sección de innovación y reporte de rendimiento. Es donde están los
puntos más baratos que quedan.

---

## 12. Archivos que voy a tocar

    Assets/Scripts/Framework/HUDController.cs      solo cadenas de texto visibles
    Assets/Editor/FireRescuePolish.cs              layout del HUD, fuego, humo
    Assets/Scripts/Framework/BoardManager.cs       feedback visual de apagado
    Assets/Scripts/Framework/FirefighterVisual.cs  escalonado por bombero
    Assets/Scenes/MainScene.unity                  secondsBetweenSteps, vía inspector

No toco: `Server/` completo, el contrato JSON, `SimulationClient.cs`,
`TopDownCameraController.cs`, los CSV de resultados.

---

## Fuentes

    Rúbrica para el Proyecto de Simulación de Búsqueda y Rescate .pdf   proyecto de Claude
    tc2008b/project-ad2026/Rúbrica AD2026.docx                          idéntica a la anterior
    tc2008b/project-ad2026/Reto AD 2026.docx
    tc2008b/project-ad2026/Flash-Point-Fire-Rescue.pdf                  GAME END, regla de colapso
    tc2008b/multi-agents/MoneyModel/MoneyModel (AD2026).ipynb           default_rng(42), batch con lista de semillas
    tc2008b/multi-agents/RobotSweepModel/RobotSweep (Step 2).ipynb      igual
    tc2008b/multi-agents/GameOfLife/GameOfLife (AD2026).ipynb           FuncAnimation
    tc2008b/multi-agents/SegregationModel/SegregationModel (AD2026).ipynb  FuncAnimation
    tc2008b/template-project/tc2008B_server.py                          plantilla cliente-servidor
    tc2008b/template-project/WebClient.cs
    tc2008b/README.md
    Flashpoint-Fire-Rescue/Server/model/flashpoint_model.py             MAX_DAMAGE linea 77, MAX_STEPS linea 80
    Flashpoint-Fire-Rescue/Assets/Scripts/Framework/SimulationClient.cs linea 143 y 46
    Flashpoint-Fire-Rescue/Assets/Scenes/MainScene.unity                lineas 101679 a 101685
    claude/ESTADO_ACTUAL.md                                             documentacion previa del equipo
