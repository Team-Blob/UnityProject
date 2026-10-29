using BlobGame.Player.StateMachine.States;
using Unity.VisualScripting;
using UnityEngine;

public class DeathPlane : MonoBehaviour
{
    public Transform SpawnPoint;
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("invalid");
        other.transform.position = SpawnPoint.position;
        //Destroy(other.gameObject);
    }
}
