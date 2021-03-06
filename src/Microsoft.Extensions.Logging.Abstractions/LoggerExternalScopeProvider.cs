// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Default implemenation of <see cref="IExternalScopeProvider"/>
    /// </summary>
    public class LoggerExternalScopeProvider : IExternalScopeProvider
    {
        private readonly AsyncLocal<Scope> _currentScope = new AsyncLocal<Scope>();

        /// <summary>
        /// 按照创建的先后顺序，为每个scope对象执行callback回调函数
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public void ForEachScope<TState>(Action<object, TState> callback, TState state)
        {
            void Report(Scope current)
            {
                if (current == null)
                {
                    return;
                }
                Report(current.Parent);
                callback(current.State, state);
            }
            Report(_currentScope.Value);
        }
        
        /// <summary>
        /// Scope是一个单向链表，此方法用于为链表添加一个新的子元素
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable Push(object state)
        {
            var parent = _currentScope.Value;
            var newScope = new Scope(this, state, parent);
            _currentScope.Value = newScope;

            return newScope;
        }

        /// <summary>
        /// 私有Scope类，只允许所属类的成员访问
        /// </summary>
        private class Scope : IDisposable
        {
            private readonly LoggerExternalScopeProvider _provider;
            private bool _isDisposed;

            internal Scope(LoggerExternalScopeProvider provider, object state, Scope parent)
            {
                _provider = provider;
                State = state;
                Parent = parent;
            }

            public Scope Parent { get; }

            public object State { get; }

            public override string ToString()
            {
                return State?.ToString();
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _provider._currentScope.Value = Parent;
                    _isDisposed = true;
                }
            }
        }
    }
}