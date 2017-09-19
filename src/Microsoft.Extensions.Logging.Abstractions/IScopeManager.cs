using System;

namespace Microsoft.Extensions.Logging.Abstractions
{
    public interface IScopeManager
    {
        IDisposable CreateScope(string category, object value);
        void EnumerateScopes(Action<object, string, object> callback, object state);
    }
}