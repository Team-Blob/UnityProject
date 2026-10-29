using System.Collections.Generic;
using BlobGame.Player;
using BlobGame.Player.PowerUps;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BlobInputReader))]
public sealed class RockAbility : MonoBehaviour, IConfigurablePowerUpAbility
{
    private const string BreakableTag = "breakable";
    private const float AttackRadius = 1f;

    private BlobInputReader input;
    private GameObject attackObject;
    private CircleCollider2D attackCollider;
    private readonly HashSet<GameObject> overlappedBreakables = new();
    private bool isActive;

    private void Awake()
    {
        input = GetComponent<BlobInputReader>();
    }

    public void Activate(ScriptableObject abilityConfig)
    {
        isActive = true;
        enabled = true;
    }

    private void Update()
    {
        bool attackHeld = isActive
            && input != null
            && input.GameplayInputEnabled
            && Keyboard.current != null
            && Keyboard.current.sKey.isPressed;

        if (!attackHeld)
        {
            RemoveAttackCollider();
            return;
        }

        EnsureAttackCollider();
        DestroyBreakablesInRange();
    }

    private void OnDisable()
    {
        RemoveAttackCollider();
    }

    private void EnsureAttackCollider()
    {
        if (attackObject == null)
        {
            attackObject = new GameObject("Rock Break Collider");
            attackObject.transform.SetParent(transform, false);
            attackCollider = attackObject.AddComponent<CircleCollider2D>();
            attackCollider.isTrigger = true;
        }

        attackObject.transform.localPosition = Vector2.zero;
        attackCollider.radius = AttackRadius;
    }

    private void DestroyBreakablesInRange()
    {
        Vector3 scale = attackObject.transform.lossyScale;
        float radius = attackCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(attackObject.transform.position, radius);
        overlappedBreakables.Clear();
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null)
                continue;

            Transform target = overlap.transform;
            while (target != null)
            {
                if (target == transform.root)
                    break;

                if (target.CompareTag(BreakableTag))
                {
                    overlappedBreakables.Add(target.gameObject);
                    break;
                }

                target = target.parent;
            }
        }

        foreach (GameObject breakable in overlappedBreakables)
            Destroy(breakable);
    }

    private void RemoveAttackCollider()
    {
        if (attackObject != null)
            Destroy(attackObject);

        attackObject = null;
        attackCollider = null;
    }
}
