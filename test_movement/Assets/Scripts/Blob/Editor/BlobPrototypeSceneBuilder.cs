#if UNITY_EDITOR
using System.IO;
using BlobGame.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace BlobGame.Editor
{
    public static class BlobPrototypeSceneBuilder
    {
        private const string TexturePath = "Assets/Art/Prototype/WhiteTestTile.png";
        private const string TilePath = "Assets/Tiles/WhiteTestTile.asset";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";
        private const string PlayerMaterialPath = "Assets/Art/Material/Physics2D/BlobZeroFriction.physicsMaterial2D";

        [MenuItem("Blob/Build Movement Test Scene")]
        public static void BuildMovementTestScene()
        {
            Sprite tileSprite = CreateWhiteTileSprite();
            Tile whiteTile = CreateWhiteTile(tileSprite);
            Tilemap tilemap = ConfigureTilemap(whiteTile);
            ConfigurePlayer();
            ConfigureCamera();

            EditorUtility.SetDirty(tilemap);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Blob movement test scene created. Controls: A/D or arrows to move, Space to jump.");
        }

        private static Sprite CreateWhiteTileSprite()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Prototype");

            Texture2D texture = new(32, 32, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(Path.GetFullPath(TexturePath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
        }

        private static Tile CreateWhiteTile(Sprite sprite)
        {
            EnsureFolder("Assets/Tiles");
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, TilePath);
            }

            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Tilemap ConfigureTilemap(Tile tile)
        {
            GameObject gridObject = GameObject.Find("Grid");
            if (gridObject == null)
                gridObject = new GameObject("Grid", typeof(Grid));
            else if (gridObject.GetComponent<Grid>() == null)
                gridObject.AddComponent<Grid>();

            Transform tilemapTransform = gridObject.transform.Find("Tilemap");
            GameObject tilemapObject;
            if (tilemapTransform == null)
            {
                tilemapObject = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
                tilemapObject.transform.SetParent(gridObject.transform, false);
            }
            else
            {
                tilemapObject = tilemapTransform.gameObject;
            }

            Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
            if (tilemap == null)
                tilemap = tilemapObject.AddComponent<Tilemap>();
            if (tilemapObject.GetComponent<TilemapRenderer>() == null)
                tilemapObject.AddComponent<TilemapRenderer>();
            if (tilemapObject.GetComponent<TilemapCollider2D>() == null)
                tilemapObject.AddComponent<TilemapCollider2D>();

            tilemap.ClearAllTiles();
            for (int x = -12; x <= 12; x++)
                tilemap.SetTile(new Vector3Int(x, -3, 0), tile);

            for (int x = -8; x <= -4; x++)
                tilemap.SetTile(new Vector3Int(x, 0, 0), tile);

            for (int x = 3; x <= 7; x++)
                tilemap.SetTile(new Vector3Int(x, -1, 0), tile);

            for (int y = -2; y <= 3; y++)
                tilemap.SetTile(new Vector3Int(11, y, 0), tile);

            return tilemap;
        }

        private static void ConfigurePlayer()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
                player = new GameObject("Player", typeof(SpriteRenderer));

            player.transform.position = new Vector3(0f, -1.65f, 0f);
            player.transform.rotation = Quaternion.identity;

            Rigidbody2D body = GetOrAdd<Rigidbody2D>(player);
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 3.5f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D collider = GetOrAdd<CapsuleCollider2D>(player);
            collider.direction = CapsuleDirection2D.Vertical;
            collider.size = new Vector2(0.8f, 1.4f);
            collider.offset = Vector2.zero;
            collider.sharedMaterial = CreatePlayerPhysicsMaterial();

            BlobInputReader input = GetOrAdd<BlobInputReader>(player);
            GetOrAdd<BlobSensors>(player);
            BlobController controller = GetOrAdd<BlobController>(player);

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            SerializedObject inputObject = new(input);
            inputObject.FindProperty("inputActions").objectReferenceValue = actions;
            inputObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject controllerObject = new(controller);
            controllerObject.FindProperty("currentWeight").floatValue = 1f;
            controllerObject.FindProperty("jumpSpeedByWeight").animationCurveValue = new AnimationCurve(
                new Keyframe(1f, 15f),
                new Keyframe(2f, 14f),
                new Keyframe(3f, 12.5f),
                new Keyframe(4f, 11f),
                new Keyframe(5f, 9.5f));
            controllerObject.FindProperty("riseGravityScale").floatValue = 3f;
            controllerObject.FindProperty("releasedRiseGravityScale").floatValue = 5f;
            controllerObject.FindProperty("fallGravityScale").floatValue = 7f;
            controllerObject.FindProperty("jumpCutMultiplier").floatValue = 0.6f;
            controllerObject.FindProperty("maxFallSpeed").floatValue = 20f;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = Color.white;
                renderer.sortingOrder = 10;
            }
        }

        private static PhysicsMaterial2D CreatePlayerPhysicsMaterial()
        {
            EnsureFolder("Assets/Art/Material");
            EnsureFolder("Assets/Art/Material/Physics2D");

            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PlayerMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("BlobZeroFriction");
                AssetDatabase.CreateAsset(material, PlayerMaterialPath);
            }

            material.friction = 0f;
            material.bounciness = 0f;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.08f, 0.16f, 0.3f, 1f);
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
