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

            Console.WriteLine("Конвертер карты глубины в 3D модель");
            Console.WriteLine();

            // Ищем файл в текущей папке
            string depthMapPath = "DepthMap_8.dat";

            if (!File.Exists(depthMapPath))
            {
                Console.WriteLine("Ошибка: файл " + depthMapPath + " не найден");
                Console.ReadKey();
                return;
            }

            try
            {
                Console.WriteLine("1. Читаю карту глубины...");
                double[,] depthMap = DepthMapReader.ReadDepthMap(depthMapPath);
                DepthMapReader.PrintDepthMapStatistics(depthMap);
                Console.WriteLine();

                Console.WriteLine("2. Преобразую в 3D координаты...");
                List<Vertex> vertices = DepthTo3DConverter.ConvertDepthMapTo3DNormalized(depthMap, 1.0f);
                Console.WriteLine();

                Console.WriteLine("3. Генерирую сетку...");
                List<Triangle> triangles = MeshGenerator.GenerateTriangles(depthMap, vertices);
                Console.WriteLine();

                Console.WriteLine("4. Экспортирую PLY...");
                string outputFile = "output.ply";
                PLYExporter.ExportToPLY(outputFile, vertices, triangles);
                Console.WriteLine();

                Console.WriteLine("5. Открываю визуализацию...");
                Console.WriteLine("Управление: стрелки - поворот, W/S - зум, ESC - выход");
                Console.WriteLine();

                using (var viewer = new DepthMapViewer(vertices, triangles))
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
