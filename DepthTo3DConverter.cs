using System;
using System.Collections.Generic;

namespace Laba3
{
    public class DepthTo3DConverter
    {
        public static List<Vertex> ConvertDepthMapTo3DNormalized(double[,] depthMap, float scale = 1.0f)
        {
            List<Vertex> vertices = new List<Vertex>();
            int height = depthMap.GetLength(0);
            int width = depthMap.GetLength(1);

            // Ищем min и max глубины
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

            // Преобразуем каждый пиксель в вершину
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double depth = depthMap[y, x];

                    if (depth == 0.0)
                        continue;

                    float x3d = ((float)x - centerX) * scale;
                    float y3d = ((float)y - centerY) * scale;
                    float z3d = (float)((depth - minDepth) / depthRange * 100.0f);

                    vertices.Add(new Vertex(x3d, y3d, z3d));
                }
            }

            Console.WriteLine("Создано вершин: " + vertices.Count);
            return vertices;
        }
    }
}
