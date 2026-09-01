using UnityEngine;
using UnityEngine.SceneManagement; 

public class PlayerHazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the hook moved into a hazard
        if (other.CompareTag("Hazard"))
        {
            Debug.Log("Hazard tag hit. Restart level. Lives not implemented.");

            // Restart the scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}