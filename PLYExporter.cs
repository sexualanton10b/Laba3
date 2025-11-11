using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Laba3
{
    public class PLYExporter
    {
        public static void ExportToPLY(string filePath, List<Vertex> vertices, List<Triangle> faces)
        {
            if (vertices == null || vertices.Count == 0)
                throw new ArgumentException("Вершины пусты");

            if (faces == null || faces.Count == 0)
                throw new ArgumentException("Грани пусты");

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
                    writer.WriteLine(v.X + " " + v.Y + " " + v.Z);

                foreach (var f in faces)
                    writer.WriteLine("3 " + f.V1 + " " + f.V2 + " " + f.V3);
            }

            Console.WriteLine("PLY файл сохранен: " + filePath);
        }
    }
}
