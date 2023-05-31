using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using MineCosmos.Bot.Entity;
using MineCosmos.Bot.Service;

namespace MineCosmos.Bot.Interactive
{
    internal class GroupFunction
    {

        public static async Task JoinTask(TaskQueueEntity model)
        {
            await DbContext.Db.Insertable(model).ExecuteCommandAsync();
        }


        public static string DownloadFilePath = "head_image";

        public static WeightedItem GetItem()
        {
            // 创建一个包含权重设定的列表
            List<WeightedItem> items = new List<WeightedItem>
        {
            //普通物品
            new WeightedItem { Name = "石头", Weight = 10 },
            new WeightedItem { Name = "下界岩", Weight = 9 },
            new WeightedItem { Name = "花岗岩", Weight = 9 },
            new WeightedItem { Name = "闪长岩", Weight = 8 },
            new WeightedItem { Name = "橡木", Weight = 8 },
            //中等
            new WeightedItem { Name = "方解石", Weight = 7 },
            new WeightedItem { Name = "滴水石块", Weight = 7 },
            new WeightedItem { Name = "南瓜", Weight = 7 },
            new WeightedItem { Name = "西瓜", Weight = 7 },    
            //不是很普通
            new WeightedItem { Name = "藤蔓", Weight = 6 },
            new WeightedItem { Name = "岩浆", Weight = 6 },
            new WeightedItem { Name = "水", Weight = 6 },
            new WeightedItem { Name = "雪块", Weight = 6 },
            new WeightedItem { Name = "雪球", Weight = 6 },
            new WeightedItem { Name = "可可豆", Weight = 6 },
            //普通稀有
            new WeightedItem { Name = "钻石", Weight = 5 },
            new WeightedItem { Name = "下界合金", Weight = 3 },
            //贼稀有
            new WeightedItem { Name = "苦力怕的头", Weight = 2 },
            new WeightedItem { Name = "骷髅射手头", Weight = 2 },
            new WeightedItem { Name = "骷髅射手头", Weight = 2 },
            new WeightedItem { Name = "凋零骷髅头", Weight = 2 },
            //传说级别
            new WeightedItem { Name = "信标", Weight = 1 },
            new WeightedItem { Name = "鞘翅", Weight = 1 },
            new WeightedItem { Name = "末影龙头", Weight = 1 },
            new WeightedItem { Name = "末影龙爪", Weight = 1 }
        };

            // 创建一个加权随机抽样选择器
            WeightedItemSelector selector = new WeightedItemSelector(items);
            // 调用方法获取随机结果
            WeightedItem result = selector.GetRandomWeightedItem();
            return result;
            // 输出结果
            //Console.WriteLine("Random item: " + result.Name);
        }

        public static async Task<string> DownloadQQImage(string qq)
        {
            string qqUrl = $"http://q1.qlogo.cn/g?b=qq&nk={qq}&s=100";
            //获取当前路径
            string currentPath = Path.Combine(Directory.GetCurrentDirectory(), DownloadFilePath);

            if (!Directory.Exists(currentPath))
            {
                Directory.CreateDirectory(currentPath);
            }



            // 获取文件类型
            string extension = "";
            //using (var client = new FlurlClient(qqUrl))
            using (var stream = await qqUrl.GetStreamAsync())
            using (var memStream = new MemoryStream())
            {
                await stream.CopyToAsync(memStream);
                using (var img = Image.FromStream(memStream))
                {
                    ImageFormat format = img.RawFormat;
                    if (format.Equals(ImageFormat.Gif))
                    {
                        extension = ".gif";
                    }
                    else if (format.Equals(ImageFormat.Jpeg))
                    {
                        extension = ".jpg";
                    }
                    else if (format.Equals(ImageFormat.Png))
                    {
                        extension = ".png";
                    }
                    else if (format.Equals(ImageFormat.Bmp))
                    {
                        extension = ".bmp";
                    }
                    else if (format.Equals(ImageFormat.Icon))
                    {
                        extension = ".ico";
                    }
                }
            }

            // 下载文件并保存到本地
            string fileName = $"{qq}{extension}";
            string filePath = Path.Combine(DownloadFilePath, fileName);
            await qqUrl.DownloadFileAsync(DownloadFilePath, fileName);
            return filePath;
        }
    }
}
