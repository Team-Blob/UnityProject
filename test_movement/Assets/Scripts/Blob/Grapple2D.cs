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

        [Header("Visual")]
        [SerializeField] private LineRenderer rope;

        private Rigidbody2D body;
        private BlobInputReader input;
        private DistanceJoint2D ropeJoint;
        private Vector2 grapplePoint;

        public bool IsAttached => ropeJoint != null && ropeJoint.enabled;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<BlobInputReader>();
            if (targetCamera == null)
                targetCamera = Camera.main;

            ropeJoint = GetComponent<DistanceJoint2D>();
            if (ropeJoint == null)
                ropeJoint = gameObject.AddComponent<DistanceJoint2D>();

            ropeJoint.enabled = false;
            ropeJoint.autoConfigureDistance = false;
            ropeJoint.autoConfigureConnectedAnchor = false;
            ropeJoint.maxDistanceOnly = true;
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
                ropeJoint.connectedBody = null;
                ropeJoint.anchor = transform.InverseTransformPoint(body.worldCenterOfMass);
                ropeJoint.connectedAnchor = grapplePoint;
                ropeJoint.distance = Vector2.Distance(body.worldCenterOfMass, grapplePoint);
                ropeJoint.maxDistanceOnly = true;
                ropeJoint.enabled = true;
                SetRopeVisible(true);
                UpdateRope();
                return;
            }
        }

        private void Release()
        {
            if (ropeJoint != null)
                ropeJoint.enabled = false;

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
