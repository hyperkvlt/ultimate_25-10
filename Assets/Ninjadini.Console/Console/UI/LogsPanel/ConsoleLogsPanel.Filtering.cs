#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using Ninjadini.Logger.Internal;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleLogsPanel
    {
        public class Filtering : VisualElement
        {
            readonly ConsoleLogsPanel _panel;
            readonly LogsHistory.ListWrapper _logsList;
            readonly TextSearch _textSearch;
            readonly Channels _channels;
            
            Button _filterDebugBtn;
            Button _filterWarnBtn;
            Button _filterErrorBtn;
            int _filterLevels;

            FilteringPanel _showingPanel;
            
            IVisualElementScheduledItem _filteringSchedule;
            readonly List<int> _searchIndexes = new ();
            readonly HashSet<(LogLine, DateTime)> _pinned = new ();
            int _lastCount = int.MaxValue;
            int _lastClearIndex;

            public LogsHistory.ListWrapper LogsList => _logsList;
            public ConsoleLogsPanel Panel => _panel;
            ConsoleContext Context => _panel._context;
            
            public Filtering(ConsoleLogsPanel panel, LogsHistory.ListWrapper logsList)
            {
                _panel = panel;
                _logsList = logsList;
                _textSearch = new TextSearch(this);
                _channels = new Channels(this);
            }

            public void Reset()
            {
                var hadFilters = HasAnyFilters();
                _textSearch.Reset();
                _channels.Reset();
                if (_filterLevels != 0)
                {
                    _filterLevels = 0;
                    UpdateFilterLevelBtns();
                }
                if (hadFilters)
                {
                    UpdateFilteringResult();
                }
            }

            public FilteringPanel ShowingFilterPanel => _showingPanel;

            public void SetShowingPanel(FilteringPanel filteringPanel)
            {
                if (_showingPanel == filteringPanel)
                {
                    return;
                }
                if (_showingPanel != null)
                {
                    _showingPanel.OnHidden();
                    _showingPanel.RemoveFromHierarchy();
                }
                _showingPanel = filteringPanel;
                if (filteringPanel != null)
                {
                    Add(filteringPanel);
                    filteringPanel.OnShown();
                }
            }

            internal void AddMenuButtons(VisualElement container)
            {
                container.Add(_textSearch.MenuBtn);
                container.Add(new VisualElement()
                {
                    style = { width = 3 }
                });
                container.Add(_channels.MenuBtn);
                container.Add(new VisualElement()
                {
                    style = { width = 3 }
                });
                container.Add(_filterDebugBtn = new Button(() => OnFilterLevelBtnClicked(InfoLevelMask))
                {
                    text = ConsoleUIStrings.LogsFilterInfo,
                });
                container.Add(_filterWarnBtn = new Button(() => OnFilterLevelBtnClicked(WarnLevelMask))
                {
                    text = ConsoleUIStrings.LogsFilterWarn,
                });
                container.Add(_filterErrorBtn = new Button(() => OnFilterLevelBtnClicked(ErrorLevelMask))
                {
                    text = ConsoleUIStrings.LogsFilterError,
                });
                _filterDebugBtn.AddToClassList("logs-level-btn");
                _filterWarnBtn.AddToClassList("logs-level-btn");
                _filterErrorBtn.AddToClassList("logs-level-btn");
                //UpdateFilterLevelBtns();
            }

            public void SetActiveChannels(IEnumerable<string> channels)
            {
                _channels.SetActiveChannels(channels);
            }

            static readonly int InfoLevelMask = 1 << (int)NjLogger.Level.Info;
            static readonly int WarnLevelMask = 1 << (int)NjLogger.Level.Warn;
            static readonly int ErrorLevelMask = 1 << (int)NjLogger.Level.Error;

            void OnFilterLevelBtnClicked(int maskToToggle)
            {
                _filterLevels ^= maskToToggle;
                UpdateFilterLevelBtns();
                UpdateFilteringResult();
            }

            void UpdateFilterLevelBtns()
            {
                if (_filterLevels == (InfoLevelMask | WarnLevelMask | ErrorLevelMask))
                {
                    _filterLevels = 0;
                }
                SetFilterButtonActive(_filterDebugBtn, (_filterLevels & InfoLevelMask) != 0);
                SetFilterButtonActive(_filterWarnBtn, (_filterLevels & WarnLevelMask) != 0);
                SetFilterButtonActive(_filterErrorBtn, (_filterLevels & ErrorLevelMask) != 0);
            }

            public static void SetFilterButtonActive(VisualElement element, bool active)
            {
                const string className = "filter-active";
                if (active) element.AddToClassList(className);
                else element.RemoveFromClassList(className);
            }
            
            bool HasAnyFilters() => _filterLevels != 0 || _channels.HasFilters() || _textSearch.HasFilters();

            public void UpdateFilteringResult()
            {
                _searchIndexes.Clear();

                if (!HasAnyFilters())
                {
                    FilteringCleared();
                    return;
                }
                var history = _logsList.History;
                _lastCount = history.Head;
                _lastClearIndex = history.ClearIndex;
                for (int i = history.FirstVisibleIndex, l = _lastCount; i < l; i++)
                {
                    if(MatchesFilters(history.GetLog(i)))
                    {
                        _searchIndexes.Add(i);
                    }
                }
                _logsList.SetSearchModeFilter(_searchIndexes);
                _panel.Refresh();
                _panel.ScrollToBottom();

                _filteringSchedule ??= _panel.schedule.Execute(Update).Every(100);
                _filteringSchedule.Resume();
            }

            public void SetPinned(LogLine line, bool pinned)
            {
                if(line == null) return;
                if (pinned)
                {
                    _pinned.Add((line, line.Time));
                }
                else
                {
                    _pinned.Remove((line, line.Time));
                }
            }

            public bool IsPinned(LogLine line)
            {
                return line != null && _pinned.Contains((line, line.Time));
            }

            bool MatchesFilters(LogLine logLine)
            {
                if (_pinned.Contains((logLine, logLine.Time)))
                {
                    return true;
                }
                if (_filterLevels != 0 && !logLine.Options.IsLevelInLevelsMask(_filterLevels))
                {
                    return false;
                }
                if (!_channels.MatchesFilter(logLine))
                {
                    return false;
                }
                return _textSearch.MatchesFilter(logLine);
            }

            void FilteringCleared()
            {
                _lastCount = int.MaxValue;
                _searchIndexes.Clear();
                if (_filteringSchedule?.isActive ?? false)
                {
                    _filteringSchedule.Pause();
                    _logsList.SetSearchModeFilter(null);
                    _panel.Refresh();
                    _panel.ScrollToBottom();
                }
            }

            void Update()
            {
                var history = _logsList.History;
                if (_lastClearIndex != history.ClearIndex)
                {
                    _searchIndexes.Clear();
                    _lastClearIndex = history.ClearIndex;
                    _lastCount = 0;
                    _panel.Refresh();
                }
                if (_lastCount < history.Head)
                {
                    // clean out the index that are now rotated out of the buffer ring.
                    var changed = CleanOutOldIndexes();
                    // Add the new items that were added to the ring since last frame.
                    if (AddNewIndexes())
                    {
                        changed = true;
                    }
                    if (changed)
                    {
                        var wasAtBottom = _panel.AtBottom();
                        _panel.Refresh();
                        if (wasAtBottom)
                        {
                            _panel.ScrollToBottom();
                        }
                    }
                }
            }

            bool CleanOutOldIndexes()
            {
                var oldIndexesCount = _searchIndexes.Count;
                var firstOkIndex = oldIndexesCount;
                var history = _logsList.History;
                for(var i = 0; i < oldIndexesCount; i++)
                {
                    if (history.GetLog(_searchIndexes[i]) != null)
                    {
                        firstOkIndex = i;
                        break;
                    }
                }
                if (firstOkIndex > 0)
                {
                    _searchIndexes.RemoveRange(0, firstOkIndex);
                    return true;
                }
                return false;
            }

            bool AddNewIndexes()
            {
                var history = _logsList.History;
                var newCount = history.Head;
                var changed = false;
                for (int i = Math.Max(_lastCount, history.FirstVisibleIndex); i < newCount; i++)
                {
                    var log = history.GetLog(i);
                    if(MatchesFilters(log))
                    {
                        changed = true;
                        _searchIndexes.Add(i);
                    }
                }
                _lastCount = newCount;
                return changed;
            }
        }
    }
}
#endif