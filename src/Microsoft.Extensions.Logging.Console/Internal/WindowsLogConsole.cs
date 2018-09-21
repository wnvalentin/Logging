// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Console.Internal
{
    /// <summary>
    /// 微软提供的针对Windows平台的 IConsole 实现。调用System.Console类打印输出。
    /// </summary>
    public class WindowsLogConsole : IConsole
    {
        private void SetColor(ConsoleColor? background, ConsoleColor? foreground)
        {
            if (background.HasValue)
            {
                System.Console.BackgroundColor = background.Value;
            }

            if (foreground.HasValue)
            {
                System.Console.ForegroundColor = foreground.Value;
            }
        }

        private void ResetColor()
        {
            System.Console.ResetColor();
        }

        public void Write(string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            SetColor(background, foreground);
            System.Console.Out.Write(message);
            ResetColor();
        }

        public void WriteLine(string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            SetColor(background, foreground);
            System.Console.Out.WriteLine(message);
            ResetColor();
        }

        public void Flush()
        {
            // No action required as for every write, data is sent directly to the console
            // output stream
        }
    }
}