using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Launcher.Device.Data;

namespace EspController.Controls;

public partial class DiceView : UserControl
{
    internal const int AnimationDurationMs = 2100;
    public static readonly DependencyProperty RollProperty = DependencyProperty.Register(
        nameof(Roll), typeof(DeviceDiceRoll), typeof(DiceView), new PropertyMetadata(null, RollChanged));
    private readonly AxisAngleRotation3D _x = new(new Vector3D(1, 0, 0), 0);
    private readonly AxisAngleRotation3D _y = new(new Vector3D(0, 1, 0), 0);
    private readonly AxisAngleRotation3D _z = new(new Vector3D(0, 0, 1), 0);
    private readonly TranslateTransform3D _lift = new(0, -0.25, 0);
    private DeviceDiceRoll? _pending, _displayed;
    private bool _rolling;
    private int _generation;

    public DeviceDiceRoll? Roll
    {
        get => (DeviceDiceRoll?)GetValue(RollProperty);
        set => SetValue(RollProperty, value);
    }

    public DiceView()
    {
        InitializeComponent();
        BuildScene();
        Loaded += (_, _) => { if (Roll is { } roll && roll != _displayed) QueueRoll(roll); };
        Unloaded += (_, _) => Reset();
    }

    private static void RollChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var view = (DiceView)sender;
        if (args.NewValue is not DeviceDiceRoll roll) view.Reset();
        else if (view.IsLoaded) view.QueueRoll(roll);
    }

    private void QueueRoll(DeviceDiceRoll roll)
    {
        if (roll.Value is < 1 or > 6 || roll == _displayed || roll == _pending) return;
        // A reconnect can occur late in the ESP cycle. Finish the current toss,
        // then animate the latest pending result instead of overlapping clocks.
        if (_rolling) { _pending = roll; return; }
        AnimateRoll(roll);
    }

    private void AnimateRoll(DeviceDiceRoll roll)
    {
        _rolling = true;
        _displayed = roll;
        int generation = ++_generation;
        ResultText.Text = "…";
        StatusText.Text = "Der Würfel fliegt …";
        SequenceText.Text = $"Wurf #{roll.Sequence}";
        var (targetX, targetY) = FaceAngles(roll.Value);
        var duration = TimeSpan.FromMilliseconds(AnimationDurationMs);
        Spin(_x, targetX + 720, duration);
        Spin(_y, targetY + 1080, duration);
        Spin(_z, 360, duration);

        var bounce = new DoubleAnimationUsingKeyFrames { Duration = duration };
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(-0.25, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1.5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420)), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(-0.25, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1150)), new QuadraticEase { EasingMode = EasingMode.EaseIn }));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(0.2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1400)), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(-0.25, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1650)), new QuadraticEase { EasingMode = EasingMode.EaseIn }));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(-0.13, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1780))));
        bounce.KeyFrames.Add(new EasingDoubleKeyFrame(-0.25, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1950))));
        bounce.Completed += (_, _) =>
        {
            if (generation != _generation || !IsLoaded) return;
            _rolling = false;
            ResultText.Text = roll.Value.ToString();
            StatusText.Text = "Augenzahl bestätigt";
            SequenceText.Text = $"Wurf #{roll.Sequence} · {roll.Value} LEDs an";
            AutomationProperties.SetName(this, $"ESP-Würfel: {roll.Value}, Wurf {roll.Sequence}");
            if (_pending is { } next) { _pending = null; AnimateRoll(next); }
        };
        _lift.BeginAnimation(TranslateTransform3D.OffsetYProperty, bounce);
        AnimateShadow(ShadowScale, ScaleTransform.ScaleXProperty, 1, 0.48, duration);
        AnimateShadow(ShadowScale, ScaleTransform.ScaleYProperty, 1, 0.7, duration);
        AnimateShadow(Shadow, OpacityProperty, 0.45, 0.15, duration);
    }

    internal static (double X, double Y) FaceAngles(byte face) => face switch
    {
        1 => (0, 0), 2 => (90, 0), 3 => (0, -90),
        4 => (0, 90), 5 => (-90, 0), 6 => (0, 180),
        _ => throw new ArgumentOutOfRangeException(nameof(face))
    };

    private static void Spin(AxisAngleRotation3D axis, double target, TimeSpan duration)
    {
        double start = axis.Angle % 360;
        axis.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);
        axis.Angle = target;
        axis.BeginAnimation(AxisAngleRotation3D.AngleProperty,
            new DoubleAnimation(start, target, duration) { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
    }

    private static void AnimateShadow(IAnimatable target, DependencyProperty property, double rest, double flight, TimeSpan duration)
    {
        var animation = new DoubleAnimationUsingKeyFrames { Duration = duration };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(rest, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(flight, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(rest, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1150))));
        target.BeginAnimation(property, animation);
    }

    private void Reset()
    {
        ++_generation;
        _rolling = false;
        _pending = _displayed = null;
        foreach (var axis in new[] { _x, _y, _z }) { axis.BeginAnimation(AxisAngleRotation3D.AngleProperty, null); axis.Angle = 0; }
        _lift.BeginAnimation(TranslateTransform3D.OffsetYProperty, null);
        Shadow.BeginAnimation(OpacityProperty, null);
        ShadowScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        ShadowScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        ResultText.Text = "—";
        StatusText.Text = "Warte auf ESP …";
        SequenceText.Text = "Ergebnis kommt von der Platine";
        AutomationProperties.SetName(this, "ESP-Würfel: warte auf Ergebnis");
    }

    private void BuildScene()
    {
        Scene.Camera = new PerspectiveCamera(new Point3D(0, 1.3, 10), new Vector3D(0, -1.3, -10), new Vector3D(0, 1, 0), 38);
        var scene = new Model3DGroup();
        scene.Children.Add(new AmbientLight(Color.FromRgb(150, 162, 182)));
        scene.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-2, -3, -4)));
        scene.Children.Add(new DirectionalLight(Color.FromRgb(110, 220, 207), new Vector3D(2, 0, -1)));
        var cube = new Model3DGroup();
        AddFace(cube, 1, new(0, 0, 1), new(1, 0, 0), new(0, 1, 0));
        AddFace(cube, 6, new(0, 0, -1), new(-1, 0, 0), new(0, 1, 0));
        AddFace(cube, 3, new(1, 0, 0), new(0, 0, -1), new(0, 1, 0));
        AddFace(cube, 4, new(-1, 0, 0), new(0, 0, 1), new(0, 1, 0));
        AddFace(cube, 2, new(0, 1, 0), new(1, 0, 0), new(0, 0, -1));
        AddFace(cube, 5, new(0, -1, 0), new(1, 0, 0), new(0, 0, 1));
        var transforms = new Transform3DGroup();
        transforms.Children.Add(new RotateTransform3D(_x));
        transforms.Children.Add(new RotateTransform3D(_y));
        transforms.Children.Add(new RotateTransform3D(_z));
        transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new(1, 0, 0), 12)));
        transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new(0, 1, 0), -22)));
        transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new(0, 0, 1), -8)));
        transforms.Children.Add(_lift);
        cube.Transform = transforms;
        scene.Children.Add(cube);
        Scene.Children.Add(new ModelVisual3D { Content = scene });
    }

    private static void AddFace(Model3DGroup cube, int value, Vector3D normal, Vector3D u, Vector3D v)
    {
        var centre = new Point3D(normal.X, normal.Y, normal.Z);
        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection { centre - u - v, centre + u - v, centre + u + v, centre - u + v },
            TextureCoordinates = new PointCollection { new(0, 1), new(1, 1), new(1, 0), new(0, 0) },
            TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 },
            Normals = new Vector3DCollection { normal, normal, normal, normal }
        };
        var drawing = new DrawingGroup();
        using (DrawingContext context = drawing.Open())
        {
            context.DrawRectangle(new SolidColorBrush(Color.FromRgb(232, 241, 249)), null, new Rect(0, 0, 160, 160));
            context.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(246, 250, 255)), new Pen(new SolidColorBrush(Color.FromRgb(191, 209, 224)), 3), new Rect(5, 5, 150, 150), 18, 18);
            var pip = new SolidColorBrush(Color.FromRgb(23, 43, 58));
            foreach (Point point in PipPositions(value)) context.DrawEllipse(pip, null, point, 12, 12);
        }
        drawing.Freeze();
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(new DrawingBrush(drawing)));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(75, 255, 255, 255)), 45));
        material.Freeze(); mesh.Freeze();
        cube.Children.Add(new GeometryModel3D(mesh, material));
    }

    internal static IEnumerable<Point> PipPositions(int value)
    {
        if (value is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(value));
        if (value % 2 == 1) yield return new(80, 80);
        if (value >= 2) { yield return new(40, 40); yield return new(120, 120); }
        if (value >= 4) { yield return new(120, 40); yield return new(40, 120); }
        if (value == 6) { yield return new(40, 80); yield return new(120, 80); }
    }
}
