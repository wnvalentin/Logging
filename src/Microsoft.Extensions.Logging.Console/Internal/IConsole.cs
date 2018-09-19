// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Console.Internal
{
    public interface IConsole
    {
        void Write(string message, ConsoleColor? background, ConsoleColor? foreground);

        void WriteLine(string message, ConsoleColor? background, ConsoleColor? foreground);

        /// <summary>
        /// 采用缓冲机制，通过Write或者WriteLine方法写入的消息并不会立即输出到控制台，而是先被保存到缓冲区，Flush方法被执行的时候会将缓冲区的所有日志消息批量输出到控制台上。
        /// </summary>
        void Flush();
    }
}