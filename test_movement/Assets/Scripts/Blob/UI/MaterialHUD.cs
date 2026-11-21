using BlobGame.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlobGame.UI
{
    /// <summary>Refreshes the HUD label and background when the player's active profile changes.</summary>
    [DisallowMultipleComponent]
    public sealed class MaterialHUD : MonoBehaviour
    {
        [SerializeField] private BlobController player;
        [SerializeField] private TMP_Text materialLabel;
        [SerializeField] private Image materialBackground;

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
            if (materialLabel != null)
            {
                string displayName = config != null ? config.DisplayName : null;
                materialLabel.text = string.IsNullOrWhiteSpace(displayName)
                    ? "Material: —"
                    : $"Material: {displayName}";
            }

            // Keep color and text tied to the same applied profile notification.
            if (materialBackground != null && config != null)
                materialBackground.color = config.HudBackgroundColor;
        }
    }
}
