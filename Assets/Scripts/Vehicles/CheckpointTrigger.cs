using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [Tooltip("Which number checkpoint is this?")]
    public int checkpointIndex;
    public bool isFinishLine = false;

    private void OnTriggerEnter(Collider other)
    {
        if (RaceManager.Instance == null) return;

        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player")) 
        {
            // Pass this exact checkpoint's transform so the manager knows where to respawn you
            RaceManager.Instance.PlayerHitCheckpoint(checkpointIndex, this.transform);
        }
        else if (isFinishLine && (other.CompareTag("AI") || other.transform.root.CompareTag("AI")))
        {
            RaceManager.Instance.RegisterAIFinish(other.transform.root);
        }
    }
}