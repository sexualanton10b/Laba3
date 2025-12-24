using System;
using OpenTK.Mathematics;

namespace Laba3
{
    public static class ShadingModels
    {
        // Единая точка входа: выбирает модель освещения по mat.Model
        public static Vector3 Shade(
            Vector3 normal,
            Vector3 worldPos,
            Vector3 viewPos,
            Vector3 lightDir,
            LightConfig light,
            ShadingConfig mat)
        {
            // V — направление на камеру, N — нормаль поверхности (оба нормализуем)
            Vector3 V = (viewPos - worldPos);
            V = (V.LengthSquared > 1e-20f) ? V.Normalized() : Vector3.UnitZ;

            Vector3 N = (normal.LengthSquared > 1e-20f) ? normal.Normalized() : Vector3.UnitZ;

            // Чтобы освещение не "инвертировалось" на обратной стороне
            if (Vector3.Dot(N, V) < 0f) N = -N;

            string m = (mat.Model ?? "lambert").Trim().ToLowerInvariant();

            return m switch
            {
                "lambert" => Lambert(N, lightDir, light, mat),
                "blinn_phong" or "blinnphong" => BlinnPhong(N, worldPos, viewPos, lightDir, light, mat),
                "oren_nayar" or "orennayar" => OrenNayar(N, worldPos, viewPos, lightDir, light, mat),
                _ => throw new InvalidOperationException(
                    $"Неизвестная модель отражения: '{mat.Model}'. Поддерживается: lambert, blinn_phong, oren_nayar")
            };
        }

        // Lambert: только диффузная компонента (N·L)
        public static Vector3 Lambert(Vector3 N, Vector3 lightDir, LightConfig light, ShadingConfig mat)
        {
            Vector3 L = (lightDir.LengthSquared > 1e-20f) ? lightDir.Normalized() : new Vector3(0, 0, -1);
            float ndotl = MathF.Max(0f, Vector3.Dot(N, L));

            Vector3 baseColor = MeshUtils.ToVector3(mat.BaseColor, new Vector3(0.9f));
            Vector3 lightColor = MeshUtils.ToVector3(light.Color, Vector3.One) * light.Intensity;

            Vector3 ambient = baseColor * mat.Ambient;
            Vector3 diffuse = baseColor * (mat.Diffuse * ndotl);

            return (ambient + diffuse) * lightColor;
        }

        // Blinn-Phong: Lambert + блик через half-vector H = normalize(L+V)
        public static Vector3 BlinnPhong(
            Vector3 N,
            Vector3 worldPos,
            Vector3 viewPos,
            Vector3 lightDir,
            LightConfig light,
            ShadingConfig mat)
        {
            Vector3 L = (lightDir.LengthSquared > 1e-20f) ? lightDir.Normalized() : new Vector3(0, 0, -1);

            Vector3 V = (viewPos - worldPos);
            if (V.LengthSquared > 1e-20f) V = V.Normalized();

            float ndotl = MathF.Max(0f, Vector3.Dot(N, L));

            Vector3 H = L + V;
            if (H.LengthSquared > 1e-20f) H = H.Normalized();

            float ndoth = MathF.Max(0f, Vector3.Dot(N, H));
            float spec = (ndotl > 0f) ? MathF.Pow(ndoth, Math.Max(1, mat.Shininess)) : 0f;

            Vector3 baseColor = MeshUtils.ToVector3(mat.BaseColor, new Vector3(0.9f));
            Vector3 lightColor = MeshUtils.ToVector3(light.Color, Vector3.One) * light.Intensity;

            Vector3 ambient = baseColor * mat.Ambient;
            Vector3 diffuse = baseColor * (mat.Diffuse * ndotl);
            Vector3 specular = Vector3.One * (mat.Specular * spec);

            return (ambient + diffuse + specular) * lightColor;
        }

        // Oren–Nayar: диффузная модель для шероховатых поверхностей (sigma = RoughnessDegrees)
        public static Vector3 OrenNayar(
            Vector3 N,
            Vector3 worldPos,
            Vector3 viewPos,
            Vector3 lightDir,
            LightConfig light,
            ShadingConfig mat)
        {
            Vector3 L = (lightDir.LengthSquared > 1e-20f) ? lightDir.Normalized() : new Vector3(0, 0, -1);

            Vector3 V = (viewPos - worldPos);
            if (V.LengthSquared > 1e-20f) V = V.Normalized();

            float NL = MathF.Max(0f, Vector3.Dot(N, L));
            float NV = MathF.Max(0f, Vector3.Dot(N, V));
            if (NL <= 0f || NV <= 0f) return Vector3.Zero;

            float thetaI = MathF.Acos(Math.Clamp(NL, -1f, 1f));
            float thetaR = MathF.Acos(Math.Clamp(NV, -1f, 1f));
            float alpha = MathF.Max(thetaI, thetaR);
            float beta = MathF.Min(thetaI, thetaR);

            Vector3 Lperp = L - N * NL;
            Vector3 Vperp = V - N * NV;

            float cosDeltaPhi = 0f;
            float lLen2 = Lperp.LengthSquared;
            float vLen2 = Vperp.LengthSquared;
            if (lLen2 > 1e-12f && vLen2 > 1e-12f)
                cosDeltaPhi = MathF.Max(0f, Vector3.Dot(Lperp, Vperp) / MathF.Sqrt(lLen2 * vLen2));

            float sigma = MathHelper.DegreesToRadians(mat.RoughnessDegrees);
            float sigma2 = sigma * sigma;

            float A = 1f - (sigma2 / (2f * (sigma2 + 0.33f)));
            float B = 0.45f * (sigma2 / (sigma2 + 0.09f));

            float oren = NL * (A + B * cosDeltaPhi * MathF.Sin(alpha) * MathF.Tan(beta));

            Vector3 baseColor = MeshUtils.ToVector3(mat.BaseColor, new Vector3(0.9f));
            Vector3 lightColor = MeshUtils.ToVector3(light.Color, Vector3.One) * light.Intensity;

            Vector3 ambient = baseColor * mat.Ambient;
            Vector3 diffuse = baseColor * (mat.Diffuse * oren);

            // Опционально: оставляем specular как в Blinn-Phong (можно обнулить Specular в JSON)
            Vector3 specular = Vector3.Zero;
            if (mat.Specular > 0f)
            {
                Vector3 H = L + V;
                if (H.LengthSquared > 1e-20f) H = H.Normalized();
                float ndoth = MathF.Max(0f, Vector3.Dot(N, H));
                float spec = MathF.Pow(ndoth, Math.Max(1, mat.Shininess));
                specular = Vector3.One * (mat.Specular * spec);
            }

            return (ambient + diffuse + specular) * lightColor;
        }
    }
}
