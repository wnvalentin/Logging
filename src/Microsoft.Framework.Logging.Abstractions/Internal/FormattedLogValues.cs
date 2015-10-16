// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Framework.Logging.Internal
{
    /// <summary>
    /// LogValues to enable formatting options supported by <see cref="string.Format"/>. 
    /// This also enables using {NamedformatItem} in the format string.
    /// </summary>
    public class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object>>, ILogValues
    {
        private static ConcurrentDictionary<string, LogValuesFormatter> _formatters = new ConcurrentDictionary<string, LogValuesFormatter>();
        private readonly LogValuesFormatter _formatter;
        private readonly object[] _values;

        public FormattedLogValues(string format, params object[] values)
        {
            _formatter = _formatters.GetOrAdd(format, f => new LogValuesFormatter(f));
            _values = values;
        }

        public override string ToString() => _formatter.Format(_values);

        KeyValuePair<string, object> IReadOnlyList<KeyValuePair<string, object>>.this[int index] => new KeyValuePair<string, object>(_formatter.ValueNames[index], _values[index]);

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _values.Length;

        IEnumerator IEnumerable.GetEnumerator() => _formatter.GetValues(_values).GetEnumerator();

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => _formatter.GetValues(_values).GetEnumerator();
    }
}
