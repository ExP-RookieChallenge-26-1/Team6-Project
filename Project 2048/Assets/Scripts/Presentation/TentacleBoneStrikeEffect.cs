using System;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace Project2048.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(SpriteSkin))]
    public sealed class TentacleBoneStrikeEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteSkin spriteSkin;
        [SerializeField, Min(0.05f)] private float defaultDurationSeconds = 0.58f;
        [SerializeField, Range(0f, 1f)] private float widthScale = 0.64f;
        [SerializeField, Range(0f, 1.5f)] private float arcHeight = 0.82f;
        [SerializeField, Range(0f, 80f)] private float recoilDegrees = 34f;
        [SerializeField, Range(0f, 90f)] private float strikeDegrees = 48f;

        private Transform rootBone;
        private Transform[] boneTransforms = Array.Empty<Transform>();
        private Quaternion[] bindRotations = Array.Empty<Quaternion>();
        private Vector3[] bindPositions = Array.Empty<Vector3>();
        private Color bodyColor = Color.white;
        private float durationSeconds;
        private float elapsedSeconds;
        private bool isPlaying;

        public int BoneCount => boneTransforms?.Length ?? 0;
        public Sprite SourceSprite => ResolveSpriteRenderer()?.sprite;
        public SpriteSkin Skin => ResolveSpriteSkin();

        private void Awake()
        {
            ConfigureBonesFromSprite();
            CaptureBindPose();
        }

        private void OnEnable()
        {
            ConfigureBonesFromSprite();
            CaptureBindPose();
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            elapsedSeconds += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsedSeconds / Mathf.Max(0.05f, durationSeconds));
            ApplyStrikePose(progress);

            if (spriteRenderer != null)
            {
                var fade = 1f - Mathf.Clamp01((progress - 0.72f) / 0.28f);
                var nextColor = bodyColor;
                nextColor.a *= fade;
                spriteRenderer.color = nextColor;
            }

            if (progress >= 1f)
            {
                isPlaying = false;
            }
        }

        public void Play(
            Vector3 sourceWorld,
            Vector3 targetWorld,
            float requestedDurationSeconds,
            float scale,
            Color primaryColor,
            Color secondaryColor,
            int sortingOrder)
        {
            ConfigureBonesFromSprite();
            CaptureBindPose();

            spriteRenderer = ResolveSpriteRenderer();
            if (spriteRenderer == null)
            {
                return;
            }

            var delta = targetWorld - sourceWorld;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                delta = Vector3.right;
                targetWorld = sourceWorld + delta;
            }

            var distance = Mathf.Max(0.1f, delta.magnitude);
            var direction = delta.normalized;
            var perpendicular = new Vector3(-direction.y, direction.x, 0f);
            var spriteHeight = spriteRenderer.sprite != null
                ? Mathf.Max(0.1f, spriteRenderer.sprite.bounds.size.y)
                : 1f;
            var resolvedScale = Mathf.Max(0.01f, scale);
            var lengthScale = distance / spriteHeight;
            var width = Mathf.Clamp(resolvedScale * widthScale, 0.28f, 1.35f);

            transform.position = Vector3.Lerp(sourceWorld, targetWorld, 0.5f) + perpendicular * arcHeight * resolvedScale * 0.18f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            transform.localScale = new Vector3(lengthScale * width, lengthScale * resolvedScale, 1f);

            bodyColor = Color.Lerp(primaryColor, secondaryColor, 0.18f);
            bodyColor.a = Mathf.Clamp01(Mathf.Max(primaryColor.a, 0.82f));
            spriteRenderer.color = bodyColor;
            spriteRenderer.sortingOrder = sortingOrder;

            durationSeconds = Mathf.Max(0.05f, requestedDurationSeconds > 0f ? requestedDurationSeconds : defaultDurationSeconds);
            elapsedSeconds = 0f;
            isPlaying = true;
            ApplyStrikePose(0f);
        }

        public void ConfigureBonesFromSprite()
        {
            spriteRenderer = ResolveSpriteRenderer();
            spriteSkin = ResolveSpriteSkin();
            var sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            if (spriteRenderer == null || spriteSkin == null || sprite == null)
            {
                return;
            }

            var spriteBones = sprite.GetBones();
            if (spriteBones == null || spriteBones.Length == 0)
            {
                return;
            }

            if (boneTransforms != null &&
                boneTransforms.Length == spriteBones.Length &&
                boneTransforms[0] != null &&
                boneTransforms[0].parent == transform)
            {
                spriteSkin.SetRootBone(rootBone);
                spriteSkin.SetBoneTransforms(boneTransforms);
                return;
            }

            RemoveExistingBoneChildren();
            boneTransforms = new Transform[spriteBones.Length];
            for (var i = 0; i < spriteBones.Length; i++)
            {
                var boneObject = new GameObject(spriteBones[i].name);
                var bone = boneObject.transform;
                boneTransforms[i] = bone;

                var parentId = spriteBones[i].parentId;
                bone.SetParent(parentId >= 0 && parentId < boneTransforms.Length && boneTransforms[parentId] != null
                    ? boneTransforms[parentId]
                    : transform, false);
                bone.localPosition = ResolveBoneLocalPosition(sprite, spriteBones[i], parentId);
                bone.localRotation = spriteBones[i].rotation;
                bone.localScale = Vector3.one;
            }

            rootBone = boneTransforms[0];
            spriteSkin.SetRootBone(rootBone);
            spriteSkin.SetBoneTransforms(boneTransforms);
            spriteSkin.alwaysUpdate = true;
            spriteSkin.autoRebind = true;
        }

        private SpriteRenderer ResolveSpriteRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            return spriteRenderer;
        }

        private SpriteSkin ResolveSpriteSkin()
        {
            if (spriteSkin == null)
            {
                spriteSkin = GetComponent<SpriteSkin>();
            }

            return spriteSkin;
        }

        private static Vector3 ResolveBoneLocalPosition(Sprite sprite, SpriteBone bone, int parentId)
        {
            var position = bone.position / sprite.pixelsPerUnit;
            if (parentId < 0)
            {
                position -= (Vector3)(sprite.pivot / sprite.pixelsPerUnit);
            }

            return position;
        }

        private void CaptureBindPose()
        {
            if (boneTransforms == null || boneTransforms.Length == 0)
            {
                bindRotations = Array.Empty<Quaternion>();
                bindPositions = Array.Empty<Vector3>();
                return;
            }

            if (bindRotations.Length != boneTransforms.Length)
            {
                bindRotations = new Quaternion[boneTransforms.Length];
                bindPositions = new Vector3[boneTransforms.Length];
            }

            for (var i = 0; i < boneTransforms.Length; i++)
            {
                if (boneTransforms[i] == null)
                {
                    continue;
                }

                bindRotations[i] = boneTransforms[i].localRotation;
                bindPositions[i] = boneTransforms[i].localPosition;
            }
        }

        private void ApplyStrikePose(float progress)
        {
            if (boneTransforms == null || boneTransforms.Length == 0 || bindRotations.Length != boneTransforms.Length)
            {
                return;
            }

            var extension = EaseOutCubic(Mathf.Clamp01(progress / 0.42f));
            var recoil = (1f - extension) * recoilDegrees;
            var hitSnap = Mathf.Sin(Mathf.Clamp01((progress - 0.22f) / 0.52f) * Mathf.PI) * strikeDegrees;
            var settle = Mathf.Sin(progress * Mathf.PI * 2f) * 7f * (1f - progress);

            for (var i = 0; i < boneTransforms.Length; i++)
            {
                var bone = boneTransforms[i];
                if (bone == null)
                {
                    continue;
                }

                var normalized = boneTransforms.Length <= 1 ? 0f : i / (float)(boneTransforms.Length - 1);
                var baseBend = Mathf.Lerp(-recoil, hitSnap, normalized);
                var wave = Mathf.Sin((normalized * 1.6f + progress * 2.2f) * Mathf.PI) * arcHeight * 18f * (1f - normalized * 0.25f);
                bone.localPosition = bindPositions[i];
                bone.localRotation = bindRotations[i] * Quaternion.Euler(0f, 0f, baseBend + wave + settle);
            }
        }

        private void RemoveExistingBoneChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null && child.name.StartsWith("bone_", StringComparison.Ordinal))
                {
                    DestroyChild(child.gameObject);
                }
            }
        }

        private static void DestroyChild(GameObject child)
        {
            if (child == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        private static float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }
    }
}
