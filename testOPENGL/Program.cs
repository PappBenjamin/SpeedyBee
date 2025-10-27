using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

class StickWindow : GameWindow
{
    private List<(Vector3 accel, Vector3 rot)> _frames = new();
    private int _currentFrame = 0;
    private float _time = 0f;
    private float _frameDuration = 0.05f;

    private int _shader;
    private int _vao;
    private int _vbo;
    private int _uniMvp;

    private float[] _stickVertices = new float[]
    {
        0f, -0.5f, 0f,
        0f,  0.5f, 0f
    };

    public StickWindow(GameWindowSettings gws, NativeWindowSettings nws)
        : base(gws, nws) { }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        LoadCSV("motion.csv");
        SetupGL();
    }

    void LoadCSV(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Missing {path}");
            Close();
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 6) continue;
            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float ax) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float ay) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float az) &&
                float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float rx) &&
                float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float ry) &&
                float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float rz))
            {
                _frames.Add((new Vector3(ax, ay, az), new Vector3(rx, ry, rz)));
            }
        }

        Console.WriteLine($"Loaded {_frames.Count} frames.");
    }

    void SetupGL()
    {
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * _stickVertices.Length, _stickVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.BindVertexArray(0);

        _shader = CreateProgram(vertexShaderSrc, fragmentShaderSrc);
        _uniMvp = GL.GetUniformLocation(_shader, "uMVP");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_frames.Count == 0)
        {
            SwapBuffers();
            return;
        }

        _time += (float)args.Time;
        if (_time > _frameDuration)
        {
            _time = 0f;
            _currentFrame = (_currentFrame + 1) % _frames.Count;
        }

        var (accel, rot) = _frames[_currentFrame];

        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), Size.X / (float)Size.Y, 0.1f, 100f);
        Matrix4 view = Matrix4.LookAt(new Vector3(0, 0, 5f), Vector3.Zero, Vector3.UnitY);

        Matrix4 model =
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rot.X)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rot.Y)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rot.Z)) *
            Matrix4.CreateTranslation(accel);

        Matrix4 mvp = model * view * projection;

        GL.UseProgram(_shader);
        GL.UniformMatrix4(_uniMvp, false, ref mvp);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, 2);
        GL.BindVertexArray(0);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_shader);
    }

    int CreateProgram(string vert, string frag)
    {
        int v = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(v, vert);
        GL.CompileShader(v);
        GL.GetShader(v, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0) throw new Exception(GL.GetShaderInfoLog(v));

        int f = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(f, frag);
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

    const string vertexShaderSrc = @"
#version 330 core
layout(location = 0) in vec3 aPos;
uniform mat4 uMVP;
void main() { gl_Position = uMVP * vec4(aPos,1.0); }
";

    const string fragmentShaderSrc = @"
#version 330 core
out vec4 FragColor;
void main() { FragColor = vec4(1.0, 0.9, 0.2, 1.0); }
";
}

class Program
{
    static void Main()
    {
        var gws = GameWindowSettings.Default;
        var nws = new NativeWindowSettings { ClientSize = new Vector2i(800, 600), Title = "Stick Visualizer" };
        using var win = new StickWindow(gws, nws);
        win.Run();
    }
}
