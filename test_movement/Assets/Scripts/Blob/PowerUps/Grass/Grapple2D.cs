using UnityEngine;
using UnityEngine.InputSystem;

namespace BlobGame.Player
{
    /// <summary>
    /// Mouse-aimed grapple unlocked by the grapple power-up. Holding the left
    /// mouse button attaches; releasing it lets the player's current momentum go.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BlobInputReader))]
    [DisallowMultipleComponent]
    public sealed class Grapple2D : MonoBehaviour, BlobGame.Player.PowerUps.IConfigurablePowerUpAbility
    {
        [Header("Targeting")]
        [SerializeField] private GrappleConfig config;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(0.1f)] private float maxDistance = 12f;
        [SerializeField] private LayerMask grappleMask = ~0;

        [Header("Visual")]
        [SerializeField] private LineRenderer rope;
        [SerializeField] private Color aimMarkerColor = Color.cyan;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private BlobInputReader input;
        private BlobController player;
        private DistanceJoint2D ropeJoint;
        private Material generatedRopeMaterial;
        private readonly RaycastHit2D[] collisionHits = new RaycastHit2D[8];
        private ContactFilter2D collisionFilter;
        private CollisionDetectionMode2D collisionModeBeforeGrapple;
        private Vector2 grapplePoint;
        private bool isUnlocked;
        private bool hasAimTarget;
        private bool waitForGrappleRelease;

        public bool IsAttached => ropeJoint != null && ropeJoint.enabled;
        public bool IsUnlocked => isUnlocked;

        private GrappleConfig ActiveConfig => player != null
            ? player.CurrentMovementConfig?.GrappleAbilityConfig ?? config
            : config;
        private float MaxDistance => ActiveConfig != null ? ActiveConfig.MaxDistance : maxDistance;
        private LayerMask GrappleMask => ActiveConfig != null ? ActiveConfig.GrappleMask : grappleMask;
        private float SwingAcceleration => ActiveConfig != null ? ActiveConfig.SwingAcceleration : 14f;
        private float ClimbSpeed => ActiveConfig != null ? ActiveConfig.ClimbSpeed : 4f;
        private Texture2D AimMarkerTexture => ActiveConfig != null ? ActiveConfig.AimMarkerTexture : null;
        private float AimMarkerSize => ActiveConfig != null ? ActiveConfig.AimMarkerSize : 24f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            input = GetComponent<BlobInputReader>();
            player = GetComponent<BlobController>();
            if (targetCamera == null)
                targetCamera = Camera.main;

            ropeJoint = GetComponent<DistanceJoint2D>();
            if (ropeJoint == null)
                ropeJoint = gameObject.AddComponent<DistanceJoint2D>();

