using System;
using System.IO;

namespace Laba3
{
    public class DepthMapReader
    {
        // Читает карту глубины из бинарного файла
        public static double[,] ReadDepthMap(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден: " + filePath);

            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                // Читаем высоту и ширину из первых двух double значений
                int height = (int)reader.ReadDouble();
                int width = (int)reader.ReadDouble();

                if (height <= 0 || width <= 0)
                    throw new InvalidOperationException("Некорректные размеры: " + width + "x" + height);

                Console.WriteLine("Размер карты: " + width + "x" + height + " пикселей");

                // Создаём 2D массив для хранения глубин
                double[,] depthMap = new double[height, width];

                // Читаем значения глубины построчно
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        depthMap[y, x] = reader.ReadDouble();

                return depthMap;
            }
        }

        // Выводит статистику карты глубины в консоль
        public static void PrintDepthMapStatistics(double[,] depthMap)
        {
            double minDepth = double.MaxValue;
            double maxDepth = double.MinValue;
            int validCount = 0;

            // Ищем минимум и максимум глубины (пропускаем нули)
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
