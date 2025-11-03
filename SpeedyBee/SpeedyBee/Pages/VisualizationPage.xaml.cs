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
        private Transform3DGroup _stickTransform;
        private bool _isPlaying = false;

        public VisualizationPage()
        {
            InitializeComponent();
            InitializeVisualization();
            LoadMotionData();
        }

        private void InitializeVisualization()
        {
            // Create stick geometry (a line represented as a thin cylinder)
            CreateStickGeometry();

            // Initialize transform group
            _stickTransform = new Transform3DGroup();
            stickModel.Transform = _stickTransform;

            // Initialize timer for animation
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 20 FPS
            };
            _timer.Tick += Timer_Tick;
        }

        private void CreateStickGeometry()
        {
            var mesh = new MeshGeometry3D();
            
            // Create a simple stick as a thin rectangular prism
            double length = 1.0;
            double thickness = 0.05;

            // Define vertices for a rectangular stick
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

            stickMesh.Positions = mesh.Positions;
            stickMesh.TriangleIndices = mesh.TriangleIndices;
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
            
            _stickTransform.Children.Clear();

            // Apply rotation transformations (in degrees)
            var rotateX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), frame.Rotation.X);
            var rotateY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), frame.Rotation.Y);
            var rotateZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), frame.Rotation.Z);

            _stickTransform.Children.Add(new RotateTransform3D(rotateX));
            _stickTransform.Children.Add(new RotateTransform3D(rotateY));
            _stickTransform.Children.Add(new RotateTransform3D(rotateZ));

            // Apply translation (acceleration data)
            _stickTransform.Children.Add(new TranslateTransform3D(
                frame.Acceleration.X,
                frame.Acceleration.Y,
                frame.Acceleration.Z
            ));
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
            
            // Reset transform
            _stickTransform.Children.Clear();
            
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
