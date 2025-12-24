using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Laba3
{
    public static class MeshUtils
    {
        // Считает нормали в вершинах: нормаль вершины = усреднение нормалей всех прилегающих треугольников
        public static Vector3[] CalculateVertexNormals(IReadOnlyList<Vertex> vertices, IReadOnlyList<Triangle> triangles)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));

            // Массив нормалей по одной на каждую вершину
            Vector3[] normals = new Vector3[vertices.Count];

            foreach (var tri in triangles)
            {
                // Берём три вершины треугольника по индексам
                Vector3 v0 = new Vector3(vertices[tri.V1].X, vertices[tri.V1].Y, vertices[tri.V1].Z);
                Vector3 v1 = new Vector3(vertices[tri.V2].X, vertices[tri.V2].Y, vertices[tri.V2].Z);
                Vector3 v2 = new Vector3(vertices[tri.V3].X, vertices[tri.V3].Y, vertices[tri.V3].Z);

                // Два ребра треугольника
                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;

                // Нормаль треугольника (перпендикуляр к плоскости)
                Vector3 n = Vector3.Cross(edge1, edge2);

                // Дегенеративный треугольник (почти нулевая площадь) — пропускаем
                if (n.LengthSquared <= 1e-20f)
                    continue;

                // Нормализуем и добавляем вклад к нормалям всех трёх вершин
                n = n.Normalized();
                normals[tri.V1] += n;
                normals[tri.V2] += n;
                normals[tri.V3] += n;
            }

            // Финальная нормализация: приводим усреднённые нормали к единичной длине
            for (int i = 0; i < normals.Length; i++)
            {
                if (normals[i].LengthSquared > 1e-20f)
                    normals[i] = normals[i].Normalized();
                else
                    normals[i] = Vector3.UnitZ; // запасной вариант, если нормаль не посчиталась
            }

            return normals;
        }

        // Безопасно превращает float[3] в Vector3 (если массив невалидный — возвращает fallback)
        public static Vector3 ToVector3(float[] arr, Vector3 fallback)
        {
            if (arr == null || arr.Length < 3) return fallback;
            return new Vector3(arr[0], arr[1], arr[2]);
        }

        // Ограничивает компоненты вектора диапазоном [0..1] (удобно для цветов)
        public static Vector3 Clamp01(Vector3 v)
        {
            return new Vector3(
                Math.Clamp(v.X, 0f, 1f),
                Math.Clamp(v.Y, 0f, 1f),
                Math.Clamp(v.Z, 0f, 1f));
        }
    }
}
