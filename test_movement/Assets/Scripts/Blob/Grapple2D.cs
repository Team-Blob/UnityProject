using UnityEngine;
using UnityEngine.InputSystem;

namespace BlobGame.Player
{
    /// <summary>
    /// Mouse-aimed grapple for a 2D Rigidbody player. Press the configured
    /// grapple key to latch to the first surface on Grapple Mask; press again
    /// to release.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BlobInputReader))]
    [DisallowMultipleComponent]
    public sealed class Grapple2D : MonoBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(0.1f)] private float maxDistance = 12f;
        [SerializeField] private LayerMask grappleMask = ~0;

        [Header("Pull")]
        [SerializeField, Min(0f)] private float frequency = 4f;
        [SerializeField, Range(0f, 1f)] private float dampingRatio = 0.8f;
        [SerializeField, Min(0f)] private float maxForce = 1000f;

        [Header("Visual")]
        [SerializeField] private LineRenderer rope;

        private Rigidbody2D body;
        private BlobInputReader input;
        private TargetJoint2D pullJoint;
        private Vector2 grapplePoint;

        public bool IsAttached => pullJoint != null && pullJoint.enabled;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<BlobInputReader>();
            if (targetCamera == null)
                targetCamera = Camera.main;

            pullJoint = GetComponent<TargetJoint2D>();
            if (pullJoint == null)
                pullJoint = gameObject.AddComponent<TargetJoint2D>();

            pullJoint.enabled = false;
            pullJoint.autoConfigureTarget = false;
            pullJoint.frequency = frequency;
            pullJoint.dampingRatio = dampingRatio;
            pullJoint.maxForce = maxForce;
            SetRopeVisible(false);
        }

        private void Update()
        {
            if (input.GrapplePressed)
            {
                if (IsAttached)
                    Release();
                else
                    TryAttach();
            }

            if (IsAttached)
                UpdateRope();
        }

        private void OnDisable()
        {
            Release();
        }

        private void TryAttach()
        {
            if (targetCamera == null || Mouse.current == null)
                return;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            float cameraDepth = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            Vector3 mouseWorld = targetCamera.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, cameraDepth));

            Vector2 origin = body.worldCenterOfMass;
            Vector2 aim = (Vector2)mouseWorld - origin;
            if (aim.sqrMagnitude <= Mathf.Epsilon)
                return;

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, aim.normalized, maxDistance, grappleMask);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.transform.root == transform.root)
                    continue;

                grapplePoint = hit.point;
                pullJoint.target = grapplePoint;
                pullJoint.frequency = frequency;
                pullJoint.dampingRatio = dampingRatio;
                pullJoint.maxForce = maxForce;
                pullJoint.enabled = true;
                SetRopeVisible(true);
                UpdateRope();
                return;
            }
        }

        private void Release()
        {
            if (pullJoint != null)
                pullJoint.enabled = false;

            SetRopeVisible(false);
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
    }
}
