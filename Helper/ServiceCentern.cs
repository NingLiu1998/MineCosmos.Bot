using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kook.Commands;
using MineCosmos.Bot.Service.Bot;
using MineCosmos.Bot.Service.Common;

namespace MineCosmos.Bot.Helper
{
    internal static class ServiceCentern
    {
        //private readonly ICommandManagerService _commandManagerService;

        public static ICommonService commonService;
        //public ServiceCentern(ICommandManagerService commandManagerService, ICommonService commonService)
        //{
        //    _commandManagerService = commandManagerService;
        //    _commonService = commonService;
        //}

        //public Stream GenerateImageToStream()
        //{
        //    return _commonService.GenerateImageToStream("测试图片");
        //}
    }
}
