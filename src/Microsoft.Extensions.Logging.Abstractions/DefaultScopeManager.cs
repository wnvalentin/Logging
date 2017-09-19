using System;
using System.Threading;

namespace Microsoft.Extensions.Logging.Abstractions
{
    public class DefaultScopeManager : IScopeManager
    {
        private readonly AsyncLocal<LoggerBaseScope> _currentScope = new AsyncLocal<LoggerBaseScope>();

        public IDisposable CreateScope(string category, object value)
        {
            LoggerBaseScope newScope;
            lock (_currentScope)
            {
                newScope = new LoggerBaseScope(this) { Parent = _currentScope.Value, Value = value, Category = category};
                _currentScope.Value = newScope;
            }
            return newScope;
        }

        public void EnumerateScopes(Action<object, string, object> callback, object state)
        {
            lock (_currentScope)
            {
                for (var scope = _currentScope.Value; scope != null; scope = scope.Parent)
                {
                    callback(scope.Value, scope.Category, state);
                }
            }
        }

        internal void RemoveScope(LoggerBaseScope scopeToRemove)
        {
            lock (_currentScope)
            {
                LoggerBaseScope previousScope = null;
                var currentScope = _currentScope.Value;

                while (currentScope != null)
                {
                    if (currentScope == scopeToRemove)
                    {
                        if (previousScope == null)
                        {
                            _currentScope.Value = scopeToRemove.Parent;
                        }
                        else
                        {
                            previousScope.Parent = currentScope.Parent;
                        }
                    }

                    previousScope = currentScope;
                    currentScope = currentScope.Parent;
                }
            }
        }

    }
}