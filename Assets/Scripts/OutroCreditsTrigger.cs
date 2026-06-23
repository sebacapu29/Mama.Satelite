using UnityEngine;

public class OutroCreditsTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (OutroCreditsUI.Instance != null)
            OutroCreditsUI.Instance.Show();
        enabled = false;
    }
}
