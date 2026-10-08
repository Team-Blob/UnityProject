#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BlobGame.Player;
using BlobGame.Player.Animation;
using BlobGame.Player.Forms;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobGame.Editor
{
    /// <summary>
    /// Builds the data and placeholder animation assets for the Blob material prototype.
    /// Existing clips, controllers, profiles, and configured Player fields are preserved.
    /// It never clears or rebuilds the scene Tilemap.
    /// </summary>
    public static class BlobMaterialAnimationPrototypeBuilder
    {
        private const string RootPath = "Assets/Art/Prototype/BlobForms";
        private const string BaseClipPath = RootPath + "/Animations/Base";
        private const string DefaultClipPath = RootPath + "/Animations/Default";
        private const string GrassClipPath = RootPath + "/Animations/Grass";
        private const string ControllerPath = RootPath + "/Controllers/BlobBase.controller";
        private const string DefaultOverridePath = RootPath + "/Controllers/DefaultBlob.overrideController";
        private const string GrassOverridePath = RootPath + "/Controllers/GrassBlob.overrideController";
        private const string ProfilePath = "Assets/Data/BlobMaterials";
        private const string DefaultProfilePath = ProfilePath + "/DefaultBlob.asset";
        private const string GrassProfilePath = ProfilePath + "/GrassBlob.asset";
        private const string PlaceholderSpritePath =
            "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Capsule.png";

        [MenuItem("Blob/Build Material Animation Prototype")]
        public static void Build()
        {
            EnsureFolders();

            Sprite placeholderSprite = ResolvePlaceholderSprite();
            Dictionary<BlobAnimationId, AnimationClip> baseClips = CreateClipSet(
                BaseClipPath, "Base", placeholderSprite);
            Dictionary<BlobAnimationId, AnimationClip> defaultClips = CreateClipSet(
                DefaultClipPath, "Default", placeholderSprite);
            Dictionary<BlobAnimationId, AnimationClip> grassClips = CreateClipSet(
                GrassClipPath, "Grass", placeholderSprite);

            AnimatorController baseController = CreateBaseController(baseClips);
            AnimatorOverrideController defaultOverride = CreateOverrideController(
                DefaultOverridePath, "DefaultBlob", baseController, baseClips, defaultClips);
            AnimatorOverrideController grassOverride = CreateOverrideController(
                GrassOverridePath, "GrassBlob", baseController, baseClips, grassClips);

            BlobMaterialProfile defaultProfile = CreateProfile(
                DefaultProfilePath,
                BlobMaterialId.Default,
                defaultOverride,
                weight: 3f,
                moveMultiplier: 1f,
                airMultiplier: 1f,
                jumpMultiplier: 1f,
                gravityMultiplier: 1f,
                spriteTint: Color.black,
                abilities: BlobAbility.None);

            BlobMaterialProfile grassProfile = CreateProfile(
                GrassProfilePath,
                BlobMaterialId.Grass,
                grassOverride,
                weight: 2.5f,
                moveMultiplier: 1.05f,
                airMultiplier: 1.05f,
                jumpMultiplier: 1.05f,
                gravityMultiplier: 0.95f,
                spriteTint: new Color(0f, 0.8f, 0.3f, 1f),
                abilities: BlobAbility.None);

            ConfigurePlayer(defaultOverride, defaultProfile, grassProfile, placeholderSprite);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log(
                "Blob material animation prototype created. " +
                "Existing authored assets and profile values were preserved.");
        }

        private static Dictionary<BlobAnimationId, AnimationClip> CreateClipSet(
            string folder,
            string prefix,
            Sprite placeholderSprite)
        {
            Dictionary<BlobAnimationId, AnimationClip> clips = new();

            foreach (BlobAnimationId animationId in Enum.GetValues(typeof(BlobAnimationId)))
            {
                float duration = GetClipDuration(animationId);
                bool loop = animationId is BlobAnimationId.Idle or
                    BlobAnimationId.Move or
                    BlobAnimationId.JumpRise or
                    BlobAnimationId.Fall or
                    BlobAnimationId.WallCling or
                    BlobAnimationId.WallSlide;

                string path = $"{folder}/{prefix}_{animationId}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    clips.Add(animationId, clip);
                    continue;
                }

                clip = new AnimationClip { name = $"{prefix}_{animationId}" };
                AssetDatabase.CreateAsset(clip, path);

                clip.frameRate = 12f;

                EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
                    string.Empty,
                    typeof(SpriteRenderer),
                    "m_Sprite");
                ObjectReferenceKeyframe[] spriteKeys =
                {
                    new() { time = 0f, value = placeholderSprite },
                    new() { time = duration, value = placeholderSprite }
                };
                AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeys);

                if (animationId == BlobAnimationId.TransformFadeOut)
                    SetAlphaCurve(clip, 1f, 0f, duration);
                else if (animationId == BlobAnimationId.TransformFadeIn)
                    SetAlphaCurve(clip, 0f, 1f, duration);

                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                settings.stopTime = duration;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
                clips.Add(animationId, clip);
            }

            return clips;
        }

        private static void SetAlphaCurve(AnimationClip clip, float start, float end, float duration)
        {
            EditorCurveBinding alphaBinding = EditorCurveBinding.FloatCurve(
                string.Empty,
                typeof(SpriteRenderer),
                "m_Color.a");
            AnimationUtility.SetEditorCurve(
                clip,
                alphaBinding,
                AnimationCurve.Linear(0f, start, duration, end));
        }

        private static AnimatorController CreateBaseController(
            IReadOnlyDictionary<BlobAnimationId, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            int index = 0;
            foreach (BlobAnimationId animationId in Enum.GetValues(typeof(BlobAnimationId)))
            {
                AnimatorState state = FindState(stateMachine, animationId.ToString());
                if (state != null)
                {
                    index++;
                    continue;
                }

                Vector3 position = new(260f + (index % 3) * 220f, 70f + (index / 3) * 110f, 0f);
                state = stateMachine.AddState(animationId.ToString(), position);
                state.motion = clips[animationId];
                state.writeDefaultValues = true;
                if (animationId == BlobAnimationId.Idle && stateMachine.defaultState == null)
                    stateMachine.defaultState = state;
                index++;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorOverrideController CreateOverrideController(
            string path,
            string assetName,
            RuntimeAnimatorController baseController,
            IReadOnlyDictionary<BlobAnimationId, AnimationClip> baseClips,
            IReadOnlyDictionary<BlobAnimationId, AnimationClip> replacementClips)
        {
            AnimatorOverrideController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (controller != null)
                return controller;

            controller = new AnimatorOverrideController { name = assetName };
            AssetDatabase.CreateAsset(controller, path);

            controller.runtimeAnimatorController = baseController;
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
            foreach (BlobAnimationId animationId in Enum.GetValues(typeof(BlobAnimationId)))
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(
                    baseClips[animationId],
                    replacementClips[animationId]));
            }

            controller.ApplyOverrides(overrides);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static BlobMaterialProfile CreateProfile(
            string path,
            BlobMaterialId materialId,
            AnimatorOverrideController animationController,
            float weight,
            float moveMultiplier,
            float airMultiplier,
            float jumpMultiplier,
            float gravityMultiplier,
            Color spriteTint,
            BlobAbility abilities)
        {
            BlobMaterialProfile profile = AssetDatabase.LoadAssetAtPath<BlobMaterialProfile>(path);
            if (profile != null)
                return profile;

            profile = ScriptableObject.CreateInstance<BlobMaterialProfile>();
            AssetDatabase.CreateAsset(profile, path);

            SerializedObject serialized = new(profile);
            serialized.FindProperty("materialId").enumValueIndex = (int)materialId;
            serialized.FindProperty("weight").floatValue = weight;
            serialized.FindProperty("moveSpeedMultiplier").floatValue = moveMultiplier;
            serialized.FindProperty("airControlMultiplier").floatValue = airMultiplier;
            serialized.FindProperty("jumpSpeedMultiplier").floatValue = jumpMultiplier;
            serialized.FindProperty("gravityMultiplier").floatValue = gravityMultiplier;
            serialized.FindProperty("animationController").objectReferenceValue = animationController;
            serialized.FindProperty("spriteTint").colorValue = spriteTint;
            serialized.FindProperty("transformFadeOutDuration").floatValue = 0.25f;
            serialized.FindProperty("transformFadeInDuration").floatValue = 0.25f;
            serialized.FindProperty("abilities").intValue = (int)abilities;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigurePlayer(
            RuntimeAnimatorController defaultController,
            BlobMaterialProfile defaultProfile,
            BlobMaterialProfile grassProfile,
            Sprite placeholderSprite)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
                throw new InvalidOperationException("The active scene does not contain a Player GameObject.");

            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(player);
            if (renderer.sprite == null)
                renderer.sprite = placeholderSprite;

            Animator animator = GetOrAdd<Animator>(player);
            if (animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = defaultController;

            GetOrAdd<BlobAnimationDriver>(player);
            BlobMaterialController materials = GetOrAdd<BlobMaterialController>(player);

            SerializedObject materialObject = new(materials);
            SerializedProperty profiles = materialObject.FindProperty("profiles");
            if (profiles.arraySize == 0)
            {
                materialObject.FindProperty("initialMaterial").enumValueIndex = (int)BlobMaterialId.Default;
                profiles.arraySize = 2;
                profiles.GetArrayElementAtIndex(0).objectReferenceValue = defaultProfile;
                profiles.GetArrayElementAtIndex(1).objectReferenceValue = grassProfile;
                materialObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Sprite ResolvePlaceholderSprite()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (sprite != null)
                return sprite;

            GameObject player = GameObject.Find("Player");
            SpriteRenderer renderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
            if (renderer != null && renderer.sprite != null)
                return renderer.sprite;

            throw new InvalidOperationException("No placeholder Sprite could be resolved for Blob animations.");
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state != null && child.state.name == name)
                    return child.state;
            }

            return null;
        }

        private static float GetClipDuration(BlobAnimationId animationId)
        {
            return animationId switch
            {
                BlobAnimationId.Idle => 1f,
                BlobAnimationId.Move => 0.5f,
                BlobAnimationId.JumpStart => 0.2f,
                BlobAnimationId.JumpRise => 0.5f,
                BlobAnimationId.Fall => 0.5f,
                BlobAnimationId.Land => 0.2f,
                BlobAnimationId.TransformFadeOut => 0.25f,
                BlobAnimationId.TransformFadeIn => 0.25f,
                BlobAnimationId.Interact => 0.5f,
                BlobAnimationId.Hurt => 0.35f,
                BlobAnimationId.Death => 0.8f,
                BlobAnimationId.WallCling => 0.5f,
                BlobAnimationId.WallSlide => 0.5f,
                BlobAnimationId.WallJump => 0.25f,
                BlobAnimationId.AbilityPrimary => 0.5f,
                BlobAnimationId.AbilitySecondary => 0.5f,
                _ => 0.5f
            };
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureFolders()
        {
            EnsureFolder(RootPath);
            EnsureFolder(RootPath + "/Animations");
            EnsureFolder(BaseClipPath);
            EnsureFolder(DefaultClipPath);
            EnsureFolder(GrassClipPath);
            EnsureFolder(RootPath + "/Controllers");
            EnsureFolder("Assets/Data");
            EnsureFolder(ProfilePath);
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
