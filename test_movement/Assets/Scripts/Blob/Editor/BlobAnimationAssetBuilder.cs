using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlobGame.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlobGame.Player.Editor
{
    /// <summary>
    /// Imports Blob animation frames, builds the shared state-name controller,
    /// creates Default/Grass overrides and profiles, and wires the test player.
    /// </summary>
    public static class BlobAnimationAssetBuilder
    {
        private const string Root = "Assets/Art/Prototype/AnimationSPRITES";
        private const string ClipsFolder = Root + "/Clips";
        private const string OverridesFolder = Root + "/Overrides";
        private const string ControllerPath = Root + "/DefaultBlobAnimatorController.controller";
        private const string DefaultOverridePath = OverridesFolder + "/DefaultBlobAnimatorOverride.overrideController";
        private const string GrassOverridePath = OverridesFolder + "/GrassBlobAnimatorOverride.overrideController";
        private const string AnimationProfilesFolder = "Assets/Scripts/Blob/Player/AnimationControl/Profiles";
        private const string DefaultAnimationProfilePath = AnimationProfilesFolder + "/DefaultAnimationProfile.asset";
        private const string GrassAnimationProfilePath = AnimationProfilesFolder + "/GrassAnimationProfile.asset";
        private const string DefaultMovementPath = "Assets/Scripts/Blob/Player/MovementsProfiles/DefaultMovement.asset";
        private const string GrassMovementPath = "Assets/Scripts/Blob/Player/MovementsProfiles/GrassMovement.asset";
        private const string ScenePath = "Assets/Scenes/TestScene_Helen.unity";

        private static readonly ClipDefinition[] BaseClips =
        {
            new("Idle", "Idle", "Idle", 12f, true),
            new("Move", "Move", "Move", 12f, true),
            new("JumpPrepare", "JumpPrepare", "JumpPrepare", 36f, false),
            new("JumpRise", "JumpRise", "JumpRise", 12f, false),
            new("JumpApex", "JumpApex", "JumpApex", 24f, false),
            new("Fall", "Fall", "Fall", 12f, true),
            new("Land", "Land", "Land", 24f, false),
            new("SprintBurst", "SprintBurst", "SprintBurst", 12f, true),
            new("TransformOut", "DefaultTransformOut", "Transformations/Default/TransformOut", 12f, false),
            new("TransformIn", "DefaultTransformIn", "Transformations/Default/TransformIn", 12f, false)
        };

        private static readonly ClipDefinition GrassTransformOut =
            new("TransformOut", "GrassTransformOut", "Transformations/Grass/TransformOut", 12f, false);

        private static readonly ClipDefinition GrassTransformIn =
            new("TransformIn", "GrassTransformIn", "Transformations/Grass/TransformIn", 12f, false);

        [MenuItem("Tools/Blob/Rebuild Default And Material Animations")]
        public static void BuildFromMenu()
        {
            BuildAll(true);
        }

        private static void BuildAll(bool logCompletion)
        {
            try
            {
                EnsureFolder(ClipsFolder);
                EnsureFolder(OverridesFolder);
                EnsureFolder(AnimationProfilesFolder);
                ConfigureAllSprites();

                var baseClips = new Dictionary<string, AnimationClip>();
                foreach (ClipDefinition definition in BaseClips)
                    baseClips.Add(definition.StateName, BuildClip(definition));

                AnimationClip grassTransformOut = BuildClip(GrassTransformOut);
                AnimationClip grassTransformIn = BuildClip(GrassTransformIn);
                AnimatorController baseController = BuildController(baseClips);

                AnimatorOverrideController defaultOverride = BuildOverride(
                    DefaultOverridePath,
                    baseController,
                    new Dictionary<AnimationClip, AnimationClip>());

                AnimatorOverrideController grassOverride = BuildOverride(
                    GrassOverridePath,
                    baseController,
                    new Dictionary<AnimationClip, AnimationClip>
                    {
                        { baseClips["TransformOut"], grassTransformOut },
                        { baseClips["TransformIn"], grassTransformIn }
                    });

                BlobAnimationProfile defaultProfile = BuildAnimationProfile(
                    DefaultAnimationProfilePath, defaultOverride);
                BlobAnimationProfile grassProfile = BuildAnimationProfile(
                    GrassAnimationProfilePath, grassOverride);

                AssignAnimationProfile(DefaultMovementPath, defaultProfile);
                AssignAnimationProfile(GrassMovementPath, grassProfile);
                ConfigurePlayer(defaultOverride, GetFirstSprite(Root + "/Idle"));

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (logCompletion)
                    Debug.Log("Blob Default/Grass animation profiles rebuilt successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void ConfigureAllSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);

                var factories = new SpriteDataProviderFactories();
                factories.Init();
                ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();

                SpriteRect[] spriteRects = provider.GetSpriteRects();
                SpriteRect spriteRect = spriteRects.Length > 0 ? spriteRects[0] : new SpriteRect
                {
                    spriteID = GUID.Generate()
                };

                spriteRect.name = Path.GetFileNameWithoutExtension(path);
                spriteRect.rect = new Rect(0f, 0f, width, height);
                spriteRect.alignment = SpriteAlignment.Custom;
                spriteRect.pivot = new Vector2(0.5f, 0.4f);

                provider.SetSpriteRects(new[] { spriteRect });
                provider.Apply();
                importer.SaveAndReimport();
            }
        }

        private static AnimationClip BuildClip(ClipDefinition definition)
        {
            List<Sprite> frames = LoadSprites(Root + "/" + definition.SourceFolder);
            if (frames.Count == 0)
                throw new InvalidOperationException($"No sprite frames found for {definition.ClipName}.");

            string clipPath = $"{ClipsFolder}/{definition.ClipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = definition.ClipName };
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = definition.FrameRate;
            clip.wrapMode = definition.Loop ? WrapMode.Loop : WrapMode.Once;

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

            var keys = new ObjectReferenceKeyframe[frames.Count + 1];
            for (int i = 0; i < frames.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / definition.FrameRate,
                    value = frames[i]
                };
            }

            keys[^1] = new ObjectReferenceKeyframe
            {
                time = frames.Count / definition.FrameRate,
                value = definition.Loop ? frames[0] : frames[^1]
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            SetLoopTime(clip, definition.Loop);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController BuildController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
                stateMachine.RemoveState(childState.state);

            AnimatorState idleState = null;
            foreach (ClipDefinition definition in BaseClips)
            {
                AnimatorState state = stateMachine.AddState(definition.StateName);
                state.motion = clips[definition.StateName];
                state.writeDefaultValues = true;
                if (definition.StateName == "Idle")
                    idleState = state;
            }

            stateMachine.defaultState = idleState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorOverrideController BuildOverride(
            string path,
            AnimatorController baseController,
            IReadOnlyDictionary<AnimationClip, AnimationClip> replacements)
        {
            AnimatorOverrideController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);

            if (controller == null)
            {
                controller = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(controller, path);
            }
            else
            {
                controller.runtimeAnimatorController = baseController;
            }

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                AnimationClip replacement = replacements.TryGetValue(original, out AnimationClip configured)
                    ? configured
                    : original;
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
            }

            controller.ApplyOverrides(overrides);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static BlobAnimationProfile BuildAnimationProfile(
            string path,
            RuntimeAnimatorController controller)
        {
            BlobAnimationProfile profile = AssetDatabase.LoadAssetAtPath<BlobAnimationProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BlobAnimationProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("animatorController").objectReferenceValue = controller;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void AssignAnimationProfile(string movementPath, BlobAnimationProfile profile)
        {
            BlobMovementConfig movement = AssetDatabase.LoadAssetAtPath<BlobMovementConfig>(movementPath);
            if (movement == null)
                throw new InvalidOperationException($"Movement profile was not found at {movementPath}.");

            var serializedMovement = new SerializedObject(movement);
            serializedMovement.FindProperty("animationProfile").objectReferenceValue = profile;
            serializedMovement.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(movement);
        }

        private static void ConfigurePlayer(RuntimeAnimatorController controller, Sprite idleFirstFrame)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForBuild = !scene.IsValid() || !scene.isLoaded;
            if (openedForBuild)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject player = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .FirstOrDefault(gameObject => gameObject.name == "Player");

            if (player == null)
                throw new InvalidOperationException("Player was not found in TestScene_Helen.");

            SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
            BlobAnimationController animation = player.GetComponent<BlobAnimationController>()
                ?? player.AddComponent<BlobAnimationController>();
            Animator animator = player.GetComponent<Animator>() ?? player.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            renderer.sprite = idleFirstFrame;
            renderer.color = Color.white;

            var serializedAnimation = new SerializedObject(animation);
            serializedAnimation.FindProperty("animator").objectReferenceValue = animator;
            serializedAnimation.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serializedAnimation.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(animation);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (openedForBuild)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static List<Sprite> LoadSprites(string folder)
        {
            string absoluteFolder = Path.GetFullPath(folder);
            return Directory.GetFiles(absoluteFolder, "*.png", SearchOption.TopDirectoryOnly)
                .Select(path => path.Replace('\\', '/'))
                .Select(path => path[(path.IndexOf("Assets/", StringComparison.Ordinal))..])
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<Sprite>()
                .ToList();
        }

        private static Sprite GetFirstSprite(string folder)
        {
            return LoadSprites(folder).FirstOrDefault();
        }

        private static void SetLoopTime(AnimationClip clip, bool loop)
        {
            var serializedClip = new SerializedObject(clip);
            SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
            settings.FindPropertyRelative("m_LoopTime").boolValue = loop;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private readonly struct ClipDefinition
        {
            public ClipDefinition(
                string stateName,
                string clipName,
                string sourceFolder,
                float frameRate,
                bool loop)
            {
                StateName = stateName;
                ClipName = clipName;
                SourceFolder = sourceFolder;
                FrameRate = frameRate;
                Loop = loop;
            }

            public string StateName { get; }
            public string ClipName { get; }
            public string SourceFolder { get; }
            public float FrameRate { get; }
            public bool Loop { get; }
        }
    }
}
