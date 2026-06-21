using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private HoverOutline currentOutline;
    private PlayerMovement playerMovement;

    private void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    void Update()
    {
        if (playerMovement != null && playerMovement.IsHiding)
        {
            if (currentOutline != null)
            {
                currentOutline.SetOutline(false);
                currentOutline = null;
            }
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            var outline = hit.transform.GetComponentInParent<HoverOutline>();

            if (outline != currentOutline)
            {
                if (currentOutline != null)
                    currentOutline.SetOutline(false);

                currentOutline = outline;

                if (currentOutline != null)
                    currentOutline.SetOutline(true);
            }

            if (Mouse.current.leftButton.wasPressedThisFrame && currentOutline != null)
            {
                var hidingSpot = currentOutline.GetComponent<HidingSpot>();
                if (hidingSpot != null && playerMovement != null)
                    hidingSpot.Interact(playerMovement);
            }
        }
        else
        {
            if (currentOutline != null)
            {
                currentOutline.SetOutline(false);
                currentOutline = null;
            }
        }
    }
}
