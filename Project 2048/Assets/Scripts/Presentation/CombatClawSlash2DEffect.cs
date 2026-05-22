using UnityEngine;

namespace Project2048.Presentation
{
    public sealed class CombatClawSlash2DEffect : MonoBehaviour
    {
        public const int DefaultSegmentCount = 28;
        public const string ShaderName = "Project2048/Effects/ClawSlash2D";
        private const string ShaderResourcePath = "Effects/ClawSlash2D";

        private const int ClawCount = 3;
        private const float TangentSampleDistance = 0.03f;
        private static readonly Vector2 CurveP0 = new(-0.45f, -0.35f);
        private static readonly Vector2 CurveP1 = new(-0.20f, 0.85f);
        private static readonly Vector2 CurveP2 = new(0.55f, 0.85f);
        private static readonly Vector2 CurveP3 = new(0.82f, -0.20f);
        private static readonly float[] ClawIndices = { -1f, 0f, 1f };
        private static readonly float[] ClawWidthScales = { 0.82f, 1f, 1.18f };
        private static readonly float[] ClawAlphaScales = { 0.74f, 0.92f, 1f };
        private static readonly float[] ClawDelaySeconds = { 0f, 0.018f, 0.036f };
        private static readonly string[] ClawNames = { "Back", "Mid", "Front" };

        [SerializeField, Min(4)] private int segmentCount = DefaultSegmentCount;
        [SerializeField, Min(0f)] private float clawSpacing = 0.22f;
        [SerializeField, Min(0.01f)] private float coreWidth = 0.082f;
        [SerializeField, Min(0.01f)] private float auraWidth = 0.18f;
        [SerializeField, Min(0.05f)] private float coreLifeSeconds = 0.24f;
        [SerializeField, Min(0.05f)] private float auraLifeSeconds = 0.38f;
        [SerializeField, Min(0.01f)] private float revealDelay = 0.18f;
        [SerializeField, Min(0.01f)] private float revealSharpness = 0.08f;
        [SerializeField] private Color coreColor = new(0.78f, 0.98f, 1f, 0.98f);
        [SerializeField] private Color outerColor = new(0.62f, 0.16f, 1f, 0.42f);
        [SerializeField, Min(0.1f)] private float coreIntensity = 1.55f;
        [SerializeField, Min(0.1f)] private float auraIntensity = 0.95f;

        private readonly MeshFilter[] coreFilters = new MeshFilter[ClawCount];
        private readonly MeshFilter[] auraFilters = new MeshFilter[ClawCount];
        private readonly MeshRenderer[] coreRenderers = new MeshRenderer[ClawCount];
        private readonly MeshRenderer[] auraRenderers = new MeshRenderer[ClawCount];
        private readonly Mesh[] coreMeshes = new Mesh[ClawCount];
        private readonly Mesh[] auraMeshes = new Mesh[ClawCount];
        private Material coreMaterial;
        private Material auraMaterial;
        private float directionSign = 1f;
        private float elapsedSeconds;
        private bool playing;
        private int sortingLayerId;
        private int sortingOrder;

        public float LifetimeSeconds => Mathf.Max(coreLifeSeconds, auraLifeSeconds) + 0.18f;

        public int SegmentCount => Mathf.Max(4, segmentCount);

