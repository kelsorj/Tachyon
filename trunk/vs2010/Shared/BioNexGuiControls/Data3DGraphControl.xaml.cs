using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace BioNex.Shared.BioNexGuiControls
{
    public delegate void ResetTransformEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Interaction logic for DataGraphControl.xaml
    /// </summary>
    public partial class Data3DGraphControl : UserControl
    {
        public event ResetTransformEventHandler ResetTransformEvent;

        public class GraphData
        {
            public Point3D point;  // Z data for this point
            public double zero;    // bottom of rendered column
            public bool marker;    // whether or not this column is a "marker" (e.g. well A1) -- rendered as Square column instead of round
            public double width;   // radius or width of column
            public Color color;    // color of column

            public GraphData(Point3D p, double z = 0.0, bool m = false, double w = 6.0, Color? c = null)
            {
                var actualColor = c ?? System.Windows.Media.Colors.Aquamarine;
                point = p;
                zero = z;
                marker = m;
                width = w;
                color = actualColor;
            }
        }
        List<GraphData> _data = new List<GraphData>();
        List<GeometryModel3D> _models = new List<GeometryModel3D>(); // list of models for re-coloring

        public void Clear() {
            lock (this) { _data = new List<GraphData>(); }
            Draw();
        }

        public void AddPoint(GraphData data)
        {
            lock (this) { _data.Add(data); } 
            AddPointsToDataModel( new List<GraphData>(){data});
        }

        public void AddPoints(List<GraphData> data, bool clear = false)
        {
            lock (this) { if (clear) _data = new List<GraphData>(); _data.AddRange(data); }
            AddPointsToDataModel( data, clear);
        }
        public void Refresh() { Draw(); } // rebuild the entire model from data -- VERY EXPENSIVE
        public void ReplaceColors(IList<Color> colors)
        {
            var action = new Action(() =>
            {
                lock (this)
                {
                    if (_data.Count != _models.Count) // generally happens when there's a race between clearing the graph and updating the colors
                        return;
                    _material_map = new Dictionary<int, DiffuseMaterial>();
                    for (int i = 0; i < colors.Count; ++i)
                    {
                        if (i >= _data.Count)
                            continue;

                        // replace color in data model
                        _data[i].color = colors[i];
                        //var c = _opacity ? _data[i].color : Color.FromArgb(0xFF, _data[i].color.R, _data[i].color.G, _data[i].color.B);
                        var c = _data[i].color;
                        var c_hash = c.GetHashCode();
                        if (!_material_map.ContainsKey(c_hash))
                            _material_map[c_hash] = new DiffuseMaterial(new SolidColorBrush(c));

                        // replace color in actual model
                        _models[i].Material = _material_map[c_hash];
                    }
                }
            });
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action);
        }

        public double PlaneBorder { get; set; }
        public double PlaneThickness { get; set; }
        public bool ShowGroundPlane { get; set; }
        public SolidColorBrush GroundColor { get; set; }

        ScaleTransform3D _scaling;
        AxisAngleRotation3D _rotation;
        TranslateTransform3D _translation;
        TranslateTransform3D _recenter_translation; // keep auto-recenter translation separate from user translation

        public RotateTransform3D Rotation { get; private set; }

        Model3DGroup _group;
        Model3DGroup _active_group;

        public Data3DGraphControl()
        {
            InitializeComponent();

            _cylinder = Column(new Point3D(0, 0, 0), -1.0, 1.0, true);
            _cube = Column(new Point3D(0, 0, 0), -1.0, 1.0, false);

            GroundColor = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0x7F, 0x50)); // coral w/ 50% alpha
            PlaneBorder = 0.25;
            PlaneThickness = 0.25;
            ShowGroundPlane = true;

            InitTransform();
            Draw();
        }

        void InitTransform()
        {
            Action action = () =>
            {
                _scaling = new ScaleTransform3D();
                _rotation = new AxisAngleRotation3D();
                _translation = new TranslateTransform3D();
                _recenter_translation = new TranslateTransform3D();
                Rotation = new RotateTransform3D(_rotation);

                // initalize Camera Transform
                var transform = new Transform3DGroup();

                // transforms occur in the reverse order that they're added, so translation being first means that it's going to be relative to the rotated reference frame, and we don't have to worry about making it parallel to the screen
                transform.Children.Add(_translation);
                transform.Children.Add(Rotation);
                transform.Children.Add(_scaling);
                transform.Children.Add(_recenter_translation); // recenter before everything else so that it's not effected by the other transforms

                Camera.Transform = transform;
//                AlphaSort(Camera);
            };
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action);

        }
     
        void Recenter()
        {
            var min_x = double.MaxValue;
            var max_x = double.MinValue;
            var min_y = double.MaxValue;
            var max_y = double.MinValue;
            var min_z = double.MaxValue;
            var max_z = double.MinValue;
            var center_x = 0.0;
            var center_y = 0.0;
            var center_z = 0.0;

            // translate the points to a new origin that's in the center of the cluster
            lock (this)
            {
                if (_data.Count == 0)
                    return;

                min_x = _data.Min(p => p.point.X);
                max_x = _data.Max(p => p.point.X);
                min_y = _data.Min(p => p.point.Y);
                max_y = _data.Max(p => p.point.Y);
                min_z = _data.Min(p => p.point.Z);
                max_z = _data.Max(p => p.point.Z);
            }

            center_x = min_x + (max_x - min_x) / 2.0;
            center_y = min_y + (max_y - min_y) / 2.0;
            center_z = min_z + (max_z - min_z) / 2.0;            

            // convert X Y screen position to an axis parallel to the screen by backing out the camera transformation
            Action action = () =>
            {
                _recenter_translation.OffsetX = center_x;
                _recenter_translation.OffsetY = center_y;
                _recenter_translation.OffsetZ = center_z;

                AdjustGroundPlane(min_x, max_x, min_y, max_y);
//                AlphaSort(Camera);
            };
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action);
        }

        GeometryModel3D _ground_model = null;
        void AdjustGroundPlane(double min_x, double max_x, double min_y, double max_y)
        {
            Action action = () =>
            {
                if( _ground_model != null)
                    _group.Children.Remove(_ground_model);
                if (!ShowGroundPlane)
                    return;

                var extension = PlaneBorder;
                var mesh = Plane(min_x - extension, max_x + extension, min_y - extension, max_y + extension, PlaneThickness);
                var material = new DiffuseMaterial(GroundColor);
                _ground_model = new GeometryModel3D(mesh, material);
                _group.Children.Add(_ground_model);
            };
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action);
        }


        Dictionary<int, DiffuseMaterial> _material_map = new Dictionary<int, DiffuseMaterial>();
        MeshGeometry3D _cylinder;
        MeshGeometry3D _cube;

        Transform3DGroup TransformModel(Point3D p, double zero, double width)
        {
            var scale = new ScaleTransform3D();
            var translate = new TranslateTransform3D();
            var transform = new Transform3DGroup();

            var scale_value = Math.Abs(p.Z - zero);
            scale.ScaleZ = scale_value;
            scale.ScaleX = width;
            scale.ScaleY = width;

            translate.OffsetX = p.X;
            translate.OffsetY = p.Y;
            translate.OffsetZ = Math.Max(p.Z, zero);

            transform.Children.Add(scale);
            transform.Children.Add(translate);
            return transform;
        }
        
        void AddPointsToDataModel( IList<GraphData> data, bool clear=false)
        {
            const int models_per_group = 100;

            Action action = () =>
            {
                if (clear)
                {
                    _models = new List<GeometryModel3D>();
                    _group = new Model3DGroup();
                    _active_group = _group;
                }

                foreach(var d in data)
                {
                    var mesh = d.marker ? _cube : _cylinder;

                    //var c = _opacity ? d.color : Color.FromArgb(0xFF, d.color.R, d.color.G, d.color.B);
                    var c = d.color;
                    var c_hash = c.GetHashCode();
                    if (!_material_map.ContainsKey(c_hash))
                        _material_map[c_hash] = new DiffuseMaterial(new SolidColorBrush(c));

                    var model = new GeometryModel3D(mesh, _material_map[c_hash]);
                    model.Transform = TransformModel(d.point, d.zero, d.width);
                    _models.Add(model); // keep a list of models so we can easily replace material
                    
                    if (_active_group.Children.Count >= models_per_group)
                    {
                        var new_group = new Model3DGroup();
                        _active_group.Children.Add(new_group);
                        _active_group = new_group;
                    }
                    _active_group.Children.Add(model);
                }

                if (clear)
                {
                    visual.Content = _group;
                    AddLights();
                }

                Recenter();
            };
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action);
        }

        void DrawData()
        {
            // for now, draw data as a field of cubes, since wpf doesn't support 3d points
            AddPointsToDataModel( _data, true);
        }

        // Column -- a cube whose bottom face extends to origin
        MeshGeometry3D Column(Point3D center_top, double zero, double size, bool use_cylinder)
        {
            if (use_cylinder)
            {
                var cyl = new Cylinder();
                cyl.Point1 = new Point3D(center_top.X, center_top.Y, zero);
                cyl.Point2 = new Point3D(center_top.X, center_top.Y, center_top.Z);
                cyl.Radius1 = size / 2.0;
                cyl.Radius2 = size / 2.0;

                return (MeshGeometry3D)cyl.Geometry;
            }

            var x = center_top.X;
            var y = center_top.Y;
            var z = center_top.Z;
            var d = size / 2.0;
            var mesh = new MeshGeometry3D();
            var ltf = new Point3D(-d + x, -d + y, zero);
            var rtf = new Point3D(d + x, -d + y, zero);
            var lbf = new Point3D(-d + x, d + y, zero);
            var rbf = new Point3D(d + x, d + y, zero);
            var ltb = new Point3D(-d + x, -d + y, z);
            var rtb = new Point3D(d + x, -d + y, z);
            var lbb = new Point3D(-d + x, d + y, z);
            var rbb = new Point3D(d + x, d + y, z);

            // front face
            mesh.Positions.Add(ltf); // 0
            mesh.Positions.Add(rtf); // 1
            mesh.Positions.Add(lbf); // 2
            mesh.Positions.Add(rbf); // 3
            mesh.TriangleIndices.Add(0); // ltf
            mesh.TriangleIndices.Add(2); // lbf
            mesh.TriangleIndices.Add(3); // rbf
            mesh.TriangleIndices.Add(3); // rbf
            mesh.TriangleIndices.Add(1); // rtf
            mesh.TriangleIndices.Add(0); // ltf

            // back face
            mesh.Positions.Add(ltb); // 4
            mesh.Positions.Add(rtb); // 5
            mesh.Positions.Add(lbb); // 6
            mesh.Positions.Add(rbb); // 7
            mesh.TriangleIndices.Add(5); // rtb
            mesh.TriangleIndices.Add(7); // rbb
            mesh.TriangleIndices.Add(6); // lbb
            mesh.TriangleIndices.Add(6); // lbb
            mesh.TriangleIndices.Add(4); // ltb
            mesh.TriangleIndices.Add(5); // rtb

            // top face
            mesh.Positions.Add(ltb); // 8
            mesh.Positions.Add(ltf); // 9
            mesh.Positions.Add(rtf); // 10
            mesh.Positions.Add(rtb); // 11
            mesh.TriangleIndices.Add(8); // ltb
            mesh.TriangleIndices.Add(9); // ltf
            mesh.TriangleIndices.Add(10); // rtf
            mesh.TriangleIndices.Add(10); // rtf
            mesh.TriangleIndices.Add(11); // rtb
            mesh.TriangleIndices.Add(8); // ltb

            // bottom face
            mesh.Positions.Add(lbf); // 12
            mesh.Positions.Add(lbb); // 13
            mesh.Positions.Add(rbb); // 14
            mesh.Positions.Add(rbf); // 15
            mesh.TriangleIndices.Add(12); // lbf
            mesh.TriangleIndices.Add(13); // lbb
            mesh.TriangleIndices.Add(14); // rbb
            mesh.TriangleIndices.Add(14); // rbb
            mesh.TriangleIndices.Add(15); // rbf
            mesh.TriangleIndices.Add(12); // lbf

            // left face
            mesh.Positions.Add(ltb); // 16
            mesh.Positions.Add(lbb); // 17
            mesh.Positions.Add(lbf); // 18
            mesh.Positions.Add(ltf); // 19
            mesh.TriangleIndices.Add(16); // ltb
            mesh.TriangleIndices.Add(17); // lbb
            mesh.TriangleIndices.Add(18); // lbf
            mesh.TriangleIndices.Add(18); // lbf
            mesh.TriangleIndices.Add(19); // ltf
            mesh.TriangleIndices.Add(16); // ltb

            // right face
            mesh.Positions.Add(rtf); // 20
            mesh.Positions.Add(rbf); // 21
            mesh.Positions.Add(rbb); // 22
            mesh.Positions.Add(rtb); // 23
            mesh.TriangleIndices.Add(20); // rtf
            mesh.TriangleIndices.Add(21); // rbf
            mesh.TriangleIndices.Add(22); // rbb
            mesh.TriangleIndices.Add(22); // rbb
            mesh.TriangleIndices.Add(23); // rtb
            mesh.TriangleIndices.Add(20); // rtf
            
            return mesh;
        }

        MeshGeometry3D Plane(double min_x, double max_x, double min_y, double max_y, double thickness)
        {
            var d = thickness;

            var mesh = new MeshGeometry3D();
            var ltf = new Point3D(min_x, min_y, -d);
            var rtf = new Point3D(max_x, min_y, -d);
            var lbf = new Point3D(min_x, max_y, -d);
            var rbf = new Point3D(max_x, max_y, -d);

            var ltb = new Point3D(min_x, min_y, 0);
            var rtb = new Point3D(max_x, min_y, 0);
            var lbb = new Point3D(min_x, max_y, 0);
            var rbb = new Point3D(max_x, max_y, 0);


            // front face
            mesh.Positions.Add(ltf); // 0
            mesh.Positions.Add(rtf); // 1
            mesh.Positions.Add(lbf); // 2
            mesh.Positions.Add(rbf); // 3
            mesh.TriangleIndices.Add(0); // ltf
            mesh.TriangleIndices.Add(2); // lbf
            mesh.TriangleIndices.Add(3); // rbf
            mesh.TriangleIndices.Add(3); // rbf
            mesh.TriangleIndices.Add(1); // rtf
            mesh.TriangleIndices.Add(0); // ltf

            // back face
            mesh.Positions.Add(ltb); // 4
            mesh.Positions.Add(rtb); // 5
            mesh.Positions.Add(lbb); // 6
            mesh.Positions.Add(rbb); // 7
            mesh.TriangleIndices.Add(5); // rtb
            mesh.TriangleIndices.Add(7); // rbb
            mesh.TriangleIndices.Add(6); // lbb
            mesh.TriangleIndices.Add(6); // lbb
            mesh.TriangleIndices.Add(4); // ltb
            mesh.TriangleIndices.Add(5); // rtb

            // top face
            mesh.Positions.Add(ltb); // 8
            mesh.Positions.Add(ltf); // 9
            mesh.Positions.Add(rtf); // 10
            mesh.Positions.Add(rtb); // 11
            mesh.TriangleIndices.Add(8); // ltb
            mesh.TriangleIndices.Add(9); // ltf
            mesh.TriangleIndices.Add(10); // rtf
            mesh.TriangleIndices.Add(10); // rtf
            mesh.TriangleIndices.Add(11); // rtb
            mesh.TriangleIndices.Add(8); // ltb

            // bottom face
            mesh.Positions.Add(lbf); // 12
            mesh.Positions.Add(lbb); // 13
            mesh.Positions.Add(rbb); // 14
            mesh.Positions.Add(rbf); // 15
            mesh.TriangleIndices.Add(12); // lbf
            mesh.TriangleIndices.Add(13); // lbb
            mesh.TriangleIndices.Add(14); // rbb
            mesh.TriangleIndices.Add(14); // rbb
            mesh.TriangleIndices.Add(15); // rbf
            mesh.TriangleIndices.Add(12); // lbf

            // left face
            mesh.Positions.Add(ltb); // 16
            mesh.Positions.Add(lbb); // 17
            mesh.Positions.Add(lbf); // 18
            mesh.Positions.Add(ltf); // 19
            mesh.TriangleIndices.Add(16); // ltb
            mesh.TriangleIndices.Add(17); // lbb
            mesh.TriangleIndices.Add(18); // lbf
            mesh.TriangleIndices.Add(18); // lbf
            mesh.TriangleIndices.Add(19); // ltf
            mesh.TriangleIndices.Add(16); // ltb

            // right face
            mesh.Positions.Add(rtf); // 20
            mesh.Positions.Add(rbf); // 21
            mesh.Positions.Add(rbb); // 22
            mesh.Positions.Add(rtb); // 23
            mesh.TriangleIndices.Add(20); // rtf
            mesh.TriangleIndices.Add(21); // rbf
            mesh.TriangleIndices.Add(22); // rbb
            mesh.TriangleIndices.Add(22); // rbb
            mesh.TriangleIndices.Add(23); // rtb
            mesh.TriangleIndices.Add(20); // rtf


            // -- acceptable normals are automatically generated
            //var normal = new Vector3D(0, 1, 0);
            //mesh.Normals.Add(normal);
            //mesh.Normals.Add(normal);
            //mesh.Normals.Add(normal);

            return mesh;
        }

        void DrawRubbish()
        {
            Random rand = new Random();
            _group = new Model3DGroup();
            for (int i = 0; i < 9; ++i)
            {
                double x_offset = 10.0 * rand.NextDouble() - 10.0;
                double y_offset = 10.0 * rand.NextDouble() - 10.0;
                double z_offset = 10.0 * rand.NextDouble() - 10.0;

                var cyl = new Cylinder();
                cyl.Point1 = new Point3D(x_offset, y_offset, z_offset);
                cyl.Point2 = new Point3D(x_offset, y_offset, z_offset + 5.0);
                cyl.Radius1 = 1.0;
                cyl.Radius2 = 1.0;

                var material = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Coral));
                var model = new GeometryModel3D(cyl.Geometry, material);
                _group.Children.Add(model);
            }
            visual.Content = _group;
        }

        void AddLights()
        {
            var light1 = new DirectionalLight(Color.FromRgb(192, 192, 192), new Vector3D(0, 0, -1));
            var light2 = new DirectionalLight(Color.FromRgb(128, 128, 128), new Vector3D(-3, 2, -1));
            var light3 = new DirectionalLight(Color.FromRgb(96, 96, 96), new Vector3D(3, -2, 1));
            var light4 = new AmbientLight(Color.FromRgb(64, 64, 64));
            _group.Children.Add(light1);
            _group.Children.Add(light2);
            _group.Children.Add(light3);
            _group.Children.Add(light4);
        }

        object draw_lock = new object();
        bool drawing = false;
        void Draw()
        {
            // don't bother drawing if we're already inside the draw routine
            lock (draw_lock)
            {
                if (drawing) return;
                drawing = true;
            }

            Action action = () =>
            {
                if (Application.Current.MainWindow == null) // draw dots at design time
                    DrawRubbish();
                else
                    DrawData();

                AddLights();

                lock (draw_lock)
                {
                    drawing = false;
                }
            };
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action);
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            bool control_down = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            switch (e.Key)
            {
                case Key.R:
                    if (control_down)
                        ResetTransform();
                    break;
                /*case Key.O:
                    if (control_down)
                        ToggleOpacity();
                    break;*/
            }
        }

        public void ResetTransform()
        {
            _scale = 1.0;
            InitTransform();
            Recenter();

            if (ResetTransformEvent != null)
                ResetTransformEvent(this, EventArgs.Empty);
        }

