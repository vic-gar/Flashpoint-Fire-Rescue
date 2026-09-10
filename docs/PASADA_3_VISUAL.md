# Tercera pasada visual: qué hacer

Todo el código está escrito en tu proyecto. Falta ejecutarlo en Unity.

---

## PASO 0 · Checkpoint

```bash
git add -- \
  Assets/Editor/FireRescuePolish.cs \
  Assets/Scripts/Framework/HUDController.cs \
  Assets/Scripts/Framework/BoardManager.cs \
  Assets/Scripts/Framework/FirefighterVisual.cs \
  Assets/Scripts/Framework/SuperficieVariada.cs \
  Assets/Scripts/Framework/MarcadorTemporal.cs \
  Assets/_Polish/Fonts \
  Assets/_Polish/Textures \
  docs/AUDITORIA_RUBRICA.md \
  docs/PASADA_3_VISUAL.md

git status --short
git commit -m "HUD en espanol, fuego por capas, vapor de apagado, ritmo de demo"
```

`HUDController.cs` va dentro a propósito: trae la corrección de los dos
errores CS1061 más los textos en español.

---

## PASO 1 · Abrir Unity, esperar, mirar la consola

Errores rojos → para y mándamelos. Warnings amarillos se ignoran.

---

## PASO 2 · Ejecutar

Con **MainScene abierta**:

```
Tools → Fire Rescue → Aplicar TODO el polish visual
```

Verás en consola un mensaje avisando que el ritmo de la demo pasó de 0.5 a
1.2 segundos por turno.

---

## PASO 3 · Guardar

```
Ctrl + S
```

---

## PASO 4 · Probar

```powershell
cd Server
python server.py
```

Unity: **Play**.

- [ ] Cero errores rojos
- [ ] **Ni una sola palabra en inglés en pantalla**
- [ ] El HUD es notoriamente más grande que antes
- [ ] Ya no hay línea roja junto al título, hay un icono de llama naranja
- [ ] Ningún texto encimado
- [ ] Al arrancar **NO** sale la pantalla negra de "sin conectar" con ceros
- [ ] Arriba a la izquierda dice ESTRATEGIA · MEJORADA
- [ ] El fuego se ve como llama con base, cuerpo y punta, no como bola
- [ ] Las llamas están de pie, no acostadas
- [ ] El humo es gris, crece al subir y no brilla
- [ ] Al apagar un fuego sale una ráfaga de vapor blanco
- [ ] Los bomberos no salen todos al mismo instante, van escalonados
- [ ] El piso y las paredes no repiten el mismo dibujo celda por celda
- [ ] La cámara sigue moviéndose con WASD, rueda y F
- [ ] **Cuando un bombero carga a alguien, se le ve el civil al hombro**
- [ ] Al levantarlo aparece la víctima un momento en esa celda
- [ ] Al dejarlo en una salida sale un destello verde
- [ ] La pantalla final **no** es una caja centrada: título grande a la
      izquierda, tablero visible detrás, sin borde ni línea vertical
- [ ] La regla bajo el título es verde si ganaste y roja si perdiste
- [ ] Las estadísticas finales salen en dos columnas alineadas

**El punto que decide todo:** con `Seed 1` en el inspector, la consola tiene
que decir exactamente:

```
victoria en 38 turnos. Rescatadas: 7, perdidas: 1, daño: 15.
```

Yo corrí el modelo aparte y da ese resultado. Si te sale otro número, algo
tocó lo que no debía.

Con `Seed 42` tiene que dar `derrota_colapso, 51 turnos, 6, 3, 24`.

---

## PASO 5 · Grabar

**Antes de darle Play:**

1. En la barra del Game View, el menú que dice `Play Focused` cámbialo a
   **`Play Maximized`**.
2. Aspecto: **Full HD (1920x1080)**.
3. Scale: **1**.

O pon el cursor sobre la pestaña Game y pulsa **Shift + Espacio** para
maximizar el panel a mano.

El `Scale` de esa barra es zoom del editor, no resolución. Que se viera
chico a 0.53 no era un error del Canvas, era el tamaño del panel.

Con 1.2 segundos por turno, una partida de 38 turnos dura unos 45 segundos.

---

## Ajustes desde el Inspector, sin tocar código

