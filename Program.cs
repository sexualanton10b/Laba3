using System;
using System.Collections.Generic;
using System.IO;

namespace Laba3
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string configPath = args.Length > 0 ? args[0] : "config.json";

            try
            {
                Console.WriteLine("ЛР4: Формирование изображения 3D поверхности");
                Console.WriteLine("Читаю конфиг: " + configPath);

                AppConfig cfg = AppConfig.Load(configPath);

                Console.WriteLine("1. Читаю карту глубины...");
                if (!File.Exists(cfg.DepthMapPath))
                    throw new FileNotFoundException("Файл карты глубины не найден: " + cfg.DepthMapPath);

                double[,] depthMap = DepthMapReader.ReadDepthMap(cfg.DepthMapPath);
                DepthMapReader.PrintDepthMapStatistics(depthMap);
                Console.WriteLine();

                Console.WriteLine("2. Преобразую в 3D координаты...");
                List<Vertex> vertices = DepthTo3DConverter.ConvertDepthMapTo3DNormalized(
                    depthMap, cfg.Mesh.XYScale, cfg.Mesh.ZScale);
                Console.WriteLine();

                Console.WriteLine("3. Генерирую сетку...");
                List<Triangle> triangles = MeshGenerator.GenerateTriangles(depthMap, vertices);
                Console.WriteLine();

                Console.WriteLine("4. Экспортирую оболочку (минимум 3 формата)...");
                if (cfg.Exports == null || cfg.Exports.Count == 0)
                    Console.WriteLine("Exports пустой — пропускаю экспорт.");
                else
                {
                    foreach (var ex in cfg.Exports)
                    {
                        ExportDispatcher.Export(ex.Format, ex.Path, vertices, triangles);
                    }
                }
                Console.WriteLine();

                Console.WriteLine("5. OpenGL визуализация + (опционально) сохранение BMP...");
                Console.WriteLine("Управление: стрелки - поворот, ESC - выход, 1/2/3 - Lambert/BlinnPhong/OrenNayar");

                using (var viewer = new DepthMapViewer(vertices, triangles, cfg))
                {
                    viewer.Run();
                }

                Console.WriteLine("Готово!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            Console.ReadKey();
        }
    }
}