/*        bool _opacity = false;
        public void ToggleOpacity()
        {
            _opacity = !_opacity;
            lock (this)
            {
                Draw();
            }
        }*/

        #region TrackBall
        bool _dragging_left;
        bool _dragging_right;
        Point _left_click;
        Point _right_click;
        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            view.Focus(); // work-around -- control isn't receiving focus in Synapsis

            if (!Mouse.Capture((IInputElement)e.Source, CaptureMode.Element))
                return;
            _dragging_left = true;

            _left_click = e.GetPosition((IInputElement)e.Source);
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging_left)
                return;
            ReleaseMouseCapture();
            _dragging_left = false;
        }

        private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            view.Focus(); // work-around -- control isn't receiving focus in Synapsis

            if (!Mouse.Capture((IInputElement)e.Source, CaptureMode.Element))
                return;
            _dragging_right = true;

            _right_click = e.GetPosition((IInputElement)e.Source);
        }

        private void UserControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging_right)
                return;
            ReleaseMouseCapture();
            _dragging_right = false;
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging_left)
            {
                DoRotation(e.GetPosition((IInputElement)e.Source));
                return;
            }
            if (_dragging_right)
            {
                DoTranslation(e.GetPosition((IInputElement)e.Source));
                return;
            }
        }

        double _scale = 1.0;
        public double Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                _scaling.ScaleX = _scale;
                _scaling.ScaleY = _scale;
                _scaling.ScaleZ = _scale;

