using System;
using System.Collections.Generic;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace TeapotShow
{
    class Program
    {
        static void Main()
        {
            var nativeWinSettings = new NativeWindowSettings
            {
                ClientSize = new Vector2i(1280, 720),
                Title = "Simple Teapot (OpenTK, Linux)"
            };

            using var win = new TeapotWindow(GameWindowSettings.Default, nativeWinSettings);
            win.Run();
        }
    }

    public class TeapotWindow : GameWindow
    {
        int shader;
        int vao, vbo, ebo;
        int vertexCount, elementCount;
        Matrix4 proj;
        float angle = 0f;

        int uniModel, uniView, uniProj, uniLightPos, uniViewPos, uniColor;

        public TeapotWindow(GameWindowSettings gws, NativeWindowSettings nws)
            : base(gws, nws) { }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.Enable(EnableCap.DepthTest);

            shader = CreateProgram(vertexShaderSource, fragmentShaderSource);
            GL.UseProgram(shader);

            var mesh = BuildTeapotLikeMesh();

            vertexCount = mesh.Vertices.Count;
            elementCount = mesh.Indices.Count;

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, mesh.Vertices.Count * Vertex.SizeInBytes, mesh.Vertices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indices.Count * sizeof(int), mesh.Indices.ToArray(), BufferUsageHint.StaticDraw);

            int posLoc = GL.GetAttribLocation(shader, "aPosition");
            int nrmLoc = GL.GetAttribLocation(shader, "aNormal");

            GL.EnableVertexAttribArray(posLoc);
            GL.VertexAttribPointer(posLoc, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 0);

            GL.EnableVertexAttribArray(nrmLoc);
            GL.VertexAttribPointer(nrmLoc, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 3 * sizeof(float));

            GL.BindVertexArray(0);

            uniModel = GL.GetUniformLocation(shader, "uModel");
            uniView = GL.GetUniformLocation(shader, "uView");
            uniProj = GL.GetUniformLocation(shader, "uProjection");
            uniLightPos = GL.GetUniformLocation(shader, "uLightPos");
            uniViewPos = GL.GetUniformLocation(shader, "uViewPos");
            uniColor = GL.GetUniformLocation(shader, "uColor");

            proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f),
                Size.X / (float)Size.Y, 0.1f, 100f);

            GL.ClearColor(0.15f, 0.15f, 0.18f, 1.0f);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f),
                Size.X / (float)Size.Y, 0.1f, 100f);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            angle += 20f * (float)args.Time;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shader);
            GL.BindVertexArray(vao);

            Vector3 camPos = new Vector3(0, 1.2f, 4.0f);
            Matrix4 view = Matrix4.LookAt(camPos, new Vector3(0, 0.6f, 0), Vector3.UnitY);

            GL.UniformMatrix4(uniView, false, ref view);
            GL.UniformMatrix4(uniProj, false, ref proj);
            GL.Uniform3(uniLightPos, new Vector3(4, 4, 4));
            GL.Uniform3(uniViewPos, camPos);

            Matrix4 model = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(angle));
            GL.UniformMatrix4(uniModel, false, ref model);

            GL.Uniform3(uniColor, new Vector3(0.9f, 0.7f, 0.3f));
            GL.DrawElements(PrimitiveType.Triangles, elementCount, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
            GL.DeleteVertexArray(vao);
            GL.DeleteProgram(shader);
        }

        // ------------------- Geometry helpers -------------------
        struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public static int SizeInBytes => (3 + 3) * sizeof(float);
        }

        class Mesh
        {
            public List<Vertex> Vertices = new();
            public List<int> Indices = new();
        }

        void AppendMesh(Mesh dest, Mesh src, Matrix4 transform)
        {
            int baseIndex = dest.Vertices.Count;
            var normalMatrix = new Matrix3(transform);
normalMatrix = normalMatrix.Inverted().Transposed();

// Convert to 4x4 so TransformNormal() works in OpenTK 5.x
var normalMatrix4 = new Matrix4(
    normalMatrix.Row0.X, normalMatrix.Row0.Y, normalMatrix.Row0.Z, 0,
    normalMatrix.Row1.X, normalMatrix.Row1.Y, normalMatrix.Row1.Z, 0,
    normalMatrix.Row2.X, normalMatrix.Row2.Y, normalMatrix.Row2.Z, 0,
    0, 0, 0, 1
);

foreach (var v in src.Vertices)
{
    var p = Vector3.TransformPosition(v.Position, transform);
    var n = Vector3.TransformNormal(v.Normal, normalMatrix4);
    n.Normalize();
    dest.Vertices.Add(new Vertex { Position = p, Normal = n });
}


            foreach (var idx in src.Indices)
                dest.Indices.Add(baseIndex + idx);
        }

        Mesh BuildTeapotLikeMesh()
        {
            var M = new Mesh();
            var body = CreateSphere(0.9f, 28, 28);
            AppendMesh(M, body, Matrix4.CreateTranslation(0, 0.55f, 0));

            var lid = CreateSphere(0.42f, 18, 18);
            AppendMesh(M, lid, Matrix4.CreateScale(1f, 0.5f, 1f) * Matrix4.CreateTranslation(0, 1.02f, 0));

            var spout = CreateCylinder(0.12f, 0.9f, 24);
            var spoutTransform =
                Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(20f)) *
                Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-10f)) *
                Matrix4.CreateTranslation(0.95f, 0.9f, 0f);
            AppendMesh(M, spout, spoutTransform);

            var handle = CreateTorus(0.45f, 0.08f, 30, 18);
            var handleTransform =
                Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90f)) *
                Matrix4.CreateScale(1f, 1.2f, 1f) *
                Matrix4.CreateTranslation(-0.85f, 0.75f, 0f);
            AppendMesh(M, handle, handleTransform);

            var knob = CreateSphere(0.08f, 12, 12);
            AppendMesh(M, knob, Matrix4.CreateTranslation(0, 1.12f, 0));

            return M;
        }

        Mesh CreateSphere(float radius, int stacks, int slices)
        {
            var m = new Mesh();
            for (int i = 0; i <= stacks; i++)
            {
                double phi = Math.PI * i / stacks;
                double y = Math.Cos(phi);
                double r = Math.Sin(phi);
                for (int j = 0; j <= slices; j++)
                {
                    double theta = 2.0 * Math.PI * j / slices;
                    double x = r * Math.Cos(theta);
                    double z = r * Math.Sin(theta);
                    var pos = new Vector3((float)x, (float)y, (float)z) * radius;
                    var n = Vector3.Normalize(new Vector3((float)x, (float)y, (float)z));
                    m.Vertices.Add(new Vertex { Position = pos, Normal = n });
                }
            }

            for (int i = 0; i < stacks; i++)
                for (int j = 0; j < slices; j++)
                {
                    int a = i * (slices + 1) + j;
                    int b = a + slices + 1;
                    m.Indices.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }

            return m;
        }

        Mesh CreateCylinder(float radius, float height, int segments)
        {
            var m = new Mesh();
            float half = height / 2f;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float ang = t * MathF.PI * 2f;
                float x = MathF.Cos(ang);
                float z = MathF.Sin(ang);
                Vector3 n = new Vector3(x, 0, z);
                m.Vertices.Add(new Vertex { Position = new Vector3(x * radius, -half, z * radius), Normal = n });
                m.Vertices.Add(new Vertex { Position = new Vector3(x * radius, half, z * radius), Normal = n });
            }

            for (int i = 0; i < segments; i++)
            {
                int idx = i * 2;
                m.Indices.AddRange(new[] {
                    idx, idx + 1, idx + 2,
                    idx + 1, idx + 3, idx + 2
                });
            }

            return m;
        }

        Mesh CreateTorus(float R, float r, int segments, int sides)
        {
            var m = new Mesh();
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments * MathF.PI * 2f;
                for (int j = 0; j <= sides; j++)
                {
                    float v = j / (float)sides * MathF.PI * 2f;
                    float x = (R + r * MathF.Cos(v)) * MathF.Cos(u);
                    float y = r * MathF.Sin(v);
                    float z = (R + r * MathF.Cos(v)) * MathF.Sin(u);
                    Vector3 n = new Vector3(MathF.Cos(u) * MathF.Cos(v), MathF.Sin(v), MathF.Sin(u) * MathF.Cos(v));
                    m.Vertices.Add(new Vertex { Position = new Vector3(x, y, z), Normal = n.Normalized() });
                }
            }

            int stride = sides + 1;
            for (int i = 0; i < segments; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = i * stride + j;
                    int b = (i + 1) * stride + j;
                    m.Indices.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }

            return m;
        }

        int CreateProgram(string vsrc, string fsrc)
        {
            int v = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(v, vsrc);
            GL.CompileShader(v);
            GL.GetShader(v, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0) throw new Exception(GL.GetShaderInfoLog(v));

            int f = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(f, fsrc);
            GL.CompileShader(f);
            GL.GetShader(f, ShaderParameter.CompileStatus, out ok);
            if (ok == 0) throw new Exception(GL.GetShaderInfoLog(f));

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, v);
            GL.AttachShader(prog, f);
            GL.LinkProgram(prog);
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out ok);
            if (ok == 0) throw new Exception(GL.GetProgramInfoLog(prog));

            GL.DeleteShader(v);
            GL.DeleteShader(f);
            return prog;
        }

        readonly string vertexShaderSource = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
uniform mat4 uModel, uView, uProjection;
out vec3 vNormal;
out vec3 vFragPos;
void main() {
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vFragPos = worldPos.xyz;
    vNormal = mat3(transpose(inverse(uModel))) * aNormal;
    gl_Position = uProjection * uView * worldPos;
}";

        readonly string fragmentShaderSource = @"
#version 330 core
in vec3 vNormal;
in vec3 vFragPos;
out vec4 FragColor;
uniform vec3 uLightPos, uViewPos, uColor;
void main() {
    vec3 norm = normalize(vNormal);
    vec3 lightDir = normalize(uLightPos - vFragPos);
    vec3 viewDir = normalize(uViewPos - vFragPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float diff = max(dot(norm, lightDir), 0.0);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
    vec3 ambient = 0.12 * uColor;
    vec3 diffuse = diff * uColor;
    vec3 specular = vec3(0.7) * spec;
    FragColor = vec4(ambient + diffuse + specular, 1.0);
}";
    }
}
