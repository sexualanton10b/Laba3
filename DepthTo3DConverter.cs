using System;
using System.Collections.Generic;

namespace Laba3
{
    public class DepthTo3DConverter
    {
        // Преобразует карту глубины в 3D координаты:
        // X,Y масштабируются XYScale, Z нормализуется и умножается на ZScale.
        public static List<Vertex> ConvertDepthMapTo3DNormalized(double[,] depthMap, float xyScale, float zScale)
        {
            List<Vertex> vertices = new List<Vertex>();
            int height = depthMap.GetLength(0);
            int width = depthMap.GetLength(1);

            double minDepth = double.MaxValue;
            double maxDepth = double.MinValue;

            foreach (double depth in depthMap)
            {
                if (depth != 0)
                {
                    if (depth < minDepth) minDepth = depth;
                    if (depth > maxDepth) maxDepth = depth;
                }
            }

            double depthRange = maxDepth - minDepth;
            if (depthRange == 0) depthRange = 1.0;

            float centerX = width / 2.0f;
            float centerY = height / 2.0f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double depth = depthMap[y, x];
                    if (depth == 0.0) continue;

                    float x3d = ((float)x - centerX) * xyScale;
                    float y3d = ((float)y - centerY) * xyScale;

                    float zn = (float)((depth - minDepth) / depthRange); // 0..1
                    float z3d = zn * zScale;

                    vertices.Add(new Vertex(x3d, y3d, z3d));
                }
            }

            Console.WriteLine("Создано вершин: " + vertices.Count);
            return vertices;
        }
    }
}
