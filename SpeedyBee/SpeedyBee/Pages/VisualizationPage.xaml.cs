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

        public VisualizationPage()
        {
            InitializeComponent();
            InitializeVisualization();
            LoadMotionData();
        }

        private void InitializeVisualization()
        {
            // Create robot body geometry (yellow rectangular prism)
            CreateBodyGeometry();

            // Create robot head geometry (red rectangular prism - top part)
            CreateHeadGeometry();

            // Initialize transform group (shared by both body and head)
            _robotTransform = new Transform3DGroup();

            bodyModel.Transform = _robotTransform;
            headModel.Transform = _robotTransform;

            // Initialize timer for animation
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 20 FPS
            };
            _timer.Tick += Timer_Tick;
        }

        private void CreateBodyGeometry()
        {
            var mesh = new MeshGeometry3D();

            // Create robot body as a rectangular prism (most of the stick - yellow)
            double length = 0.8;  // Body is 80% of total length
            double thickness = 0.05;

            // Define vertices for the body (centered at origin)
            // Bottom rectangle
            mesh.Positions.Add(new Point3D(-thickness, -length / 2, -thickness));
            mesh.Positions.Add(new Point3D(thickness, -length / 2, -thickness));
            mesh.Positions.Add(new Point3D(thickness, -length / 2, thickness));
            mesh.Positions.Add(new Point3D(-thickness, -length / 2, thickness));

            // Top rectangle
            mesh.Positions.Add(new Point3D(-thickness, length / 2, -thickness));
            mesh.Positions.Add(new Point3D(thickness, length / 2, -thickness));
            mesh.Positions.Add(new Point3D(thickness, length / 2, thickness));
            mesh.Positions.Add(new Point3D(-thickness, length / 2, thickness));

            // Define triangles for all faces
            // Bottom face
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(2);

            // Top face
            mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(5); mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(7);

            // Front face
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(5); mesh.TriangleIndices.Add(4);

            // Back face
            mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(7); mesh.TriangleIndices.Add(6);

            // Left face
            mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(7);

            // Right face
            mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(5);

            bodyMesh.Positions = mesh.Positions;
            bodyMesh.TriangleIndices = mesh.TriangleIndices;
        }

        private void CreateHeadGeometry()
        {
            var mesh = new MeshGeometry3D();

            // Create robot head as a small red rectangular prism (top part of the stick)
            double headLength = 0.2;  // Head is 20% of total length
            double thickness = 0.05;

            // Define vertices for the head (positioned above the body)
            // Bottom rectangle (connects to top of body)
            mesh.Positions.Add(new Point3D(-thickness, 0.4 - headLength / 2, -thickness));
            mesh.Positions.Add(new Point3D(thickness, 0.4 - headLength / 2, -thickness));
            mesh.Positions.Add(new Point3D(thickness, 0.4 - headLength / 2, thickness));
            mesh.Positions.Add(new Point3D(-thickness, 0.4 - headLength / 2, thickness));

            // Top rectangle
            mesh.Positions.Add(new Point3D(-thickness, 0.4 + headLength / 2, -thickness));
            mesh.Positions.Add(new Point3D(thickness, 0.4 + headLength / 2, -thickness));
            mesh.Positions.Add(new Point3D(thickness, 0.4 + headLength / 2, thickness));
            mesh.Positions.Add(new Point3D(-thickness, 0.4 + headLength / 2, thickness));

            // Define triangles for all faces
            // Bottom face
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(2);

            // Top face
            mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(5); mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(7);

            // Front face
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(5); mesh.TriangleIndices.Add(4);

            // Back face
            mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(7); mesh.TriangleIndices.Add(6);

            // Left face
            mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(7);

            // Right face
            mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(5);

            headMesh.Positions = mesh.Positions;
            headMesh.TriangleIndices = mesh.TriangleIndices;
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

            // Apply rotation transformations (in degrees)
            var rotateX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), frame.Rotation.X);
            var rotateY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), frame.Rotation.Y);
            var rotateZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), frame.Rotation.Z);

            _robotTransform.Children.Add(new RotateTransform3D(rotateX));
            _robotTransform.Children.Add(new RotateTransform3D(rotateY));
            _robotTransform.Children.Add(new RotateTransform3D(rotateZ));

            // Apply translation (acceleration data + base offset to sit on ground)
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

            // Reset robot transform
            _robotTransform.Children.Clear();

            // Reset camera to initial position
            camera.Position = new Point3D(0, 0, 5);
            camera.LookDirection = new Vector3D(0, 0, -1);

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
