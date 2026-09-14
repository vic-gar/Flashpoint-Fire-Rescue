# Flash Point: Fire Rescue — simulación multiagente

Proyecto de la materia TC2008B, Modelación de Sistemas Multiagentes con
Gráficas Computacionales. Tec de Monterrey, agosto–diciembre 2026.

Es una simulación del juego de mesa *Flash Point: Fire Rescue* con seis
bomberos que entran a un edificio en llamas a sacar civiles. Las reglas y los
agentes corren en Python con Mesa; Unity solo dibuja. Los dos se comunican por
HTTP: Unity pide un turno, Python lo calcula y devuelve el estado del tablero.

Se implementaron tres variantes de estrategia para comparar el comportamiento
de los agentes: una estrategia aleatoria, una estrategia mejorada con A\* y
priorización de objetivos, y una variante mejorada sin coordinación para
aislar el efecto de la coordinación entre bomberos.

## Equipo

| Integrante | Matrícula |
|---|---|
| Víctor Adrián García Galván | A01713062 |
| Carlos Arturo Gómez Ayala | A01711027 |
| Luis Fernando Martínez Barragán | A01613426 |

## Requisitos

- Unity 6.5 (6000.5.7f1), pipeline Built-in
- Python 3.12 o superior (Mesa 3.5.1 no instala en versiones anteriores)
- Las dependencias de Python están en `Server/requirements.txt`

## Instalar y correr

Primero el servidor:

```bash
cd Server
pip install -r requirements.txt
python server.py
```

Queda escuchando en `http://localhost:3000`. Se puede comprobar abriendo esa
dirección en el navegador: responde con la estrategia activa y la lista de
estrategias disponibles.

Después Unity:

1. Abrir el proyecto con Unity 6.5.
2. Abrir `Assets/Scenes/MainScene.unity`.
3. Darle Play.

Unity manda `POST /reset` al arrancar y luego un `POST /step` cada 1.2
segundos hasta que la partida termina.

## Configuración desde el inspector

Todo se cambia en el objeto `Systems`, componente `Simulation Client`:

| Campo | Qué hace |
|---|---|
| `Strategy` | `aleatoria`, `mejorada` o `mejorada_sin_coordinacion` |
| `Seed` | Semilla de la partida. La misma semilla da siempre la misma partida |
| `Seconds Between Steps` | Cada cuánto pide Unity el siguiente turno. No afecta al resultado |
| `Port` | Puerto del servidor, 3000 por defecto |

## Las tres estrategias

- **aleatoria**: elige al azar entre las acciones legales. Es la línea base
  que pide el reto.
- **mejorada**: A\* sobre la topología real del tablero (paredes, puertas y
  fuego), priorización de qué víctima atender y una tabla compartida para que
  dos bomberos no vayan por la misma.
- **mejorada_sin_coordinacion**: igual que la anterior pero sin la tabla
  compartida. Sirve para medir cuánto aporta coordinar, por separado del resto
  de las mejoras.

## Experimentos

```bash
cd Server
python run_batch.py 100 comparar
```

Corre las tres estrategias con las semillas 0 a 99, cada una con el mismo
tablero y los mismos dados, y escribe los CSV de resultados. Tarda unos once
segundos.

## Pruebas

```bash
cd Server
python -m unittest discover -s tests -v
```

150 pruebas: reglas del juego, servidor HTTP, estrategias, separación de
generadores aleatorios y el espacio de Mesa.

## Uso de IA

El uso de herramientas de IA generativa se documenta en
`docs/USO_DE_IA.txt`.

## Estructura

```
Assets/
    Scenes/MainScene.unity      la escena del proyecto
    Scripts/Data/               clases del JSON y lectura del tablero
    Scripts/Framework/          BoardManager, cliente HTTP, cámara, visuales
    Prefabs/                    celda, muro, puerta, salida, fuego, humo,
                                bombero, POI, víctima, falsa alarma
    Models/FireRescue/          modelos OBJ hechos para el proyecto
    _Polish/                    materiales, texturas y tipografía
    Resources/final.txt         el tablero que lee Unity

Server/
    model/                      reglas, tablero y fase del fuego
    agents/                     el agente bombero
    strategies/                 las tres estrategias
    tests/                      150 pruebas
    data/final.txt              el mismo tablero, para Python
    server.py                   servidor HTTP
    run_batch.py                experimentos

docs/
    REPORTE_FINAL.md            reporte del proyecto, con la narrativa
    USO_DE_IA.txt               declaración de uso de IA
    LICENCIAS.md                licencias de tipografía e iconos
```

El tablero `final.txt` está dos veces a propósito: Unity lo lee desde
`Assets/Resources/` y Python desde `Server/data/`. Son idénticos.

## API del servidor

`Server/README.md` documenta los cinco endpoints y el contrato JSON completo.
En corto:

| Ruta | Qué hace |
|---|---|
| `GET /` | información y estrategias disponibles |
| `GET /board` | geometría fija: celdas, paredes, puertas y salidas |
| `GET /state` | estado actual de la partida |
| `POST /step` | avanza un turno |
| `POST /reset` | reinicia; acepta `{"semilla": n, "estrategia": "..."}` |
