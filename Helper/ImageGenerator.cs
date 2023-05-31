using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineCosmos.Bot.Helper
{
    internal class ImageGenerator
    {
        public static string GenerateImage(string text)
        {
            // 设置字体和字号
            Font font = new Font("微软雅黑", 16);

            // 设置图片大小
            int width = 400;
            int height = 300;

            // 创建位图对象
            Bitmap bitmap = new Bitmap(width, height);

            // 创建绘图对象
            Graphics graphics = Graphics.FromImage(bitmap);

            // 设置背景色和前景色
            graphics.Clear(Color.White);
            graphics.DrawString(text, font, Brushes.Black, new RectangleF(0, 0, width, height), new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.FitBlackBox
            });

            // 保存图片到本地
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Guid.NewGuid()}.png");
            bitmap.Save(path, ImageFormat.Png);

            // 释放资源
            graphics.Dispose();
            bitmap.Dispose();

            // 返回图片路径
            return path;
        }
    }
}
