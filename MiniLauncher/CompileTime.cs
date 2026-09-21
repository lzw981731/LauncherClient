// 编译时间戳（本地编译时自动取当天日期，格式 YYMMDD，与原版一致）
using System;

namespace KartRider
{
    public static class CompileTime
    {
        public static readonly string Time = DateTime.Now.ToString("yyMMdd");
    }
}
