using System.Collections.Generic;
using BlobGame.Player;
using BlobGame.Player.PowerUps;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private RockAbilityConfig config;
    private GameObject destructionObject;
    private CircleCollider2D destructionCollider;
    private ContactFilter2D collisionFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private readonly HashSet<GameObject> overlappedBreakables = new();
    private SlamPhase phase;
    private float hopStartY;
    private bool isActive;

    private float DestructionRadius => config != null ? config.DestructionRadius : 0.75f;
    private Vector2 DestructionOffset => config != null ? config.DestructionOffset : Vector2.zero;
    private float HopHeight => config != null ? config.HopHeight : 1f;
    private float SlamSpeed => config != null ? config.SlamSpeed : 20f;
    private float GroundProbeDistance => config != null ? config.GroundProbeDistance : 0.12f;

    private void Awake()
    {
        input = GetComponent<BlobInputReader>();
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
        if (isActive
            && phase == SlamPhase.Inactive
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
        if (phase == SlamPhase.Inactive)
            return;

        if (phase == SlamPhase.Hopping)
        {
            if (body.position.y >= hopStartY + HopHeight || body.linearVelocity.y <= 0f)
                BeginSlam();

            return;
        }

        DestroyBreakablesInRange();
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
        EnsureDestructionCollider();
        body.linearVelocity = new Vector2(body.linearVelocity.x, -SlamSpeed);
    }

    private void EnsureDestructionCollider()
    {
        if (destructionObject == null)
        {
            destructionObject = new GameObject("Rock Slam Destruction Collider");
            destructionObject.transform.SetParent(transform, false);
            destructionCollider = destructionObject.AddComponent<CircleCollider2D>();
            destructionCollider.isTrigger = true;
        }

        destructionObject.transform.localPosition = DestructionOffset;
        destructionCollider.radius = DestructionRadius;
    }

    private void DestroyBreakablesInRange()
    {
        if (destructionCollider == null)
            return;

        Vector3 scale = destructionObject.transform.lossyScale;
        float radius = destructionCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(destructionObject.transform.position, radius);
        overlappedBreakables.Clear();

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null)
                continue;

            Transform target = overlap.transform;
            while (target != null && target != transform.root)
            {
                if (target.CompareTag(BreakableTag))
                {
                    overlappedBreakables.Add(target.gameObject);
                    break;
                }

                target = target.parent;
            }
        }

        foreach (GameObject breakable in overlappedBreakables)
        {
            if (breakable != null)
            {
                breakable.SetActive(false);
                Destroy(breakable);
            }
        }
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
        RemoveDestructionCollider();
    }

    private void OnDisable()
    {
        phase = SlamPhase.Inactive;
        RemoveDestructionCollider();
    }

    private void OnDestroy()
    {
        RemoveDestructionCollider();
    }

    private void RemoveDestructionCollider()
    {
        if (destructionObject != null)
            Destroy(destructionObject);

        destructionObject = null;
        destructionCollider = null;
    }
}
