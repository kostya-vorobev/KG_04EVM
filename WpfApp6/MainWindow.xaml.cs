using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpGL.SceneGraph;
using SharpGL;
using SharpGL.WPF;
using SharpGL.RenderContextProviders;
using SharpGL.Version;
using Assimp;
using System.Numerics;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using SharpGL.SceneGraph.Assets;

namespace WpfApp6
{
    public partial class MainWindow : Window
    {
        private OpenGL gl;
        private Model model;
        private float rotation = 0.0f;
        private uint[] texture = new uint[1];
        private float eyeX = 500.0f, eyeY = 200.0f, eyeZ = 200.0f;
        Texture tex = new Texture();
        private uint[] depthTexture = new uint[1];
        private int shadowWidth = 512;
        private int shadowHeight = 512;
        public MainWindow()
        {
            InitializeComponent();
            openGLControl.OpenGLVersion = OpenGLVersion.OpenGL4_0;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Перемещение камеры с помощью стрелок
            switch (e.Key)
            {
                case Key.Q:
                    eyeY += 10.0f; // Двигать вверх
                    gl.LookAt(eyeX, eyeY, eyeZ, 0, 0, 0, 0, 1, 0);
                    break;
                case Key.E:
                    eyeY -= 10.0f; // Двигать вниз
                    gl.LookAt(eyeX, eyeY, eyeZ, 0, 0, 0, 0, 1, 0);
                    break;
                case Key.A:
                    eyeX -= 10.0f; // Двигать влево
                    gl.LookAt(eyeX, eyeY, eyeZ, 0, 0, 0, 0, 1, 0);
                    break;
                case Key.D:
                    eyeX += 10.0f; // Двигать вправо
                    gl.LookAt(eyeX, eyeY, eyeZ, 0, 0, 0, 0, 1, 0);
                    break;
                case Key.W:
                    eyeZ -= 10.0f; // Двигать вперед
                    gl.LookAt(eyeX, eyeY, eyeZ, 0, 0, 0, 0, 1, 0);
                    break;
                case Key.S:
                    eyeZ += 10.0f; // Двигать назад
                    gl.LookAt(eyeX, eyeY, eyeZ, 0, 0, 0, 0, 1, 0);
                    break;
            }
        }

