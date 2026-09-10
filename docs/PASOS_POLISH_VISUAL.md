# Punto de partida: aplicar el polish visual

Checklist de Carlos. Entrega: jueves 10 de septiembre, 10:00.
Todo el código y los modelos ya están escritos en el proyecto.
Falta ejecutarlo dentro de Unity, que es lo único que no se puede hacer
desde fuera.

---

## PASO 0 · Checkpoint con rutas exactas (30 segundos, no lo saltes)

Nada de `git add -A`: mete SampleScene, ProjectSettings y otros archivos
que Unity ensucia solos con finales de línea. Solo lo nuevo:

```bash
git add -- \
  Assets/Scripts/Framework/BoardManager.cs \
  Assets/Scripts/Framework/FirefighterVisual.cs \
  Assets/Scripts/Framework/FirefighterVisual.cs.meta \
  Assets/Scripts/Framework/HUDController.cs \
  Assets/Scripts/Framework/HUDController.cs.meta \
  Assets/Scripts/Framework/PulseVisual.cs \
  Assets/Scripts/Framework/PulseVisual.cs.meta \
  Assets/Editor/FireRescuePolish.cs \
  Assets/Editor/FireRescuePolish.cs.meta \
  Assets/Models

git status --short
git commit -m "Capa visual: HUD, VFX, modelos y polish de materiales"
```

Fuera a propósito: `SampleScene`, `MainScene.unity.meta`, todo
`ProjectSettings/` y `docs/MainScene.antes-de-camara.unity.bak`.

Si algo se rompe más adelante: **no reviertas carpetas enteras.** Corre
`git status --short`, identifica el archivo concreto y revierte solo ese:

```bash
git checkout -- <ruta/exacta/del/archivo>
```

Nunca `git checkout -- Assets` ni `git checkout -- ProjectSettings`: se
llevarían por delante cosas que no tienen que ver con el problema.

---

## PASO 1 · Abrir Unity y esperar la compilación

Al abrir, Unity va a importar 11 modelos OBJ y compilar 5 scripts nuevos.
Tarda un poco. **Mira la consola.**

- Consola limpia o solo warnings amarillos → sigue al paso 2.
- **Errores rojos → PARA. Copia el error completo y mándalo.** Sin
  compilar, nada de lo demás funciona.

Warning conocido y sin importancia:
`MaterialLocation.External is obsolete`.

---

## PASO 2 · TextMeshPro (solo la primera vez)

```
Window → TextMeshPro → Import TMP Essential Resources → Import
```

Es un clic. Sin esto el HUD no tiene tipografía y el paso 5 se detiene
solo avisando.

---

## PASO 3 · Ejecutar el polish

```
Tools → Fire Rescue → Aplicar TODO el polish visual
```

Corre cinco bloques en orden. Si prefieres ir uno por uno para ver qué
hace cada cual, están sueltos en el mismo menú:

| Paso | Qué hace |
|---|---|
| 1 Materiales e iluminación | 20 materiales, luz direccional, ambiente en gradiente, niebla |
| 2 Fuego y humo | textura suave de partícula, prefabs FireVisual y SmokeVisual |
| 3 Personajes y marcadores | modelos en los prefabs, aro de suelo, POI giratorio |
| 4 Vestir habitaciones | 14 muebles en Environment/Props |
| 5 Construir HUD | Canvases/GameHUD completo |

---

## PASO 4 · Guardar

```
Ctrl + S
```

Sin esto se pierde todo lo de la escena (HUD, luz, muebles). Los
materiales y prefabs sí quedan guardados solos.

---

## PASO 5 · Probar

Una terminal:

```powershell
cd Server
python server.py
```

Unity: **Play**.

Checklist de lo que debe pasar:

- [ ] Cero errores rojos en consola
- [ ] El HUD aparece arriba a la izquierda con turno, rescatadas, perdidas y daño
- [ ] Los números del HUD cambian solos conforme avanza la partida
- [ ] La barra de daño cambia de verde a amarillo a rojo
- [ ] Los bomberos se deslizan entre celdas en vez de saltar
- [ ] Los bomberos giran hacia donde caminan
- [ ] **Los seis bomberos tienen colores distintos** (rojo, azul, amarillo, verde, morado, naranja)
- [ ] Los seis conservan el mismo casco amarillo y el mismo tanque oscuro
- [ ] El fuego se ve como llama animada, no como cubos
- [ ] El humo es gris, lento y grande: no se confunde con el fuego
- [ ] Los POI sin revelar son un rombo azul que gira
- [ ] Al revelarse aparece una víctima acostada o una falsa alarma gris
- [ ] Las salidas se ven verdes y brillan
- [ ] Hay muebles en las habitaciones y no tapan el tablero
- [ ] Al terminar la partida sale la pantalla final con las estadísticas
- [ ] La cámara sigue moviéndose con WASD, rueda y F

---

## PASO 6 · Grabar el video ANTES de seguir tocando nada

En cuanto la lista de arriba pase, **graba**. Aunque falten detalles.
Un video de algo que funcionaba a las 7 vale más que una demo en vivo
que falla a las 10.

---

## Si algo sale mal

| Problema | Qué hacer |
|---|---|
| Errores rojos al compilar | Manda el error completo. No sigas. |
| El HUD no aparece | Falta el paso 2 de TextMeshPro. Hazlo y corre solo el paso 5 del menú. |
| Los modelos salen gigantes o enterrados | Selecciona el hijo `Visual` del prefab y ajusta su Y. Nada más. |
| El fuego sigue viéndose como cubos | El paso 2 del menú no corrió. Ejecútalo suelto. |
| Los muebles estorban o atraviesan una pared | Están en `Environment/Props`. Muévelos o bórralos como cualquier objeto de la escena. |
| Los seis bomberos salen del mismo color | Selecciona `Assets/Prefabs/Firefighter.prefab` y revisa que `Firefighter Visual > Colores Por Id` tenga los 6 materiales. |
| Todo se ve peor que antes | **No hagas un rollback amplio.** Corre `git status --short`, mira qué cambió de verdad y revierte solo esos archivos, uno por uno. |

---

## Lo que NO hay que tocar

- `Server/` completo. El backend está congelado y validado.
- El contrato JSON entre Python y Unity.
- La rama: se trabaja en `carlos`, sin merge, sin rebase, sin reset.

---

## Después del video

1. Reporte
2. Presentación de 10 minutos
3. Push final a `origin/carlos`
