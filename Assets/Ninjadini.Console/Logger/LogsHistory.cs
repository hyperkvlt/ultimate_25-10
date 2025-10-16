using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ninjadini.Logger.Internal
{
    /// <summary>
    /// Logs History.<br/>
    /// Logs are stored in a rotating ring - the size of the ring depends on what you set in project setttings for player or editor.
    /// The easiest way for you to pull logs out of history is by using ForEachLogNewestToOldest() or ForEachLogOldestToNewest()<br/>
    /// See GenerateHistoryNewestToOldest for example.
    /// </summary>
    public class LogsHistory
    {
        public static int DesiredStartingLogsHistorySize = DefaultMaxHistoryCount;
        
        public const int MaxNumParams = 6;
        public const int DefaultMaxHistoryCount = 128;
        
        int _head;
        LogLine[] _logs;
        int _backLogLocks;
        readonly List<LogLine> _backlogs = new ();
        
        public int MaxHistoryCount => _logs.Length;
        
        /// This is the total number of logs that has rotated through. We will only actually keep max number of logs based on MaxHistoryCount;
        public int Head => _head;

        /// The lowest log index that can produce actual data. (the ones below this index will return null as they are now pooled/rotated out)
        public int FirstVisibleIndex => Math.Max(0, _head - _logs.Length) - _backlogs.Count;

        /// Number of log items with actual data.
        public int NumVisibleItems => Math.Min(_head, _logs.Length) + _backlogs.Count;

        /// this number is changed each time the log is cleared. to detect if the drawn content has become invalid
        public int ClearIndex { get; private set; }

        /// Number of 'BackLogging' locks active
        public int NumBackLoggingLocks => _backLogLocks;

        /// Number of 'BackLogged' items.
        /// This is the number of old logs that are now out of the log ring buffer, but still kept in a separate list.
        /// This allows users to scroll back to the earlier logs without them being lost when new logs overwrite old ones.
        public int NumBackLoggedItems => _backlogs.Count;

        public LogsHistory(int count)
        {
            _logs = Array.Empty<LogLine>();
            SetMaxHistoryCount(count);
        }

        public void SetMaxHistoryCount(int newSize)
        {
            if (newSize < 1 || newSize == _logs.Length)
            {
                return;
            }

            var oldSize = _logs.Length;
            var oldIndex = oldSize <= 0 ? 0 : _head % oldSize;
            var newIndex = _head % newSize;
            var offset = (newIndex - oldIndex + newSize) % newSize;

            var newLogs = new LogLine[newSize];

            for (var i = 0; i < oldSize; i++)
            {
                var to = (i + offset) % newSize;
                newLogs[to] = _logs[i];
            }
            var newSlots = newSize - oldSize;
            if (newSlots > 0)
            {
                var fillStart = (oldSize + offset) % newSize;
                var fillEnd = (fillStart + newSlots) % newSize;
                if (fillStart < fillEnd)
                {
                    for (var i = fillStart; i < fillEnd; i++)
                    {
                        newLogs[i] = new LogLine(MaxNumParams);
                    }
                }
                else
                {
                    for (var i = fillStart; i < newSize; i++)
                    {
                        newLogs[i] = new LogLine(MaxNumParams);
                    }
                    for (var i = 0; i < fillEnd; i++)
                    {
                        newLogs[i] = new LogLine(MaxNumParams);
                    }
                }
            }
            _logs = newLogs;
        }
        
        /// Add log to history.
        /// When the number of logs exceeds MaxHistoryCount, the oldest log is pushed out of the ring buffer.
        /// If BackLoggingLock is enabled, the oldest log is instead added to the _backlogs list.
        /// This allows users to scroll back to the earlier logs without them being lost when new logs overwrite old ones.
        public void Add(ref NjLogger.LogRow logRow)
        {
            LogLine line;
            lock (_logs)
            {
                var index = _head++ % _logs.Length;
                line = _logs[index];
                if (_backLogLocks > 0 && _head > _logs.Length)
                {
                    _backlogs.Add(line.GetCopy());
                }
            }
            line.Set(ref logRow);
        }
        
        /// Retrieves the log at the specified index.
        /// Returns null if the index is outside the visible range (before FirstVisibleIndex or after FirstVisibleIndex + NumVisibleItems),
        /// Logs are stored in a ring buffer. Only the most recent entries up to MaxHistoryCount are retained.
        public LogLine GetLog(int index)
        {
            if(index >= _head || index < 0)
            {
                return null;
            }
            if (index >= _head - _logs.Length)
            {
                return _logs[index % _logs.Length];
            }
            var backlogIndex = (_head - _logs.Length) - index;
            if (backlogIndex > 0 && backlogIndex <= _backlogs.Count)
            {
                return _backlogs[^backlogIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Append logs history to target StringBuilder - Newest to older log.
        /// </summary>
        /// <param name="stringBuilder">Target string builder to generate the text</param>
        /// <param name="maxLogs">Max number of logs. Any value less than 0 = no limits</param>
        /// <param name="appendLogString">Custom log line generator</param>
        public void GenerateHistoryNewestToOldest(StringBuilder stringBuilder, int maxLogs = -1, Action<LogLine, StringBuilder> appendLogString = null)
        {
            if (_head == 0)
            {
                stringBuilder.AppendLine("No logs to generate...");
            }
            else
            {
                ForEachLogNewestToOldest((log) =>
                {
                    if (appendLogString != null)
                    {
                        appendLogString(log, stringBuilder);
                    }
                    else
                    {
                        stringBuilder.AppendLine(log.GetLineString());
                    }
                }, maxLogs);
            }
        }

        
        /// <summary>
        /// Callback with each log in history - from newest to oldest.
        /// </summary>
        /// <param name="callback">Callback to invoke for each log item.</param>
        /// <param name="maxLogs">Max number of logs. Any value less than 0 = no limits</param>
        public void ForEachLogNewestToOldest(Action<LogLine> callback, int maxLogs = -1)
        {
            if (callback == null) return;
            lock (_logs)
            {
                var firstIndex = FirstVisibleIndex;
                var lastIndex = _head - 1;
                if (maxLogs >= 0 && lastIndex - firstIndex > maxLogs)
                {
                    firstIndex = lastIndex - maxLogs + 1;
                }
                for (var i = lastIndex; i >= firstIndex; i--)
                {
                    var log = GetLog(i);
                    if (log != null)
                    {
                        callback(log);
                    }
                }
            }
        }

        /// <summary>
        /// Callback with each log in history - from oldest to newest.
        /// </summary>
        /// <param name="callback">Callback to invoke for each log item.</param>
        /// <param name="maxLogs">Max number of logs. Any value less than 0 = no limits</param>
        public void ForEachLogOldestToNewest(Action<LogLine> callback, int maxLogs = -1)
        {
            if (callback == null) return;
            lock (_logs)
            {
                var firstIndex = FirstVisibleIndex;
                var lastIndex = _head;
                if (maxLogs >= 0 && lastIndex - firstIndex > maxLogs)
                {
                    firstIndex = lastIndex - maxLogs;
                }
                for (var i = firstIndex; i < lastIndex; i++)
                {
                    var log = GetLog(i);
                    if (log != null)
                    {
                        callback(log);
                    }
                }
            }
        }

        /// BackLogging temporarily stores old logs instead of discarding them from the ring buffer.
        /// This allows you to scroll to the top and remain there without logs being lost due to rotation
        /// (normally, older logs are removed from the ring buffer to maintain MaxHistoryCount).
        /// Make sure to call StopBackLoggingLock() for every StartBackLoggingLock() call.
        /// If the call count doesn't match you might end up storing all logs in the backlog list.
        public void StartBackLoggingLock()
        {
            _backLogLocks++;
        }
        
        /// Stops backlogging and resumes normal log rotation.
        /// Once stopped, old logs may be discarded again as new logs are added beyond MaxHistoryCount.
        /// This must be called to balance a previous StartBackLoggingLock() call.
        public void StopBackLoggingLock()
        {
            _backLogLocks--;
            if (_backLogLocks <= 0)
            {
                _backLogLocks = 0;
                _backlogs.Clear();
                if (_backlogs.Capacity > 1000)
                {
                    _backlogs.Capacity = 512;
                }
            }
        }

        /// <summary>
        /// Clear logs history.
        /// It'll increment the ClearIndex, which you may you to know if the previous log indexes are invalidated.
        /// </summary>
        public void Clear()
        {
            _head = 0;
            ClearIndex++;
        }

        public class ListWrapper : IList
        {
            readonly LogsHistory _history;
            List<int> _searchIndexes;

            public ListWrapper(LogsHistory history)
            {
                _history = history;
            }

            public LogsHistory History => _history;
            
            public int Count => _searchIndexes?.Count ?? _history.NumVisibleItems;
            public int RealCount => _history.Head;

            public LogLine GetLog(int index)
            {
                if (_searchIndexes != null)
                {
                    if (index >= 0 && index < _searchIndexes.Count)
                    {
                        return _history.GetLog(_searchIndexes[index]);
                    }
                    return null;
                }
                return _history.GetLog(_history.FirstVisibleIndex + index);
            }

            public object this[int index]
            {
                get => GetLog(index);
                set => throw new NotSupportedException();
            }

            public void SetSearchModeFilter(List<int> indexes)
            {
                _searchIndexes = indexes;
            }
            
            public void Clear()
            {
                _history.Clear();
            }

            bool IList.IsReadOnly => true;
            bool IList.IsFixedSize => false;
            object ICollection.SyncRoot => this;
            bool ICollection.IsSynchronized => false;
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();
            int IList.Add(object value) => throw new NotImplementedException();
            bool IList.Contains(object value) => throw new NotImplementedException();
            int IList.IndexOf(object value) => throw new NotImplementedException();
            void IList.Insert(int index, object value) => throw new NotImplementedException();
            void IList.Remove(object value) => throw new NotImplementedException();
            void IList.RemoveAt(int index) => throw new NotImplementedException();
        }
    }
}