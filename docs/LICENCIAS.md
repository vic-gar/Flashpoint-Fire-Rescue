# Licencias de recursos de terceros

Casi todo lo visual del proyecto lo hicimos nosotros. Lo que no, está aquí.

---

## Tipografía: Barlow Condensed

- Archivos: `Assets/_Polish/Fonts/BarlowCondensed-{SemiBold,Medium,Bold}.ttf`
- Autor: Jeremy Tribby y The Barlow Project Authors
- Origen: https://github.com/google/fonts/tree/main/ofl/barlowcondensed
- Licencia: **SIL Open Font License 1.1**
- Texto completo: `Assets/_Polish/Fonts/OFL-BarlowCondensed.txt`

La OFL permite usar, modificar y redistribuir la fuente, incluso en proyectos
comerciales. Pide que el texto de la licencia viaje con los archivos, que es
por lo que el `OFL-BarlowCondensed.txt` está dentro de la misma carpeta.

Se usa en todo el HUD y en la pantalla de fin de partida.

---

## Iconos: Lucide

- Archivos: `Assets/_Polish/Textures/T_Icon*.png`
- Origen: https://github.com/lucide-icons/lucide
- Licencia: **ISC License**

Iconos usados: `user-round-check` (civiles rescatados), `skull` (civiles
perdidos), `building-2` (daño estructural) y `flame` (emblema del título).
Se descargaron como SVG, se rasterizaron a PNG en blanco y Unity los tinta
desde el HUD.

Texto de la licencia:

```
ISC License

Copyright (c) 2026 Lucide Icons and Contributors

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
```

---

## Recursos hechos para el proyecto

No llevan licencia de terceros:

- Los once modelos OBJ de `Assets/Models/FireRescue/`: bombero, víctima y
  nueve muebles.
- Las texturas de piso, pared, llama, chispa, humo y las viñetas del HUD, en
  `Assets/_Polish/Textures/`.
- Los 27 materiales de `Assets/_Polish/Materials/`.
- Los prefabs de partículas: fuego, humo, vapor y rescate.

---

## Software

- **Unity 6.5** (6000.5.7f1), licencia educativa. Pipeline Built-in.
- **Mesa 3.5.1**, licencia Apache 2.0. https://github.com/projectmesa/mesa
- **TextMesh Pro**, incluido en `com.unity.ugui`, licencia Unity Companion.

---

## El juego original

*Flash Point: Fire Rescue* es un juego de mesa de Kevin Lanzing publicado por
Indie Boards & Cards. Este proyecto es una implementación de sus reglas con
fines académicos, para la materia TC2008B. No incluye arte ni componentes del
juego original: el tablero, los modelos y las reglas están reimplementados
desde el reglamento.
