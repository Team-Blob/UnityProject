using BlobGame.Player;
using BlobGame.Player.PowerUps;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(BlobInputReader))]
[DisallowMultipleComponent]
public sealed class RockAbility : MonoBehaviour, IConfigurablePowerUpAbility
{
    private const string BreakableTag = "breakable";

    private enum SlamPhase
    {
        Inactive,
        Hopping,
        Slamming
    }

    private BlobInputReader input;
    private BlobController player;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private RockAbilityConfig config;
    private ContactFilter2D collisionFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private SlamPhase phase;
    private float hopStartY;
    private bool isActive;
    private bool IsRockMovementActive =>
        player != null
        && player.CurrentMovementConfig != null
        && player.CurrentMovementConfig.AbilityDefinition != null
        && player.CurrentMovementConfig.AbilityDefinition.AbilityConfig == config;
    private float HopHeight => config != null ? config.HopHeight : 1f;
    private float SlamSpeed => config != null ? config.SlamSpeed : 20f;
    private float GroundProbeDistance => config != null ? config.GroundProbeDistance : 0.12f;

    private void Awake()
    {
        input = GetComponent<BlobInputReader>();
        player = GetComponent<BlobController>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        collisionFilter = new ContactFilter2D
        {
            useTriggers = false
        };
    }

    public void Activate(ScriptableObject abilityConfig)
    {
        if (abilityConfig != null && !(abilityConfig is RockAbilityConfig))
        {
            Debug.LogWarning("RockAbility requires a RockAbilityConfig.", this);
            return;
        }

        config = abilityConfig as RockAbilityConfig;
        isActive = true;
        enabled = true;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (!isActive || !IsRockMovementActive)
        {
            if (phase != SlamPhase.Inactive)
                CancelSlam();

            return;
        }

        if (phase == SlamPhase.Inactive
            && input != null
            && input.GameplayInputEnabled
            && Keyboard.current != null
            && Keyboard.current.sKey.wasPressedThisFrame)
        {
            BeginHop();
        }
    }

    private void FixedUpdate()
    {
        if (!IsRockMovementActive)
        {
            if (phase != SlamPhase.Inactive)
                CancelSlam();

            return;
        }

        if (phase == SlamPhase.Inactive)
            return;

        if (phase == SlamPhase.Hopping)
        {
            if (body.position.y >= hopStartY + HopHeight || body.linearVelocity.y <= 0f)
                BeginSlam();

            return;
        }

        if (HasGroundBelow())
        {
            FinishSlam();
            return;
        }

        body.linearVelocity = new Vector2(body.linearVelocity.x, -SlamSpeed);
    }

    private void BeginHop()
    {
        phase = SlamPhase.Hopping;
        hopStartY = body.position.y;
        float gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
        float hopVelocity = Mathf.Sqrt(2f * gravity * HopHeight);
        body.linearVelocity = new Vector2(body.linearVelocity.x, hopVelocity);
    }

    private void BeginSlam()
    {
        phase = SlamPhase.Slamming;
        body.linearVelocity = new Vector2(body.linearVelocity.x, -SlamSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBreakBreakable(collision.collider, GetContactPoint(collision));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryBreakBreakable(collision.collider, GetContactPoint(collision));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBreakBreakable(other, other.ClosestPoint(body.worldCenterOfMass));
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryBreakBreakable(other, other.ClosestPoint(body.worldCenterOfMass));
    }

    private Vector2 GetContactPoint(Collision2D collision)
    {
        return collision.contactCount > 0
            ? collision.GetContact(0).point
            : collision.collider.ClosestPoint(body.worldCenterOfMass);
    }

    private void TryBreakBreakable(Collider2D contact, Vector2 contactPoint)
    {
        if (phase != SlamPhase.Slamming
            || contact == null
            || contact.transform.root == transform.root
            || !contact.CompareTag(BreakableTag))
            return;

        Tilemap tilemap = contact.GetComponent<Tilemap>();
        if (tilemap != null)
        {
            RemoveContactedTile(tilemap, contactPoint);
            return;
        }

        contact.gameObject.SetActive(false);
        Destroy(contact.gameObject);
    }

    private void RemoveContactedTile(Tilemap tilemap, Vector2 contactPoint)
    {
        Vector2 towardTile = (contactPoint - body.worldCenterOfMass).normalized;
        Vector3 cellWidth = tilemap.CellToWorld(Vector3Int.right) - tilemap.CellToWorld(Vector3Int.zero);
        Vector3 cellHeight = tilemap.CellToWorld(Vector3Int.up) - tilemap.CellToWorld(Vector3Int.zero);
        float probeDistance = Mathf.Min(cellWidth.magnitude, cellHeight.magnitude) * 0.05f;
        Vector3Int cell = tilemap.WorldToCell(contactPoint + towardTile * probeDistance);

        if (tilemap.HasTile(cell))
            tilemap.SetTile(cell, null);
    }

    private bool HasGroundBelow()
    {
        if (bodyCollider == null)
            return false;

        int hitCount = bodyCollider.Cast(Vector2.down, collisionFilter, groundHits, GroundProbeDistance);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = groundHits[i];
            if (hit.collider == null
                || hit.transform.root == transform.root
                || hit.normal.y < 0.5f
                || IsBreakable(hit.transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsBreakable(Transform target)
    {
        while (target != null)
        {
            if (target.CompareTag(BreakableTag))
                return true;
            target = target.parent;
        }

        return false;
    }

    private void FinishSlam()
    {
        phase = SlamPhase.Inactive;
        body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Max(0f, body.linearVelocity.y));
    }

    private void CancelSlam()
    {
        phase = SlamPhase.Inactive;
    }

    private void OnDisable()
    {
        phase = SlamPhase.Inactive;
    }
}
