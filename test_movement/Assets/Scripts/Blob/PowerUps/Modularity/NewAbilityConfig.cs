using BlobGame.Player;
using BlobGame.Player.PowerUps;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAbilityConfig", menuName = "Blob/Power Ups/Effects/NewAbilityConfig")]
public class NewAbilityConfig : BlobPowerUpEffect
{
    [SerializeField] private ScriptableObject abilityConfig;
    [Tooltip("Assign this script from a prefab so its component type can be added to the player when the power-up is applied.")]
    [SerializeField] private MonoBehaviour abilityScript;
    
    public ScriptableObject AbilityConfig => abilityConfig;
    public MonoBehaviour AbilityScript => abilityScript;

    public override void Apply(BlobController player)
    {
        if (player == null)
            return;
        

        if (abilityScript == null)
        {
            Debug.LogWarning($"Cannot apply {name}: no ability script is assigned.", this);
            return;
        }

        System.Type abilityType = abilityScript.GetType();
        MonoBehaviour ability = player.GetComponent(abilityType) as MonoBehaviour;
        if (ability == null)
            ability = player.gameObject.AddComponent(abilityType) as MonoBehaviour;

        if (ability is Grapple2D grapple)
            grapple.Unlock(abilityConfig as GrappleConfig);
    }
}
