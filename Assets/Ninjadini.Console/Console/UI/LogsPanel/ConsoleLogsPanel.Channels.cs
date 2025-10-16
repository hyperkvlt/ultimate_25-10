#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleLogsPanel
    {
        public class Channels : FilteringPanel
        {
            readonly HashSet<string> _activeChannels = new ();
            readonly Dictionary<string, Button> _drawnElements = new ();
            
            int _lastCount;
            int _lastClearIndex;
            readonly Button _allChBtn;
            readonly Button _nonChBtn;
            Label _noChannelsLbl;
            
            public Channels(Filtering filtering) : base(filtering)
            {
                AddToClassList("logs-channel-items");
                
                _allChBtn = MakeButton(null, "[ * ]");
                _allChBtn.tooltip = ConsoleUIStrings.LogsChAllTooltip;
                
                _nonChBtn = MakeButton(string.Empty, "[ - ]");
                _nonChBtn.tooltip = ConsoleUIStrings.LogsChNoChTooltip;

                schedule.Execute(Update).Every(200);
            }

            protected override Button CreateMenuButton()
            {
                var btn = new Button(ToggleShowHide)
                {
                    text = ConsoleUIStrings.LogsChannels
                };
                btn.AddToClassList("nav-btn");
                return btn;
            }

            public override void Reset()
            {
                if (_activeChannels.Count > 0)
                {
                    _activeChannels.Clear();
                    UpdateAllChannelButtons();
                    UpdateHasSearchesStatus();
                }
                base.Reset();
            }

            public void SetActiveChannels(IEnumerable<string> channels)
            {
                _activeChannels.Clear();
                foreach (var ch in channels)
                {
                    _activeChannels.Add(ch);
                }
                UpdateAllChannelButtons();
                UpdateHasSearchesStatus();
            }

            public bool HasFilters()
            {
                return _activeChannels.Count > 0;
            }

            public bool MatchesFilter(LogLine logLine)
            {
                if (_activeChannels.Count == 0)
                {
                    return true;
                }
                return _activeChannels.Contains(logLine.GetChannelName());
            }

            public override void OnShown()
            {
                base.OnShown();
                RefreshChannelElements();
            }

            void RefreshChannelElements()
            {
                foreach (var kv in _drawnElements)
                {
                    kv.Value.RemoveFromHierarchy();
                }
                _drawnElements.Clear();
                
                _allChBtn.RemoveFromHierarchy();
                _nonChBtn.RemoveFromHierarchy();
                
                _lastCount = 0;
                UpdateChannelButtons();
            }

            void UpdateChannelButtons()
            {
                var hadButtons = _drawnElements.Count > 0;
                
                var history = Filtering.LogsList.History;
                var newCount = history.Head;
                if (_lastClearIndex != history.ClearIndex)
                {
                    _lastClearIndex = history.ClearIndex;
                    _lastCount = 0;
                }
                for (int i = Math.Max(_lastCount, history.FirstVisibleIndex); i < newCount; i++)
                {
                    var channel = history.GetLog(i)?.GetChannelName();
                    if(string.IsNullOrEmpty(channel) || _drawnElements.ContainsKey(channel))
                    {
                        continue;
                    }
                    var btn = MakeButton(channel, channel);
                    if (hadButtons)
                    {
                        Add(btn);
                    }
                }
                _lastCount = newCount;
                if (_drawnElements.Count > 0)
                {
                    if (!hadButtons)
                    {
                        AddChannelsAsRefresh();
                    }
                }
                else if(_noChannelsLbl == null)
                {
                    AddNoChannels();
                }
            }

            void AddChannelsAsRefresh()
            {
                Add(_allChBtn);
                Add(_nonChBtn);
                _drawnElements[string.Empty] = _nonChBtn;
                foreach (var key in _drawnElements.Keys.OrderBy(k => k))
                {
                    if (key == string.Empty)
                    {
                        continue;
                    }
                    var v = _drawnElements[key];
                    Add(v);
                }
                if (_noChannelsLbl != null)
                {
                    _noChannelsLbl.RemoveFromHierarchy();
                    _noChannelsLbl = null;
                }
            }

            void AddNoChannels()
            {
                _noChannelsLbl = new Label(Application.isEditor ? ConsoleUIStrings.LogsChNoChannelsHelpEditor : ConsoleUIStrings.LogsChNoChannelsHelpPlayer);
                _noChannelsLbl.AddToClassList("monoFont");
                Add(_noChannelsLbl);
            }

            bool IsChannelSelected(string channel)
            {
                if (channel == null)
                {
                    return _activeChannels.Count == 0;
                }
                return _activeChannels.Contains(channel);
            }

            Button MakeButton(string channel, string displayText)
            {
                var btn = new Button()
                {
                    text = displayText
                };
                btn.AddToClassList("logs-channel-item");
                btn.RegisterCallback<ClickEvent, string>((evt, ch) =>
                {
                    OnChannelBtnClicked(ch, (Button)evt.currentTarget, evt);
                }, channel);
                if (channel != null)
                {
                    UpdateChannelBtn(btn, IsChannelSelected(channel));
                    _drawnElements[channel] = btn;
                }
                return btn;
            }

            void UpdateChannelBtn(Button btn, bool active)
            {
                if (active)
                {
                    btn.AddToClassList("filter-active");
                }
                else
                {
                    btn.RemoveFromClassList("filter-active");
                }
            }

            void UpdateAllChannelButtons()
            {
                foreach (var kv in _drawnElements)
                {
                    UpdateChannelBtn(kv.Value, IsChannelSelected(kv.Key));
                }
            }

            void OnChannelBtnClicked(string channel, Button btn, ClickEvent evt)
            {
                if (channel == null)
                {
                    _activeChannels.Clear();
                    foreach (var kv in _drawnElements)
                    {
                        UpdateChannelBtn(kv.Value, false);
                    }
                }
                else if (evt.ctrlKey)
                {
                    if (_activeChannels.Count == 1 && _activeChannels.Contains(channel))
                    {
                        _activeChannels.Clear();
                        foreach (var kv in _drawnElements)
                        {
                            if (kv.Key != channel)
                            {
                                _activeChannels.Add(kv.Key);
                            }
                        }
                    }
                    else
                    {
                        _activeChannels.Clear();
                        _activeChannels.Add(channel);
                    }
                    UpdateAllChannelButtons();
                }
                else if (_activeChannels.Remove(channel))
                {
                    UpdateChannelBtn(btn, false);
                }
                else
                {
                    _activeChannels.Add(channel);
                    UpdateChannelBtn(btn, true);
                }
                UpdateHasSearchesStatus();
                Filtering.UpdateFilteringResult();
            }

            void Update()
            {
                UpdateChannelButtons();
            }

            void UpdateHasSearchesStatus()
            {
                Filtering.SetFilterButtonActive(MenuBtn, HasFilters());
            }
        }
    }
}
#endif