        public void Play(float attackDirectionSign, SpriteRenderer sortingReference, bool previewComplete = false)
        {
            directionSign = attackDirectionSign < 0f ? -1f : 1f;
            elapsedSeconds = previewComplete ? Mathf.Max(coreLifeSeconds, auraLifeSeconds) * 0.5f : 0f;
            playing = !previewComplete;

            ResolveSorting(sortingReference);
            EnsureMeshesAndRenderers();
            ConfigureSorting();
            RenderFrame(previewComplete ? Mathf.Max(coreLifeSeconds, auraLifeSeconds) * 0.45f : 0.001f);
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            RenderFrame(elapsedSeconds);

            if (elapsedSeconds >= LifetimeSeconds)
            {
                playing = false;
                ClearMeshes();
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            DestroyMaterial(ref coreMaterial);
            DestroyMaterial(ref auraMaterial);
            DestroyMeshes(coreMeshes);
            DestroyMeshes(auraMeshes);
        }

        private void ResolveSorting(SpriteRenderer sortingReference)
        {
            sortingLayerId = sortingReference != null ? sortingReference.sortingLayerID : 0;
            sortingOrder = sortingReference != null ? sortingReference.sortingOrder : 0;
        }

        private void EnsureMeshesAndRenderers()
        {
            coreMaterial ??= CreateEffectMaterial("ClawSlashCoreMaterial", coreColor, coreIntensity);
            auraMaterial ??= CreateEffectMaterial("ClawSlashAuraMaterial", outerColor, auraIntensity);

            for (var index = 0; index < ClawCount; index++)
            {
                EnsureStrip(
                    auraFilters,
                    auraRenderers,
                    auraMeshes,
                    index,
                    $"Claw_Aura_{ClawNames[index]}",
                    auraMaterial);
                EnsureStrip(
                    coreFilters,
                    coreRenderers,
                    coreMeshes,
                    index,
                    $"Claw_Core_{ClawNames[index]}",
                    coreMaterial);
            }
        }

        private void EnsureStrip(
            MeshFilter[] filters,
            MeshRenderer[] renderers,
            Mesh[] meshes,
            int index,
            string objectName,
            Material material)
        {
            if (filters[index] == null || renderers[index] == null)
            {
                var stripObject = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
                stripObject.transform.SetParent(transform, false);
                filters[index] = stripObject.GetComponent<MeshFilter>();
                renderers[index] = stripObject.GetComponent<MeshRenderer>();
            }

            if (meshes[index] == null)
            {
                meshes[index] = new Mesh
                {
                    name = $"{objectName}_Mesh",
                };
            }

            filters[index].sharedMesh = meshes[index];
            renderers[index].sharedMaterial = material;
        }

        private void ConfigureSorting()
        {
            for (var index = 0; index < ClawCount; index++)
            {
                ConfigureRendererSorting(auraRenderers[index], sortingOrder + 5 + index);
                ConfigureRendererSorting(coreRenderers[index], sortingOrder + 8 + index);
            }
        }

        private void ConfigureRendererSorting(Renderer renderer, int order)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = order;
        }

        private void RenderFrame(float ageSeconds)
        {
            UpdateStripGroup(auraMeshes, ageSeconds, auraLifeSeconds, auraWidth, 0.54f);
            UpdateStripGroup(coreMeshes, ageSeconds, coreLifeSeconds, coreWidth, 1f);
        }

        private void UpdateStripGroup(
            Mesh[] meshes,
            float ageSeconds,
            float lifetimeSeconds,
            float baseWidth,
            float alphaMultiplier)
        {
            for (var index = 0; index < meshes.Length; index++)
            {
                var mesh = meshes[index];
                if (mesh == null)
                {
                    continue;
                }

                var localAge = ageSeconds - ClawDelaySeconds[index];
                if (localAge <= 0f)
                {
                    mesh.Clear();
                    continue;
                }

                var revealDuration = Mathf.Max(0.01f, lifetimeSeconds * Mathf.Max(0.01f, revealDelay + revealSharpness));
                var visibleEnd = Mathf.Clamp01(SmoothStep01(localAge / revealDuration));
                var fade = 1f - SmoothStep01((localAge - lifetimeSeconds * 0.55f) / Mathf.Max(0.01f, lifetimeSeconds * 0.45f));
                var alpha = fade * ClawAlphaScales[index] * alphaMultiplier;
                if (visibleEnd <= 0.001f || alpha <= 0.001f)
                {
                    mesh.Clear();
                    continue;
                }

                var pointCount = Mathf.Clamp(
                    Mathf.CeilToInt((SegmentCount - 1) * visibleEnd) + 1,
                    2,
                    SegmentCount);
                BuildStripMesh(
                    mesh,
                    pointCount,
                    visibleEnd,
                    ClawIndices[index],
                    baseWidth * ClawWidthScales[index],
                    alpha);
            }
        }

