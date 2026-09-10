# Segunda pasada visual: qué hacer

Todo el código y los archivos ya están escritos en tu proyecto.
Falta ejecutarlo dentro de Unity.

---

## PASO 0 · Checkpoint (30 segundos)

```bash
git add -- \
  Assets/Editor/FireRescuePolish.cs \
  Assets/Scripts/Framework/SuperficieVariada.cs \
  Assets/_Polish/Fonts \
  Assets/_Polish/Textures

git status --short
git commit -m "Pasada visual 2: HUD nuevo, texturas de piso y pared, tipografia"
```

Nada de `git add -A`. Si algo sale mal después, revierte archivo por
archivo con `git checkout -- <ruta exacta>`, nunca carpetas enteras.

---

## PASO 1 · Abrir Unity y esperar

Unity importa 3 TTF, 18 PNG y compila 1 script nuevo.

- Consola limpia o solo warnings amarillos → sigue.
- **Errores rojos → PARA y mándame el error completo.**

---

## PASO 2 · Ejecutar el polish

Con **MainScene abierta** (no una escena Untitled):

```
Tools → Fire Rescue → Aplicar TODO el polish visual
```

El script se niega solo si estás en la escena equivocada.

Si prefieres ir por partes, lo que cambió esta vez es:

| Paso del menú | Qué es nuevo |
|---|---|
| 1 Materiales e iluminación | Piso y pared con textura + normal map. Componente de variación en Cell y Wall. |
| 5 Construir HUD | HUD rehecho de cero: sin tarjetas, tipografía Barlow, iconos Lucide. |

Los pasos 2, 3 y 4 no cambiaron.

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

Checklist:

- [ ] Cero errores rojos
- [ ] El piso se ve de duela de madera, no de color plano
- [ ] Las tablas **no** se repiten idénticas celda por celda
- [ ] Las paredes tienen grano visible, no color liso
- [ ] El HUD ya **no** tiene recuadros: título arriba izquierda, turno arriba derecha, tres medidores abajo izquierda
- [ ] **Ningún texto se encima con otro**
- [ ] La tipografía se ve condensada y angulosa (Barlow), no la de Unity
- [ ] Los iconos son de línea fina, no bloques
- [ ] Los números del HUD siguen cambiando solos
- [ ] La barra de daño sigue cambiando de verde a ámbar a rojo
- [ ] La pantalla final sigue apareciendo al terminar
- [ ] **La consola sigue diciendo el mismo resultado de siempre:**
      `derrota_colapso, 51 turnos, 6 rescatadas, 3 perdidas, daño 24`

El último punto es el importante: si ese número cambió, la capa visual
tocó algo que no debía y hay que revisarlo antes de grabar.

---

## PASO 5 · Grabar

En cuanto la lista pase, **graba el video**. Antes de seguir tocando.

---

## Ajustes rápidos desde el Inspector

Todo esto se toca sin código:

| Si no te gusta | Dónde | Qué mover |
|---|---|---|
| El oscurecido de arriba/abajo pesa mucho | `GameHUD > VinetaSuperior` / `VinetaInferior` | Alpha del `Image`, o el Height del RectTransform |
| El HUD está muy pegado al borde | `GameHUD > Identidad` / `Turno` / `Objetivos` | Pos X y Pos Y del RectTransform |
| Quieres piso de loseta en vez de duela | `Assets/Prefabs/Cell.prefab` | Cambia el material `M_Piso` por `M_PisoAlterno` |
| Las celdas varían demasiado (o muy poco) | `Cell.prefab > Superficie Variada` | `Variacion Brillo` y `Desplazamiento` |
| El rojo del título no te late | `GameHUD > Identidad > Acento` | Color del `Image` |
| Un texto se ve chico o grande | El objeto de texto | `Font Size` del TextMeshPro |

---

## Si algo sale mal

| Problema | Qué hacer |
|---|---|
| Errores rojos al compilar | Manda el error completo. No sigas. |
| El HUD sale con la tipografía vieja | Mira la consola: hay un warning que dice por qué. El HUD funciona igual. |
| Piso y pared siguen de color plano | El paso 1 del menú no corrió. Ejecútalo suelto. |
| Las paredes se ven casi negras | `M_Pared` en `Assets/_Polish/Materials`, sube el Albedo color hacia el gris claro |
| Todo se ve peor que antes | `git status --short`, identifica el archivo y revierte solo ese |

---

## Lo que NO se tocó

- `Server/` completo. Backend congelado y validado.
- El contrato JSON entre Python y Unity.
- `SimulationClient.cs`, `HUDController.cs`, `BoardManager.cs`.
- `TopDownCameraController.cs`. La cámara que ya validaste sigue igual.
- La rama: `carlos`, sin merge, sin rebase, sin reset.
