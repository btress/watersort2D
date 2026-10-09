using UnityEngine;

public class BottleConfetti : MonoBehaviour
{
    private const int BurstCount = 50;

    private ParticleSystem _particles;

    public void Build(Transform mouth, Material material)
    {
        GameObject go = new GameObject("Confetti");
        go.transform.SetParent(mouth, false);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        _particles = go.AddComponent<ParticleSystem>();
        _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = _particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        Gradient palette = new Gradient();
        palette.mode = GradientMode.Fixed;
        palette.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.30f, 0.30f), 0.2f),
                new GradientColorKey(new Color(1f, 0.90f, 0.20f), 0.4f),
                new GradientColorKey(new Color(0.30f, 0.90f, 0.40f), 0.6f),
                new GradientColorKey(new Color(0.30f, 0.60f, 1f), 0.8f),
                new GradientColorKey(Color.white, 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        ParticleSystem.MinMaxGradient startColor = new ParticleSystem.MinMaxGradient(palette);
        startColor.mode = ParticleSystemGradientMode.RandomColor;
        main.startColor = startColor;

        ParticleSystem.EmissionModule emission = _particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, BurstCount) });

        ParticleSystem.ShapeModule shape = _particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.08f;

        ParticleSystem.RotationOverLifetimeModule rotation = _particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        ParticleSystem.ColorOverLifetimeModule fadeModule = _particles.colorOverLifetime;
        fadeModule.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        fadeModule.color = fade;

        ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = material;
        particleRenderer.sortingOrder = 50;
    }

    public void Play()
    {
        _particles.Play();
    }

    public void Clear()
    {
        if (_particles != null) _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
