// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Abstractions
{
    internal class LoggerBaseScope: IDisposable
    {
        private readonly DefaultScopeManager _provider;

        public LoggerBaseScope(DefaultScopeManager provider)
        {
            _provider = provider;
        }

        public string Category { get; set; }

        public object Value { get; set; }

        public LoggerBaseScope Parent { get; set; }

        public void Dispose()
        {
            _provider.RemoveScope(this);
        }
    }
}
