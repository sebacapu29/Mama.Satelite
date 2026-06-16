using UnityEngine;

/// <summary>
/// Colocar en el objeto "escondite" (ej: cama).
/// Requiere un Transform hijo "HidePoint" posicionado donde debe quedar la camara del jugador.
/// </summary>
public class HidingSpot : MonoBehaviour
{
    [SerializeField] private Transform hidePoint;

    public void Interact(PlayerMovement player)
    {
        player.EnterHiding(hidePoint);
        
    }
}
