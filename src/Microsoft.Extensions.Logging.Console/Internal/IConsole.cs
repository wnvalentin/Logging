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
        /// ���û�����ƣ�ͨ��Write����WriteLine����д�����Ϣ�������������������̨�������ȱ����浽��������Flush������ִ�е�ʱ��Ὣ��������������־��Ϣ�������������̨�ϡ�
        /// </summary>
        void Flush();
    }
}