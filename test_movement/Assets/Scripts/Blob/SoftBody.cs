using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SoftBody : MonoBehaviour
{
    #region constants
    private const float splineOffset = 0.5f;
    #endregion

    #region fields
    [SerializeField]
    public SpriteShapeController spriteShape;
    [SerializeField]
    public Transform[] points;
    #endregion

    //The code for splitting up the behavior, as advised by Gemini
    //The values have been adjusted to match my editor version
    public float circleRadius = 0.5f;
    public float spawnSpacing = 1.0f;
    public float springFrequency = 3.0f;
    public float springDampingRatio = 0.5f;
    private GameObject[] circles = new GameObject[6];
    private Rigidbody2D[] rigidBodies = new Rigidbody2D[6];

    #region Monobehavior Callbacks
    private void Awake()
    {
        //Make sure to turn all of the circles other children off before testing this one
        //You can do that by selecting them and clicking the checkmaerk, though I'm sure you know that
        //spawnCircles();

        //This method specifically doesn't work.
        //SetupSpriteShape();

        //Make sure to turn all of the circles other children off before testing this one
        //You can do that by selecting them and clicking the checkmaerk, though I'm sure you know that
        //ConnectSpringJoints();
        
        UpdateVertices();
    }

    private void Update()
    {
        UpdateVertices();
    }
    #endregion

    #region private Methods
    private void UpdateVertices()
    {
        for(int i = 0; i < points.Length - 1; i++)
        {
            Vector2 _vertex = points[i].localPosition;
            Vector2 _towardsCenter = (Vector2.zero - _vertex).normalized;
            float _colliderRadius = points[i].gameObject.GetComponent<CircleCollider2D>().radius;
            try
            {
                spriteShape.spline.SetPosition(i, (_vertex - _towardsCenter * _colliderRadius));
            } catch
            {
                Debug.Log("Spline points are too close to each other. Recalculate.");
                spriteShape.spline.SetPosition(i, (_vertex - _towardsCenter * (_colliderRadius + splineOffset)));
            }
            Vector2 _lt = spriteShape.spline.GetLeftTangent(i);
            Vector2 _newRt = Vector2.Perpendicular(_towardsCenter) * _lt.magnitude;
            Vector2 _newLt = Vector2.zero - (_newRt);
            spriteShape.spline.SetRightTangent(i, _newRt);
            spriteShape.spline.SetLeftTangent(i, _newLt);
        }
    }

    //More advised methods
    void spawnCircles()
    {
        // Generate a procedural circle texture and sprite to reuse for all circles
        Texture2D circleTex = CreateCircleTexture(64);
        Sprite circleSprite = Sprite.Create(
            circleTex,
            new Rect(0, 0, circleTex.width, circleTex.height),
            new Vector2(0.5f, 0.5f),
            circleTex.width
        );

        for (int i = 0; i < 6; i++)
        {
            circles[i] = new GameObject($"Circle_{i}");
            circles[i].transform.SetParent(this.transform);

            // Arrange them roughly in a circle layout
            float angle = i * Mathf.PI * 2 / 6;
            circles[i].transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnSpacing;

            // 1. Add required 2D physics components
            rigidBodies[i] = circles[i].AddComponent<Rigidbody2D>();
            rigidBodies[i].gravityScale *= 0.5f;
            rigidBodies[i].mass *= 0.5f;

            CircleCollider2D collider = circles[i].AddComponent<CircleCollider2D>();
            collider.radius = circleRadius;

            // 2. Add visual representation with the procedural circle sprite
            SpriteRenderer renderer = circles[i].AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;

            // Scale the object to match the collider size
            circles[i].transform.localScale = Vector3.one * (circleRadius * 2);
        }
    }

    // Helper method to dynamically draw a smooth circle texture
    Texture2D CreateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        float center = size / 2f;
        float radius = center - 1f; // Slight padding to avoid sharp edge cutoff

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance <= radius)
                {
                    colors[y * size + x] = Color.white;
                }
                else
                {
                    colors[y * size + x] = Color.clear; // Transparent outside the radius
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    void ConnectSpringJoints()
    {
        // Get the Rigidbody2D on the parent object (where this script is attached)
        Rigidbody2D parentRb = GetComponent<Rigidbody2D>();

        for (int i = 0; i < 6; i++)
        {
            // 1. Calculate indices for the next and previous circles in the ring
            int nextIndex = (i + 1) % 6;
            int prevIndex = (i - 1 + 6) % 6; // Adding 6 ensures we don't get negative numbers in C#

            // --- Spring 1: Connect to the NEXT circle ---
            SpringJoint2D jointNext = circles[i].AddComponent<SpringJoint2D>();
            jointNext.connectedBody = rigidBodies[nextIndex];
            jointNext.frequency = springFrequency;
            jointNext.dampingRatio = springDampingRatio;
            jointNext.autoConfigureDistance = true;

            // --- Spring 2: Connect to the PREVIOUS circle ---
            SpringJoint2D jointPrev = circles[i].AddComponent<SpringJoint2D>();
            jointPrev.connectedBody = rigidBodies[prevIndex];
            jointPrev.frequency = springFrequency;
            jointPrev.dampingRatio = springDampingRatio;
            jointPrev.autoConfigureDistance = true;

            // --- Spring 3: Connect to the PARENT object ---
            SpringJoint2D jointParent = circles[i].AddComponent<SpringJoint2D>();
            if (parentRb != null)
            {
                jointParent.connectedBody = parentRb;
            }
            else
            {
                Debug.LogWarning("Parent object is missing a Rigidbody2D for the spring joint to connect to!", this);
            }
            jointParent.frequency = springFrequency;
            jointParent.dampingRatio = springDampingRatio;
            jointParent.autoConfigureDistance = true;
        }
    }

    // Call this inside Awake() before ConnectSpringJoints()
    void SetupSpriteShape()
    {
        // 1. Create the SpriteShape GameObject if not already assigned
        if (spriteShape == null)
        {
            GameObject ssObj = new GameObject("SoftBody_SpriteShape");
            ssObj.transform.SetParent(this.transform);
            ssObj.transform.localPosition = Vector3.zero;

            spriteShape = ssObj.AddComponent<SpriteShapeController>();

            // Note: You must assign a SpriteShapeProfile asset. 
            // You can load it from a Resources folder like this:
            // spriteShape.spriteShapeProfile = Resources.Load<SpriteShapeProfile>("PathToYourProfile");
        }

        // 2. Configure the spline to be a closed loop
        spriteShape.spline.isOpenEnded = false;

        // 3. Clear existing spline points if any
        while (spriteShape.spline.GetPointCount() > 0)
        {
            spriteShape.spline.RemovePointAt(0);
        }

        // 4. Initialize points array and populate spline points
        points = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            points[i] = circles[i].transform;

            float angle = i * Mathf.PI * 2 / 6;
            Vector3 localPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnSpacing;

            spriteShape.spline.InsertPointAt(i, localPos);
            spriteShape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
        }
    }
    #endregion
}
/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SoftBody : MonoBehaviour
{
    #region constants
    private const float splineOffset = 0.5f;
    #endregion

    #region fields
    [SerializeField]
    public SpriteShapeController spriteShape;
    [SerializeField]
    public Transform[] points;
    #endregion

    public float circleRadius = 0.5f;
    public float spawnSpacing = 1.0f;
    public float springFrequency = 3.0f;
    public float springDampingRatio = 0.5f;

    private GameObject[] circles = new GameObject[6];
    private Rigidbody2D[] rigidBodies = new Rigidbody2D[6];

    #region Monobehavior Callbacks
    private void Awake()
    {
        spawnCircles();
        SetupSpriteShape();
        ConnectSpringJoints();
        UpdateVertices();
    }

    private void Update()
    {
        UpdateVertices();
    }
    #endregion

    #region private Methods
    private void UpdateVertices()
    {
        if (spriteShape == null || spriteShape.spriteShape == null) return;

        // FIXED: Loop through ALL points (removed the - 1 so the last vertex isn't skipped)
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;

            Vector2 _vertex = points[i].localPosition;
            Vector2 _towardsCenter = (Vector2.zero - _vertex).normalized;
            float _colliderRadius = points[i].gameObject.GetComponent<CircleCollider2D>().radius;

            try
            {
                spriteShape.spline.SetPosition(i, (_vertex - _towardsCenter * _colliderRadius));
            }
            catch
            {
                Debug.Log("Spline points are too close to each other. Recalculate.");
                spriteShape.spline.SetPosition(i, (_vertex - _towardsCenter * (_colliderRadius + splineOffset)));
            }

            Vector2 _lt = spriteShape.spline.GetLeftTangent(i);
            Vector2 _newRt = Vector2.Perpendicular(_towardsCenter) * _lt.magnitude;
            Vector2 _newLt = Vector2.zero - (_newRt);

            spriteShape.spline.SetRightTangent(i, _newRt);
            spriteShape.spline.SetLeftTangent(i, _newLt);
        }
    }

    void spawnCircles()
    {
        Texture2D circleTex = CreateCircleTexture(64);
        Sprite circleSprite = Sprite.Create(
            circleTex,
            new Rect(0, 0, circleTex.width, circleTex.height),
            new Vector2(0.5f, 0.5f),
            circleTex.width
        );

        for (int i = 0; i < 6; i++)
        {
            circles[i] = new GameObject($"Circle_{i}");
            circles[i].transform.SetParent(this.transform);

            float angle = i * Mathf.PI * 2 / 6;
            circles[i].transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnSpacing;

            rigidBodies[i] = circles[i].AddComponent<Rigidbody2D>();
            rigidBodies[i].gravityScale *= 0.5f;
            rigidBodies[i].mass *= 0.5f;

            CircleCollider2D collider = circles[i].AddComponent<CircleCollider2D>();
            collider.radius = circleRadius;

            SpriteRenderer renderer = circles[i].AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;

            circles[i].transform.localScale = Vector3.one * (circleRadius * 2);
        }
    }

    Texture2D CreateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        float center = size / 2f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance <= radius)
                {
                    colors[y * size + x] = Color.white;
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    void ConnectSpringJoints()
    {
        Rigidbody2D parentRb = GetComponent<Rigidbody2D>();

        for (int i = 0; i < 6; i++)
        {
            int nextIndex = (i + 1) % 6;
            int prevIndex = (i - 1 + 6) % 6;

            // Next Circle Joint
            SpringJoint2D jointNext = circles[i].AddComponent<SpringJoint2D>();
            jointNext.connectedBody = rigidBodies[nextIndex];
            jointNext.frequency = springFrequency;
            jointNext.dampingRatio = springDampingRatio;
            jointNext.autoConfigureDistance = true;

            // Previous Circle Joint
            SpringJoint2D jointPrev = circles[i].AddComponent<SpringJoint2D>();
            jointPrev.connectedBody = rigidBodies[prevIndex];
            jointPrev.frequency = springFrequency;
            jointPrev.dampingRatio = springDampingRatio;
            jointPrev.autoConfigureDistance = true;

            // Parent Joint
            SpringJoint2D jointParent = circles[i].AddComponent<SpringJoint2D>();
            if (parentRb != null)
            {
                jointParent.connectedBody = parentRb;
            }
            jointParent.frequency = springFrequency;
            jointParent.dampingRatio = springDampingRatio;
            jointParent.autoConfigureDistance = true;
        }
    }

    void SetupSpriteShape()
    {
        // Recommended: Assign your SpriteShapeController in the Inspector so it already has its profile!
        if (spriteShape == null)
        {
            Debug.LogError("SpriteShapeController is not assigned! Please assign it in the Inspector.", this);
            return;
        }

        spriteShape.spline.isOpenEnded = false;

        while (spriteShape.spline.GetPointCount() > 0)
        {
            spriteShape.spline.RemovePointAt(0);
        }

        points = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            points[i] = circles[i].transform;

            float angle = i * Mathf.PI * 2 / 6;
            Vector3 localPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnSpacing;

            spriteShape.spline.InsertPointAt(i, localPos);
            spriteShape.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
        }
    }
    #endregion
}
*/