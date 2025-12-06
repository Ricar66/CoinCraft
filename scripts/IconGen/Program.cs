using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IconGen
{
    class Program
    {
        static void Main(string[] args)
        {
            // Aumentando a resolução para 512x512
            int size = 512;
            // Caminhos absolutos
            string baseDir = AppContext.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            
            string outputPath = Path.Combine(projectRoot, "src", "CoinCraft.App", "coincraft.ico");
            string installerIconPath = Path.Combine(projectRoot, "installer", "coincraft.ico");
            
            Console.WriteLine($"Root: {projectRoot}");
            Console.WriteLine($"Output: {outputPath}");

            // Generate SVG
            string svgPath = Path.ChangeExtension(outputPath, ".svg");
            File.WriteAllText(svgPath, GenerateSvg());
            Console.WriteLine($"SVG icon created at: {svgPath}");

            // Generate Multi-size ICO
            // Sizes: 16, 32, 48, 64, 128, 256, 512
            int[] sizes = new[] { 16, 32, 48, 64, 128, 256, 512 };
            
            // We need to store image data and offsets to write the ICO file correctly
            var imagesData = new System.Collections.Generic.List<byte[]>();
            
            // Also save the largest PNG (512x512) for reference
            using (Bitmap largeBmp = new Bitmap(512, 512))
            using (Graphics g = Graphics.FromImage(largeBmp))
            {
                ConfigureGraphics(g);
                DrawIcon(g, 512);
                string pngPath = Path.ChangeExtension(outputPath, ".png");
                largeBmp.Save(pngPath, ImageFormat.Png);
                Console.WriteLine($"PNG icon created at: {pngPath}");
            }

            // Generate individual images for ICO
            foreach (int s in sizes)
            {
                using (Bitmap resized = new Bitmap(s, s))
                using (Graphics gr = Graphics.FromImage(resized))
                {
                    ConfigureGraphics(gr);
                    DrawIcon(gr, s);

                    using (var ms = new MemoryStream())
                    {
                        resized.Save(ms, ImageFormat.Png);
                        imagesData.Add(ms.ToArray());
                    }
                }
            }

            // Write ICO file
            using (var stream = new FileStream(outputPath, FileMode.Create))
            {
                // Header
                stream.Write(BitConverter.GetBytes((short)0), 0, 2); // Reserved
                stream.Write(BitConverter.GetBytes((short)1), 0, 2); // Type (1=ICO)
                stream.Write(BitConverter.GetBytes((short)sizes.Length), 0, 2); // Count

                int offset = 6 + (16 * sizes.Length);
                
                // Write Directory Entries
                for (int i = 0; i < sizes.Length; i++)
                {
                    int s = sizes[i];
                    byte[] data = imagesData[i];
                    
                    stream.WriteByte((byte)(s >= 256 ? 0 : s)); // Width
                    stream.WriteByte((byte)(s >= 256 ? 0 : s)); // Height
                    stream.WriteByte(0); // Color count
                    stream.WriteByte(0); // Reserved
                    stream.Write(BitConverter.GetBytes((short)1), 0, 2); // Planes
                    stream.Write(BitConverter.GetBytes((short)32), 0, 2); // BitCount
                    stream.Write(BitConverter.GetBytes(data.Length), 0, 4); // Size
                    stream.Write(BitConverter.GetBytes(offset), 0, 4); // Offset
                    
                    offset += data.Length;
                }

                // Write Image Data
                foreach (var data in imagesData)
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            
            File.Copy(outputPath, installerIconPath, true);
            Console.WriteLine($"ICO created at: {outputPath}");
            Console.WriteLine($"ICO copied to: {installerIconPath}");
        }

        static void ConfigureGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        }

        static void DrawIcon(Graphics g, int size)
        {
             // 1. Background (Gold Circle)
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(1, 1, size - 2, size - 2);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(255, 255, 240, 150); // #FFF096
                    brush.SurroundColors = new[] { Color.FromArgb(255, 218, 165, 32) }; // #DAA520
                    g.FillPath(brush, path);
                }
            }

            // 2. Border
            float borderWidth = Math.Max(1, size * 0.03f);
            using (Pen borderPen = new Pen(Color.FromArgb(184, 134, 11), borderWidth)) // #B8860B
            {
                g.DrawEllipse(borderPen, 1, 1, size - 2, size - 2);
            }
            
            // Inner highlight
            float highlightWidth = Math.Max(1, size * 0.01f);
            float margin = size * 0.08f;
            using (Pen highlightPen = new Pen(Color.FromArgb(100, 255, 255, 255), highlightWidth))
            {
                g.DrawEllipse(highlightPen, margin, margin, size - (margin * 2), size - (margin * 2));
            }

            // 3. Symbol ($)
            Color symbolColor = Color.FromArgb(0, 100, 0); // #006400
            float fontSize = size * 0.6f; 
            using (Font font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Brush textBrush = new SolidBrush(symbolColor))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                
                // Shadow
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.DrawString("$", font, shadowBrush, new RectangleF(size * 0.02f, size * 0.02f, size, size), sf);
                }
                
                g.DrawString("$", font, textBrush, new RectangleF(0, 0, size, size), sf);
            }
        }

        static string GenerateSvg()
        {
            return @"<svg width=""512"" height=""512"" viewBox=""0 0 512 512"" xmlns=""http://www.w3.org/2000/svg"">
  <defs>
    <radialGradient id=""goldGrad"" cx=""50%"" cy=""50%"" r=""50%"" fx=""50%"" fy=""50%"">
      <stop offset=""0%"" style=""stop-color:#FFF096;stop-opacity:1"" />
      <stop offset=""100%"" style=""stop-color:#DAA520;stop-opacity:1"" />
    </radialGradient>
  </defs>
  <circle cx=""256"" cy=""256"" r=""250"" fill=""url(#goldGrad)"" stroke=""#B8860B"" stroke-width=""16"" />
  <circle cx=""256"" cy=""256"" r=""210"" fill=""none"" stroke=""rgba(255,255,255,0.4)"" stroke-width=""4"" />
  <text x=""50%"" y=""50%"" font-family=""Segoe UI, Arial"" font-weight=""bold"" font-size=""300"" fill=""#006400"" text-anchor=""middle"" dy=""0.35em"" filter=""drop-shadow(4px 4px 4px rgba(0,0,0,0.2))"">$</text>
</svg>";
        }
    }
}