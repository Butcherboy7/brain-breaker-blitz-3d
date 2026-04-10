using System.Collections;
using UnityEngine;

/// <summary>
/// NeonVisuals — Cyberpunk Neon Aesthetic Helpers
/// Static utility class providing neon color palette, material factories,
/// and reusable coroutine helpers for the cyberpunk neon visual theme.
/// No MonoBehaviour required — pure static helpers any script can call.
/// </summary>
public static class NeonVisuals
{
    // ── Cyberpunk Neon Palette ─────────────────────────────────
    public static readonly Color BgDeep      = new Color(0.039f, 0.031f, 0.078f); // #0A0814
    public static readonly Color BgMid       = new Color(0.102f, 0.039f, 0.180f); // #1A0A2E
    public static readonly Color NeonPink    = new Color(1f, 0.063f, 0.941f);     // #FF10F0
    public static readonly Color NeonCyan    = new Color(0f, 0.941f, 1f);          // #00F0FF
    public static readonly Color NeonPurple  = new Color(0.690f, 0.149f, 1f);     // #B026FF
    public static readonly Color NeonYellow  = new Color(1f, 0.937f, 0f);          // #FFEF00
    public static readonly Color NeonGreen   = new Color(0.082f, 1f, 0.494f);     // #15FF7E
    public static readonly Color White       = Color.white;

    // Brick row neon colors (top→bottom matches request gradient)
    public static readonly Color[] BrickNeonColors =
    {
        new Color(1f, 0.063f, 0.941f),   // Row 0: Hot Pink  #FF10F0
        new Color(1f, 0.412f, 0f),        // Row 1: Neon Orange #FF6900
        new Color(0.082f, 1f, 0.494f),   // Row 2: Neon Green  #15FF7E
        new Color(0f, 0.941f, 1f),        // Row 3: Neon Cyan   #00F0FF
        new Color(0.690f, 0.149f, 1f),   // Row 4: Neon Purple #B026FF
        new Color(1f, 0.937f, 0f),        // Row 5: Neon Yellow #FFEF00
        new Color(1f, 0.063f, 0.063f),   // Row 6: Neon Red    #FF1010
        new Color(0.063f, 0.063f, 1f),   // Row 7: Neon Blue   #1010FF
    };

    // ── Material Factory ───────────────────────────────────────

    /// <summary>Creates a neon-glowing Standard material.</summary>
    public static Material MakeNeonMaterial(Color baseColor, float emissionIntensity = 2.0f,
                                             float metallic = 0.4f, float smoothness = 0.85f)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = baseColor * 0.6f; // slightly darker base so emission pops
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", baseColor * emissionIntensity);
        mat.SetFloat("_Metallic",   metallic);
        mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }

    /// <summary>Creates a transparent/fade material (for dissolve animations).</summary>
    public static void SetTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 2);
        mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite",    0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    // ── Gradient Color Helpers ─────────────────────────────────

    /// <summary>Builds a neon trail gradient fading from color to transparent.</summary>
    public static Gradient MakeTrailGradient(Color startColor)
    {
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(startColor * 0.4f, 0.7f),
                new GradientColorKey(Color.black, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        return g;
    }

    /// <summary>Lissajous-rainbow color shifted by t ∈ [0,1].</summary>
    public static Color RainbowColor(float t)
    {
        float r = Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f;
        float g = Mathf.Sin(t * Mathf.PI * 2f + 2.094f) * 0.5f + 0.5f; // 2π/3 offset
        float b = Mathf.Sin(t * Mathf.PI * 2f + 4.189f) * 0.5f + 0.5f; // 4π/3 offset
        return new Color(r, g, b, 1f);
    }

    // ── Particle System Factory ────────────────────────────────

    /// <summary>Configures a particle system for a neon brick-break burst.</summary>
    public static void ConfigureBrickBurst(ParticleSystem ps, Color brickColor, Vector3 brickScale)
    {
        // Main
        var main             = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 12f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        main.startColor      = new ParticleSystem.MinMaxGradient(brickColor, Color.white);
        main.maxParticles    = 40;
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emission burst
        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 40) });

        // Shape
        var shape    = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = brickScale;

        // Color over lifetime: fade from bright neon to transparent
        var col     = ps.colorOverLifetime;
        col.enabled = true;
        var grad    = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(brickColor * 2f, 0f),
                    new GradientColorKey(brickColor,      0.5f),
                    new GradientColorKey(Color.white,     1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.5f),
                    new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Size over lifetime: shrink
        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        var sizeCurve  = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.1f);
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
    }

    /// <summary>Configures a particle system for ambient background drift.</summary>
    public static void ConfigureAmbientDrift(ParticleSystem ps, Color color)
    {
        var main              = ps.main;
        main.loop             = true;
        main.playOnAwake      = true;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startColor       = new ParticleSystem.MinMaxGradient(color, color * 0.3f);
        main.maxParticles     = 80;
        main.gravityModifier  = -0.05f; // drift upward slightly
        main.simulationSpace  = ParticleSystemSimulationSpace.World;

        var emission        = ps.emission;
        emission.enabled    = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(12f);

        var shape           = ps.shape;
        shape.enabled       = true;
        shape.shapeType     = ParticleSystemShapeType.Box;
        shape.scale         = new Vector3(18f, 14f, 1f);
    }
}
