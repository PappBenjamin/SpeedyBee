using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using OpenTK.Mathematics;

namespace SpeedyBee.Pages
{
    public partial class VisualizationPage : Page
    {
        private List<MotionFrame> _frames = new();
        private int _currentFrame = 0;
        private DispatcherTimer _timer;
        private Transform3DGroup _robotTransform;
        private bool _isPlaying = false;
        private Point3D _modelCenter;

        public VisualizationPage()
        {
            InitializeComponent();
            InitializeVisualization();
            LoadMotionData();
        }

        private void InitializeVisualization()
        {
            // Load robot 3D model from OBJ file
            LoadRobotModel();

            // Initialize transform group (shared by the robot model)
            _robotTransform = new Transform3DGroup();

            // Apply initial transform
            ApplyBaseTransform();

            bodyModel.Transform = _robotTransform;
            headModel.Transform = _robotTransform;

            // Initialize timer for animation
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 20 FPS
            };
            _timer.Tick += Timer_Tick;
        }

        private void ApplyBaseTransform()
        {
            _robotTransform.Children.Clear();

            // Step 1: Translate to origin (center the model at 0,0,0)
            _robotTransform.Children.Add(new TranslateTransform3D(
                -_modelCenter.X,
                -_modelCenter.Y,
                -_modelCenter.Z
            ));

            // Step 2: Rotate to lay flat (rotate -90 degrees around X axis)
            _robotTransform.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)
            ));

            // Step 3: Rotate to face forward (rotate -90 degrees around Z axis)
            _robotTransform.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90)
            ));

            // Step 4: Scale down the robot
            _robotTransform.Children.Add(new ScaleTransform3D(0.1, 0.1, 0.1));
        }

        private void LoadRobotModel()
        {
            try
            {
                string objPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robot.obj");

                if (!File.Exists(objPath))
                {
                    MessageBox.Show($"Robot model file not found at: {objPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var mesh = LoadObjMesh(objPath);
                bodyMesh.Positions = mesh.Positions;
                bodyMesh.TriangleIndices = mesh.TriangleIndices;

                // Calculate the center of the model
                _modelCenter = CalculateModelCenter(mesh.Positions);

                // Clear head mesh since we're using a single model
                headMesh.Positions.Clear();
                headMesh.TriangleIndices.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading robot model: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Point3D CalculateModelCenter(Point3DCollection positions)
        {
            if (positions.Count == 0)
                return new Point3D(0, 0, 0);

            double sumX = 0, sumY = 0, sumZ = 0;
            foreach (var point in positions)
            {
                sumX += point.X;
                sumY += point.Y;
                sumZ += point.Z;
            }

            return new Point3D(
                sumX / positions.Count,
                sumY / positions.Count,
                sumZ / positions.Count
            );
        }

        private MeshGeometry3D LoadObjMesh(string filePath)
        {
            var mesh = new MeshGeometry3D();
            var vertices = new List<Point3D>();
            var indices = new List<int>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                if (parts[0] == "v" && parts.Length >= 4)
                {
                    // Vertex: v x y z
                    if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                        double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                    {
                        vertices.Add(new Point3D(x, y, z));
                    }
                }
                else if (parts[0] == "f" && parts.Length >= 4)
                {
                    // Face: f v1 v2 v3 ... (assuming triangles, take first 3)
                    var faceIndices = new List<int>();
                    for (int i = 1; i < parts.Length && faceIndices.Count < 3; i++)
                    {
                        var vertexPart = parts[i].Split('/')[0]; // Ignore texture/normal indices
                        if (int.TryParse(vertexPart, out int idx))
                        {
                            faceIndices.Add(idx - 1); // OBJ is 1-based
                        }
                    }
                    if (faceIndices.Count == 3)
                    {
                        indices.AddRange(faceIndices);
                    }
                }
            }

            mesh.Positions = new Point3DCollection(vertices);
            mesh.TriangleIndices = new Int32Collection(indices);
            return mesh;
        }

        private void LoadMotionData()
        {
            try
            {
                string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "motion.csv");
                
                if (!File.Exists(csvPath))
                {
                    MessageBox.Show($"Motion data file not found at: {csvPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (var line in File.ReadLines(csvPath))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 6) continue;

                    if (int.TryParse(parts[0], out int ax) &&
                        int.TryParse(parts[1], out int ay) &&
                        int.TryParse(parts[2], out int az) &&
                        int.TryParse(parts[3], out int rx) &&
                        int.TryParse(parts[4], out int ry) &&
                        int.TryParse(parts[5], out int rz))
                    {
                        // Convert raw accelerometer integers to small floats
                        Vector3 accel = new Vector3(
                            (ax - 32768) / 10000f,
                            (ay - 32768) / 10000f,
                            (az - 32768) / 10000f
                        );

                        // Convert raw rotation integers (0-65535) to degrees 0-360
                        Vector3 rot = new Vector3(
                            rx / 65535f * 360f,
                            ry / 65535f * 360f,
                            rz / 65535f * 360f
                        );

                        _frames.Add(new MotionFrame { Acceleration = accel, Rotation = rot });
                    }
                }

                MessageBox.Show($"Loaded {_frames.Count} motion frames", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading motion data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_frames.Count == 0) return;

            _currentFrame = (_currentFrame + 1) % _frames.Count;
            UpdateStickTransform();
        }

        private void UpdateStickTransform()
        {
            if (_frames.Count == 0 || _currentFrame >= _frames.Count) return;

            var frame = _frames[_currentFrame];

            // Update robot transform (shared by both body and head)
            _robotTransform.Children.Clear();

            // Step 1: Translate to origin (center the model at 0,0,0)
            _robotTransform.Children.Add(new TranslateTransform3D(
                -_modelCenter.X,
                -_modelCenter.Y,
                -_modelCenter.Z
            ));

            // Step 2: Apply motion rotations (around the centered origin)
            var rotateX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), frame.Rotation.X);
            var rotateY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), frame.Rotation.Y);
            var rotateZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), frame.Rotation.Z);

            _robotTransform.Children.Add(new RotateTransform3D(rotateX));
            _robotTransform.Children.Add(new RotateTransform3D(rotateY));
            _robotTransform.Children.Add(new RotateTransform3D(rotateZ));

            // Step 3: Rotate to lay flat (rotate -90 degrees around X axis)
            _robotTransform.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)
            ));

            // Step 4: Rotate to face forward (rotate -90 degrees around Z axis)
            _robotTransform.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90)
            ));

            // Step 5: Scale down the robot
            _robotTransform.Children.Add(new ScaleTransform3D(0.1, 0.1, 0.1));

            // Step 6: Apply translation (acceleration data + base offset to sit on ground)
            _robotTransform.Children.Add(new TranslateTransform3D(
                frame.Acceleration.X,
                frame.Acceleration.Y + 0.4, // Offset to make robot sit on ground
                frame.Acceleration.Z
            ));

            // Update camera to follow the robot
            UpdateCameraPosition(frame.Acceleration);
        }

        private void UpdateCameraPosition(Vector3 stickPosition)
        {
            // Camera follows the stick at a fixed distance
            double cameraDistance = 5.0; // Distance from camera to stick
            double cameraHeight = 2.0;   // Height offset for better viewing angle

            // Position camera behind and above the stick (accounting for ground offset)
            camera.Position = new Point3D(
                stickPosition.X,
                stickPosition.Y + cameraHeight + 0.4, // Account for robot sitting on ground
                stickPosition.Z + cameraDistance
            );

            // Look at the stick's current position (accounting for ground offset)
            Vector3D lookDirection = new Vector3D(
                stickPosition.X - camera.Position.X,
                (stickPosition.Y + 0.4) - camera.Position.Y, // Look at robot on ground
                stickPosition.Z - camera.Position.Z
            );

            camera.LookDirection = lookDirection;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_frames.Count == 0)
            {
                MessageBox.Show("No motion data loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isPlaying = true;
            _timer.Start();
            btnStart.IsEnabled = false;
            btnPause.IsEnabled = true;
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            _isPlaying = false;
            _timer.Stop();
            btnStart.IsEnabled = true;
            btnPause.IsEnabled = false;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _currentFrame = 0;
            _isPlaying = false;

            // Reset robot transform to initial state
            ApplyBaseTransform();

            // Reset camera to initial position
            camera.Position = new Point3D(0, 2.4, 5);
            camera.LookDirection = new Vector3D(0, -0.4, -1);

            btnStart.IsEnabled = true;
            btnPause.IsEnabled = false;
        }

        private class MotionFrame
        {
            public Vector3 Acceleration { get; set; }
            public Vector3 Rotation { get; set; }
        }
    }
}