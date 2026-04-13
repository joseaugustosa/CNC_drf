using CNC_Drf.Core;

namespace CNC_Drf.Views;

public partial class VisualizerPanel : UserControl
{
    public VisualizerPanel() => InitializeComponent();

    public void Render(GCodeParser parser)
    {
        var rapid = new Point3DCollection();
        var cut   = new Point3DCollection();
        var pts   = parser.Points;
        var lines = parser.Lines.Where(l => l.IsMove).ToList();

        for (int i = 1; i < lines.Count && i < pts.Count; i++)
        {
            var p0 = pts[i - 1]; var p1 = pts[i];
            if (lines[i].IsRapid) { rapid.Add(p0); rapid.Add(p1); }
            else                  { cut.Add(p0);   cut.Add(p1); }
        }

        PathRapid.Points = rapid;
        PathCut.Points   = cut;
        Viewport.ZoomExtents(animationTime: 400);
    }

    public void SetToolPosition(Point3D pos)
    {
        ToolMarker.Center  = pos;
        ToolMarker.Visible = true;
    }

    // ── Zoom ──────────────────────────────────────────────────────────────
    private void BtnZoomIn_Click(object sender, RoutedEventArgs e)  => ApplyZoom(1.3);
    private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => ApplyZoom(1.0 / 1.3);
    private void BtnZoom1_Click(object sender, RoutedEventArgs e)   => ResetCamera();
    private void BtnZoomFit_Click(object sender, RoutedEventArgs e) => Viewport.ZoomExtents(400);

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        ApplyZoom(e.Delta > 0 ? 1.12 : 1.0 / 1.12);
        e.Handled = true;
    }

    // ── Vistas ────────────────────────────────────────────────────────────
    private void BtnViewXY_Click(object sender, RoutedEventArgs e)
        => SetCamera(new Point3D(0, 0, 400), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
    private void BtnViewXZ_Click(object sender, RoutedEventArgs e)
        => SetCamera(new Point3D(0, -400, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1));
    private void BtnViewZY_Click(object sender, RoutedEventArgs e)
        => SetCamera(new Point3D(400, 0, 0), new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1));
    private void BtnView3D_Click(object sender, RoutedEventArgs e)
        => SetCamera(new Point3D(250, -250, 200), new Vector3D(-1, 1, -0.8), new Vector3D(0, 0, 1));

    private void SetCamera(Point3D pos, Vector3D look, Vector3D up)
    {
        if (Viewport.Camera is PerspectiveCamera pc)
        { pc.Position = pos; pc.LookDirection = look; pc.UpDirection = up; }
    }

    private void ResetCamera()
        => SetCamera(new Point3D(0, 0, 300), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));

    private void ApplyZoom(double factor)
    {
        if (Viewport.Camera is PerspectiveCamera pc)
        {
            var target  = pc.Position + pc.LookDirection;
            var newLook = pc.LookDirection / factor;
            if (newLook.LengthSquared < 1e-6) return;
            pc.LookDirection = newLook;
            pc.Position      = target - newLook;
        }
    }
}
