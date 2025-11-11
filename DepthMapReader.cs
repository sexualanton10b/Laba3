using System;
using System.IO;

namespace Laba3
{
    public class DepthMapReader
    {
        public static double[,] ReadDepthMap(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден: " + filePath);

            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                int height = (int)reader.ReadDouble();
                int width = (int)reader.ReadDouble();

                if (height <= 0 || width <= 0)
                    throw new InvalidOperationException("Некорректные размеры: " + width + "x" + height);

                Console.WriteLine("Размер карты: " + width + "x" + height + " пикселей");

                double[,] depthMap = new double[height, width];

                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        depthMap[y, x] = reader.ReadDouble();

                return depthMap;
            }
        }

        public static void PrintDepthMapStatistics(double[,] depthMap)
        {
            double minDepth = double.MaxValue;
            double maxDepth = double.MinValue;
            int validCount = 0;

            foreach (double depth in depthMap)
            {
                if (depth != 0)
                {
                    validCount++;
                    if (depth < minDepth) minDepth = depth;
                    if (depth > maxDepth) maxDepth = depth;
                }
            }

            Console.WriteLine("Глубина: " + minDepth.ToString("F2") + " - " + maxDepth.ToString("F2"));
            Console.WriteLine("Валидных пикселей: " + validCount);
        }
    }
}
