using UnityEngine;
using UnityEngine.SceneManagement;

public class Reset_Game : MonoBehaviour
{
    public Transform SpawnPoint;
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("invalid");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
    }
}
