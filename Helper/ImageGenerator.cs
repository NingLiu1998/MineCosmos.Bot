using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineCosmos.Bot.Helper
{
    public static class ImageGenerator
    {
        public static Stream GenerateImageToStream(string text, int marginTop = 10, int marginBottom = 5, int marginLeft = 10, int marginRight = 10)
        {
            Font font = new Font("LXGW WenKai GB Screen", 18, GraphicsUnit.Pixel);
            int fontHeight = font.Height;

            int width, height;
            CalculateImageSize(text, font, marginTop, marginBottom, marginLeft, marginRight, out width, out height);

            Bitmap bitmap = CreateBitmap(width, height);

            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            DrawImage(graphics, text, font, marginTop, marginBottom, marginLeft, marginRight, width, height);

            MemoryStream stream = new MemoryStream();
            SaveBitmapToStream(bitmap, stream);

            return stream;
        }

        /// <summary>
        /// 计算图片大小
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="marginTop"></param>
        /// <param name="marginBottom"></param>
        /// <param name="marginLeft"></param>
        /// <param name="marginRight"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private static void CalculateImageSize(string text, Font font, int marginTop, int marginBottom, int marginLeft, int marginRight, out int width, out int height)
        {
            Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);

            StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.MeasureTrailingSpaces
            };

            if (text.Contains("\r\n"))
            {
                format.FormatFlags |= StringFormatFlags.LineLimit;
                format.Trimming = StringTrimming.Word;
                string[] lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                width = 0;
                height = 0;
                foreach (string line in lines)
                {
                    SizeF lineSize = graphics.MeasureString(line, font, int.MaxValue, format);
                    width = Math.Max(width, (int)lineSize.Width);
                    height += ((int)lineSize.Height + (height / 2));
                }
                height += marginTop + marginBottom + (lines.Length - 1) * 10;
            }
            else
            {
                SizeF textSize = graphics.MeasureString(text, font, int.MaxValue, format);
                width = (int)textSize.Width;
                height = (int)textSize.Height;
            }
            width += marginLeft + marginRight;
            height += marginTop + marginBottom;
        }

        /// <summary>
        /// 创建位图
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Bitmap CreateBitmap(int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            bitmap.MakeTransparent();
            bitmap.SetResolution(300, 300);
            return bitmap;
        }

        /// <summary>
        /// 绘制图片
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="marginTop"></param>
        /// <param name="marginBottom"></param>
        /// <param name="marginLeft"></param>
        /// <param name="marginRight"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private static void DrawImage(Graphics graphics, string text, Font font, int marginTop, int marginBottom, int marginLeft, int marginRight, int width, int height)
        {
            Color textColor = ColorTranslator.FromHtml("#fcfcfc");
            Brush textBrush = new SolidBrush(textColor);

            //圆角背景
            Color bgcColor = ColorTranslator.FromHtml("#1f1b1d");
            Brush bgcBrush = new SolidBrush(bgcColor);

            graphics.Clear(Color.Transparent);

            #region 圆角绘制
            GraphicsPath path = new GraphicsPath();
            int radius = 20; // 圆角半径
            Rectangle rect = new Rectangle(0, 0, width, height);
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            graphics.FillPath(bgcBrush, path);
            #endregion

            StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.FitBlackBox
            };

            if (text.Contains("\r\n"))
            {
                format.FormatFlags |= StringFormatFlags.LineLimit;
                format.Trimming = StringTrimming.Word;
                string[] lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                float y = marginTop;
                foreach (string line in lines)
                {
                    SizeF lineSize = graphics.MeasureString(line, font, int.MaxValue, format);
                    graphics.DrawString(line, font, textBrush, new RectangleF(marginLeft, y, width - marginLeft - marginRight, lineSize.Height), format);
                    y += lineSize.Height + 10;
                }
            }
            else
            {
                graphics.DrawString(text, font, textBrush, new RectangleF(marginLeft, marginTop, width - marginLeft - marginRight, height - marginTop - marginBottom), format);
            }
        }

        /// <summary>
        /// 保存位图成流
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="stream"></param>
        private static void SaveBitmapToStream(Bitmap bitmap, Stream stream)
        {
            bitmap.Save(stream, ImageFormat.Png);
        }

        private static string SaveBitmapToFile(Bitmap bitmap, Stream stream)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Guid.NewGuid()}.png");
            bitmap.Save(path, ImageFormat.Png);
            return path;
        }
    }

}
