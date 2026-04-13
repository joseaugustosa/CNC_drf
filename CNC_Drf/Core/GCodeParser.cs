using System.Text.RegularExpressions;

namespace CNC_Drf.Core;

public record GCodeLine(int Number, string Raw, bool IsComment, bool IsRapid, bool IsMove);

public record GCodeBounds(double XMin, double XMax, double YMin, double YMax, double ZMin, double ZMax);

public class GCodeParser
{
    public List<GCodeLine> Lines { get; private set; } = [];
    public List<Point3D>   Points { get; private set; } = [];
    public GCodeBounds     Bounds { get; private set; } = new(0, 0, 0, 0, 0, 0);

    private static readonly Regex _reWord = new(@"([A-Za-z])\s*(-?[\d.]+)", RegexOptions.Compiled);

    public void Load(string[] rawLines)
    {
        Lines  = [];
        Points = [];

        double x = 0, y = 0, z = 0;
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        double zMin = double.MaxValue, zMax = double.MinValue;
        bool absMode = true;
        bool rapid = false;

        for (int i = 0; i < rawLines.Length; i++)
        {
            var raw = rawLines[i].Trim();
            if (raw.Length == 0) continue;

            bool isComment = raw.StartsWith(';') || raw.StartsWith('(');
            bool isMove = false;

            if (!isComment)
            {
                var words = _reWord.Matches(raw);
                bool hasXYZ = false;
                double nx = x, ny = y, nz = z;

                foreach (Match m in words)
                {
                    char letter = char.ToUpper(m.Groups[1].Value[0]);
                    double val  = double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

                    switch (letter)
                    {
                        case 'G':
                            if (val == 0)       rapid = true;
                            else if (val == 1)  rapid = false;
                            else if (val == 90) absMode = true;
                            else if (val == 91) absMode = false;
                            break;
                        case 'X': nx = absMode ? val : x + val; hasXYZ = true; break;
                        case 'Y': ny = absMode ? val : y + val; hasXYZ = true; break;
                        case 'Z': nz = absMode ? val : z + val; hasXYZ = true; break;
                    }
                }

                if (hasXYZ)
                {
                    x = nx; y = ny; z = nz;
                    Points.Add(new Point3D(x, y, z));
                    isMove = true;
                    if (x < xMin) xMin = x; if (x > xMax) xMax = x;
                    if (y < yMin) yMin = y; if (y > yMax) yMax = y;
                    if (z < zMin) zMin = z; if (z > zMax) zMax = z;
                }
            }

            Lines.Add(new GCodeLine(i + 1, raw, isComment, rapid, isMove));
        }

        Bounds = Points.Count > 0
            ? new GCodeBounds(xMin, xMax, yMin, yMax, zMin, zMax)
            : new GCodeBounds(0, 0, 0, 0, 0, 0);
    }
}
