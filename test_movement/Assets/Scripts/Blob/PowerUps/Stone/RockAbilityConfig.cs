using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Rock/Ability Config", fileName = "RockAbilityConfig")]
public sealed class RockAbilityConfig : ScriptableObject
{
    [SerializeField, Min(0.1f)] private float destructionRadius = 0.75f;
    [SerializeField] private Vector2 destructionOffset;
    [SerializeField, Min(0.1f)] private float hopHeight = 1f;
    [SerializeField, Min(0.1f)] private float slamSpeed = 20f;
    [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.12f;

    public float DestructionRadius => destructionRadius;
    public Vector2 DestructionOffset => destructionOffset;
    public float HopHeight => hopHeight;
    public float SlamSpeed => slamSpeed;
    public float GroundProbeDistance => groundProbeDistance;
}
