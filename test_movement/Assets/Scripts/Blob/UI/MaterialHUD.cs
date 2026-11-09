using BlobGame.Player;
using TMPro;
using UnityEngine;

namespace BlobGame.UI
{
    /// <summary>Displays the player's active movement profile without polling it each frame.</summary>
    [DisallowMultipleComponent]
    public sealed class MaterialHUD : MonoBehaviour
    {
        [SerializeField] private BlobController player;
        [SerializeField] private TMP_Text materialLabel;

        private bool hasStarted;

        private void OnEnable()
        {
            if (player != null)
                player.MovementConfigChanged += RefreshMaterial;

            // The player may have changed material while this HUD was disabled.
            if (hasStarted)
                RefreshCurrentMaterial();
        }

        private void Start()
        {
            // The first read must occur after the player's Awake initialization.
            hasStarted = true;
            RefreshCurrentMaterial();
        }

        private void OnDisable()
        {
            if (player != null)
                player.MovementConfigChanged -= RefreshMaterial;
        }

        private void RefreshCurrentMaterial()
        {
            RefreshMaterial(player != null ? player.CurrentMovementConfig : null);
        }

        private void RefreshMaterial(BlobMovementConfig config)
        {
            if (materialLabel == null)
                return;

            string displayName = config != null ? config.DisplayName : null;
            materialLabel.text = string.IsNullOrWhiteSpace(displayName)
                ? "Material: —"
                : $"Material: {displayName}";
        }
    }
}
