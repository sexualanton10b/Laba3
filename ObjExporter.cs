using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Laba3
{
    public static class ObjExporter
    {
        public static void ExportToObj(string filePath, List<Vertex> vertices, List<Triangle> triangles)
        {
            if (vertices == null || vertices.Count == 0)
                throw new ArgumentException("Вершины пусты");
            if (triangles == null || triangles.Count == 0)
                throw new ArgumentException("Треугольники пусты");

            var inv = CultureInfo.InvariantCulture;
            using var writer = new StreamWriter(filePath, false, Encoding.ASCII);
            writer.WriteLine("# Depth map mesh exported as OBJ");
            writer.WriteLine($"# vertices: {vertices.Count}");
            writer.WriteLine($"# triangles: {triangles.Count}");

            foreach (var v in vertices)
                writer.WriteLine($"v {v.X.ToString(inv)} {v.Y.ToString(inv)} {v.Z.ToString(inv)}");

            foreach (var t in triangles)
                writer.WriteLine($"f {t.V1 + 1} {t.V2 + 1} {t.V3 + 1}");

            Console.WriteLine("OBJ файл сохранен: " + filePath);
        }
    }
}
