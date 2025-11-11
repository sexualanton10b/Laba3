using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Laba3
{
    // Класс для визуализации 3D модели с использованием OpenGL
    public class DepthMapViewer : GameWindow
    {
        // OpenGL буферы
        private int vao;
        private int vbo;
        private int ebo;
        private int shaderProgram;
        private int triangleCount;

        // Матрицы трансформации
        private Matrix4 projection;
        private Matrix4 view;
        private Matrix4 model;

        // Параметры камеры
        private float rotationX = 180.0f;
        private float rotationY = 0.0f;
        private float zoom = -500.0f;

        // Список вершин для вычисления нормалей
        private List<Vertex> vertices;

        // Конструктор визуализатора
        public DepthMapViewer(List<Vertex> vertices, List<Triangle> triangles)
            : base(GameWindowSettings.Default,
                   new NativeWindowSettings
                   {
                       ClientSize = (1200, 800),
                       Title = "Depth Map Viewer"
                   })
        {
            Console.WriteLine("Инициализирую визуализатор...");
            this.vertices = vertices;
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

        // Вызывается при загрузке окна
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
                Console.WriteLine("Освещение включено - источник света движется с камерой");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка OpenGL: " + ex.Message);
                throw;
            }
        }

        // Вызывается каждый кадр для рендеринга
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(shaderProgram);

            // Обновляем матрицу вида в зависимости от позиции камеры
            view = Matrix4.LookAt(
                new Vector3(0, 0, zoom),
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0));

            // Создаём матрицу модели с ротациями
            model = Matrix4.Identity;
            model *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotationX));
            model *= Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationY));

            // Получаем расположение переменных uniform в шейдере
            int modelLoc = GL.GetUniformLocation(shaderProgram, "model");
            int viewLoc = GL.GetUniformLocation(shaderProgram, "view");
            int projLoc = GL.GetUniformLocation(shaderProgram, "projection");
            int normalMatLoc = GL.GetUniformLocation(shaderProgram, "normalMatrix");

            // Передаём матрицы в шейдер
            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);

            // Вычисляем матрицу нормалей для правильного освещения при трансформациях
            var normalMatrix = Matrix4.Transpose(Matrix4.Invert(model));
            GL.UniformMatrix4(normalMatLoc, false, ref normalMatrix);

            // Позиция источника света (движется с камерой)
            Vector3 lightPos = new Vector3(0, 0, zoom);
            int lightPosLoc = GL.GetUniformLocation(shaderProgram, "lightPos");
            GL.Uniform3(lightPosLoc, lightPos);
            // Позиция камеры (для specular компонента освещения)
            Vector3 viewPos = new Vector3(0, 0, zoom);
            int viewPosLoc = GL.GetUniformLocation(shaderProgram, "viewPos");
            GL.Uniform3(viewPosLoc, viewPos);
            // Рисуем модель
            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, triangleCount * 3, DrawElementsType.UnsignedInt, 0);
            // Показываем результат
            SwapBuffers();
        }
        
        // Вызывается каждый кадр для обновления логики
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            // Получаем статус клавиш
            var input = KeyboardState;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
                Close(); // Закрыть окно при нажатии ESC

            // Управление поворотом стрелками (чувствительность 0.5 градуса за кадр)
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up))
                rotationX += 0.5f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down))
                rotationX -= 0.5f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left))
                rotationY -= 0.5f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right))
                rotationY += 0.5f;
            // Управление зумом клавишами W/S
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W))
                zoom += 1.0f;
            if (input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S))
                zoom -= 1.0f;
        }
        // Инициализируем OpenGL буферы
        private void InitializeBuffers(List<Vertex> vertices, List<Triangle> triangles)
        {   
            // Вычисляем нормали для каждой вершины
            Vector3[] normals = CalculateNormals(triangles);
             // Создаём буфер вершин (позиция 3 float + нормаль 3 float = 6 float на вершину)
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
            // Создаём буфер индексов (какие вершины составляют каждый треугольник)
            int[] indexData = new int[triangles.Count * 3];
            for (int i = 0; i < triangles.Count; i++)
            {
                indexData[i * 3] = triangles[i].V1;
                indexData[i * 3 + 1] = triangles[i].V2;
                indexData[i * 3 + 2] = triangles[i].V3;
            }
            // Создаём Vertex Array Object (хранит информацию о структуре буферов)
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            // Создаём Vertex Buffer Object (содержит вершины и нормали)
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float),
                          vertexData, BufferUsageHint.StaticDraw);
            // Создаём Element Buffer Object (содержит индексы треугольников)
            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexData.Length * sizeof(int),
                          indexData, BufferUsageHint.StaticDraw);
            // Описываем формат позиций вершин в шейдере (location = 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
                                   6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // Описываем формат нормалей в шейдере (location = 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false,
                                   6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }
        // Вычисляем нормали для каждой вершины
        private Vector3[] CalculateNormals(List<Triangle> triangles)
        {
            Vector3[] normals = new Vector3[vertices.Count];
            // Для каждого треугольника вычисляем его нормаль
            foreach (var triangle in triangles)
            {
                // Получаем позиции трёх вершин треугольника
                Vector3 v0 = new Vector3(vertices[triangle.V1].X, vertices[triangle.V1].Y, vertices[triangle.V1].Z);
                Vector3 v1 = new Vector3(vertices[triangle.V2].X, vertices[triangle.V2].Y, vertices[triangle.V2].Z);
                Vector3 v2 = new Vector3(vertices[triangle.V3].X, vertices[triangle.V3].Y, vertices[triangle.V3].Z);
                // Вычисляем два края треугольника
                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                // Нормаль = векторное произведение (перпендикулярно к поверхности)
                Vector3 triangleNormal = Vector3.Cross(edge1, edge2).Normalized();
                // Добавляем нормаль к каждой вершине треугольника
                normals[triangle.V1] += triangleNormal;
                normals[triangle.V2] += triangleNormal;
                normals[triangle.V3] += triangleNormal;
            }
            // Нормализуем нормали вершин (усредняем нормали всех треугольников)
            for (int i = 0; i < normals.Length; i++)
            {
                if (normals[i].Length > 0)
                    normals[i] = normals[i].Normalized();
            }

            return normals;
        }

        // Создаём и компилируем шейдеры
        private int CreateShaderProgram()
        {
            // Вершинный шейдер (обрабатывает позиции и нормали вершин)
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

            // Фрагментный шейдер (обрабатывает цвет пикселя с учетом освещения Phong)
            string fragmentShaderSource = @"
                #version 330 core
                in vec3 FragPos;
                in vec3 Normal;

                uniform vec3 lightPos;
                uniform vec3 viewPos;

                out vec4 FragColor;

                void main()
                {
                    vec3 objectColor = vec3(0.6, 0.7, 0.9);
                    vec3 lightColor = vec3(1.0, 1.0, 1.0);

                    float ambientStrength = 0.3;
                    vec3 ambient = ambientStrength * lightColor;

                    vec3 norm = normalize(Normal);
                    vec3 lightDir = normalize(lightPos - FragPos);
                    float diff = max(dot(norm, lightDir), 0.0);
                    vec3 diffuse = diff * lightColor;

                    float specularStrength = 0.5;
                    vec3 viewDir = normalize(viewPos - FragPos);
                    vec3 reflectDir = reflect(-lightDir, norm);
                    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
                    vec3 specular = specularStrength * spec * lightColor;

                    vec3 result = (ambient + diffuse + specular) * objectColor;
                    FragColor = vec4(result, 1.0);
                }
            ";
            // Компилируем вершинный шейдер
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            // Компилируем фрагментный шейдер
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            // Связываем оба шейдера в одну программу
            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            // Удаляем отдельные шейдеры (они уже в программе)
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return program;
        }

        // Вызывается при изменении размера окна
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

        // Вызывается при закрытии окна
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
