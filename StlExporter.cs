using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OpenTK.Mathematics;

namespace Laba3
{
    public static class StlExporter
    {
        public static void ExportToStlAscii(string filePath, List<Vertex> vertices, List<Triangle> triangles)
        {
            if (vertices == null || vertices.Count == 0)
                throw new ArgumentException("Вершины пусты");
            if (triangles == null || triangles.Count == 0)
                throw new ArgumentException("Треугольники пусты");

            var inv = CultureInfo.InvariantCulture;
            using var writer = new StreamWriter(filePath, false, Encoding.ASCII);
            writer.WriteLine("solid depthmap");

            foreach (var t in triangles)
            {
                var v0 = new Vector3(vertices[t.V1].X, vertices[t.V1].Y, vertices[t.V1].Z);
                var v1 = new Vector3(vertices[t.V2].X, vertices[t.V2].Y, vertices[t.V2].Z);
                var v2 = new Vector3(vertices[t.V3].X, vertices[t.V3].Y, vertices[t.V3].Z);

                var n = Vector3.Cross(v1 - v0, v2 - v0);
                if (n.LengthSquared > 0)
                    n = n.Normalized();
                else
                    n = Vector3.UnitZ;

                writer.WriteLine($"  facet normal {n.X.ToString(inv)} {n.Y.ToString(inv)} {n.Z.ToString(inv)}");
                writer.WriteLine("    outer loop");
                writer.WriteLine($"      vertex {v0.X.ToString(inv)} {v0.Y.ToString(inv)} {v0.Z.ToString(inv)}");
                writer.WriteLine($"      vertex {v1.X.ToString(inv)} {v1.Y.ToString(inv)} {v1.Z.ToString(inv)}");
                writer.WriteLine($"      vertex {v2.X.ToString(inv)} {v2.Y.ToString(inv)} {v2.Z.ToString(inv)}");
                writer.WriteLine("    endloop");
                writer.WriteLine("  endfacet");
            }

            writer.WriteLine("endsolid depthmap");
            Console.WriteLine("STL файл сохранен: " + filePath);
        }
    }
}
