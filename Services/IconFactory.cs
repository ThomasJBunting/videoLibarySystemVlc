using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VideoLibrarySystemVlc.Services;

public static class IconFactory
{
    public static Icon CreateTrayIcon()
    {
        using var bitmap = CreateVhsBitmap(32);
        var handle = bitmap.GetHicon();
        using var icon = Icon.FromHandle(handle);
        var clone = (Icon)icon.Clone();
        DestroyIcon(handle);
        return clone;
    }

    public static ImageSource CreateWindowIcon()
    {
        using var bitmap = CreateVhsBitmap(256);
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    private static Bitmap CreateVhsBitmap(int size)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(System.Drawing.Color.Transparent);

        var body = new RectangleF(size * 0.08f, size * 0.28f, size * 0.84f, size * 0.44f);
        using var bodyBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(38, 38, 48));
        using var bodyPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(15, 15, 20), Math.Max(1, size * 0.03f));
        graphics.FillRoundedRectangle(bodyBrush, body, size * 0.10f);
        graphics.DrawRoundedRectangle(bodyPen, body, size * 0.10f);

        var reelRadius = size * 0.14f;
        using var reelBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(220, 220, 225));
        using var reelRingPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(90, 90, 100), Math.Max(1, size * 0.02f));

        DrawReel(graphics, reelBrush, reelRingPen, size * 0.32f, size * 0.48f, reelRadius);
        DrawReel(graphics, reelBrush, reelRingPen, size * 0.68f, size * 0.48f, reelRadius);

        using var labelBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(210, 210, 220));
        var labelRect = new RectangleF(size * 0.20f, size * 0.62f, size * 0.60f, size * 0.08f);
        graphics.FillRectangle(labelBrush, labelRect);

        using var accentBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(180, 50, 60));
        graphics.FillRectangle(accentBrush, size * 0.22f, size * 0.34f, size * 0.12f, size * 0.10f);

        return bitmap;
    }

    private static void DrawReel(Graphics graphics, System.Drawing.Brush brush, System.Drawing.Pen pen, float cx, float cy, float radius)
    {
        var rect = new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2);
        graphics.FillEllipse(brush, rect);
        graphics.DrawEllipse(pen, rect);
        using var innerBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 70, 70, 80));
        graphics.FillEllipse(innerBrush, cx - radius * 0.32f, cy - radius * 0.32f, radius * 0.64f, radius * 0.64f);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, System.Drawing.Brush brush, RectangleF bounds, float radius)
    {
        using var path = RoundedRect(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, System.Drawing.Pen pen, RectangleF bounds, float radius)
    {
        using var path = RoundedRect(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
