using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Laba3
{
    // Три требуемые модели освещения (вариант 8)
    public enum ShadingModel
    {
        Lambert = 0,
        BlinnPhong = 1,
        OrenNayar = 2
    }

    // Корневой класс конфигурации (читается из config.json)
    public sealed class AppConfig
    {
        public string DepthMapPath { get; set; } = "DepthMaps1-20/DepthMap_8.dat";
        public MeshConfig Mesh { get; set; } = new();
        public List<ExportConfig> Exports { get; set; } = new();
        public RenderConfig Render { get; set; } = new();
        public CameraConfig Camera { get; set; } = new();
        public LightConfig Light { get; set; } = new();
        public ShadingConfig Shading { get; set; } = new();

        // Загрузка JSON-конфига
        public static AppConfig Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Config JSON не найден: " + path);

            var json = File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (cfg == null)
                throw new InvalidOperationException("Не удалось разобрать config JSON: " + path);

            return cfg;
        }
    }

    // Масштабирование сетки при преобразовании depthmap -> 3D
    public sealed class MeshConfig
    {
        public float XYScale { get; set; } = 1.0f;
        public float ZScale { get; set; } = 100.0f;
    }

    // Настройки экспорта
    public sealed class ExportConfig
    {
        public string Format { get; set; } = "ply"; // ply | obj | stl
        public string Path { get; set; } = "out/output.ply";
    }

    // Параметры камеры (позиция, куда смотрит, вектор "вверх")
    public sealed class CameraConfig
    {
        public float[] Position { get; set; } = new float[] { 0, 0, -500 };
        public float[] Target { get; set; } = new float[] { 0, 0, 50 };
        public float[] Up { get; set; } = new float[] { 0, 1, 0 };
    }

    // Направленный (параллельный) источник света
    public sealed class LightConfig
    {
        // Direction — направление лучей (не позиция!)
        public float[] Direction { get; set; } = new float[] { -0.3f, -0.2f, -1.0f };
        public float[] Color { get; set; } = new float[] { 1, 1, 1 };
        public float Intensity { get; set; } = 2.5f;
    }

    // Параметры материала/освещения (используются в Lambert/Blinn-Phong/Oren–Nayar)
    public sealed class ShadingConfig
    {
        public string Model { get; set; } = "BlinnPhong";

        public float AmbientStrength { get; set; } = 0.25f;
        public float Kd { get; set; } = 1.0f;          // диффузный коэффициент
        public float Ks { get; set; } = 0.5f;          // зеркальный коэффициент
        public float Shininess { get; set; } = 32.0f;  // степень блика (Blinn-Phong)
        public float RoughnessDegrees { get; set; } = 25.0f; // шероховатость (Oren–Nayar)

        private float[] _baseColor = new float[] { 0.6f, 0.7f, 0.9f };

        // Базовый цвет объекта (RGB в 0..1)
        public float[] BaseColor
        {
            get => _baseColor;
            set => _baseColor = value ?? _baseColor;
        }

        public float[] ObjectColor
        {
            get => BaseColor;
            set => BaseColor = value;
        }

        // Алиасы для совместимости с ShadingModels.cs
        public float Ambient
        {
            get => AmbientStrength;
            set => AmbientStrength = value;
        }

        public float Diffuse
        {
            get => Kd;
            set => Kd = value;
        }

        public float Specular
        {
            get => Ks;
            set => Ks = value;
        }
    }

    // Параметры рендера/окна + сохранение BMP
    public sealed class RenderConfig
    {
        public int Width { get; set; } = 1200;
        public int Height { get; set; } = 800;
        public string Title { get; set; } = "Depth Map Viewer (LR4)";

        // Если true — сохраняем кадр в BMP
        public bool SaveBmp { get; set; } = true;
        public string BmpPath { get; set; } = "out/render.bmp";

        // Начальный поворот модели (использует и OpenGL viewer, и SoftwareRenderer)
        public float ModelRotationXDegrees { get; set; } = 180.0f;
        public float ModelRotationYDegrees { get; set; } = 0.0f;

        public int[] BackgroundRgb { get; set; } = new int[] { 30, 30, 30 };

        public float Exposure { get; set; } = 1.0f;
        public float Gamma { get; set; } = 2.2f;
    }
}