//                AlphaSort(Camera);
            }
        }
        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var temp = _scale - (e.Delta / 100.0);
            temp = Math.Max(0.001, temp);
            Scale = temp;
        }

        private static Matrix3D GetViewMatrix(ProjectionCamera camera)
        {
            // This math is identical to what you find documented for
            // D3DXMatrixLookAtRH with the exception that WPF uses a
            // LookDirection vector rather than a LookAt point.

            Vector3D zAxis = -camera.LookDirection;
            zAxis.Normalize();

            Vector3D xAxis = Vector3D.CrossProduct(camera.UpDirection, zAxis);
            xAxis.Normalize();

            Vector3D yAxis = Vector3D.CrossProduct(zAxis, xAxis);

            Vector3D position = (Vector3D)camera.Position;
            double offsetX = -Vector3D.DotProduct(xAxis, position);
            double offsetY = -Vector3D.DotProduct(yAxis, position);
            double offsetZ = -Vector3D.DotProduct(zAxis, position);

            return new Matrix3D(
                xAxis.X, yAxis.X, zAxis.X, 0,
                xAxis.Y, yAxis.Y, zAxis.Y, 0,
                xAxis.Z, yAxis.Z, zAxis.Z, 0,
                offsetX, offsetY, offsetZ, 1);
        }

        private Vector3D PointOnSphere(Point p)
        {
            // scale bounds to [-1,1]...[1,-1]
            var height = this.ActualHeight;
            var width = this.ActualWidth;
            var smaller = Math.Min(height, width); // scale to the smaller of the two dimensions so that equal motion in either direction has same rotation affect

            // the 2.0 * stuff is to get the coordinates centered on the view
            var x0 = ((2.0 * p.X / width) - 1) * width / smaller;
            var y0 = (1 - (2.0 * p.Y / height)) * height / smaller; // invert from screen Y

            // translate point of rotation by our translation transform, otherwise the center of rotation doesn't match the point where we clicked ...
            x0 += (_translation.OffsetX / _scale);
            y0 += (_translation.OffsetY / _scale);

            // get z as location on surface of radius 1 sphere
            var temp = 1 - (x0 * x0) - (y0 * y0);
            var z0 = temp > 0 ? Math.Sqrt(temp) : 0;

            return new Vector3D(x0, y0, z0);
        }

        void DoRotation(Point new_position)
        {
            var v0 = PointOnSphere(_left_click);
            var v1 = PointOnSphere(new_position);
            var axis = Vector3D.CrossProduct(v0, v1);
            var angle = Vector3D.AngleBetween(v0, v1);

            RotateAroundAxis(axis, angle);
            _left_click = new_position;

//            AlphaSort(Camera);
        }

        public void RotateAroundAxis(Vector3D axis, double angle)
        {
            if (axis.Length == 0)
                return;

            // Get the camera's current view matrix 
            // and transform the rotation axis relative to the camera orientation
            // This lets us set up the initial camera in any orientation we want, and rotations still work intuitively
            // since our PointOnSphere code uses screen position space, which is always going to be X,Y looking down Z. 
            Matrix3D viewMatrix = GetViewMatrix(Camera);
            viewMatrix.Invert();
            axis = viewMatrix.Transform(axis);

            // get quaternion representing the new rotation
            var delta = new Quaternion(axis, -angle); // neg angle because rotating view.  pos angle would rotate scene

            // get current world rotation transformation as quaternion
            var q = new Quaternion(_rotation.Axis, _rotation.Angle);

            q *= delta; // multiply to apply new rotation

            // put the new orientation back into the camera rotation transformation
            _rotation.Axis = q.Axis;
            _rotation.Angle = q.Angle;
        }

        void DoTranslation(Point new_position)
        {
            var height = this.ActualHeight;
            var width = this.ActualWidth;
            var larger = Math.Max(height, width);
            var const_factor = 250.0 / _scale;

            var dx = (_right_click.X - new_position.X) / larger * const_factor;
            var dy = (new_position.Y - _right_click.Y) / larger * const_factor;
            _right_click = new_position;

            _translation.OffsetX += dx;
            _translation.OffsetY += dy;

//            AlphaSort(Camera);
        }

        #endregion
        
        /* #region SceneSorting

        // don't look in here unless you want to be sad
        #region ProjectPointHelp
        // ripped out of Petzold
        public static Matrix3D GetTotalTransform(Viewport3D viewport)
        {
            Matrix3D matx = GetCameraTransform(viewport);
            matx.Append(GetViewportTransform(viewport));
            return matx;
        }
        public static Matrix3D GetCameraTransform(Viewport3D viewport)
        {
            return GetTotalTransform(viewport.Camera,
                                viewport.ActualWidth / viewport.ActualHeight);
        }
        public static Matrix3D GetViewportTransform(Viewport3D viewport)
        {
            return new Matrix3D(viewport.ActualWidth / 2, 0, 0, 0,
                                0, -viewport.ActualHeight / 2, 0, 0,
                                                 0, 0, 1, 0,
                                                 viewport.ActualWidth / 2,
                                                 viewport.ActualHeight / 2, 0, 1);
        }
        public static readonly Matrix3D ZeroMatrix = new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0,
                                                                  0, 0, 0, 0, 0, 0, 0, 0);
        public static Matrix3D GetTotalTransform(Camera cam, double aspectRatio)
        {
            Matrix3D matx = Matrix3D.Identity;
            if (cam == null)
                matx = ZeroMatrix;
            else
            {
                if (cam.Transform != null)
                {
                    Matrix3D matxCameraTransform = cam.Transform.Value;

                    if (!matxCameraTransform.HasInverse)
                        matx = ZeroMatrix;
                    else
                        matxCameraTransform.Invert();
                    matx.Append(matxCameraTransform);
                }
                matx.Append(GetViewMatrix(cam));
                matx.Append(GetProjectionMatrix(cam, aspectRatio));
            }
            return matx;
        }
        public static Matrix3D GetViewMatrix(Camera camera)
        {
            Matrix3D matx = Matrix3D.Identity;

            if (camera == null)
            {
                matx = ZeroMatrix;
            }

            else if (camera is MatrixCamera)
            {
                matx = (camera as MatrixCamera).ViewMatrix;
            }
            else if (camera is ProjectionCamera)
            {
                ProjectionCamera projcam = camera as ProjectionCamera;

                Vector3D zAxis = -projcam.LookDirection;
                zAxis.Normalize();

                Vector3D xAxis = Vector3D.CrossProduct(projcam.UpDirection, zAxis);
                xAxis.Normalize();

                Vector3D yAxis = Vector3D.CrossProduct(zAxis, xAxis);
                Vector3D pos = (Vector3D)projcam.Position;

                matx = new Matrix3D(xAxis.X, yAxis.X, zAxis.X, 0,
                                             xAxis.Y, yAxis.Y, zAxis.Y, 0,
                                             xAxis.Z, yAxis.Z, zAxis.Z, 0,
                                             -Vector3D.DotProduct(xAxis, pos),
                                             -Vector3D.DotProduct(yAxis, pos),
                                             -Vector3D.DotProduct(zAxis, pos), 1);

            }
            else if (camera != null)
            {
                throw new ApplicationException("ViewMatrix");
            }
            return matx;
        }
        public static Matrix3D GetProjectionMatrix(Camera cam, double aspectRatio)
        {
            Matrix3D matx = Matrix3D.Identity;

            if (cam == null)
            {
                matx = ZeroMatrix;
            }

            else if (cam is MatrixCamera)
            {
                matx = (cam as MatrixCamera).ProjectionMatrix;
            }

            else if (cam is OrthographicCamera)
            {
                OrthographicCamera orthocam = cam as OrthographicCamera;

                double xScale = 2 / orthocam.Width;
                double yScale = xScale * aspectRatio;
                double zNear = orthocam.NearPlaneDistance;
                double zFar = orthocam.FarPlaneDistance;

                // Hey, check this out!
                if (Double.IsPositiveInfinity(zFar))
                    zFar = 1E10;

                matx = new Matrix3D(xScale, 0, 0, 0,
                                    0, yScale, 0, 0,
                                    0, 0, 1 / (zNear - zFar), 0,
                                    0, 0, zNear / (zNear - zFar), 1);

            }

            else if (cam is PerspectiveCamera)
            {
                PerspectiveCamera perscam = cam as PerspectiveCamera;

                // The angle-to-radian formula is a little off because only
                //  half the angle enters the calculation.
                double xScale = 1 / Math.Tan(Math.PI * perscam.FieldOfView / 360);
                double yScale = xScale * aspectRatio;
                double zNear = perscam.NearPlaneDistance;
                double zFar = perscam.FarPlaneDistance;
                double zScale = (zFar == double.PositiveInfinity ? -1 : (zFar / (zNear - zFar)));
                double zOffset = zNear * zScale;

                matx = new Matrix3D(xScale, 0, 0, 0,
                                    0, yScale, 0, 0,
                                    0, 0, zScale, -1,
                                    0, 0, zOffset, 0);
            }

            else if (cam != null)
            {
                throw new ApplicationException("ProjectionMatrix");
            }

            return matx;
        }
        public static Point Point3DtoPoint2D(Viewport3D viewport, Point3D point)
        {
            Matrix3D matx = GetTotalTransform(viewport);
            Point3D pointTransformed = matx.Transform(point);
            Point pt = new Point(pointTransformed.X, pointTransformed.Y);
            return pt;
        }
        #endregion

        public void AlphaSort(PerspectiveCamera camera)
        {
            if (_group == null || _group.Children.Count < 2)
                return;

            if (!_opacity)
                return;

            List<ModelDistance> list = new List<ModelDistance>();

            foreach (Model3D model in _group.Children)
            {
                // special test for ground plane, so that it is transparent from below
                if (model == _ground_model)
                {
                    // get the model points projected into 2d space so that we can check for backface cull
                    var geo = model as GeometryModel3D;
                    var mesh = geo.Geometry as MeshGeometry3D;
                    var p1 = Point3DtoPoint2D( view, mesh.Positions[4]);
                    var p2 = Point3DtoPoint2D( view, mesh.Positions[5]);
                    var p3 = Point3DtoPoint2D( view, mesh.Positions[6]);
                    var e1 = p3 - p1;
                    var e2 = p3 - p2;
                    var normal = Vector.CrossProduct(e1, e2);
                    
                    // if the surface normal points at negative z, then we're below it
                    list.Add(new ModelDistance(normal < 0.0 ? double.MaxValue : double.MinValue, model));
                    continue;
                }

                var geoModel = model as GeometryModel3D;
                // put solid objects in back, so we always see them through everything else
                if( geoModel != null)
                {
                    var mat = geoModel.Material as DiffuseMaterial;
                    if( mat != null)
                    {
                        var brush = mat.Brush as SolidColorBrush;
                        if (brush != null && brush.Color.A == 0xFF)
                        {
                            list.Add(new ModelDistance(double.PositiveInfinity, model));
                            continue;
                        }
                    }
                }

                // sort transparent objects according to their actual positions
                double distance = (Point3D.Subtract(camera.Position, camera.Transform.Inverse.Transform(model.Bounds.Location))).Length;
                list.Add(new ModelDistance(distance, model));
            }
            var comparer = new DistanceComparer(SortDirection.FarToNear);
            list.Sort(comparer);

            var models = new Model3DCollection(list.Select(m => m.model));
            _group.Children = models;
        }
        
        public static Point3D GetCenter(Rect3D box)
        {
            return new Point3D(box.X + box.SizeX / 2, box.Y + box.SizeY / 2, box.Z + box.SizeZ / 2);
        }

        private class ModelDistance
        {
            public ModelDistance(double distance, Model3D model)
            {
                this.distance = distance;
                this.model = model;
            }

            public double distance;
            public Model3D model;
        }

        private enum SortDirection
        {
            NearToFar,
            FarToNear
        }

        private class DistanceComparer : IComparer<ModelDistance>
        {
            public DistanceComparer(SortDirection sortDirection)
            {
                _sortDirection = sortDirection;
            }
            int IComparer<ModelDistance>.Compare(ModelDistance x, ModelDistance y)
            {
                double x1 = x.distance;
                double x2 = y.distance;
                return _sortDirection == SortDirection.NearToFar ? x1.CompareTo(x2) : x2.CompareTo(x1);
            }
            private SortDirection _sortDirection;

        }        
        #endregion
        */
    }
}
