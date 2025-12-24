using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Laba3
{
    public static class SoftwareRenderer
    {
        // CPU-рендер: растеризация треугольников в Bitmap (без OpenGL)
        public static Bitmap RenderToBitmap(
            IReadOnlyList<Vertex> vertices,
            IReadOnlyList<Triangle> triangles,
            CameraConfig cameraCfg,
            LightConfig lightCfg,
            ShadingConfig shadingCfg,
            RenderConfig renderCfg)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));
            if (renderCfg == null) throw new ArgumentNullException(nameof(renderCfg));

            int width = renderCfg.Width;
            int height = renderCfg.Height;

            // Поворот модели (из конфига)
            float rx = MathHelper.DegreesToRadians(renderCfg.ModelRotationXDegrees);
            float ry = MathHelper.DegreesToRadians(renderCfg.ModelRotationYDegrees);

            // Камера (нужна для моделей с бликом/углом обзора)
            Vector3 camPos = MeshUtils.ToVector3(cameraCfg.Position, new Vector3(0, 0, -500));

            // Направление направленного света (нормализуем)
            Vector3 lightDir = MeshUtils.ToVector3(lightCfg.Direction, new Vector3(-0.3f, -0.2f, -1f));
            lightDir = (lightDir.LengthSquared > 1e-20f) ? lightDir.Normalized() : new Vector3(0, 0, -1);

            // Нормали в вершинах (для интерполяции по пикселям)
            Vector3[] normalsLocal = MeshUtils.CalculateVertexNormals(vertices, triangles);

            // Вершины/нормали после поворота + границы для авто-вписывания
            Vector3[] world = new Vector3[vertices.Count];
            Vector3[] normals = new Vector3[vertices.Count];

            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 p = new Vector3(vertices[i].X, vertices[i].Y, vertices[i].Z);
                Vector3 n = normalsLocal[i];

                p = RotateX(p, rx);
                p = RotateY(p, ry);

                n = RotateX(n, rx);
                n = RotateY(n, ry);
                n = (n.LengthSquared > 1e-20f) ? n.Normalized() : Vector3.UnitZ;

                world[i] = p;
                normals[i] = n;

                minX = MathF.Min(minX, p.X); maxX = MathF.Max(maxX, p.X);
                minY = MathF.Min(minY, p.Y); maxY = MathF.Max(maxY, p.Y);
                minZ = MathF.Min(minZ, p.Z); maxZ = MathF.Max(maxZ, p.Z);
            }

            float rangeX = MathF.Max(1e-6f, maxX - minX);
            float rangeY = MathF.Max(1e-6f, maxY - minY);
            float rangeZ = MathF.Max(1e-6f, maxZ - minZ);

            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;

            // Автоматически масштабируем модель так, чтобы влезла в кадр
            float pad = 0.90f;
            float scaleX = (width * pad) / rangeX;
            float scaleY = (height * pad) / rangeY;
            float scale = MathF.Min(scaleX, scaleY);

            // Цветовой буфер + Z-buffer
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int stride = data.Stride;
            byte[] buffer = new byte[stride * height];

            int[] bg = renderCfg.BackgroundRgb ?? new[] { 30, 30, 30 };
            byte bgR = (byte)Math.Clamp(bg[0], 0, 255);
            byte bgG = (byte)Math.Clamp(bg[1], 0, 255);
            byte bgB = (byte)Math.Clamp(bg[2], 0, 255);

            // Заполняем фон один раз
            for (int y = 0; y < height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int idx = row + x * 3;
                    buffer[idx + 0] = bgB;
                    buffer[idx + 1] = bgG;
                    buffer[idx + 2] = bgR;
                }
            }

            float[] zBuf = new float[width * height];
            for (int i = 0; i < zBuf.Length; i++) zBuf[i] = float.PositiveInfinity;

            // Постобработка яркости/гаммы (чтобы картинка не была слишком тёмной)
            float exposure = MathF.Max(0.01f, renderCfg.Exposure);
            float gamma = MathF.Max(0.01f, renderCfg.Gamma);
            float invGamma = 1f / gamma;

            // Растеризация треугольников по барицентрическим координатам
            for (int ti = 0; ti < triangles.Count; ti++)
            {
                Triangle t = triangles[ti];
                int i0 = t.V1;
                int i1 = t.V2;
                int i2 = t.V3;

                Vector3 w0 = world[i0];
                Vector3 w1 = world[i1];
                Vector3 w2 = world[i2];

                Vector2 s0 = WorldToScreen(w0, cx, cy, scale, width, height);
                Vector2 s1 = WorldToScreen(w1, cx, cy, scale, width, height);
                Vector2 s2 = WorldToScreen(w2, cx, cy, scale, width, height);

                // Нормализуем Z в 0..1 для Z-buffer
                float z0 = (w0.Z - minZ) / rangeZ;
                float z1 = (w1.Z - minZ) / rangeZ;
                float z2 = (w2.Z - minZ) / rangeZ;

                int minXpx = (int)MathF.Floor(MathF.Min(s0.X, MathF.Min(s1.X, s2.X)));
                int maxXpx = (int)MathF.Ceiling(MathF.Max(s0.X, MathF.Max(s1.X, s2.X)));
                int minYpx = (int)MathF.Floor(MathF.Min(s0.Y, MathF.Min(s1.Y, s2.Y)));
                int maxYpx = (int)MathF.Ceiling(MathF.Max(s0.Y, MathF.Max(s1.Y, s2.Y)));

                if (maxXpx < 0 || maxYpx < 0 || minXpx >= width || minYpx >= height)
                    continue;

                minXpx = Math.Clamp(minXpx, 0, width - 1);
                maxXpx = Math.Clamp(maxXpx, 0, width - 1);
                minYpx = Math.Clamp(minYpx, 0, height - 1);
                maxYpx = Math.Clamp(maxYpx, 0, height - 1);

                float area = Edge(s0, s1, s2);
                if (Math.Abs(area) < 1e-7f) continue;

                Vector3 n0 = normals[i0];
                Vector3 n1 = normals[i1];
                Vector3 n2 = normals[i2];

                for (int y = minYpx; y <= maxYpx; y++)
                {
                    for (int x = minXpx; x <= maxXpx; x++)
                    {
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                        float wA = Edge(s1, s2, p) / area;
                        float wB = Edge(s2, s0, p) / area;
                        float wC = Edge(s0, s1, p) / area;

                        if (wA < 0f || wB < 0f || wC < 0f) continue;

                        float z = z0 * wA + z1 * wB + z2 * wC;
                        int zIndex = y * width + x;
                        if (z >= zBuf[zIndex]) continue;
                        zBuf[zIndex] = z;

                        Vector3 wp = w0 * wA + w1 * wB + w2 * wC;
                        Vector3 nn = (n0 * wA + n1 * wB + n2 * wC);
                        nn = (nn.LengthSquared > 1e-20f) ? nn.Normalized() : Vector3.UnitZ;

                        Vector3 rgb = ShadingModels.Shade(nn, wp, camPos, lightDir, lightCfg, shadingCfg);

                        rgb *= exposure;
                        rgb = MeshUtils.Clamp01(rgb);
                        rgb = new Vector3(
                            MathF.Pow(rgb.X, invGamma),
                            MathF.Pow(rgb.Y, invGamma),
                            MathF.Pow(rgb.Z, invGamma)
                        );

                        int bufIdx = y * stride + x * 3;
                        buffer[bufIdx + 0] = (byte)Math.Clamp((int)(rgb.Z * 255f), 0, 255); // B
                        buffer[bufIdx + 1] = (byte)Math.Clamp((int)(rgb.Y * 255f), 0, 255); // G
                        buffer[bufIdx + 2] = (byte)Math.Clamp((int)(rgb.X * 255f), 0, 255); // R
                    }
                }
            }

            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        // Ортографическое преобразование мира в экран с авто-масштабом
        private static Vector2 WorldToScreen(Vector3 w, float cx, float cy, float scale, int width, int height)
        {
            float sx = (w.X - cx) * scale + width * 0.5f;
            float sy = height * 0.5f - (w.Y - cy) * scale;
            return new Vector2(sx, sy);
        }

        // Знак площади (используется для барицентрических весов)
        private static float Edge(Vector2 a, Vector2 b, Vector2 c)
        {
            return (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
        }

        private static Vector3 RotateX(Vector3 v, float a)
        {
            float ca = MathF.Cos(a);
            float sa = MathF.Sin(a);
            return new Vector3(
                v.X,
                v.Y * ca - v.Z * sa,
                v.Y * sa + v.Z * ca
            );
        }

        private static Vector3 RotateY(Vector3 v, float a)
        {
            float ca = MathF.Cos(a);
            float sa = MathF.Sin(a);
            return new Vector3(
                v.X * ca + v.Z * sa,
                v.Y,
                -v.X * sa + v.Z * ca
            );
        }
    }
}
