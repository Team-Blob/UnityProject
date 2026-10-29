using UnityEngine;

public class Reset_Game : MonoBehaviour
{
    public Transform SpawnPoint;
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("invalid");
        other.transform.position = SpawnPoint.position;
        
    }
}
