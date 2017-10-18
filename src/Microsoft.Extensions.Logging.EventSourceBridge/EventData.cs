using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Represents the data collected from an EventSource event
    /// </summary>
    public struct EventData : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// Gets the data associated with the event
        /// </summary>
        public EventWrittenEventArgs Event { get; }

        internal EventData(EventWrittenEventArgs evt)
        {
            Event = evt;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the payload values.
        /// </summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(Event);
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates the payload values of a <see cref="EventData"/>
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private const int SyntheticPayloadCount = 5;

            private int _index;

            private readonly int _count;
            private readonly EventWrittenEventArgs _eventData;

            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            public KeyValuePair<string, object> Current
            {
                get
                {
                    switch (_index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>("_tags", _eventData.Tags);
                        case 1:
                            return new KeyValuePair<string, object>("_task", _eventData.Task);
                        case 2:
                            return new KeyValuePair<string, object>("_activityId", _eventData.ActivityId);
                        case 3:
                            return new KeyValuePair<string, object>("_relatedActivityId", _eventData.RelatedActivityId);
                        case 4:
                            return new KeyValuePair<string, object>("_channel", _eventData.Channel);
                        case 5:
                            return new KeyValuePair<string, object>("_opcode", _eventData.Opcode);
                        default:
                            if (_index < 0 || _index > _count)
                            {
                                throw new InvalidOperationException("Cannot retrieve Current item, enumerator is positioned before the start of the list, or after the end of the list");
                            }
                            return new KeyValuePair<string, object>(
                                _eventData.PayloadNames[_index - SyntheticPayloadCount],
                                _eventData.Payload[_index - SyntheticPayloadCount]);
                    }
                }
            }

            object IEnumerator.Current => Current;

            internal Enumerator(EventWrittenEventArgs eventData)
            {
                _eventData = eventData;

                _index = -1;
                _count = eventData.Payload.Count + SyntheticPayloadCount;
            }

            /// <summary>
            /// Releases all the resources used by the <see cref="EventData.Enumerator"/>.
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Advances the enumerator to the next element of the payload.
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                _index += 1;
                return _index < _count;
            }

            /// <summary>
            /// Sets the enumerator to it's initial position, which is before the first element in the collection
            /// </summary>
            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }
    }
}