        private void BuildStripMesh(
            Mesh mesh,
            int pointCount,
            float visibleEnd,
            float clawIndex,
            float width,
            float alpha)
        {
            var vertices = new Vector3[pointCount * 2];
            var uvs = new Vector2[pointCount * 2];
            var colors = new Color[pointCount * 2];
            var triangles = new int[(pointCount - 1) * 6];

            for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                var normalizedPoint = pointCount <= 1 ? 0f : pointIndex / (float)(pointCount - 1);
                var t = Mathf.Clamp01(normalizedPoint * visibleEnd);
                var center = ResolveClawPoint(t, clawIndex);
                var normal = ResolveRenderedNormal(t);
                var taper = ResolveTaper(t);
                var halfWidth = width * taper * 0.5f;
                var vertexAlpha = alpha * taper;
                var vertexColor = new Color(1f, 1f, 1f, vertexAlpha);
                var vertexBase = pointIndex * 2;

                vertices[vertexBase] = center - normal * halfWidth;
                vertices[vertexBase + 1] = center + normal * halfWidth;
                uvs[vertexBase] = new Vector2(t, 0f);
                uvs[vertexBase + 1] = new Vector2(t, 1f);
                colors[vertexBase] = vertexColor;
                colors[vertexBase + 1] = vertexColor;
            }

            for (var segmentIndex = 0; segmentIndex < pointCount - 1; segmentIndex++)
            {
                var vertexBase = segmentIndex * 2;
                var triangleBase = segmentIndex * 6;
                triangles[triangleBase] = vertexBase;
                triangles[triangleBase + 1] = vertexBase + 2;
                triangles[triangleBase + 2] = vertexBase + 1;
                triangles[triangleBase + 3] = vertexBase + 1;
                triangles[triangleBase + 4] = vertexBase + 2;
                triangles[triangleBase + 5] = vertexBase + 3;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private Vector3 ResolveClawPoint(float t, float clawIndex)
        {
            var point = EvaluateBezier(t);
            var normal = EvaluateNormal(t);
            var offsetPoint = point + normal * clawSpacing * clawIndex;
            return new Vector3(offsetPoint.x * directionSign, offsetPoint.y, 0f);
        }

        private Vector3 ResolveRenderedNormal(float t)
        {
            var normal = EvaluateNormal(t);
            var renderedNormal = new Vector2(normal.x * directionSign, normal.y);
            if (renderedNormal.sqrMagnitude <= 0.00001f)
            {
                return Vector3.up;
            }

            renderedNormal.Normalize();
            return new Vector3(renderedNormal.x, renderedNormal.y, 0f);
        }

        private static Vector2 EvaluateBezier(float t)
        {
            var a = Vector2.Lerp(CurveP0, CurveP1, t);
            var b = Vector2.Lerp(CurveP1, CurveP2, t);
            var c = Vector2.Lerp(CurveP2, CurveP3, t);
            var d = Vector2.Lerp(a, b, t);
            var e = Vector2.Lerp(b, c, t);
            return Vector2.Lerp(d, e, t);
        }

        private static Vector2 EvaluateNormal(float t)
        {
            var aheadT = Mathf.Clamp01(t + TangentSampleDistance);
            var behindT = Mathf.Clamp01(t - TangentSampleDistance);
            var current = EvaluateBezier(t);
            var tangent = aheadT > t
                ? EvaluateBezier(aheadT) - current
                : current - EvaluateBezier(behindT);
            if (tangent.sqrMagnitude <= 0.00001f)
            {
                return Vector2.up;
            }

            tangent.Normalize();
            return new Vector2(-tangent.y, tangent.x);
        }

        private void ClearMeshes()
        {
            ClearMeshes(coreMeshes);
            ClearMeshes(auraMeshes);
        }

        private static void ClearMeshes(Mesh[] meshes)
        {
            foreach (var mesh in meshes)
            {
                mesh?.Clear();
            }
        }

        private static float ResolveTaper(float t)
        {
            return SmoothStep(0.02f, 0.13f, t) * (1f - SmoothStep(0.84f, 1f, t));
        }

        private static float SmoothStep(float edge0, float edge1, float value)
        {
            var t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static float SmoothStep01(float value)
        {
            var t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static Material CreateEffectMaterial(string materialName, Color color, float intensity)
        {
            var shader = Resources.Load<Shader>(ShaderResourcePath)
                ?? Shader.Find(ShaderName)
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = materialName,
                renderQueue = 3000,
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Intensity"))
            {
                material.SetFloat("_Intensity", intensity);
            }

            return material;
        }

        private static void DestroyMaterial(ref Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }

            material = null;
        }

        private static void DestroyMeshes(Mesh[] meshes)
        {
            foreach (var mesh in meshes)
            {
                if (mesh == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(mesh);
                }
                else
                {
                    DestroyImmediate(mesh);
                }
            }
        }
    }
}
