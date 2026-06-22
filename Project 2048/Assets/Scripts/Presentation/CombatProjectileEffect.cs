using UnityEngine;

namespace Project2048.Presentation
{
    public class CombatProjectileEffect : MonoBehaviour
    {
        [SerializeField] private Vector3 sourceLocalOffset = new(0.45f, 0.25f, 0f);
        [SerializeField] private Vector3 targetLocalOffset = new(-0.2f, 0.35f, 0f);
        [SerializeField, Min(0.05f)] private float travelSeconds = 0.55f;
        [SerializeField, Min(0f)] private float arcHeight;
        [SerializeField, Min(0f)] private float impactLifetimeSeconds = 0.55f;
        [SerializeField] private ParticleSystem[] travelParticles;
        [SerializeField] private ParticleSystem[] impactParticles;

        private Transform source;
        private Transform target;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 activeTargetOffset;
        private float elapsedSeconds;
        private bool launched;
        private bool impacted;

        public float TravelSeconds => Mathf.Max(0.05f, travelSeconds);

        public float EstimatedLifetimeSeconds => TravelSeconds + Mathf.Max(0f, impactLifetimeSeconds);

        public void Launch(Transform sourceTransform, Transform targetTransform, Vector3 targetOffsetOverride)
        {
            source = sourceTransform;
            target = targetTransform;
            activeTargetOffset = targetOffsetOverride == Vector3.zero ? targetLocalOffset : targetOffsetOverride;
            startPosition = ResolveSourcePosition();
            BeginLaunch();
        }

        public void LaunchFromWorldPosition(Vector3 sourceWorldPosition, Transform targetTransform, Vector3 targetOffsetOverride)
        {
            source = null;
            target = targetTransform;
            activeTargetOffset = targetOffsetOverride == Vector3.zero ? targetLocalOffset : targetOffsetOverride;
            startPosition = sourceWorldPosition;
            BeginLaunch();
        }

        private void BeginLaunch()
        {
            endPosition = ResolveTargetPosition();
            elapsedSeconds = 0f;
            launched = true;
            impacted = false;
            transform.SetParent(null, true);
            transform.position = startPosition;
            PlayParticles(travelParticles);
            StopParticles(impactParticles, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (!launched || impacted)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            var duration = Mathf.Max(0.05f, travelSeconds);
            var progress = Mathf.Clamp01(elapsedSeconds / duration);
            var eased = progress * progress * (3f - 2f * progress);
            var position = Vector3.Lerp(startPosition, ResolveTargetPosition(), eased);
            position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
            transform.position = position;

            if (progress >= 1f)
            {
                Impact();
            }
        }

        private Vector3 ResolveSourcePosition()
        {
            if (source == null)
            {
                return transform.position;
            }

            return source.position + source.TransformVector(sourceLocalOffset);
        }

        private Vector3 ResolveTargetPosition()
        {
            if (target == null)
            {
                return endPosition == Vector3.zero ? transform.position + activeTargetOffset : endPosition;
            }

            return target.position + target.TransformVector(activeTargetOffset);
        }

        private void Impact()
        {
            impacted = true;
            transform.position = ResolveTargetPosition();
            StopParticles(travelParticles, ParticleSystemStopBehavior.StopEmitting);
            PlayParticles(impactParticles);

            if (impactLifetimeSeconds > 0f)
            {
                Destroy(gameObject, impactLifetimeSeconds);
            }
        }

        private static void PlayParticles(ParticleSystem[] particles)
        {
            if (particles == null)
            {
                return;
            }

            foreach (var particle in particles)
            {
                if (particle == null)
                {
                    continue;
                }

                particle.Play(true);
            }
        }

        private static void StopParticles(ParticleSystem[] particles, ParticleSystemStopBehavior stopBehavior)
        {
            if (particles == null)
            {
                return;
            }

            foreach (var particle in particles)
            {
                if (particle == null)
                {
                    continue;
                }

                particle.Stop(true, stopBehavior);
            }
        }
    }
}
