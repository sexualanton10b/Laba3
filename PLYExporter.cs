using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Laba3
{
    public class PLYExporter
    {
        public static void ExportToPLY(string filePath, List<Vertex> vertices, List<Triangle> faces)
        {
            if (vertices == null || vertices.Count == 0) throw new ArgumentException("Вершины пусты");
            if (faces == null || faces.Count == 0) throw new ArgumentException("Грани пусты");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                writer.WriteLine("ply");
                writer.WriteLine("format ascii 1.0");
                writer.WriteLine("comment Depth map to 3D mesh");
                writer.WriteLine("element vertex " + vertices.Count);
                writer.WriteLine("property float x");
                writer.WriteLine("property float y");
                writer.WriteLine("property float z");
                writer.WriteLine("element face " + faces.Count);
                writer.WriteLine("property list uchar int vertex_index");
                writer.WriteLine("end_header");

                foreach (var v in vertices)
                    writer.WriteLine($"{v.X.ToString(CultureInfo.InvariantCulture)} {v.Y.ToString(CultureInfo.InvariantCulture)} {v.Z.ToString(CultureInfo.InvariantCulture)}");

                foreach (var f in faces)
                    writer.WriteLine($"3 {f.V1} {f.V2} {f.V3}");
            }

            Console.WriteLine("PLY файл сохранен: " + filePath);
        }
    }

    public static class OBJExporter
    {
        public static void ExportToOBJ(string filePath, List<Vertex> vertices, List<Triangle> faces)
        {
            if (vertices == null || vertices.Count == 0) throw new ArgumentException("Вершины пусты");
            if (faces == null || faces.Count == 0) throw new ArgumentException("Грани пусты");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                writer.WriteLine("# Depth map to OBJ");
                foreach (var v in vertices)
                    writer.WriteLine($"v {v.X.ToString(CultureInfo.InvariantCulture)} {v.Y.ToString(CultureInfo.InvariantCulture)} {v.Z.ToString(CultureInfo.InvariantCulture)}");

                // OBJ индексы с 1
                foreach (var f in faces)
                    writer.WriteLine($"f {f.V1 + 1} {f.V2 + 1} {f.V3 + 1}");
            }

            Console.WriteLine("OBJ файл сохранен: " + filePath);
        }
    }

    public static class STLExporter
    {
        public static void ExportToSTLAscii(string filePath, List<Vertex> vertices, List<Triangle> faces)
        {
            if (vertices == null || vertices.Count == 0) throw new ArgumentException("Вершины пусты");
            if (faces == null || faces.Count == 0) throw new ArgumentException("Грани пусты");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                writer.WriteLine("solid depthmesh");

                foreach (var t in faces)
                {
                    var v0 = vertices[t.V1];
                    var v1 = vertices[t.V2];
                    var v2 = vertices[t.V3];

                    // Нормаль грани (простая)
                    float ux = v1.X - v0.X; float uy = v1.Y - v0.Y; float uz = v1.Z - v0.Z;
                    float vx = v2.X - v0.X; float vy = v2.Y - v0.Y; float vz = v2.Z - v0.Z;

                    float nx = uy * vz - uz * vy;
                    float ny = uz * vx - ux * vz;
                    float nz = ux * vy - uy * vx;

                    float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    if (len > 1e-8f) { nx /= len; ny /= len; nz /= len; }

                    writer.WriteLine($"  facet normal {nx.ToString(CultureInfo.InvariantCulture)} {ny.ToString(CultureInfo.InvariantCulture)} {nz.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine("    outer loop");

                    writer.WriteLine($"      vertex {v0.X.ToString(CultureInfo.InvariantCulture)} {v0.Y.ToString(CultureInfo.InvariantCulture)} {v0.Z.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"      vertex {v1.X.ToString(CultureInfo.InvariantCulture)} {v1.Y.ToString(CultureInfo.InvariantCulture)} {v1.Z.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"      vertex {v2.X.ToString(CultureInfo.InvariantCulture)} {v2.Y.ToString(CultureInfo.InvariantCulture)} {v2.Z.ToString(CultureInfo.InvariantCulture)}");

                    writer.WriteLine("    endloop");
                    writer.WriteLine("  endfacet");
                }

                writer.WriteLine("endsolid depthmesh");
            }

            Console.WriteLine("STL (ASCII) файл сохранен: " + filePath);
        }
    }

    public static class ExportDispatcher
    {
        public static void Export(string format, string path, List<Vertex> vertices, List<Triangle> triangles)
        {
            format = (format ?? "").Trim().ToLowerInvariant();
            switch (format)
            {
                case "ply":
                    PLYExporter.ExportToPLY(path, vertices, triangles);
                    break;
                case "obj":
                    OBJExporter.ExportToOBJ(path, vertices, triangles);
                    break;
                case "stl":
                    STLExporter.ExportToSTLAscii(path, vertices, triangles);
                    break;
                default:
                    throw new ArgumentException("Неизвестный формат экспорта: " + format);
            }
        }
    }
}