| Si no te gusta | Dónde | Qué mover |
|---|---|---|
| El ritmo de la demo | `Systems > Simulation Client` | `Seconds Between Steps` |
| Qué partida se juega | `Systems > Simulation Client` | `Seed` |
| Qué estrategia corre | `Systems > Simulation Client` | `Strategy` |
| El oscurecido de arriba | `GameHUD > VinetaSuperior` | Alpha del `Image` |
| El oscurecido de la esquina | `GameHUD > VinetaEsquina` | Alpha, o Width/Height |
| Posición de un bloque del HUD | `GameHUD > Identidad`/`Turno`/`Objetivos` | Pos X, Pos Y |
| Tamaño de un texto | El objeto de texto | `Font Size` |
| El naranja del emblema | `GameHUD > Identidad > Emblema` | Color del `Image` |
| Cuánto se escalonan los bomberos | `Firefighter.prefab > Firefighter Visual` | `Escalon Por Id` |
| Intensidad del fuego | `Assets/_Polish/Prefabs/FireVisual` | `Rate over Time` de cada capa |
| Que no salga vapor al apagar | `Board Manager` | vaciar `Steam Prefab` |
| Variación entre celdas | `Cell.prefab > Superficie Variada` | `Variacion Brillo` |
| Cuánto dura la víctima encontrada | `Board Manager` | `Segundos Victima Visible` (0 la apaga) |
| Que no salga destello al rescatar | `Board Manager` | vaciar `Rescue Prefab` |
| El civil al hombro estorba | `Firefighter.prefab > Visual > VictimaCargada` | Position, Scale, o bórralo |
| Tamaño del título final | `GameHUD > EndGameOverlay > Contenido > Titulo` | `Font Size` |
| Posición del bloque final | `GameHUD > EndGameOverlay > Contenido` | Pos X, Pos Y |

---

## Si algo sale mal

| Problema | Qué hacer |
|---|---|
| Errores rojos | Manda el error completo. No sigas. |
| El HUD sale con tipografía de Unity | Mira el warning en consola. Funciona igual. |
| El fuego sigue viéndose como antes | El paso 2 del menú no corrió. Ejecútalo suelto. |
| Las llamas salen acostadas | El renderer volvió a Billboard. En `FireVisual > LlamaCuerpo > Renderer`, ponlo en `Vertical Billboard`. |
| Sale vapor donde no debería | Vacía `Steam Prefab` en el Board Manager. |
| Las estadísticas finales salen en una sola columna | La etiqueta `<pos=>` de TMP no se aplicó. Se lee igual, no es error. |
| El civil al hombro sale del color equivocado | `Assets/_Polish/Materials/M_Victima`, cambia el Albedo. |
| Todo peor que antes | `git status --short`, revierte solo el archivo culpable |

---

## Lo que NO se tocó

- `Server/` completo, congelado y validado.
- El contrato JSON entre Python y Unity.
- `SimulationClient.cs` (el archivo; solo se cambió un valor del inspector).
- `TopDownCameraController.cs`.
- Los CSV de resultados.
- La rama: `carlos`, sin merge, sin rebase, sin reset.

---

## Por qué antes no se veían las víctimas

Un `/step` es un turno entero. Dentro del mismo turno un bombero puede
llegar al POI, revelarlo y cargarlo. Unity solo recibe el estado del final
del turno, así que ese marcador de víctima **nunca existe** en ningún
estado y la partida puede terminar con siete rescatadas sin que se haya
visto a nadie.

La solución no necesitó tocar Python: el JSON ya trae
`bomberos[i].cargando` (`flashpoint_model.py` línea 612, y el campo ya
estaba en `SimulationState.cs`). Comparando ese campo entre dos estados
seguidos se sabe cuándo levantan a alguien y cuándo lo entregan.

Ahora se ve la secuencia completa: POI azul girando, víctima al revelarse,
civil al hombro del bombero mientras camina, destello verde en la salida.

---

## Lo que sigue después de grabar

Por orden de puntos que faltan, según la auditoría:

1. **Narrativa, 10%.** No existe nada escrito. Es el hueco más grande.
2. **Reporte de rendimiento**, parte del 20%. Los números ya están.
3. **Innovación, 5%.** El material existe, falta redactarlo.
4. **Presentación, 5%.**
