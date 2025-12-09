using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using OpenTK.Mathematics;
using SpeedyBee.ViewModels;

public class MaterialInfo
{
    public string Name { get; set; }
    public Color DiffuseColor { get; set; }
    public MaterialInfo(string name, Color color)
    {
        Name = name;
        DiffuseColor = color;
    }
}

public class GeometryWithMaterial
{
    public MeshGeometry3D Mesh { get; set; }
    public MaterialInfo Material { get; set; }

    public GeometryWithMaterial(MeshGeometry3D mesh, MaterialInfo material)
    {
        Mesh = mesh;
        Material = material;
    }
}

namespace SpeedyBee.Views
{
    public class DataTableItem : INotifyPropertyChanged
    {
        public int AccelX { get; set; }
        public int AccelY { get; set; }
        public int AccelZ { get; set; }
        public int GyroX { get; set; }
        public int GyroY { get; set; }
        public int GyroZ { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public DataTableItem(ImuData data)
        {
            AccelX = (int)Math.Round(data.accel_x);
            AccelY = (int)Math.Round(data.accel_y);
            AccelZ = (int)Math.Round(data.accel_z);
            GyroX = (int)Math.Round(data.gyro_x);
            GyroY = (int)Math.Round(data.gyro_y);
            GyroZ = (int)Math.Round(data.gyro_z);
        }
    }

    public partial class VisualizationViewport : UserControl
    {
        private Point3D _modelCenter;
        private double _cameraPitch = -0.1;
        private double _cameraYaw = 0;
        private double _cameraRoll = 0;
        private const double CameraRotationSpeed = 0.05;
        private const double CameraMovementSpeed = 0.2;
        private Vector3 _accumulatedRotation = Vector3.Zero;
        private Vector3 _accumulatedPosition = Vector3.Zero;
        private const int MaxDataBufferSize = 3;
        private ObservableCollection<DataTableItem> _dataBuffer = new ObservableCollection<DataTableItem>();
        private List<ModelVisual3D> _robotParts = new List<ModelVisual3D>();

        public VisualizationViewport()
        {
            InitializeComponent();
            InitializeVisualization();
        }

        private void InitializeVisualization()
        {
            LoadRobotModel();
            ApplyBaseTransform();
            UpdateCameraDirection();

            // Initialize DataGrid binding
            dataGrid.ItemsSource = _dataBuffer;
        }

