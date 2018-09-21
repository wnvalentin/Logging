using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// 为创建的ConsoleLogger指定配置的接口
    /// </summary>
    public interface IConsoleLoggerSettings
    {
        /// <summary>
        /// 是否将日志写入操作纳入当前上下文范围
        /// </summary>
        bool IncludeScopes { get; }

        /// <summary>
        /// 项应用通知配置源发生改变的令牌。用于当配置源（文件，数据库，环境变量等）发生变化时应用与配置源的同步。
        /// </summary>
        IChangeToken ChangeToken { get; }

        /// <summary>
        /// 根据日志类型获取日志等级（过滤条件/Switch = 开关），返回level。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        bool TryGetSwitch(string name, out LogLevel level);

        /// <summary>
        /// 配置源发生改变时，重新加载配置。
        /// 例如当配置文件有变时，可以自动重新加载该文件。
        /// </summary>
        /// <returns></returns>
        IConsoleLoggerSettings Reload();
    }
}
