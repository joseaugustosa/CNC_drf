using System.Windows.Controls.Primitives;
using CNC_Drf.Core;

namespace CNC_Drf.Views;

public partial class VisualizerPanel : UserControl
{
    public VisualizerPanel() => InitializeComponent();

    public void Render(GCodeParser parser)
    {
        var rapidPts = new Point3DCollection();
        var cutPts   = new Point3DCollection();

        var pts = parser.Points;
        var lines = parser.Lines.Where(l => l.IsMove).ToList();

        for (int i = 1; i < lines.Count; i++)
        {
            if (pts.Count <= i) break;
            var p0 = pts[i - 1];
            var p1 = pts[i];
            if (lines[i].IsRapid) { rapidPts.Add(p0); rapidPts.Add(p1); }
            else                  { cutPts.Add(p0);   cutPts.Add(p1); }
        }

        PathRapid.Points = rapidPts;
        PathCut.Points   = cutPts;
        Viewport.ZoomExtents(animationTime: 500);
    }

    public void SetToolPosition(Point3D pos)
    {
        ToolMarker.Center  = pos;
        ToolMarker.Visible = true;
    }

    private void BtnFit_Click(object sender, RoutedEventArgs e)
        => Viewport.ZoomExtents(animationTime: 400);

    private void BtnZoomIn_Click(object sender, RoutedEventArgs e)  => ApplyZoom(1.25);
    private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => ApplyZoom(1.0 / 1.25);

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        ApplyZoom(e.Delta > 0 ? 1.12 : 1.0 / 1.12);
        e.Handled = true;
    }

    private void BtnPlane_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton tb) return;
        BtnXY.IsChecked = false; BtnXZ.IsChecked = false; BtnZY.IsChecked = false;
        tb.IsChecked = true;

        switch (tb.Name)
        {
            case "BtnXY":
                SetCamera(new Point3D(0, 0, 300), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
                break;
            case "BtnXZ":
                SetCamera(new Point3D(0, -300, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1));
                break;
            case "BtnZY":
                SetCamera(new Point3D(300, 0, 0), new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1));
                break;
        }
    }

    private void SetCamera(Point3D pos, Vector3D look, Vector3D up)
    {
        if (Viewport.Camera is PerspectiveCamera pc)
        {
            pc.Position      = pos;
            pc.LookDirection = look;
            pc.UpDirection   = up;
        }
    }

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