        private void ApplyBaseTransform()
        {
            // Apply base transform to all robot parts
            foreach (var part in _robotParts)
            {
                if (part.Transform is Transform3DGroup transformGroup)
                {
                    // Clear existing transforms and reapply base
                    transformGroup.Children.Clear();

                    transformGroup.Children.Add(new TranslateTransform3D(
                        -_modelCenter.X,
                        -_modelCenter.Y,
                        -_modelCenter.Z
                    ));

                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90)
                    ));

                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)
                    ));

                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)
                    ));

                    transformGroup.Children.Add(new ScaleTransform3D(0.1, 0.1, 0.1));
                }
            }
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

                // Clear any existing robot parts
                foreach (var part in _robotParts)
                {
                    viewport.Children.Remove(part);
                }
                _robotParts.Clear();

                // FULL MULTI-MATERIAL APPROACH: Separate geometry by materials for proper colors
                var materials = LoadMtlFile(objPath);
                Console.WriteLine($"Loaded {materials.Count} materials");

                Console.WriteLine($"Available materials:");
                foreach (var mat in materials.Values)
                {
                    Console.WriteLine($"  {mat.Name}: {mat.DiffuseColor}");
                }

                // Load geometry grouped by materials (optimized fast version)
                var geometries = LoadObjWithMaterialsFast(objPath, materials);
                Console.WriteLine($"Created {geometries.Count} material geometry groups");

                // Calculate center from all vertices (assumes all geometries share the same vertices)
                Point3DCollection allPositions = null;
                if (geometries.Count > 0 && geometries[0].Mesh.Positions != null)
                {
                    allPositions = geometries[0].Mesh.Positions;
                }
                else
                {
                    // Fallback: calculate center from single mesh
                    var fallbackMesh = LoadObjMeshFast(objPath);
                    _modelCenter = CalculateModelCenter(fallbackMesh.Positions);
                }
                _modelCenter = CalculateModelCenter(allPositions);

                // Create a combined model with multiple geometry models for different materials
                var combinedModel = new Model3DGroup();

                foreach (var geom in geometries)
                {
                    if (geom.Mesh.TriangleIndices.Count > 0)
                    {
                        var model3D = new GeometryModel3D
                        {
                            Geometry = geom.Mesh,
                            Material = new DiffuseMaterial(new SolidColorBrush(geom.Material.DiffuseColor))
                        };
                        combinedModel.Children.Add(model3D);
                        Console.WriteLine($"Added geometry group: {geom.Material.Name} with {geom.Mesh.TriangleIndices.Count / 3} triangles");
                    }
                }

                var visual3D = new ModelVisual3D
                {
                    Content = combinedModel
                };

                // Create transform for the robot
                var transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(new TranslateTransform3D(-_modelCenter.X, -_modelCenter.Y, -_modelCenter.Z));
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90)));
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90)));
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)));
                transformGroup.Children.Add(new ScaleTransform3D(0.1, 0.1, 0.1));

                visual3D.Transform = transformGroup;

                _robotParts.Add(visual3D);
                viewport.Children.Add(visual3D);

                Console.WriteLine("Loaded robot model as single fast mesh");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading robot model: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private MeshGeometry3D LoadObjMeshFast(string filePath)
        {
            // Ultra-fast single-pass loading
            var positions = new List<Point3D>();
            var indices = new List<int>();

            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;

                    if (parts[0] == "v" && parts.Length >= 4)
                    {
                        if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                            double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                        {
                            positions.Add(new Point3D(x, y, z));
                        }
                    }
                    else if (parts[0] == "f" && parts.Length >= 4)
                    {
                        // Only take first 3 vertices for triangles
                        for (int i = 1; i < Math.Min(4, parts.Length); i++)
                        {
                            var vertexPart = parts[i].Split('/')[0];
                            if (int.TryParse(vertexPart, out int idx) && idx > 0 && idx <= positions.Count)
                            {
                                indices.Add(idx - 1);
                            }
                        }
                    }
                }
            }

            return new MeshGeometry3D
            {
                Positions = new Point3DCollection(positions),
                TriangleIndices = new Int32Collection(indices)
            };
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

        private Dictionary<string, MaterialInfo> LoadMtlFile(string objFilePath)
        {
            var materials = new Dictionary<string, MaterialInfo>();
            string mtlPath = Path.ChangeExtension(objFilePath, ".mtl");

            if (!File.Exists(mtlPath))
            {
                return materials; // Return empty if no MTL file
            }

            MaterialInfo currentMaterial = null;

            foreach (var line in File.ReadLines(mtlPath))
            {
                var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                if (parts[0] == "newmtl" && parts.Length >= 2)
                {
                    // Save previous material if any
                    if (currentMaterial != null)
                    {
                        materials[currentMaterial.Name] = currentMaterial;
                    }

                    // Create new material
                    currentMaterial = new MaterialInfo(parts[1], Colors.Gray); // Default color
                }
                else if (parts[0] == "Kd" && parts.Length >= 4 && currentMaterial != null)
                {
                    // Diffuse color (Kd - red green blue)
                    if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double r) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double g) &&
                        double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double b))
                    {
                        currentMaterial.DiffuseColor = Color.FromRgb(
                            (byte)(r * 255),
                            (byte)(g * 255),
                            (byte)(b * 255)
                        );
                    }
                }
            }

            // Save the last material
            if (currentMaterial != null)
            {
                materials[currentMaterial.Name] = currentMaterial;
            }

            return materials;
        }

        private List<GeometryWithMaterial> LoadObjWithMaterialsFast(string filePath, Dictionary<string, MaterialInfo> materials)
        {
            var geometries = new Dictionary<string, Tuple<List<Point3D>, List<int>>>();
            var vertices = new List<Point3D>();

            // Create a default material for parts without explicit materials
            MaterialInfo defaultMaterial = new MaterialInfo("Default", Colors.Silver);
            string currentMaterialKey = "Default";

            // Initialize the first geometry group
            geometries[currentMaterialKey] = new Tuple<List<Point3D>, List<int>>(vertices, new List<int>());

            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;

                    if (parts[0] == "v" && parts.Length >= 4)
                    {
                        // Vertex - shared across all materials
                        if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                            double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                        {
                            vertices.Add(new Point3D(x, y, z));
                        }
                    }
                    else if (parts[0] == "usemtl" && parts.Length >= 2)
                    {
                        // Switch to new material
                        string materialName = parts[1];
                        if (materials.ContainsKey(materialName))
                        {
                            currentMaterialKey = materialName;
                        }
                        else
                        {
                            currentMaterialKey = "Default";
                        }

                        // Initialize geometry group for this material if not exists
                        if (!geometries.ContainsKey(currentMaterialKey))
                        {
                            geometries[currentMaterialKey] = new Tuple<List<Point3D>, List<int>>(vertices, new List<int>());
                        }
                    }
                    else if (parts[0] == "f" && parts.Length >= 4)
                    {
                        // Face - add to current material's geometry
                        var faceIndices = new List<int>();
                        for (int i = 1; i < parts.Length && faceIndices.Count < 3; i++)
                        {
                            var vertexPart = parts[i].Split('/')[0];
                            if (int.TryParse(vertexPart, out int idx) && idx > 0 && idx <= vertices.Count)
                            {
                                faceIndices.Add(idx - 1);
                            }
                        }
                        if (faceIndices.Count == 3)
                        {
                            geometries[currentMaterialKey].Item2.AddRange(faceIndices);
                        }
                    }
                }
            }

            // Convert to GeometryWithMaterial objects
            var result = new List<GeometryWithMaterial>();
            foreach (var kvp in geometries)
            {
                if (kvp.Value.Item2.Count > 0) // Only if has faces
                {
                    var mesh = new MeshGeometry3D
                    {
                        Positions = new Point3DCollection(vertices),
                        TriangleIndices = new Int32Collection(kvp.Value.Item2)
                    };

                    // Get the material
                    MaterialInfo material;
                    if (materials.ContainsKey(kvp.Key))
                    {
                        material = materials[kvp.Key];
                    }
                    else
                    {
                        material = defaultMaterial;
                    }

                    result.Add(new GeometryWithMaterial(mesh, material));
                }
            }

            return result;
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
                    if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                        double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                    {
                        vertices.Add(new Point3D(x, y, z));
                    }
                }
                else if (parts[0] == "f" && parts.Length >= 4)
                {
                    var faceIndices = new List<int>();
                    for (int i = 1; i < parts.Length && faceIndices.Count < 3; i++)
                    {
                        var vertexPart = parts[i].Split('/')[0];
                        if (int.TryParse(vertexPart, out int idx))
                        {
                            faceIndices.Add(idx - 1);
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

        public void UpdateImuTransform(ImuData data)
        {
            Console.WriteLine($"UpdateImuTransform called with data: accel({data.accel_x}, {data.accel_y}, {data.accel_z}), gyro({data.gyro_x}, {data.gyro_y}, {data.gyro_z})");

            Vector3 acceleration = new Vector3(
                data.accel_x,
                data.accel_y,
                data.accel_z
            );

            const float SensitivityFactor = 3.0f;
            const float PositionSensitivityFactor = 1f;
            const float AccelerationThreshold = 2f;
            Vector3 rotation = new Vector3(
                data.gyro_x / SensitivityFactor,
                -data.gyro_y / SensitivityFactor,
                data.gyro_z / SensitivityFactor
            );

            Vector3 filteredAcceleration = new Vector3(
                Math.Abs(acceleration.X) > AccelerationThreshold ? acceleration.X : 0.0f,
                Math.Abs(acceleration.Y) > AccelerationThreshold ? acceleration.Y : 0.0f,
                Math.Abs(acceleration.Z) > AccelerationThreshold ? acceleration.Z : 0.0f
            );

            _accumulatedRotation += rotation;
            _accumulatedPosition += filteredAcceleration * PositionSensitivityFactor;

            // Apply transforms to all robot parts
            foreach (var part in _robotParts)
            {
                if (part.Transform is Transform3DGroup transformGroup)
                {
                    // Remove dynamic transforms (rotation and position), keep base transforms
                    while (transformGroup.Children.Count > 5) // Base has 5 transforms
                    {
                        transformGroup.Children.RemoveAt(5);
                    }

                    // Add rotation transforms
                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(1, 0, 0), _accumulatedRotation.Y)));
                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(0, 1, 0), -_accumulatedRotation.X)));
                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(0, 0, 1), _accumulatedRotation.Z)));

                    // Add position transform
                    transformGroup.Children.Add(new TranslateTransform3D(
                        _accumulatedPosition.X,
                        _accumulatedPosition.Y + 0.4,
                        _accumulatedPosition.Z
                    ));
                }
            }

            // Update data table with latest sensor values
            UpdateDataBuffer(data);
        }

        private void UpdateDataBuffer(ImuData data)
        {
            Console.WriteLine($"UpdateDataBuffer called - current thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            // Ensure we're on the UI thread and update the collection
            if (!Dispatcher.CheckAccess())
            {
                Console.WriteLine("Not on UI thread, dispatching...");
                Dispatcher.Invoke(() => UpdateDataBuffer(data));
                return;
            }

            Console.WriteLine("On UI thread, adding data to buffer");

            _dataBuffer.Add(new DataTableItem(data));
            if (_dataBuffer.Count > MaxDataBufferSize)
            {
                _dataBuffer.RemoveAt(0);
            }

            Console.WriteLine($"Data buffer now has {_dataBuffer.Count} items");

            // Force DataGrid refresh
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = _dataBuffer;
        }

        public void UpdateFrameTransform(Vector3 acceleration, Vector3 rotation)
        {
            // Apply transforms to all robot parts
            foreach (var part in _robotParts)
            {
                if (part.Transform is Transform3DGroup transformGroup)
                {
                    // Remove dynamic transforms, keep base transforms
                    while (transformGroup.Children.Count > 5) // Base has 5 transforms
                    {
                        transformGroup.Children.RemoveAt(5);
                    }

                    // Add rotation transforms
                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(1, 0, 0), rotation.X)));
                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(0, 1, 0), rotation.Z)));
                    transformGroup.Children.Add(new RotateTransform3D(
                        new AxisAngleRotation3D(new Vector3D(0, 0, 1), rotation.Y)));

                    // Add position transform
                    transformGroup.Children.Add(new TranslateTransform3D(
                        acceleration.X,
                        acceleration.Y + 0.4,
                        acceleration.Z
                    ));
                }
            }
        }

        private void UpdateCameraDirection()
        {
            Vector3D forward = new Vector3D(
                Math.Cos(_cameraYaw) * Math.Cos(_cameraPitch),
                Math.Sin(_cameraPitch),
                Math.Sin(_cameraYaw) * Math.Cos(_cameraPitch)
            );

            Vector3D right = Vector3D.CrossProduct(forward, new Vector3D(0, 1, 0));
            Vector3D up = new Vector3D(0, 1, 0);

            double cosRoll = Math.Cos(_cameraRoll);
            double sinRoll = Math.Sin(_cameraRoll);
            up = new Vector3D(
                up.X * cosRoll + right.X * sinRoll,
                up.Y * cosRoll + right.Y * sinRoll,
                up.Z * cosRoll + right.Z * sinRoll
            );

            right = Vector3D.CrossProduct(forward, up);

            camera.LookDirection = forward;
            camera.UpDirection = up;
        }

        public void HandleKeyDown(KeyEventArgs e)
        {
            bool updated = false;

            switch (e.Key)
            {
                case Key.W:
                    _cameraPitch -= CameraRotationSpeed;
                    updated = true;
                    break;
                case Key.S:
                    _cameraPitch += CameraRotationSpeed;
                    updated = true;
                    break;
                case Key.A:
                    _cameraYaw -= CameraRotationSpeed;
                    updated = true;
                    break;
                case Key.D:
                    _cameraYaw += CameraRotationSpeed;
                    updated = true;
                    break;
                case Key.Q:
                    _cameraRoll -= CameraRotationSpeed;
                    updated = true;
                    break;
                case Key.E:
                    _cameraRoll += CameraRotationSpeed;
                    updated = true;
                    break;
                case Key.Z:
                    camera.Position += camera.LookDirection * CameraMovementSpeed;
                    break;
                case Key.X:
                    camera.Position -= camera.LookDirection * CameraMovementSpeed;
                    break;
            }

            if (updated)
            {
                UpdateCameraDirection();
            }
        }
        
        public void Reset()
        {
            _accumulatedRotation = Vector3.Zero;
            _accumulatedPosition = Vector3.Zero;

            ApplyBaseTransform();
            camera.Position = new Point3D(0, 2.4, 5);
            _cameraPitch = -0.1;
            _cameraYaw = 0;
            _cameraRoll = 0;
            UpdateCameraDirection();

            // Clear data buffer
            _dataBuffer.Clear();
        }
    }
}
