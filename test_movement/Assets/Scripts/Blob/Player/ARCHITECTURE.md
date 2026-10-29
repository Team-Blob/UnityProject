# Blob ScriptableObject architecture

Gameplay is organized around a simple boundary: ScriptableObjects hold shared,
authored settings and definitions; components and plain C# objects hold changing
runtime state for one player or one run.

## Config assets

- `DefaultMovement` configures movement, sprint, and jump tuning.
- `MovementProfiles` is the ordered list cycled by holding **T**. Add each new
  movement config to this list to include it in the cycle.
- `GrassMovement` grants access to the grapple ability while that profile is active.
- `DefaultSensors` configures ground detection size and layer mask.
- `DefaultGrapple` configures grapple range and target layers.
- `DefaultAnimation` configures sustained sprint playback speed.
- `DefaultSoftBody` configures soft-body shape and spring tuning.
- Power-up definitions and their effects are already reusable assets.

To switch a player's movement profile from a pickup, create a
**Blob > Power Ups > Effects > Change Movement Config** effect, assign its target
movement config asset, and add the effect to that pickup's power-up definition.
The change applies to that player's controller and does not modify either
shared asset.

Each related MonoBehaviour accepts an optional config asset and retains its
serialized component values as a fallback. This keeps existing scenes and
prefabs compatible during migration. Assign the corresponding `Default...`
asset in each component's Inspector to use the shared config. Duplicate assets
when a character or level needs a different tuning profile.

The Grass pickup selects the Grass profile and unlocks `Grapple2D`. Grappling is
usable only while Grass is the active profile. Hold **T** to cycle through the
ordered profiles in `MovementProfiles`; hold the left mouse button on a valid
target to attach, use horizontal movement input to steer, release to detach, or
press jump to detach into a jump while retaining horizontal momentum.

## Runtime state boundaries

`BlobController` owns per-player upgrade multipliers. `BlobPowerUpManager` owns
the set of upgrades collected by that player. `BlobStateMachine` and its states
are instantiated per controller. These values must not be stored on shared assets.

`BlobInputReader` already references Unity's shared `InputActionAsset`; action
instances are resolved and enabled at runtime. `BlobAnimationController` holds
references to the player Animator and SpriteRenderer. Those are scene or prefab
references, so they remain on their components rather than config assets.

Use event-channel assets when a future system needs decoupled communication,
such as a UI reacting to a pickup. There is no current listener that needs one,
so the existing direct pickup-to-manager call remains simpler.
