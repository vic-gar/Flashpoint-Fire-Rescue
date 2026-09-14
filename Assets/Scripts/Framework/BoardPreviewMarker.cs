using UnityEngine;

/// <summary>
/// Marca un objeto como generado por BoardManager.
///
/// Sirve para poder distinguir lo que generó el tablero de lo que
/// forma parte de la escena original. Gracias a esta marca, tanto
/// el botón "Clear Preview" del editor como el arranque del juego
/// pueden limpiar exactamente lo generado sin tocar nada más.
///
/// No tiene lógica propia a proposito: solo existe para etiquetar.
/// </summary>
public class BoardPreviewMarker : MonoBehaviour
{
}
