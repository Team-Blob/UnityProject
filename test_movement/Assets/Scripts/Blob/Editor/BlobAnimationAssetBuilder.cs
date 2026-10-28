using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlobGame.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BlobGame.Player.Editor
{
    /// <summary>
    /// Ensures missing Blob animation assets exist. This is not a rebuild tool:
    /// authored Sprite imports, clips, Animator graphs, override slots, profile
    /// settings, and scene configuration are preserved when run again.
    /// </summary>
    public static class BlobAnimationAssetBuilder
    {
        private const string BlobArtRoot = "Assets/Art/Characters/Blob";
        private const string SpritesRoot = BlobArtRoot + "/Sprites";
        private const string AnimationsRoot = BlobArtRoot + "/Animations";
        private const string DefaultClipsFolder = AnimationsRoot + "/Clips/Default";
        private const string GrassClipsFolder = AnimationsRoot + "/Clips/Grass";
        private const string ControllersFolder = AnimationsRoot + "/Controllers";
        private const string OverridesFolder = AnimationsRoot + "/Overrides";
        private const string ControllerPath = ControllersFolder + "/DefaultBlobAnimatorController.controller";
        private const string DefaultOverridePath = OverridesFolder + "/DefaultBlobAnimatorOverride.overrideController";
        private const string GrassOverridePath = OverridesFolder + "/GrassBlobAnimatorOverride.overrideController";
        private const string RockOverridePath = OverridesFolder + "/RockBlobAnimatorOverride.overrideController";
        private const string AnimationProfilesFolder = "Assets/Scripts/Blob/Player/AnimationControl/Profiles";
        private const string DefaultAnimationProfilePath = AnimationProfilesFolder + "/DefaultAnimationProfile.asset";
        private const string GrassAnimationProfilePath = AnimationProfilesFolder + "/GrassAnimationProfile.asset";
        private const string DefaultMovementPath = "Assets/Scripts/Blob/Player/MovementsProfiles/DefaultMovement.asset";
        private const string GrassMovementPath = "Assets/Scripts/Blob/Player/MovementsProfiles/GrassMovement.asset";

        private static readonly ClipDefinition[] BaseClips =
        {
            new("Idle", "Idle", "Default/Idle", DefaultClipsFolder, 12f, true),
            new("Move", "Move", "Default/Move", DefaultClipsFolder, 12f, true),
            new("JumpPrepare", "JumpPrepare", "Default/JumpPrepare", DefaultClipsFolder, 36f, false),
            new("JumpRise", "JumpRise", "Default/JumpRise", DefaultClipsFolder, 12f, false),
            new("JumpApex", "JumpApex", "Default/JumpApex", DefaultClipsFolder, 24f, false),
            new("Fall", "Fall", "Default/Fall", DefaultClipsFolder, 12f, true),
            new("Land", "Land", "Default/Land", DefaultClipsFolder, 24f, false),
            new("SprintBurst", "SprintBurst", "Default/SprintBurst", DefaultClipsFolder, 12f, true),
            new("TransformOut", "DefaultTransformOut", "Default/TransformOut", DefaultClipsFolder, 12f, false),
            new("TransformIn", "DefaultTransformIn", "Default/TransformIn", DefaultClipsFolder, 12f, false)
        };

        private static readonly ClipDefinition GrassTransformOut =
            new("TransformOut", "GrassTransformOut", "Grass/TransformOut", GrassClipsFolder, 12f, false);

        private static readonly ClipDefinition GrassTransformIn =
            new("TransformIn", "GrassTransformIn", "Grass/TransformIn", GrassClipsFolder, 12f, false);

        [MenuItem("Tools/Blob/Set Up Missing Blob Animation Assets")]
        public static void BuildFromMenu()
        {
            try
            {
                EnsureMissingAssets();
                Debug.Log("Missing Blob animation assets set up; existing art settings and scenes were preserved.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Ensure-only automation entry point. Errors propagate to the caller.
        /// Creates missing assets and fills empty references, but never opens,
        /// modifies, or saves a scene and never resets authored animation data.
        /// </summary>
        public static void EnsureMissingAssets()
        {
            EnsureFolder(DefaultClipsFolder);
            EnsureFolder(GrassClipsFolder);
            EnsureFolder(ControllersFolder);
            EnsureFolder(OverridesFolder);
            EnsureFolder(AnimationProfilesFolder);
            ConfigureMissingSprites();

            var baseClips = new Dictionary<string, AnimationClip>();
            foreach (ClipDefinition definition in BaseClips)
                baseClips.Add(definition.StateName, BuildClip(definition));

            AnimationClip grassTransformOut = BuildClip(GrassTransformOut);
            AnimationClip grassTransformIn = BuildClip(GrassTransformIn);
            AnimatorController baseController = BuildController(baseClips);

            AnimatorOverrideController defaultOverride = BuildOverride(
                DefaultOverridePath, baseController, new Dictionary<AnimationClip, AnimationClip>());
            AnimatorOverrideController grassOverride = BuildOverride(
                GrassOverridePath, baseController, new Dictionary<AnimationClip, AnimationClip>
                {
                    { baseClips["TransformOut"], grassTransformOut },
                    { baseClips["TransformIn"], grassTransformIn }
                });

            // Rock is an art placeholder only: the same ten original Default
            // clips, without adding a Rock movement profile or gameplay form.
            BuildOverride(RockOverridePath, baseController, new Dictionary<AnimationClip, AnimationClip>());

            BlobAnimationProfile defaultProfile = BuildAnimationProfile(DefaultAnimationProfilePath, defaultOverride);
            BlobAnimationProfile grassProfile = BuildAnimationProfile(GrassAnimationProfilePath, grassOverride);
            AssignAnimationProfile(DefaultMovementPath, defaultProfile);
            AssignAnimationProfile(GrassMovementPath, grassProfile);
        }

        private static void ConfigureMissingSprites()
        {
            if (!AssetDatabase.IsValidFolder(SpritesRoot))
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                // A Sprite importer is already configured, even if its current
                // slicing yields no sprites. Never alter existing settings or
                // Sprite IDs; mode conversion is a separate migration operation.
                if (importer.textureType == TextureImporterType.Sprite)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;

                // One PNG is one complete animation frame. New imports use
                // Single mode, PPU 100, and normalized custom pivot (0.5, 0.4).
                // Read/modify/write settings so unrelated importer values stay intact.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0.4f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static AnimationClip BuildClip(ClipDefinition definition)
        {
            string clipPath = $"{definition.ClipsFolder}/{definition.ClipName}.anim";
            AnimationClip clip = LoadExistingAsset<AnimationClip>(clipPath);
            // Existing FPS, key timings, sprite references and Loop Time are
            // authored data, not defaults to apply every time the tool runs.
            if (clip != null)
                return clip;

            List<Sprite> frames = LoadSprites(SpritesRoot + "/" + definition.SourceFolder);
            if (frames.Count == 0)
                throw new InvalidOperationException($"No sprite frames found for {definition.ClipName}.");

            clip = new AnimationClip { name = definition.ClipName };
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
            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static AnimatorController BuildController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = LoadExistingAsset<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            if (controller.layers.Length == 0)
                controller.AddLayer("Base Layer");

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            bool needsDefaultState = stateMachine.defaultState == null;
            bool changed = false;

            AnimatorState idleState = null;
            foreach (ClipDefinition definition in BaseClips)
            {
                // Never remove/recreate states or replace existing motions:
                // that would discard transitions and artist-authored layout.
                AnimatorState state = FindState(stateMachine, definition.StateName);
                if (state == null)
                {
                    int index = Array.IndexOf(BaseClips, definition);
                    state = stateMachine.AddState(definition.StateName,
                        new Vector3(280f * (index % 3), 90f * (index / 3), 0f));
                    state.writeDefaultValues = true;
                    changed = true;
                }

                if (state.motion == null)
                {
                    state.motion = clips[definition.StateName];
                    EditorUtility.SetDirty(state);
                    changed = true;
                }
                if (definition.StateName == "Idle")
                    idleState = state;
            }

            if (needsDefaultState && idleState != null)
            {
                stateMachine.defaultState = idleState;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(stateMachine);
                SaveChangedAsset(controller);
            }
            return controller;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
                if (child.state.name == stateName)
                    return child.state;

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
            {
                AnimatorState nested = FindState(child.stateMachine, stateName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static AnimatorOverrideController BuildOverride(
            string path,
            AnimatorController baseController,
            IReadOnlyDictionary<AnimationClip, AnimationClip> replacements)
        {
            AnimatorOverrideController controller =
                LoadExistingAsset<AnimatorOverrideController>(path);

            if (controller != null)
            {
                // Null override slots deliberately inherit from the base clip;
                // they are not missing references to "repair". Preserve them,
                // every custom override, and a non-empty assigned controller.
                if (controller.runtimeAnimatorController == null)
                {
                    controller.runtimeAnimatorController = baseController;
                    SaveChangedAsset(controller);
                }

                return controller;
            }

            controller = new AnimatorOverrideController(baseController);
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
            AssetDatabase.CreateAsset(controller, path);
            return controller;
        }

        private static BlobAnimationProfile BuildAnimationProfile(
            string path,
            RuntimeAnimatorController controller)
        {
            BlobAnimationProfile profile = LoadExistingAsset<BlobAnimationProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BlobAnimationProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var serializedProfile = new SerializedObject(profile);
            SerializedProperty animatorController = serializedProfile.FindProperty("animatorController");
            if (animatorController.objectReferenceValue == null)
            {
                animatorController.objectReferenceValue = controller;
                serializedProfile.ApplyModifiedPropertiesWithoutUndo();
                SaveChangedAsset(profile);
            }
            return profile;
        }

        private static void AssignAnimationProfile(string movementPath, BlobAnimationProfile profile)
        {
            BlobMovementConfig movement = AssetDatabase.LoadAssetAtPath<BlobMovementConfig>(movementPath);
            if (movement == null)
                throw new InvalidOperationException($"Movement profile was not found at {movementPath}.");

            var serializedMovement = new SerializedObject(movement);
            SerializedProperty animationProfile = serializedMovement.FindProperty("animationProfile");
            if (animationProfile.objectReferenceValue == null)
            {
                animationProfile.objectReferenceValue = profile;
                serializedMovement.ApplyModifiedPropertiesWithoutUndo();
                SaveChangedAsset(movement);
            }
        }

        private static List<Sprite> LoadSprites(string folder)
        {
            // Editor automation need not share the project's working directory.
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absoluteFolder = Path.Combine(projectRoot, folder);
            if (!Directory.Exists(absoluteFolder))
                return new List<Sprite>();

            return Directory.GetFiles(absoluteFolder, "*.png", SearchOption.TopDirectoryOnly)
                .Select(path => folder + "/" + Path.GetFileName(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<Sprite>()
                .ToList();
        }

        private static T LoadExistingAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null && AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException($"Expected {typeof(T).Name} but another asset occupies {path}.");
            return asset;
        }

        private static void SaveChangedAsset(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            // Do not save unrelated dirty assets or any open scene.
            AssetDatabase.SaveAssetIfDirty(asset);
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
                string clipsFolder,
                float frameRate,
                bool loop)
            {
                StateName = stateName;
                ClipName = clipName;
                SourceFolder = sourceFolder;
                ClipsFolder = clipsFolder;
                FrameRate = frameRate;
                Loop = loop;
            }

            public string StateName { get; }
            public string ClipName { get; }
            public string SourceFolder { get; }
            public string ClipsFolder { get; }
            public float FrameRate { get; }
            public bool Loop { get; }
        }
    }
}
