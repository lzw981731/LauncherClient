// 此文件为固定编译时间戳（云编译环境下的静态替代）。
// 原项目用 PreBuild 目标动态生成，但 glob 评估早于 PreBuild 执行，
// 全新检出时文件不会被编入，导致 CS0103。改为静态文件提交。
namespace KartRider
{
    public static class CompileTime
    {
        public static readonly string Time = "260921";
    }
}