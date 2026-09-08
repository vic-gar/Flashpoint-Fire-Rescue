using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Añade al inspector de BoardManager dos botones para ver el
/// tablero en la pestaña Scene sin necesidad de entrar a Play.
///
/// Generate Preview construye el tablero completo (celdas, paredes,
/// puertas, fuego, POI, salidas y bomberos) usando exactamente el
/// mismo código que corre durante el juego, así que lo que se ve en
/// el editor es lo mismo que se verá al darle Play.
///
/// Clear Preview retira lo generado y deja la escena como estaba.
///
/// Este archivo vive en una carpeta Editor, de modo que Unity lo
/// excluye automáticamente de las compilaciones del juego.
/// </summary>
[CustomEditor(typeof(BoardManager))]
public class BoardManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BoardManager manager = (BoardManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(
            "Vista previa en el editor",
            EditorStyles.boldLabel
        );

        int generated = manager.CountGeneratedObjects();

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "El juego está corriendo. El tablero lo construye " +
                "Start(); los botones se rehabilitan al salir de Play.",
                MessageType.Info
            );

            return;
        }

        if (generated > 0)
        {
            EditorGUILayout.HelpBox(
                $"Preview activo: {generated} objetos generados.\n" +
                "Se pueden seleccionar y mover en Scene. Si guardas " +
                "la escena así, el preview queda guardado y el archivo " +
                "MainScene.unity crece bastante. Al darle Play se " +
                "limpia solo, no habrá duplicados.",
                MessageType.None
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No hay preview en la escena. El tablero solo " +
                "aparecería al darle Play.",
                MessageType.None
            );
        }

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Preview", GUILayout.Height(28)))
            {
                GeneratePreview(manager);
            }

            using (new EditorGUI.DisabledScope(generated == 0))
            {
                if (GUILayout.Button("Clear Preview", GUILayout.Height(28)))
                {
                    ClearPreview(manager);
                }
            }
        }
    }

    private void GeneratePreview(BoardManager manager)
    {
        // Se limpia primero para que pulsar el botón dos veces no
        // acumule dos tableros encima.
        manager.ClearBoard();
        manager.BuildBoard();

        int generated = manager.CountGeneratedObjects();

        if (generated == 0)
        {
            Debug.LogWarning(
                "BoardManager: no se generó nada. Revisa que los " +
                "prefabs estén asignados en el inspector y que exista " +
                "Assets/Resources/final.txt.",
                manager
            );

            return;
        }

        MarkSceneDirty(manager);

        Debug.Log(
            $"BoardManager: preview generado con {generated} objetos.",
            manager
        );
    }

    private void ClearPreview(BoardManager manager)
    {
        int removed = manager.ClearBoard();

        MarkSceneDirty(manager);

        Debug.Log(
            $"BoardManager: preview limpiado, {removed} objetos " +
            "eliminados.",
            manager
        );
    }

    private void MarkSceneDirty(BoardManager manager)
    {
        if (Application.isPlaying)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}
