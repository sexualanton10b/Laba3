using System;
using System.Collections.Generic;
using System.IO;

namespace Laba3
{
    public static class MeshExportDispatcher
    {
        public static void Export(ExportConfig export, List<Vertex> vertices, List<Triangle> triangles)
        {
            if (export == null) throw new ArgumentNullException(nameof(export));
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));

            string fmt = (export.Format ?? "").Trim().ToLowerInvariant();
            string path = export.Path ?? "";
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Export.Path пуст");

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            switch (fmt)
            {
                case "ply":
                    PLYExporter.ExportToPLY(path, vertices, triangles);
                    break;
                case "obj":
                    ObjExporter.ExportToObj(path, vertices, triangles);
                    break;
                case "stl":
                    StlExporter.ExportToStlAscii(path, vertices, triangles);
                    break;
                default:
                    throw new InvalidOperationException($"Неизвестный формат экспорта: '{export.Format}'. Поддерживается: ply, obj, stl");
            }
        }
    }
}
