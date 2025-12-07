using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace HelpImagesGen
{
    class Program
    {
        static void Main(string[] args)
        {
            // Caminhos absolutos
            string baseDir = AppContext.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            string outputDir = Path.Combine(projectRoot, "src", "CoinCraft.App", "Images", "Help");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                Console.WriteLine($"Created directory: {outputDir}");
            }

            var sections = new[]
            {
                (1, "Visão Geral", Color.LightBlue),
                (2, "Dashboard", Color.LightGreen),
                (3, "Lançamentos", Color.LightYellow),
                (4, "Contas", Color.LightCoral),
                (5, "Categorias", Color.Plum),
                (6, "Recorrentes", Color.PeachPuff),
                (7, "Importação", Color.LightSkyBlue),
                (8, "Exemplos", Color.LightGoldenrodYellow),
                (9, "FAQ", Color.LightGray),
                (10, "Glossário", Color.Lavender)
            };

            int width = 800;
            int height = 400;

            foreach (var (id, title, color) in sections)
            {
                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.White);

                    // Background with color
                    using (Brush bgBrush = new SolidBrush(color))
                    {
                        g.FillRectangle(bgBrush, 0, 0, width, height);
                    }

                    // Border
                    using (Pen pen = new Pen(Color.DarkGray, 4))
                    {
                        g.DrawRectangle(pen, 2, 2, width - 4, height - 4);
                    }

                    // Title
                    using (Font font = new Font("Segoe UI", 48, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (Brush textBrush = new SolidBrush(Color.Black))
                    {
                        StringFormat sf = new StringFormat();
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString(title, font, textBrush, new RectangleF(0, 0, width, height), sf);
                    }

                    // Subtitle (Simulating content)
                    using (Font font = new Font("Segoe UI", 24, FontStyle.Italic, GraphicsUnit.Pixel))
                    using (Brush textBrush = new SolidBrush(Color.DarkSlateGray))
                    {
                        StringFormat sf = new StringFormat();
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Near;
                        g.DrawString($"(Imagem Ilustrativa - Seção {id})", font, textBrush, new RectangleF(0, height - 50, width, 50), sf);
                    }

                    string fileName = $"section{id}.png";
                    string fullPath = Path.Combine(outputDir, fileName);
                    bmp.Save(fullPath, ImageFormat.Png);
                    Console.WriteLine($"Generated: {fullPath}");
                }
            }
        }
    }
}