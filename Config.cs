namespace Laba4;

public sealed class AppConfig
{
    public string DepthMapPath { get; set; } = "DepthMaps1-20/DepthMap_8.dat";
    public MeshConfig Mesh { get; set; } = new();
    public List<ExportConfig> Exports { get; set; } = new();

    public CameraConfig Camera { get; set; } = new();
    public LightConfig Light { get; set; } = new();
    public MaterialConfig Material { get; set; } = new();
    public RenderConfig Render { get; set; } = new();

    // "Lambert" / "BlinnPhong" / "OrenNayar"
    public string ShadingModel { get; set; } = "BlinnPhong";
}

public sealed class MeshConfig
{
    public float XYScale { get; set; } = 1.0f;
    public float ZScale { get; set; } = 100.0f;
}

public sealed class ExportConfig
{
    public string Format { get; set; } = "ply"; // ply/obj/stl
    public string Path { get; set; } = "out/output.ply";
}

public sealed class CameraConfig
{
    public float[] Position { get; set; } = [0, 0, -500];
    public float[] Target { get; set; } = [0, 0, 0];
    public float[] Up { get; set; } = [0, 1, 0];

    // "perspective" or "ortho"
    public string Projection { get; set; } = "perspective";
    public float FovDegrees { get; set; } = 45.0f;
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 2000.0f;

    // для ortho
    public float OrthoHalfSize { get; set; } = 300.0f;
}

public sealed class LightConfig
{
    // Направление параллельных лучей L[Lx Ly Lz]
    public float[] Direction { get; set; } = [-0.3f, -0.2f, -1.0f];
    public float[] Color { get; set; } = [1, 1, 1];
    public float Intensity { get; set; } = 2.5f;
}

public sealed class MaterialConfig
{
    public float[] ObjectColor { get; set; } = [0.6f, 0.7f, 0.9f];
    public float AmbientStrength { get; set; } = 0.25f;

    // Blinn-Phong
    public float SpecularStrength { get; set; } = 0.6f;
    public float Shininess { get; set; } = 32.0f;

    // Oren–Nayar
    public float OrenNayarSigmaDegrees { get; set; } = 30.0f;
}

public sealed class RenderConfig
{
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 800;

    public string SaveBmpPath { get; set; } = "out/render.bmp";
    public bool AutoSave { get; set; } = true;
}
