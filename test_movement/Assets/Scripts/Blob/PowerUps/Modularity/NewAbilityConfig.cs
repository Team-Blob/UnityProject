using BlobGame.Player;
using BlobGame.Player.PowerUps;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAbilityConfig", menuName = "Abilities/New Ability")]
public class NewAbilityConfig : BlobPowerUpEffect
{
    [SerializeField] private ScriptableObject abilityConfig;
    [Tooltip("Assign a component from a prefab that implements IConfigurablePowerUpAbility.")]
    [SerializeField] private MonoBehaviour abilityScript;

    public ScriptableObject AbilityConfig => abilityConfig;
    public MonoBehaviour AbilityScript => abilityScript;

    public override void Apply(BlobController player)
    {
        if (player == null)
            return;

        if (abilityScript == null || !(abilityScript is IConfigurablePowerUpAbility))
        {
            Debug.LogWarning($"Cannot apply {name}: assign an ability script that implements IConfigurablePowerUpAbility.", this);
            return;
        }

        System.Type abilityType = abilityScript.GetType();
        MonoBehaviour ability = player.GetComponent(abilityType) as MonoBehaviour;
        if (ability == null)
            ability = player.gameObject.AddComponent(abilityType) as MonoBehaviour;

        if (ability is IConfigurablePowerUpAbility configurableAbility)
            configurableAbility.Activate(abilityConfig);
    }
}
