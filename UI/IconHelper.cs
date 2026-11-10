using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace FreeRdpWrapper.UI
{
    public static class IconHelper
    {
        public static void EnsureAppIconExists(string iconPath)
        {
            if (File.Exists(iconPath)) return;

            string? dir = Path.GetDirectoryName(iconPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write((short)0); // Reserved
            bw.Write((short)1); // ICO type
            bw.Write((short)sizes.Length);

            int offset = 6 + (16 * sizes.Length);
            var pngStreams = new MemoryStream[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];
                using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    int pad = Math.Max(1, size / 16);
                    int rWidth = size - (pad * 2);
                    int rHeight = (int)(rWidth * 0.72f);
                    int cornerR = Math.Max(2, size / 10);

                    var rect = new Rectangle(pad, pad, rWidth, rHeight);
                    using var path = CreateRoundedRect(rect, cornerR);
                    using var bodyBrush = new LinearGradientBrush(rect, Color.FromArgb(32, 38, 50), Color.FromArgb(16, 20, 28), LinearGradientMode.Vertical);
                    g.FillPath(bodyBrush, path);

                    using var borderPen = new Pen(Color.FromArgb(0, 150, 255), Math.Max(1, size / 28f));
                    g.DrawPath(borderPen, path);

                    // Screen display inside
                    int sPad = Math.Max(2, size / 10);
                    var screenRect = new Rectangle(pad + sPad, pad + sPad, rWidth - (sPad * 2), rHeight - (sPad * 2));
                    using var screenBrush = new SolidBrush(Color.FromArgb(10, 14, 22));
                    g.FillRectangle(screenBrush, screenRect);

                    // Lightning / remote signal
                    using var glowPen = new Pen(Color.FromArgb(0, 210, 255), Math.Max(1.5f, size / 14f));
                    glowPen.StartCap = LineCap.Round;
                    glowPen.EndCap = LineCap.Round;

                    float cx = screenRect.Left + screenRect.Width / 2f;
                    float cy = screenRect.Top + screenRect.Height / 2f;
                    float sw = screenRect.Width * 0.25f;
                    float sh = screenRect.Height * 0.32f;

                    PointF[] bolt = new PointF[] {
                        new PointF(cx + sw * 0.35f, cy - sh),
                        new PointF(cx - sw * 0.35f, cy + sh * 0.1f),
                        new PointF(cx + sw * 0.1f, cy + sh * 0.1f),
                        new PointF(cx - sw * 0.35f, cy + sh)
                    };
                    g.DrawLines(glowPen, bolt);

                    // Stand & Base
                    int standW = Math.Max(2, size / 8);
                    int standH = Math.Max(2, size / 8);
                    int standX = (size - standW) / 2;
                    int standY = pad + rHeight;
                    using var standBrush = new SolidBrush(Color.FromArgb(80, 90, 105));
                    g.FillRectangle(standBrush, standX, standY, standW, standH);

                    int baseW = rWidth / 2;
                    int baseX = (size - baseW) / 2;
                    int baseY = standY + standH;
                    int baseH = Math.Max(2, size / 18);
                    g.FillRectangle(standBrush, baseX, baseY, baseW, baseH);
                }

                var pngMs = new MemoryStream();
                bmp.Save(pngMs, ImageFormat.Png);
                pngStreams[i] = pngMs;

                byte w = size >= 256 ? (byte)0 : (byte)size;
                byte h = size >= 256 ? (byte)0 : (byte)size;

                bw.Write(w);
                bw.Write(h);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write((int)pngMs.Length);
                bw.Write(offset);

                offset += (int)pngMs.Length;
            }

            for (int i = 0; i < sizes.Length; i++)
            {
                bw.Write(pngStreams[i].ToArray());
                pngStreams[i].Dispose();
            }

            File.WriteAllBytes(iconPath, ms.ToArray());
        }

        public static Icon CreateAppIcon()
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
            try
            {
                EnsureAppIconExists(iconPath);
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch { }

            // Fallback runtime icon
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(0, 120, 215));
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
