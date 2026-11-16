using BlobGame.Player;
using UnityEngine;

public class DeathPlane : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        BlobController player = other.GetComponentInParent<BlobController>();
        if (player == null)
            return;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;

        player.transform.position = player.LastGroundedPosition;
    }
}
