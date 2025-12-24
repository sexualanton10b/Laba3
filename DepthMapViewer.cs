using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Laba3
{
    public class DepthMapViewer : GameWindow
    {
        private int vao;
        private int vbo;
        private int ebo;
        private int shaderProgram;
        private int triangleCount;

        private Matrix4 projection;
        private Matrix4 view;
        private Matrix4 model;

        private Vector3 camPos;
        private Vector3 camTarget;
        private Vector3 camUp;

        private float rotationX = 180.0f;
        private float rotationY = 0.0f;

        private readonly List<Vertex> vertices;
        private readonly AppConfig config;

        private int shadingMode = 1; // 0 Lambert, 1 BlinnPhong, 2 OrenNayar
        private bool bmpSaved = false;

        public DepthMapViewer(List<Vertex> vertices, List<Triangle> triangles, AppConfig config)
            : base(GameWindowSettings.Default,
                   new NativeWindowSettings
                   {
                       ClientSize = (config.Render.Width, config.Render.Height),
                       Title = config.Render.Title
                   })
        {
            this.vertices = vertices;
            this.config = config;
            triangleCount = triangles.Count;

            camPos = new Vector3(config.Camera.Position[0], config.Camera.Position[1], config.Camera.Position[2]);
            camTarget = new Vector3(config.Camera.Target[0], config.Camera.Target[1], config.Camera.Target[2]);
            camUp = new Vector3(config.Camera.Up[0], config.Camera.Up[1], config.Camera.Up[2]);

            shadingMode = ParseModel(config.Shading.Model);

            rotationX = config.Render.ModelRotationXDegrees;
            rotationY = config.Render.ModelRotationYDegrees;

            InitializeBuffers(vertices, triangles);
        }

        private int ParseModel(string modelName)
        {
            modelName = (modelName ?? "").Trim().ToLowerInvariant();
            return modelName switch
            {
                "lambert" => 0,
                "blinnphong" => 1,
                "phong-blinn" => 1,
                "phongblinn" => 1,
                "oren-nayar" => 2,
                "orennayar" => 2,
                _ => 1
            };
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            var bg = config.Render.BackgroundRgb ?? new int[] { 30, 30, 30 };
            float br = (bg.Length > 0 ? Math.Clamp(bg[0], 0, 255) : 30) / 255f;
            float bgc = (bg.Length > 1 ? Math.Clamp(bg[1], 0, 255) : 30) / 255f;
            float bb = (bg.Length > 2 ? Math.Clamp(bg[2], 0, 255) : 30) / 255f;

            GL.ClearColor(br, bgc, bb, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            shaderProgram = CreateShaderProgram();

            projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                (float)Size.X / Size.Y,
                0.1f,
                5000.0f);

            model = Matrix4.Identity;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(shaderProgram);

            view = Matrix4.LookAt(camPos, camTarget, camUp);

            model = Matrix4.Identity;
            model *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotationX));
            model *= Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationY));

            int modelLoc = GL.GetUniformLocation(shaderProgram, "model");
            int viewLoc = GL.GetUniformLocation(shaderProgram, "view");
            int projLoc = GL.GetUniformLocation(shaderProgram, "projection");
            int normalMatLoc = GL.GetUniformLocation(shaderProgram, "normalMatrix");

            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);

            var normalMatrix = Matrix4.Transpose(Matrix4.Invert(model));
            GL.UniformMatrix4(normalMatLoc, false, ref normalMatrix);

            int viewPosLoc = GL.GetUniformLocation(shaderProgram, "viewPos");
            GL.Uniform3(viewPosLoc, camPos);

            var raysDir = new Vector3(config.Light.Direction[0], config.Light.Direction[1], config.Light.Direction[2]);
            if (raysDir.LengthSquared < 1e-12f) raysDir = new Vector3(0, 0, -1);
            raysDir = raysDir.Normalized();

            int lightDirLoc = GL.GetUniformLocation(shaderProgram, "lightDirRays");
            GL.Uniform3(lightDirLoc, raysDir);

            int lightColorLoc = GL.GetUniformLocation(shaderProgram, "lightColor");
            GL.Uniform3(lightColorLoc, new Vector3(config.Light.Color[0], config.Light.Color[1], config.Light.Color[2]));

            int lightIntLoc = GL.GetUniformLocation(shaderProgram, "lightIntensity");
            GL.Uniform1(lightIntLoc, config.Light.Intensity);

            var baseCol = config.Shading.BaseColor;
            int objectColorLoc = GL.GetUniformLocation(shaderProgram, "objectColor");
            GL.Uniform3(objectColorLoc, new Vector3(baseCol[0], baseCol[1], baseCol[2]));

            int ambientLoc = GL.GetUniformLocation(shaderProgram, "ambientStrength");
            GL.Uniform1(ambientLoc, config.Shading.Ambient);

            int kdLoc = GL.GetUniformLocation(shaderProgram, "kd");
            GL.Uniform1(kdLoc, config.Shading.Diffuse);

            int ksLoc = GL.GetUniformLocation(shaderProgram, "ks");
            GL.Uniform1(ksLoc, config.Shading.Specular);

            int shinLoc = GL.GetUniformLocation(shaderProgram, "shininess");
            GL.Uniform1(shinLoc, config.Shading.Shininess);

            int sigmaLoc = GL.GetUniformLocation(shaderProgram, "roughnessSigma");
            float sigmaRad = MathHelper.DegreesToRadians(config.Shading.RoughnessDegrees);
            GL.Uniform1(sigmaLoc, sigmaRad);

            int modeLoc = GL.GetUniformLocation(shaderProgram, "shadingMode");
            GL.Uniform1(modeLoc, shadingMode);

            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, triangleCount * 3, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();

            if (config.Render.SaveBmp && !bmpSaved)
            {
                bmpSaved = true;
                TrySaveBmp(config.Render.BmpPath);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            var input = KeyboardState;

            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
                Close();

            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up))
                rotationX += 0.5f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down))
                rotationX -= 0.5f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left))
                rotationY -= 0.5f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right))
                rotationY += 0.5f;

            if (input.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D1)) shadingMode = 0;
            if (input.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D2)) shadingMode = 1;
            if (input.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D3)) shadingMode = 2;
        }

        private void TrySaveBmp(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                int w = Size.X;
                int h = Size.Y;

                byte[] pixels = new byte[w * h * 4];

                GL.ReadPixels(0, 0, w, h,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    pixels);

                using (var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var rect = new Rectangle(0, 0, w, h);
                    var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                    try
                    {
                        int stride = data.Stride;

                        for (int y = 0; y < h; y++)
                        {
                            int srcY = h - 1 - y;
                            int srcOffset = srcY * w * 4;

                            IntPtr dstRow = IntPtr.Add(data.Scan0, y * stride);
                            Marshal.Copy(pixels, srcOffset, dstRow, w * 4);
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(data);
                    }

                    bmp.Save(path, ImageFormat.Bmp);
                }

                Console.WriteLine("BMP сохранён: " + path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось сохранить BMP: " + ex.Message);
            }
        }

        private void InitializeBuffers(List<Vertex> vertices, List<Triangle> triangles)
        {
            Vector3[] normals = CalculateNormals(triangles);

            float[] vertexData = new float[vertices.Count * 6];
            for (int i = 0; i < vertices.Count; i++)
            {
                vertexData[i * 6] = vertices[i].X;
                vertexData[i * 6 + 1] = vertices[i].Y;
                vertexData[i * 6 + 2] = vertices[i].Z;
                vertexData[i * 6 + 3] = normals[i].X;
                vertexData[i * 6 + 4] = normals[i].Y;
                vertexData[i * 6 + 5] = normals[i].Z;
            }

            int[] indexData = new int[triangles.Count * 3];
            for (int i = 0; i < triangles.Count; i++)
            {
                indexData[i * 3] = triangles[i].V1;
                indexData[i * 3 + 1] = triangles[i].V2;
                indexData[i * 3 + 2] = triangles[i].V3;
            }

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexData.Length * sizeof(int), indexData, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        private Vector3[] CalculateNormals(List<Triangle> triangles)
        {
            Vector3[] normals = new Vector3[vertices.Count];

            foreach (var triangle in triangles)
            {
                Vector3 v0 = new Vector3(vertices[triangle.V1].X, vertices[triangle.V1].Y, vertices[triangle.V1].Z);
                Vector3 v1 = new Vector3(vertices[triangle.V2].X, vertices[triangle.V2].Y, vertices[triangle.V2].Z);
                Vector3 v2 = new Vector3(vertices[triangle.V3].X, vertices[triangle.V3].Y, vertices[triangle.V3].Z);

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;

                Vector3 n = Vector3.Cross(edge1, edge2);
                if (n.LengthSquared > 1e-12f) n = n.Normalized();

                normals[triangle.V1] += n;
                normals[triangle.V2] += n;
                normals[triangle.V3] += n;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                if (normals[i].LengthSquared > 1e-12f)
                    normals[i] = normals[i].Normalized();
            }

            return normals;
        }

        private int CreateShaderProgram()
        {
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec3 aPosition;
                layout (location = 1) in vec3 aNormal;

                uniform mat4 model;
                uniform mat4 view;
                uniform mat4 projection;
                uniform mat4 normalMatrix;

                out vec3 FragPos;
                out vec3 Normal;

                void main()
                {
                    FragPos = vec3(model * vec4(aPosition, 1.0));
                    Normal = mat3(normalMatrix) * aNormal;
                    gl_Position = projection * view * vec4(FragPos, 1.0);
                }
            ";

            string fragmentShaderSource = @"
                #version 330 core
                in vec3 FragPos;
                in vec3 Normal;

                uniform vec3 viewPos;

                uniform vec3 lightDirRays;
                uniform vec3 lightColor;
                uniform float lightIntensity;

                uniform vec3 objectColor;

                uniform float ambientStrength;
                uniform float kd;
                uniform float ks;
                uniform float shininess;
                uniform float roughnessSigma;

                uniform int shadingMode;

                out vec4 FragColor;

                float orenNayarDiffuse(vec3 N, vec3 V, vec3 L, float sigma)
                {
                    float NdotL = max(dot(N, L), 0.0);
                    float NdotV = max(dot(N, V), 0.0);
                    if (NdotL <= 0.0 || NdotV <= 0.0) return 0.0;

                    float theta_i = acos(clamp(NdotL, 0.0, 1.0));
                    float theta_r = acos(clamp(NdotV, 0.0, 1.0));

                    float alpha = max(theta_i, theta_r);
                    float beta  = min(theta_i, theta_r);

                    vec3 Lp = normalize(L - N * dot(L, N));
                    vec3 Vp = normalize(V - N * dot(V, N));
                    float cosPhi = max(dot(Lp, Vp), 0.0);

                    float sigma2 = sigma * sigma;
                    float A = 1.0 - (sigma2 / (2.0 * (sigma2 + 0.33)));
                    float B = 0.45 * sigma2 / (sigma2 + 0.09);

                    return NdotL * (A + B * cosPhi * sin(alpha) * tan(beta));
                }

                void main()
                {
                    vec3 N = normalize(Normal);
                    vec3 L = normalize(-lightDirRays);
                    vec3 V = normalize(viewPos - FragPos);

                    vec3 ambient = ambientStrength * lightColor * lightIntensity;

                    float diffLambert = max(dot(N, L), 0.0);
                    vec3 diffuseLambert = kd * diffLambert * lightColor * lightIntensity;

                    vec3 diffuse = diffuseLambert;
                    vec3 specular = vec3(0.0);

                    if (shadingMode == 0)
                    {
                        diffuse = diffuseLambert;
                    }
                    else if (shadingMode == 1)
                    {
                        vec3 H = normalize(L + V);
                        float spec = pow(max(dot(N, H), 0.0), shininess);
                        specular = ks * spec * lightColor * lightIntensity;
                        diffuse = diffuseLambert;
                    }
                    else
                    {
                        float on = orenNayarDiffuse(N, V, L, roughnessSigma);
                        diffuse = kd * on * lightColor * lightIntensity;
                    }

                    vec3 result = (ambient + diffuse + specular) * objectColor;
                    FragColor = vec4(result, 1.0);
                }
            ";

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vertexShaderSource);
            GL.CompileShader(vs);

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fragmentShaderSource);
            GL.CompileShader(fs);

            int program = GL.CreateProgram();
            GL.AttachShader(program, vs);
            GL.AttachShader(program, fs);
            GL.LinkProgram(program);

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);

            return program;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);

            projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                (float)Size.X / Size.Y,
                0.1f,
                5000.0f);
        }

        protected override void OnUnload()
        {
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
            GL.DeleteVertexArray(vao);
            GL.DeleteProgram(shaderProgram);
            base.OnUnload();
        }
    }
}