            ropeJoint.enabled = false;
            ropeJoint.autoConfigureDistance = false;
            ropeJoint.autoConfigureConnectedAnchor = false;
            ropeJoint.maxDistanceOnly = true;
            collisionFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = ~0,
                useTriggers = false
            };
            EnsureRopeRenderer();
            SetRopeVisible(false);
        }

        public void Activate(ScriptableObject abilityConfig)
        {
            if (abilityConfig != null && !(abilityConfig is GrappleConfig))
            {
                Debug.LogWarning("Grapple2D requires a GrappleConfig ability config.", this);
                return;
            }

            if (abilityConfig is GrappleConfig grappleConfig)
                config = grappleConfig;

            isUnlocked = true;
            enabled = true;
        }

        private void Update()
        {
            if (!isUnlocked || player == null || !player.CurrentProfileHasGrappleAbility)
            {
                if (IsAttached)
                    Release();
                hasAimTarget = false;
                return;
            }

            // Jumping releases the rope before the normal jump state is entered.
            // No horizontal velocity is changed, so swing momentum carries on.
            if (IsAttached)
            {
                if (input.JumpPressed)
                {
                    waitForGrappleRelease = true;
                    Release();
                    player?.JumpFromGrapple();
                    return;
                }

                if (!input.GrappleHeld)
                {
                    Release();
                    return;
                }

                UpdateRope();
                return;
            }

            if (waitForGrappleRelease)
            {
                if (!input.GrappleHeld)
                    waitForGrappleRelease = false;

                hasAimTarget = false;
                return;
            }

            hasAimTarget = TryGetAimTarget(out grapplePoint);
            if (input.GrappleHeld && hasAimTarget)
                Attach(grapplePoint);
        }

        private void FixedUpdate()
        {
            if (!isUnlocked || !IsAttached)
                return;

            // Horizontal force changes the pendulum arc while the distance joint
            // keeps the player attached to the same anchor.
            Vector2 acceleration = Vector2.right * (input.MoveX * SwingAcceleration);
            float climbInput = input.MoveY;
            if (Mathf.Abs(climbInput) > Mathf.Epsilon)
            {
                ropeJoint.distance = Mathf.Clamp(
                    ropeJoint.distance - climbInput * ClimbSpeed * Time.fixedDeltaTime,
                    0.5f,
                    MaxDistance);
            }

            Vector2 predictedVelocity = body.linearVelocity + acceleration * Time.fixedDeltaTime;
            predictedVelocity += Physics2D.gravity * (body.gravityScale * Time.fixedDeltaTime);
            if (TryBlockSwingThroughObstacle(predictedVelocity))
                return;

            body.AddForce(acceleration, ForceMode2D.Force);
        }

        private void OnDisable()
        {
            Release();
            hasAimTarget = false;
        }

        private void OnDestroy()
        {
            if (generatedRopeMaterial != null)
                Destroy(generatedRopeMaterial);
        }

        private void EnsureRopeRenderer()
        {
            if (rope != null)
                return;

            GameObject ropeObject = new GameObject("Grapple Line");
            ropeObject.transform.SetParent(transform, false);
            rope = ropeObject.AddComponent<LineRenderer>();
            rope.useWorldSpace = true;
            rope.positionCount = 2;
            rope.startWidth = 0.04f;
            rope.endWidth = 0.04f;
            rope.numCapVertices = 3;
            rope.startColor = Color.white;
            rope.endColor = Color.white;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                generatedRopeMaterial = new Material(shader);
                rope.sharedMaterial = generatedRopeMaterial;
            }
        }

        private bool TryGetAimTarget(out Vector2 target)
        {
            target = default;
            if (targetCamera == null || Mouse.current == null)
                return false;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            float cameraDepth = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            Vector3 mouseWorld = targetCamera.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, cameraDepth));

            Vector2 origin = body.worldCenterOfMass;
            Vector2 aim = (Vector2)mouseWorld - origin;
            float aimDistance = aim.magnitude;
            if (aimDistance <= Mathf.Epsilon || aimDistance > MaxDistance)
                return false;

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, aim.normalized, aimDistance, GrappleMask);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.transform.root == transform.root)
                    continue;

                target = hit.point;
                return true;
            }

            return false;
        }

        private void Attach(Vector2 point)
        {
            collisionModeBeforeGrapple = body.collisionDetectionMode;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            grapplePoint = point;
            ropeJoint.connectedBody = null;
            ropeJoint.anchor = transform.InverseTransformPoint(body.worldCenterOfMass);
            ropeJoint.connectedAnchor = grapplePoint;
            ropeJoint.distance = Vector2.Distance(body.worldCenterOfMass, grapplePoint);
            ropeJoint.maxDistanceOnly = false;
            ropeJoint.enabled = true;
            SetRopeVisible(true);
            UpdateRope();
        }

        public void Release()
        {
            bool wasAttached = IsAttached;
            if (ropeJoint != null)
                ropeJoint.enabled = false;

            if (wasAttached && body != null)
                body.collisionDetectionMode = collisionModeBeforeGrapple;

            SetRopeVisible(false);
            if (wasAttached)
                player?.PreserveMomentumAfterGrapple();
        }

        private bool TryBlockSwingThroughObstacle(Vector2 velocity)
        {
            if (bodyCollider == null || velocity.sqrMagnitude <= Mathf.Epsilon)
                return false;

            float distance = velocity.magnitude * Time.fixedDeltaTime;
            int hitCount = bodyCollider.Cast(velocity.normalized, collisionFilter, collisionHits, distance);
            RaycastHit2D closestHit = default;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = collisionHits[i];
                if (hit.collider == null || hit.transform.root == transform.root || hit.normal.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestHit = hit;
                    closestDistance = hit.distance;
                }
            }

            if (closestDistance == float.PositiveInfinity)
                return false;

            float velocityIntoSurface = Vector2.Dot(velocity, closestHit.normal);
            if (velocityIntoSurface < 0f)
                body.linearVelocity = velocity - velocityIntoSurface * closestHit.normal;

            return true;
        }

        private void UpdateRope()
        {
            if (rope == null)
                return;

            rope.positionCount = 2;
            rope.SetPosition(0, body.worldCenterOfMass);
            rope.SetPosition(1, grapplePoint);
        }

        private void SetRopeVisible(bool visible)
        {
            if (rope != null)
                rope.enabled = visible;
        }

        private void OnGUI()
        {
            if (!isUnlocked || IsAttached || !hasAimTarget || targetCamera == null || !IsMouseInRange())
                return;

            Vector3 screenPoint = targetCamera.WorldToScreenPoint(grapplePoint);
            if (screenPoint.z <= 0f)
                return;

            // Draw a small target reticle at the valid grapple point without
            // requiring a separate scene object or sprite assignment.
            const float armLength = 5f;
            const float gap = 3f;
            const float thickness = 2f;
            float x = screenPoint.x;
            float y = Screen.height - screenPoint.y;
            Color previousColor = GUI.color;
            GUI.color = aimMarkerColor;
            if (AimMarkerTexture != null)
            {
                float size = AimMarkerSize;
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), AimMarkerTexture, ScaleMode.ScaleToFit, true);
                GUI.color = previousColor;
                return;
            }

            GUI.DrawTexture(new Rect(x - gap - armLength, y - thickness * 0.5f, armLength, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + gap, y - thickness * 0.5f, armLength, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - thickness * 0.5f, y - gap - armLength, thickness, armLength), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - thickness * 0.5f, y + gap, thickness, armLength), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private bool IsMouseInRange()
        {
            if (targetCamera == null || Mouse.current == null || body == null)
                return false;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            float cameraDepth = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            Vector3 mouseWorld = targetCamera.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, cameraDepth));
            return ((Vector2)mouseWorld - body.worldCenterOfMass).sqrMagnitude <= MaxDistance * MaxDistance;
        }
    }
}
