using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

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

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;
        private float zoom = -50.0f;

        public DepthMapViewer(List<Vertex> vertices, List<Triangle> triangles)
            : base(GameWindowSettings.Default,
                   new NativeWindowSettings
                   {
                       ClientSize = (1200, 800),
                       Title = "Depth Map Viewer"
                   })
        {
            Console.WriteLine("Инициализирую визуализатор...");
            triangleCount = triangles.Count;
            
            try
            {
                InitializeBuffers(vertices, triangles);
                Console.WriteLine("Буферы инициализированы");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка инициализации: " + ex.Message);
                throw;
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            Console.WriteLine("OpenGL окно загружено");

            try
            {
                GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
                GL.Enable(EnableCap.DepthTest);

                shaderProgram = CreateShaderProgram();

                projection = Matrix4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(45.0f),
                    (float)Size.X / Size.Y,
                    0.1f,
                    1000.0f);

                view = Matrix4.LookAt(
                    new Vector3(0, 0, 30),
                    new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0));

                model = Matrix4.Identity;

                Console.WriteLine("Шейдеры скомпилированы");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка OpenGL: " + ex.Message);
                throw;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(shaderProgram);

            view = Matrix4.LookAt(
                new Vector3(0, 0, zoom),
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0));

            model = Matrix4.Identity;
            model *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotationX));
            model *= Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationY));

            int modelLoc = GL.GetUniformLocation(shaderProgram, "model");
            int viewLoc = GL.GetUniformLocation(shaderProgram, "view");
            int projLoc = GL.GetUniformLocation(shaderProgram, "projection");

            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);

            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, triangleCount * 3, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            var input = KeyboardState;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
                Close();

            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up))
                rotationX += 2.0f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down))
                rotationX -= 2.0f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left))
                rotationY -= 2.0f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right))
                rotationY += 2.0f;

            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W))
                zoom += 1.0f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S))
                zoom -= 1.0f;
        }

        private void InitializeBuffers(List<Vertex> vertices, List<Triangle> triangles)
        {
            float[] vertexData = new float[vertices.Count * 3];
            for (int i = 0; i < vertices.Count; i++)
            {
                vertexData[i * 3] = vertices[i].X;
                vertexData[i * 3 + 1] = vertices[i].Y;
                vertexData[i * 3 + 2] = vertices[i].Z;
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
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float),
                          vertexData, BufferUsageHint.StaticDraw);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexData.Length * sizeof(int),
                          indexData, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
                                   3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);
        }

        private int CreateShaderProgram()
        {
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec3 aPosition;

                uniform mat4 model;
                uniform mat4 view;
                uniform mat4 projection;

                void main()
                {
                    gl_Position = projection * view * model * vec4(aPosition, 1.0);
                }
            ";

            string fragmentShaderSource = @"
                #version 330 core
                out vec4 FragColor;

                void main()
                {
                    FragColor = vec4(0.7, 0.7, 0.9, 1.0);
                }
            ";

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

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
                1000.0f);
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
