using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.Notify
{
    public interface INotifier
    {
        void EnlistTarget(object target);

        bool ShouldNotify(string notificationName);

        void Notify(string notificationName, object parameters);
    }

    public interface INotifyParameterAdapter
    {
        object Adapt(object inputParameter, Type outputType);
    }

    public class NotificationNameAttribute : Attribute
    {
        public NotificationNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    public class NotifierParameterAdapter : INotifyParameterAdapter
    {
        public object Adapt(object inputParameter, Type outputType)
        {
            if (inputParameter == null) { return null; }
#if NET45 || DNX451
            return Internal.Converter.Convert(outputType, inputParameter.GetType(), inputParameter);
#else
            return inputParameter;
#endif
        }
    }

    public class Notifier : INotifier
    {
        private readonly ConcurrentDictionary<string, List<Entry>> _notificationNames = new ConcurrentDictionary<string, List<Entry>>(StringComparer.Ordinal);
        private readonly INotifyParameterAdapter _parameterAdapter;

        public Notifier(INotifyParameterAdapter parameterAdapter)
        {
            _parameterAdapter = parameterAdapter;
        }

        public void EnlistTarget(object target)
        {
            var typeInfo = target.GetType().GetTypeInfo();

            var methodInfos = typeInfo.DeclaredMethods;

            foreach (var methodInfo in methodInfos)
            {
                Console.WriteLine("MethodInfo " + methodInfo.Name);
                var notificationNameAttribute = methodInfo.CustomAttributes.OfType<NotificationNameAttribute>().FirstOrDefault();
                if (notificationNameAttribute != null)
                {
                    Console.WriteLine("NotificationName " + notificationNameAttribute.Name);
                    Enlist(notificationNameAttribute.Name, target, methodInfo);
                }
            }
        }

        private void Enlist(string notificationName, object target, MethodInfo methodInfo)
        {
            var entries = _notificationNames.GetOrAdd(
                notificationName,
                _ => new List<Entry>());
            entries.Add(new Entry(target, methodInfo));
        }

        public bool ShouldNotify(string notificationName)
        {
            return _notificationNames.ContainsKey(notificationName);
        }

        public void Notify(string notificationName, object parameters)
        {
            List<Entry> entries;
            if (_notificationNames.TryGetValue(notificationName, out entries))
            {
                foreach (var entry in entries)
                {
                    entry.Send(parameters, _parameterAdapter);
                }
            }
        }

        internal class Entry
        {
            private MethodInfo _methodInfo;
            private object _target;

            public Entry(object target, MethodInfo methodInfo)
            {
                _target = target;
                _methodInfo = methodInfo;
            }

            internal void Send(object parameters, INotifyParameterAdapter parameterAdapter)
            {
                var methodParameterInfos = _methodInfo.GetParameters();
                var methodParameterCount = methodParameterInfos.Length;
                var methodParameterValues = new object[methodParameterCount];

                var objectTypeInfo = parameters.GetType().GetTypeInfo();
                for (var index = 0; index != methodParameterCount; ++index)
                {
                    var objectPropertyInfo = objectTypeInfo.GetDeclaredProperty(methodParameterInfos[index].Name);
                    if (objectPropertyInfo == null)
                    {
                        Console.WriteLine("objectPropertyInfo == null");
                        continue;
                    }

                    var objectPropertyValue = objectPropertyInfo.GetValue(parameters);
                    if (objectPropertyValue == null)
                    {
                        Console.WriteLine("objectPropertyInfo == null");
                        continue;
                    }

                    var methodParameterInfo = methodParameterInfos[index];
                    if (methodParameterInfo.ParameterType.GetTypeInfo().IsAssignableFrom(objectPropertyInfo.PropertyType.GetTypeInfo()))
                    {
                        methodParameterValues[index] = objectPropertyValue;
                    }
                    else
                    {
                        methodParameterValues[index] = parameterAdapter.Adapt(objectPropertyValue, methodParameterInfo.ParameterType);
                    }
                }

                _methodInfo.Invoke(_target, methodParameterValues);
            }
        }
    }
}
