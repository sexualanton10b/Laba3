using System;
using System.Collections.Generic;

namespace Laba3
{
    public class MeshGenerator
    {
        public static List<Triangle> GenerateTriangles(double[,] depthMap, List<Vertex> vertices)
        {
            List<Triangle> triangles = new List<Triangle>();
            int height = depthMap.GetLength(0);
            int width = depthMap.GetLength(1);

            // Создаём карту пикселей на индексы вершин
            Dictionary<(int, int), int> pixelToIndex = new Dictionary<(int, int), int>();
            int idx = 0;

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (depthMap[y, x] != 0.0)
                        pixelToIndex[(x, y)] = idx++;

            // Генерируем треугольники
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    if (depthMap[y, x] != 0 && depthMap[y, x + 1] != 0 && 
                        depthMap[y + 1, x] != 0 && depthMap[y + 1, x + 1] != 0)
                    {
                        int v00 = pixelToIndex[(x, y)];
                        int v10 = pixelToIndex[(x + 1, y)];
                        int v01 = pixelToIndex[(x, y + 1)];
                        int v11 = pixelToIndex[(x + 1, y + 1)];

                        triangles.Add(new Triangle(v00, v10, v01));
                        triangles.Add(new Triangle(v10, v11, v01));
                    }
                }
            }

            Console.WriteLine("Создано треугольников: " + triangles.Count);
            return triangles;
        }
    }
}
