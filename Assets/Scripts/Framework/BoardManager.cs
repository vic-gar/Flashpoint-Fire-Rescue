using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye el tablero de Flash Point a partir del archivo de
/// configuración y lo instancia en la escena.
///
/// Se puede usar de dos maneras:
///
/// - En Play: Start() construye el tablero como siempre.
/// - En el editor: los botones "Generate Preview" y "Clear Preview"
///   del inspector permiten ver y acomodar el tablero sin entrar a
///   Play.
///
/// Todo lo que genera queda etiquetado con BoardPreviewMarker, así
/// que siempre se puede limpiar exactamente lo generado sin tocar
/// los objetos propios de la escena. Start() limpia antes de
/// construir, de modo que si quedó un preview guardado en la escena
/// no aparecen objetos duplicados al darle Play.
/// </summary>
public class BoardManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject firePrefab;
    public GameObject poiPrefab;
    public GameObject exitPrefab;
    public GameObject firefighterPrefab;

    [Tooltip("Opcional. Si se deja vacío se reutiliza el prefab de POI.")]
    public GameObject victimPrefab;

    [Tooltip("Opcional. Si se deja vacío el humo se dibuja con el prefab de fuego.")]
    public GameObject smokePrefab;

    [Tooltip("Opcional. Marcador de falsa alarma ya revelada.")]
    public GameObject falseAlarmPrefab;

    public Transform firefightersParent;

    public int rows = 6;
    public int columns = 8;

    public float cellSize = 1.0f;

    private BoardData boardData;

    // Referencias a los objetos que cambian durante la partida, para
    // poder moverlos o retirarlos cuando llega un estado nuevo del
    // servidor en vez de reconstruir el tablero entero cada turno.
    private readonly Dictionary<int, GameObject> firefighterObjects =
        new Dictionary<int, GameObject>();

    private readonly Dictionary<string, GameObject> fireObjects =
        new Dictionary<string, GameObject>();

    private readonly Dictionary<string, GameObject> smokeObjects =
        new Dictionary<string, GameObject>();

    private readonly Dictionary<string, GameObject> poiObjects =
        new Dictionary<string, GameObject>();

    // Qué prefab se usó para cada POI. Sin esto no se puede saber si un
    // marcador ya dibujado corresponde al estado actual: un POI que se
    // revela como víctima seguiría viéndose como marcador de búsqueda.
    private readonly Dictionary<string, GameObject> poiPrefabUsed =
        new Dictionary<string, GameObject>();

    void Start()
    {
        // Si la escena venía con un preview guardado, se retira antes
        // de construir para no terminar con el tablero por duplicado.
        ClearBoard();

        BuildBoard();
    }

    // =========================================================
    // API pública: construir y limpiar
    // =========================================================

    /// <summary>
    /// Lee el archivo del tablero y genera todos los objetos.
    /// Funciona igual en Play y en el editor.
    /// </summary>
    public void BuildBoard()
    {
        boardData = BoardFileReader.LoadBoard("final");

        if (boardData == null)
        {
            return;
        }

        GenerateBoard();
        GenerateDoors();
        GenerateFires();
        GeneratePOIs();
        GenerateExits();
        GenerateFirefighters();
    }

    /// <summary>
    /// Elimina únicamente los objetos generados por este componente.
    /// Los objetos que forman parte de la escena original no se tocan
    /// porque no llevan la etiqueta BoardPreviewMarker.
    /// </summary>
    public int ClearBoard()
    {
        BoardPreviewMarker[] generated =
            FindObjectsByType<BoardPreviewMarker>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int removed = 0;

        foreach (BoardPreviewMarker marker in generated)
        {
            if (marker == null)
            {
                continue;
            }

            DestroySafely(marker.gameObject);
            removed++;
        }

        firefighterObjects.Clear();
        fireObjects.Clear();
        smokeObjects.Clear();
        poiObjects.Clear();
        poiPrefabUsed.Clear();

        return removed;
    }

    /// <summary>
    /// Cuenta cuántos objetos generados hay ahora en la escena.
    /// El inspector lo usa para informar el estado del preview.
    /// </summary>
    public int CountGeneratedObjects()
    {
        return FindObjectsByType<BoardPreviewMarker>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ).Length;
    }

    // =========================================================
    // Instanciación
    // =========================================================

    /// <summary>
    /// Instancia un prefab y lo marca como generado por el tablero.
    /// Todas las creaciones pasan por aquí para que nunca quede un
    /// objeto sin etiquetar que después no se pueda limpiar.
    /// </summary>
    private GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        string name
    )
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(
            prefab,
            position,
            rotation,
            parent
        );

        if (!string.IsNullOrEmpty(name))
        {
            instance.name = name;
        }

        if (instance.GetComponent<BoardPreviewMarker>() == null)
        {
            instance.AddComponent<BoardPreviewMarker>();
        }

        return instance;
    }

    /// <summary>
    /// Destruye un objeto de forma válida tanto en Play como en el
    /// editor. Destroy no surte efecto inmediato fuera de Play, por
    /// eso ahí se usa DestroyImmediate.
    /// </summary>
    private void DestroySafely(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    // =========================================================
    // Generación del escenario
    // =========================================================

    void GenerateBoard()
    {
        for (int row = 0; row < boardData.rows; row++)
        {
            for (int column = 0; column < boardData.columns; column++)
            {
                Vector3 position = new Vector3(
                    column * cellSize,
                    0,
                    -row * cellSize
                );

                GameObject cellObject = Spawn(
                    cellPrefab,
                    position,
                    Quaternion.identity,
                    transform,
                    null
                );

                if (cellObject != null)
                {
                    CellView cellView = cellObject.GetComponent<CellView>();

                    if (cellView != null)
                    {
                        cellView.Initialize(row, column);
                    }
                }

                CellData data = boardData.cells[row][column];

                CreateWalls(data, position);
            }
        }
    }

    void GenerateFires()
    {
        foreach (FireData fire in boardData.fires)
        {
            Vector3 position = new Vector3(
                fire.column * cellSize,
                0.25f,
                -fire.row * cellSize
            );

            GameObject fireObject = Spawn(
                firePrefab,
                position,
                Quaternion.identity,
                transform,
                $"Fire_{fire.row + 1}_{fire.column + 1}"
            );

            if (fireObject != null)
            {
                fireObjects[CellKey(fire.row, fire.column)] = fireObject;
            }
        }
    }

    void GenerateDoors()
    {
        foreach (DoorData door in boardData.doors)
        {
            Vector3 cell1Position = new Vector3(
                door.column1 * cellSize,
                0,
                -door.row1 * cellSize
            );

            Vector3 cell2Position = new Vector3(
                door.column2 * cellSize,
                0,
                -door.row2 * cellSize
            );

            Vector3 doorPosition =
                (cell1Position + cell2Position) / 2f;

            Quaternion rotation = Quaternion.identity;

            // Si cambia la columna, la puerta divide izquierda/derecha
            if (door.column1 != door.column2)
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }

            Spawn(
                doorPrefab,
                doorPosition + new Vector3(0, 0.5f, 0),
                rotation,
                transform,
                $"Door_{door.row1 + 1}_{door.column1 + 1}_" +
                $"{door.row2 + 1}_{door.column2 + 1}"
            );
        }
    }

    void GenerateExits()
    {
        foreach (ExitData exit in boardData.exits)
        {
            Vector3 position = new Vector3(
                exit.column * cellSize,
                0.08f,
                -exit.row * cellSize
            );

            Spawn(
                exitPrefab,
                position,
                Quaternion.identity,
                transform,
                $"Exit_{exit.row + 1}_{exit.column + 1}"
            );
        }
    }

    void GeneratePOIs()
    {
        foreach (POIData poi in boardData.pois)
        {
            Vector3 position = new Vector3(
                poi.column * cellSize,
                0.15f,
                -poi.row * cellSize
            );

            GameObject poiObject = Spawn(
                poiPrefab,
                position,
                Quaternion.identity,
                transform,
                $"POI_{poi.row + 1}_{poi.column + 1}"
            );

            if (poiObject != null)
            {
                POIView poiView = poiObject.GetComponent<POIView>();

                if (poiView != null)
                {
                    poiView.Initialize(poi);
                }

                poiObjects[CellKey(poi.row, poi.column)] = poiObject;
            }
        }
    }

    void CreateWalls(CellData data, Vector3 cellPosition)
    {
        float half = cellSize / 2f;

        // =========================
        // Pared superior
        // =========================

        bool doorUp = false;

        if (data.row > 0)
        {
            doorUp = HasDoorBetween(
                data.row,
                data.column,
                data.row - 1,
                data.column
            );
        }

        if (data.wallUp && !doorUp)
        {
            Spawn(
                wallPrefab,
                cellPosition + new Vector3(0, 0.5f, half),
                Quaternion.identity,
                transform,
                $"Wall_Up_{data.row + 1}_{data.column + 1}"
            );
        }

        // =========================
        // Pared izquierda
        // =========================

        bool doorLeft = false;

        if (data.column > 0)
        {
            doorLeft = HasDoorBetween(
                data.row,
                data.column,
                data.row,
                data.column - 1
            );
        }

        if (data.wallLeft && !doorLeft)
        {
            Spawn(
                wallPrefab,
                cellPosition + new Vector3(-half, 0.5f, 0),
                Quaternion.Euler(0, 90, 0),
                transform,
                $"Wall_Left_{data.row + 1}_{data.column + 1}"
            );
        }

        // =========================
        // Borde inferior
        // =========================

        if (data.row == rows - 1 && data.wallDown)
        {
            Spawn(
                wallPrefab,
                cellPosition + new Vector3(0, 0.5f, -half),
                Quaternion.identity,
                transform,
                $"Wall_Down_{data.row + 1}_{data.column + 1}"
            );
        }

        // =========================
        // Borde derecho
        // =========================

        if (data.column == columns - 1 && data.wallRight)
        {
            Spawn(
                wallPrefab,
                cellPosition + new Vector3(half, 0.5f, 0),
                Quaternion.Euler(0, 90, 0),
                transform,
                $"Wall_Right_{data.row + 1}_{data.column + 1}"
            );
        }
    }

    bool HasDoorBetween(
        int row1,
        int column1,
        int row2,
        int column2
    )
    {
        foreach (DoorData door in boardData.doors)
        {
            bool sameDirection =
                door.row1 == row1 &&
                door.column1 == column1 &&
                door.row2 == row2 &&
                door.column2 == column2;

            bool oppositeDirection =
                door.row1 == row2 &&
                door.column1 == column2 &&
                door.row2 == row1 &&
                door.column2 == column1;

            if (sameDirection || oppositeDirection)
            {
                return true;
            }
        }

        return false;
    }

    void GenerateFirefighters()
    {
        foreach (FirefighterData firefighter in boardData.firefighters)
        {
            Vector3 position = new Vector3(
                firefighter.column * cellSize,
                0.4f,
                -firefighter.row * cellSize
            );

            // Si no se asignó un contenedor en el inspector se usa el
            // propio Board, para que el preview nunca deje objetos
            // sueltos en la raíz de la escena.
            Transform parent = firefightersParent != null
                ? firefightersParent
                : transform;

            GameObject firefighterObject = Spawn(
                firefighterPrefab,
                position,
                Quaternion.identity,
                parent,
                null
            );

            if (firefighterObject != null)
            {
                FirefighterView view =
                    firefighterObject.GetComponent<FirefighterView>();

                if (view != null)
                {
                    view.Initialize(firefighter);
                }

                firefighterObjects[firefighter.id] = firefighterObject;
            }
        }
    }

    // =========================================================
    // Sincronización con el servidor
    // =========================================================

    /// <summary>
    /// Actualiza la escena con el estado que envía el servidor.
    ///
    /// En vez de reconstruir el tablero cada turno, se mueven los
    /// bomberos y se crean o retiran únicamente los marcadores que
    /// cambiaron. Así la simulación se ve fluida y no se pierde lo
    /// que el usuario tenga seleccionado en la escena.
    /// </summary>
    public void ApplyState(SimulationState state)
    {
        if (state == null)
        {
            return;
        }

        SyncFirefighters(state.bomberos);

        SyncMarkers(
            fireObjects,
            state.fuegos,
            firePrefab,
            "Fire",
            0.25f,
            1.0f
        );

        // El humo tiene su propio prefab. Si no se asignó uno se cae
        // al de fuego a media escala, que es como se veía antes.
        bool humoPropio = smokePrefab != null;

        SyncMarkers(
            smokeObjects,
            state.humos,
            humoPropio ? smokePrefab : firePrefab,
            "Smoke",
            0.2f,
            humoPropio ? 1.0f : 0.5f
        );

        SyncPOIs(state.pois);
    }

    private void SyncFirefighters(FirefighterState[] firefighters)
    {
        if (firefighters == null)
        {
            return;
        }

        foreach (FirefighterState firefighter in firefighters)
        {
            if (!firefighterObjects.TryGetValue(firefighter.id, out GameObject go))
            {
                continue;
            }

            if (go == null)
            {
                continue;
            }

            go.transform.position = CellToWorld(
                firefighter.fila,
                firefighter.columna,
                0.4f
            );
        }
    }

    private void SyncMarkers(
        Dictionary<string, GameObject> registry,
        CellPosition[] positions,
        GameObject prefab,
        string prefix,
        float height,
        float scale
    )
    {
        if (positions == null)
        {
            return;
        }

        HashSet<string> present = new HashSet<string>();

        foreach (CellPosition position in positions)
        {
            string key = CellKey(position.fila, position.columna);

            present.Add(key);

            if (registry.ContainsKey(key) && registry[key] != null)
            {
                continue;
            }

            GameObject marker = Spawn(
                prefab,
                CellToWorld(position.fila, position.columna, height),
                Quaternion.identity,
                transform,
                $"{prefix}_{position.fila + 1}_{position.columna + 1}"
            );

            if (marker == null)
            {
                continue;
            }

            marker.transform.localScale *= scale;

            registry[key] = marker;
        }

        RemoveMissing(registry, present);
    }

    private void SyncPOIs(POIState[] pois)
    {
        if (pois == null)
        {
            return;
        }

        HashSet<string> present = new HashSet<string>();

        foreach (POIState poi in pois)
        {
            string key = CellKey(poi.fila, poi.columna);

            present.Add(key);

            // Qué debería verse ahora mismo en esa celda.
            GameObject prefab = poiPrefab;

            if (poi.revelado && poi.tipo == "v" && victimPrefab != null)
            {
                prefab = victimPrefab;
            }
            else if (poi.revelado && poi.tipo == "f" && falseAlarmPrefab != null)
            {
                prefab = falseAlarmPrefab;
            }

            // Si ya hay un marcador y es del tipo correcto, se deja.
            // Si el POI se acaba de revelar, el prefab cambió y hay que
            // sustituirlo: antes se quedaba el marcador viejo para
            // siempre y víctima y falsa alarma se veían igual.
            if (HasLiveEntry(poiObjects, key))
            {
                bool mismoPrefab =
                    poiPrefabUsed.ContainsKey(key) &&
                    poiPrefabUsed[key] == prefab;

                if (mismoPrefab)
                {
                    continue;
                }

                DestroySafely(poiObjects[key]);
                poiObjects.Remove(key);
            }

            GameObject marker = Spawn(
                prefab,
                CellToWorld(poi.fila, poi.columna, 0.15f),
                Quaternion.identity,
                transform,
                $"POI_{poi.fila + 1}_{poi.columna + 1}"
            );

            if (marker != null)
            {
                poiObjects[key] = marker;
                poiPrefabUsed[key] = prefab;
            }
        }

        RemoveMissing(poiObjects, present);
    }

    private bool HasLiveEntry(
        Dictionary<string, GameObject> registry,
        string key
    )
    {
        return registry.ContainsKey(key) && registry[key] != null;
    }

    private void RemoveMissing(
        Dictionary<string, GameObject> registry,
        HashSet<string> present
    )
    {
        List<string> obsolete = new List<string>();

        foreach (KeyValuePair<string, GameObject> entry in registry)
        {
            if (!present.Contains(entry.Key))
            {
                obsolete.Add(entry.Key);
            }
        }

        foreach (string key in obsolete)
        {
            DestroySafely(registry[key]);
            registry.Remove(key);
        }
    }

    /// <summary>Posición en el mundo del centro de una celda.</summary>
    public Vector3 CellToWorld(int row, int column, float height)
    {
        return new Vector3(
            column * cellSize,
            height,
            -row * cellSize
        );
    }

    private string CellKey(int row, int column)
    {
        return $"{row}_{column}";
    }
}