        private void SetupShadowMapping()
        {
            gl.GenTextures(1, depthTexture);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, depthTexture[0]);
            gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_DEPTH_COMPONENT24, shadowWidth, shadowHeight, 0,
                OpenGL.GL_DEPTH_COMPONENT, OpenGL.GL_UNSIGNED_BYTE, IntPtr.Zero); 
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
        }

        private void RenderShadowMap()
        {
            // Устанавливаем viewport на размер текстуры
            gl.Viewport(0, 0, shadowWidth, shadowHeight);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, depthTexture[0]);
            gl.Clear(OpenGL.GL_DEPTH_BUFFER_BIT);

            // обнуляем матрицы
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            gl.Ortho(-100, 100, -100, 100, -100, 100); // Убедитесь, что размеры соответствуют сцене

            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();
            gl.LookAt(0, 50, 50, 0, 0, 0, 0, 1, 0); // Позиция источника света

            // Отрисовка модели для текстуры глубины
            foreach (var mesh in model.Meshes)
            {
                DrawModelForShadow(mesh);
            }

            // Возвращаем параметры с экрана
            gl.Viewport(0, 0, (int)openGLControl.ActualWidth, (int)openGLControl.ActualHeight);
        }

        private void DrawModelForShadow(Mesh mesh)
        {
            gl.Begin(OpenGL.GL_TRIANGLES);
            for (int i = 0; i < mesh.Faces.Count; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    int index = mesh.Faces[i + j];
                    Vector3 vertex = mesh.Vertices[index];
                    gl.Vertex(vertex.X, vertex.Y, vertex.Z); // Рендеринг для текстуры глубины
                }
            }
            gl.End();
        }

        private void LoadModel(string filePath)
        {
            AssimpContext importer = new AssimpContext();
            var scene = importer.ImportFile(filePath, PostProcessSteps.Triangulate);

            // Инициализация модели
            model = new Model();

            // Загружаем материалы
            foreach (var material in scene.Materials)
            {
                var newMaterial = new Material
                {
                    AmbientColor = new float[] { material.ColorAmbient.R, material.ColorAmbient.G, material.ColorAmbient.B, 1.0f },
                    DiffuseColor = new float[] { material.ColorDiffuse.R, material.ColorDiffuse.G, material.ColorDiffuse.B, 1.0f },
                    SpecularColor = new float[] { material.ColorSpecular.R, material.ColorSpecular.G, material.ColorSpecular.B, 1.0f },
                    Shininess = material.Shininess
                };

                model.Materials.Add(newMaterial);
            }

            foreach (var mesh in scene.Meshes)
            {
                var newMesh = new Mesh();

                // Загружаем вершины
                foreach (var vertex in mesh.Vertices)
                {
                    var v = new Vector3((float)vertex.X, (float)vertex.Y, (float)vertex.Z);
                    newMesh.Vertices.Add(v);
                }

                // Загружаем текстурные координаты
                foreach (var texCoord in mesh.TextureCoordinateChannels[0]) // Берем первую текстурную координату
                {
                    var t = new Vector2((float)texCoord.X, (float)texCoord.Y);
                    newMesh.TextureCoordinates.Add(t);
                }

                // Загружаем индексы для отрисовки
                foreach (var face in mesh.Faces)
                {
                    foreach (var index in face.Indices)
                    {
                        newMesh.Faces.Add(index);
                    }
                }

                model.Meshes.Add(newMesh);
            }
        }

        private void LoadTexture(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Texture file not found.", filePath);

            Bitmap bmp = new Bitmap(filePath);
            tex.Create(gl, bmp);
            gl.Enable(OpenGL.GL_TEXTURE_2D);
            bmp.Dispose();
        }

        private void openGLControl_OpenGLDraw(object sender, OpenGLRoutedEventArgs args)
        {
            

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            gl.LoadIdentity();
            gl.Translate(-20f, -20f, 0.5f);
            // Позиция камеры и вращение
            gl.LookAt(eyeX, eyeY, eyeZ, 0, 0, 0, 0, 1, 0);
            gl.Rotate(rotation, 0.0f, 1.0f, 0.0f);
            rotation += 1.0f; // Динамическое вращение

            // Отрисовка модели или примитивов
            if (model.Meshes.Count > 0)
            {
                DrawModel();
            }
            else
            {
                DrawPrimitives(); // Если модель не загружена, рисуем примитивы
            }
        }
        
        private void DrawModel()
        {
            int iTex = 0;

            tex.Bind(gl);

            foreach (var mesh in model.Meshes)
            {
                if (iTex < 6)
                LoadTexture("J_Room/"+iTex.ToString()+".jpg");
                tex.Bind(gl);
                iTex++;
                Material material = model.Materials[mesh.MaterialIndex];

                // Устанавливаем материал
                gl.Material(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT, material.AmbientColor);
                gl.Material(OpenGL.GL_FRONT, OpenGL.GL_DIFFUSE, material.DiffuseColor);
                gl.Material(OpenGL.GL_FRONT, OpenGL.GL_SPECULAR, material.SpecularColor);
                gl.Material(OpenGL.GL_FRONT, OpenGL.GL_SHININESS, new float[] { material.Shininess });

                gl.Begin(OpenGL.GL_TRIANGLES);
                for (int i = 0; i < mesh.Faces.Count; i += 3)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = mesh.Faces[i + j];  // Индекс текущей вершины
                        Vector3 vertex = mesh.Vertices[index];  // Получаем вершину

                        // Устанавливаем текстурные координаты, если они есть
                        if (index < mesh.TextureCoordinates.Count)
                        {
                            Vector2 texCoord = mesh.TextureCoordinates[index];
                            gl.TexCoord(texCoord.X, texCoord.Y);
                        }
                        else
                        {
                            gl.TexCoord(0.0f, 0.0f);
                        }

                        // Устанавливаем вершину
                        gl.Vertex(vertex.X, vertex.Y, vertex.Z);
                    }
                }
                gl.End();
            }

        }

        private void openGLControl_OpenGLInitialized(object sender, OpenGLRoutedEventArgs args)
        {
            gl = openGLControl.OpenGL;
            gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            gl.Enable(OpenGL.GL_LIGHTING);
            gl.Enable(OpenGL.GL_LIGHT0);
            gl.Enable(OpenGL.GL_LIGHT1);
            gl.Enable(OpenGL.GL_COLOR_MATERIAL);
            //gl.Disable(OpenGL.GL_LIGHTING);

            LoadModel("J_Room.obj");
            
            SetupShadowMapping(); 
            SetupLighting();
            RenderShadowMap();
        }

        private void SetupLighting()
        {
            // Параметры первого источника света (основного)
            float[] light0Position = new float[4] { 0.0f, 50.0f, 100.0f, 1.0f }; // Позиция источника света
            float[] light0Ambient = new float[4] { 0.2f, 0.2f, 0.2f, 1.0f }; // Параметры окружающего света
            float[] light0Diffuse = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f }; // Параметры рассеянного света
            float[] light0Specular = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f }; // Параметры зеркального света

            // Параметры второго источника света (акцентный)
            float[] light1Position = new float[4] { 50.0f, 100.0f, 50.0f, 1.0f }; // Позиция источника света
            float[] light1Ambient = new float[4] { 0.1f, 0.1f, 0.1f, 1.0f }; // Параметры окружающего света
            float[] light1Diffuse = new float[4] { 0.8f, 0.8f, 0.8f, 1.0f }; // Параметры рассеянного света
            float[] light1Specular = new float[4] { 0.8f, 0.8f, 0.8f, 1.0f }; // Параметры зеркального света

            gl.ClearColor(0.0f, 0.2f, 0.2f, 0.0f);
            gl.ClearDepth(1f);
            gl.DepthFunc(OpenGL.GL_LEQUAL);
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.ShadeModel(OpenGL.GL_SMOOTH);

            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPECULAR, light0Specular);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, light0Ambient);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, light0Diffuse);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, light0Position);

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPECULAR, light1Specular);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_AMBIENT, light1Ambient);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_DIFFUSE, light1Diffuse);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_POSITION, light1Position);
        }

        private void DrawPrimitives()
        {
            tex.Bind(gl);

            gl.Begin(OpenGL.GL_QUADS);

            // Передняя сторона
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, 1.0f); // Левая нижняя
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(1.0f, -1.0f, 1.0f);  // Правая нижняя
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(1.0f, 1.0f, 1.0f);   // Правая верхняя
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, 1.0f);  // Левая верхняя

            // Задняя сторона
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, -1.0f); // Левая нижняя
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(1.0f, -1.0f, -1.0f);  // Правая нижняя
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(1.0f, 1.0f, -1.0f);   // Правая верхняя
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, -1.0f);  // Левая верхняя

            // Левая сторона
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, -1.0f); // Левая нижняя
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, 1.0f);  // Правая нижняя
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, 1.0f);   // Правая верхняя
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, -1.0f);  // Левая верхняя

            // Правая сторона
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(1.0f, -1.0f, -1.0f); // Левая нижняя
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(1.0f, 1.0f, -1.0f);  // Правая нижняя
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(1.0f, 1.0f, 1.0f);   // Правая верхняя
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(1.0f, -1.0f, 1.0f);  // Левая верхняя

            // Верхняя сторона
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(-1.0f, 1.0f, -1.0f); // Левая нижняя
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(1.0f, 1.0f, -1.0f);  // Правая нижняя
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(1.0f, 1.0f, 1.0f);   // Правая верхняя
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(-1.0f, 1.0f, 1.0f);  // Левая верхняя

            // Нижняя сторона
            gl.TexCoord(0.0f, 0.0f); gl.Vertex(-1.0f, -1.0f, -1.0f); // Левая нижняя
            gl.TexCoord(1.0f, 0.0f); gl.Vertex(1.0f, -1.0f, -1.0f);  // Правая нижняя
            gl.TexCoord(1.0f, 1.0f); gl.Vertex(1.0f, -1.0f, 1.0f);   // Правая верхняя
            gl.TexCoord(0.0f, 1.0f); gl.Vertex(-1.0f, -1.0f, 1.0f);  // Левая верхняя

            gl.End();
        }

        private void openGLControl_Resized(object sender, OpenGLRoutedEventArgs args)
        {
            gl.Viewport(0, 0, (int)openGLControl.ActualWidth, (int)openGLControl.ActualHeight);
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();

            // Увеличение дальности видимости
            gl.Perspective(45.0f, (double)openGLControl.ActualWidth / openGLControl.ActualHeight, 0.1f, 1000.0f);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }
    }
    public class Mesh
    {
        public List<Vector3> Vertices { get; set; } = new List<Vector3>();
        public List<Vector2> TextureCoordinates { get; set; } = new List<Vector2>();
        public List<int> Faces { get; set; } = new List<int>(); // Индексы вершин для отрисовки
        public int MaterialIndex { get; set; } // Индекс материала для этого меша
    }
    public class Model
    {
        public List<Material> Materials { get; set; } = new List<Material>(); // Хранение материалов
        public List<Mesh> Meshes { get; set; } = new List<Mesh>();
    }
    public class Material
    {
        public float[] AmbientColor { get; set; }
        public float[] DiffuseColor { get; set; }
        public float[] SpecularColor { get; set; }
        public float Shininess { get; set; }
        public uint TextureId { get; set; }

        public Material()
        {
            AmbientColor = new float[4] { 0.2f, 0.2f, 0.2f, 1.0f };
            DiffuseColor = new float[4] { 0.8f, 0.8f, 0.8f, 1.0f };
            SpecularColor = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
            Shininess = 32.0f;
        }
    }


}
