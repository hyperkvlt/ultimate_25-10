#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ninjadini.Console.Internal;
using Ninjadini.Console.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console
{
    public partial class ConsoleOptions
    {
        public class PanelElement : VisualElement, IConsolePanelModule.IElement
        {
            readonly ConsoleOptions _options;
            readonly ConsoleContext _context;
            readonly Button _shortCutsBtn;
            readonly ScrollView _scrollView;
            readonly List<GroupItem> _stack = new ();
            
            readonly Button _searchBtn;
            readonly Button _historyBtn;
            VisualElement _searchPanel;
            TextField _searchField;
            
            uint _populatedChangeIndex;
            int _searchOrHistoryMode;
            
            const int MaxSearchResult = 100;

            public ConsoleOptions Options => _options;
            public ConsoleContext Context => _context;

            const string HomeBtnName = "\u25c0";
            
            internal PanelElement(ConsoleContext context, ConsoleOptions options)
            {
                _options = options;
                _context = context;
                _stack.Add(options._root);
                AddToClassList("options-panel");
                schedule.Execute(Update).Every(100);
                
                var header = new VisualElement();
                header.AddToClassList("options-header");
                Add(header);
                _searchBtn = new Button(OnSearchBtnClicked)
                {
                    text = "Search",
                };
                _searchBtn.AddToClassList("options-header-btn");
                _searchBtn.AddToClassList("nav-btn");
                header.Add(_searchBtn);
                _historyBtn = new Button(OnHistoryClicked)
                {
                    text = "History",
                };
                _historyBtn.AddToClassList("options-header-btn");
                _historyBtn.AddToClassList("nav-btn");
                header.Add(_historyBtn);
                
                var gap = new VisualElement()
                {
                    style = { flexGrow = 1 }
                };
                header.Add(gap);
                _shortCutsBtn = new Button(() => _context.RuntimeOverlay.ShowShortcuts(true))
                {
                    text = ConsoleUIStrings.OptsShowShortcuts
                };
                _shortCutsBtn.AddToClassList("nav-btn");
                header.Add(_shortCutsBtn);

                _scrollView = new ScrollView();
                _scrollView.mode = ScrollViewMode.Vertical;
                _scrollView.contentContainer.style.flexGrow = 0;
                Add(_scrollView);
                
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            }

            void IConsolePanelModule.IElement.OnReselected()
            {
                GotoRoot();
            }

            void OnAttachToPanel(AttachToPanelEvent evt)
            {
                if (_stack.Count == 1 && _stack[0].ChildItems.Count == 0 && _stack[0].ChildGroups?.Count == 1)
                {
                    // kinda a dirty hack so that it auto opens build in NJConsole/... group if everything else is empty.
                    _stack.Add(_stack[0].ChildGroups[0]);
                }
                Populate();
            }

            public void GotoRoot()
            {
                _stack.RemoveRange(1, _stack.Count - 1);
                Populate();
            }

            void Update()
            {
                if (_searchOrHistoryMode == 0 && _populatedChangeIndex != _stack[^1].ChangeIndex)
                {
                    var group = _stack[0];
                    // this tries to keep the same path if the group instance changed or removed
                    for (var i = 1; i < _stack.Count; i++)
                    {
                        var oldChildGroup = _stack[i];
                        var realChildGroup = group.ChildGroups.Find(g => g.Name == oldChildGroup.Name);
                        if (realChildGroup != null)
                        {
                            _stack[i] = realChildGroup;
                        }
                        else
                        {
                            _stack.RemoveRange(i, _stack.Count - i);
                            break;
                        }
                    }
                    Populate();
                }
            }

            public void Populate()
            {
                _scrollView.Clear();
                var scrollView = _scrollView;

                UpdateShortcutBtnVisibility();
                
                var group = _stack[^1];
                _populatedChangeIndex = group.ChangeIndex;
                if (_stack.Count > 1)
                {
                    var stack = new VisualElement();
                    stack.AddToClassList("options-breadcrumb");
                    scrollView.Add(stack);
                    for (int index = 0, count = _stack.Count; index < count; index++)
                    {
                        var i = index;
                        var isHead = index == count - 1;
                        var stackGroup = _stack[index];
                        var btn = new Button(() => GotoParent(i));
                        var text = index == 0 ? HomeBtnName : (stackGroup.Name + " /");
                        if (index == 0)
                        {
                            btn.AddToClassList("options-breadcrumb-btn");
                            btn.text = text;
                        }
                        else if (isHead)
                        {
                            AddFolderIcon(btn, text);
                            btn.AddToClassList("options-group-btn");
                            btn.style.height = 28;
                        }
                        else
                        {
                            AddFolderIcon(btn, text);
                            btn.AddToClassList("options-breadcrumb-btn");
                        }
                        btn.SetEnabled(!isHead);
                        stack.Add(btn);
                    }
                }
                
                if (group.ChildGroups != null)
                {
                    var groups = new VisualElement();
                    groups.AddToClassList("options-groups");
                    scrollView.Add(groups);
                    foreach (var groupItem in group.ChildGroups)
                    {
                        AddGroupItem(groupItem, groups);
                    }
                }

                var headerMap = new Dictionary<string, VisualElement>();
                
                var noHeaderItems = new VisualElement();
                noHeaderItems.AddToClassList("options-options");
                scrollView.Add(noHeaderItems);
                headerMap[string.Empty] = noHeaderItems;
                var hiddenItemCount = 0;
                foreach (var optionItem in group.ChildItems)
                {
                    var header = optionItem.Header ?? string.Empty;
                    if (!headerMap.TryGetValue(header, out var itemsContainer))
                    {
                        var row = new VisualElement();
                        row.style.flexShrink = 0;
                        
                        var lbl = new Label(header);
                        lbl.AddToClassList("options-options-header");
                        row.Add(lbl);
                        
                        itemsContainer = new VisualElement();
                        itemsContainer.AddToClassList("options-options");
                        row.Add(itemsContainer);
                        headerMap[header] = itemsContainer;
                        
                        scrollView.Add(row);
                    }
                    if (AddItem(optionItem, itemsContainer) == null)
                    {
                        hiddenItemCount++;
                    }
                }
                if (noHeaderItems.childCount == 0)
                {
                    noHeaderItems.RemoveFromHierarchy();
                }
                var hasnoChildOrGroups = group.ChildItems.Count == 0 && (group.ChildGroups?.Count ?? 0) == 0;
                if (hasnoChildOrGroups && _stack.Count <= 1)
                {
                    var str = "No options set up...";
                    if (Application.isEditor && _options.GetType() == typeof(ConsoleOptions))
                    {
                        str += ConsoleUIStrings.OptsHowToSetup;
                    }
                    var lbl = new Label(str);
                    lbl.AddToClassList("monoFont");
                    _scrollView.Add(lbl);
                }

                PrintHiddenItems(hiddenItemCount);
                if (!hasnoChildOrGroups)
                {
                    TryAddConsoleShortCutHint();
                }
            }

            void PrintHiddenItems(int hiddenItemCount)
            {
                if (hiddenItemCount <= 0) return;
                var horizontal = new VisualElement();
                horizontal.style.flexDirection = FlexDirection.Row;
                horizontal.style.opacity = 0.6f;
                horizontal.AddToClassList("options-shortcut-hint");
                horizontal.AddToClassList("monoFont");
                _scrollView.Add(horizontal);
                var label = new Label()
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        flexWrap = Wrap.Wrap,
                        whiteSpace = WhiteSpace.Normal
                    }
                };
                horizontal.Add(label);

                if (_options.CommandLinePath == null || (!Application.isEditor && !_context.Settings.inPlayerCommandLine))
                {
                    label.text = $"\u2298 {hiddenItemCount} hidden item(s) — accessible only via CommandLine (but disabled for this options module)";
                    return;
                }
                label.text = $"\u2298 {hiddenItemCount} hidden item(s) — accessible only via";
                var btn = new Button(GotoCommandLine);
                btn.SetEnabled(Application.isEditor || _context.Settings.inPlayerCommandLine);
                btn.text = "Command Line";
                horizontal.Add(btn);
            }

            void GotoCommandLine()
            {
                var clPath = _options.CommandLinePath;
                if (clPath == null)
                {
                    return;
                }
                var clElement = _context.Window?.OpenAndFocusOnCommandLine();
                if (clElement == null)
                {
                    return;
                }
                var path = string.Join("/", _stack.Skip(1).Select(s => s.Name)) + "/";
                
                if (!string.IsNullOrEmpty(clPath))
                {
                    path = (clPath + "/" +path).Replace("//", "/");
                }
                if (path.Length > 1)
                {
                    clElement.SetInputText(path);
                }
            }

            void UpdateShortcutBtnVisibility()
            {
                var show = _context.RuntimeOverlay?.Shortcuts?.SearchIfItemsInAnyGroup() ?? false;
                _shortCutsBtn.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void TryAddConsoleShortCutHint()
            {
                if (_context.RuntimeOverlay?.Shortcuts?.HasItemsInCurrentGroup() ?? true)
                {
                    return;
                }
                var label = new Label(ConsoleUIStrings.OptsPlaceShortcutHint);
                label.AddToClassList("options-shortcut-hint");
                _scrollView.Add(label);
            }

            void OnSearchBtnClicked()
            {
                if (_searchOrHistoryMode == 1)
                {
                    CloseSearchMode();
                    Populate();
                }
                else
                {
                    CloseHistoryView();
                    PopulateSearchMode();
                }
            }

            void PopulateSearchMode()
            {
                _searchOrHistoryMode = 1;
                _searchBtn.AddToClassList("logs-menus-item-expanded");
                
                if (_searchPanel == null)
                {
                    _searchPanel = new VisualElement();
                    _searchPanel.AddToClassList("options-header");
                        
                    _searchField = new TextField();
                    _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
                    _searchField.style.flexGrow = 1f;
                    _searchField.AddToClassList("options-search");
                    _searchPanel.Add(_searchField);
                }
                Insert(1, _searchPanel);

                var searchValue = _context.Storage.GetString(StandardStorageKeys.OptionsSearch) ?? "";
                _searchField.SetValueWithoutNotify(searchValue);
                _searchField.Focus();
                
                UpdateSearchResult(_searchField.value, true);
            }

            void CloseSearchMode()
            {
                _searchOrHistoryMode = 0;
                _searchPanel?.RemoveFromHierarchy();
                _searchBtn.RemoveFromClassList("logs-menus-item-expanded");
                _searchBtn.RemoveFromClassList("filter-active");
            }

            void UpdateSearchResult(string searchString, bool limitAmount)
            {
                if (string.IsNullOrEmpty(searchString))
                {
                    _searchBtn.RemoveFromClassList("filter-active");
                    Populate();
                    return;
                }
                _searchBtn.AddToClassList("filter-active");
                _scrollView.Clear();
                var groups = new List<(GroupItem, string)>();
                var items = new List<(OptionItem, string)>();
                var terms = Regex.Split(searchString, @"\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                var searchStack = new List<GroupItem>(){_stack[0]};
                VisitSearch(searchStack, terms, groups, items);
                var count = 0;
                foreach (var optionItem in items)
                {
                    if (limitAmount && count > MaxSearchResult)
                    {
                        break;
                    }
                    var item = CreateItemAndPath(optionItem.Item1, optionItem.Item2);
                    _scrollView.Add(item);
                    count++;
                }
                foreach (var groupItem in groups)
                {
                    if (limitAmount && count > MaxSearchResult)
                    {
                        break;
                    }
                    var horizontal = new VisualElement();
                    horizontal.style.flexDirection = FlexDirection.Row;
                    horizontal.style.alignItems = Align.Center;
                    _scrollView.Add(horizontal);
                    AddGroupItem(groupItem.Item1, horizontal);
                    horizontal.Add(new Label($"@ {groupItem.Item2}/"));
                    count++;
                }
                var realCount = groups.Count + items.Count;
                if (limitAmount && count < realCount)
                {
                    _scrollView.Add(new Button(() =>
                    {
                        UpdateSearchResult(searchString, false);
                    })
                    {
                        text = $"Show All (showing {count} of {realCount})"
                    });
                }
                else if (realCount == 0)
                {
                    var lbl = new Label("No matches found...");
                    lbl.AddToClassList("warning");
                    _scrollView.Add(lbl);
                }
            }

            void VisitSearch(List<GroupItem> searchStack, string[] terms, List<(GroupItem, string)> groupResults, List<(OptionItem, string)> itemResults)
            {
                var groupItem = searchStack[^1];
                if (groupItem.ChildGroups != null)
                {
                    foreach (var otherGroup in groupItem.ChildGroups)
                    {
                        if (MatchesTerm(otherGroup.Name, terms))
                        {
                            groupResults.Add((otherGroup, CreatePathString(searchStack)));
                        }
                        searchStack.Add(otherGroup);
                        VisitSearch(searchStack, terms, groupResults, itemResults);
                        searchStack.Remove(otherGroup);
                    }
                }
                foreach (var item in groupItem.ChildItems)
                {
                    if (MatchesTerm(item.Name, terms)) //  || (!string.IsNullOrEmpty(item.Header) && MatchesTerm(item.Header, terms))
                    {
                        itemResults.Add((item, CreatePathString(searchStack)));
                    }
                }
            }

            static string CreatePathString(List<GroupItem> searchStack)
            {
                return string.Join("/", searchStack.Skip(1).Select(s => s.Name));
            }

            bool MatchesTerm(string itemName, string[] terms)
            {
                foreach (var term in terms)
                {
                    if (!itemName.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            }
            
            void AddGroupItem(GroupItem groupItem, VisualElement container)
            {
                var btn = new Button(() => GotoChildGroup(groupItem));
                AddFolderIcon(btn, groupItem.Name + " /");
                btn.AddToClassList("options-group-btn");
                container.Add(btn);
                if (_context.IsRuntimeOverlay)
                {
                    ConsoleUIUtils.ListenForLongHold(btn, () =>
                    {
                        ConsoleUIUtils.SendPointerCancelEvent(btn);
                        OnLongHold(groupItem, btn);
                    });
                }
            }

            void AddFolderIcon(Button btn, string txt)
            {
                var img = new VisualElement();
                img.AddToClassList("options-group-icon");
                btn.Add(img);
                var lbl = new Label(txt);
                btn.text = "";
                btn.Add(lbl);
                btn.style.flexDirection = FlexDirection.Row;
            }

            internal static Button CreateGroupBtnForShortcut(ConsoleContext context, string paneName, GroupItem groupItem)
            {
                var btn = new Button(() =>
                {
                    var overlay = context.RuntimeOverlay;
                    if (overlay == null) return;
                    overlay.ShowWithAccessChallenge();
                    var window = overlay.Window;
                    if (window == null) return;
                    window.SetActivePanel(paneName);
                    if(window.ActivePanel?.Name != paneName) return;
                    if (window.ActivePanelElement is not PanelElement panelElement) return;
                    panelElement.SetGroup(groupItem);
                })
                {
                    text = groupItem.Name+" /"
                };
                btn.AddToClassList("options-group-btn");
                return btn;
            }

            VisualElement AddItem(OptionItem optionItem, VisualElement container)
            {
                var btn = optionItem.CreateElement(_options, Context);
                if (btn != null)
                {
                    SetupBtn(btn, optionItem);
                    container.Add(btn);
                    if (_context.IsRuntimeOverlay)
                    {
                        ConsoleUIUtils.ListenForLongHold(btn, () =>
                        {
                            ConsoleUIUtils.SendPointerCancelEvent(btn);
                            OnLongHold(optionItem, btn);
                        });
                    }
                }
                return btn;
            }

            public static void SetupBtn(VisualElement btn, OptionItem item)
            {
                if (btn != null)
                {
                    btn.AddToClassList("options-option-btn");
                    btn.tooltip = item?.Tooltip;
                }
            }

            void OnLongHold(OptionItem optionItem, VisualElement element)
            {
                var copy = optionItem.CreateElement(_options, Context);
                SetupBtn(copy, optionItem);
                var path = CreateChildPath(optionItem.Name);
                _context.RuntimeOverlay?.Shortcuts.StartPlacementOf(copy, element.worldBound, _options, path);
            }

            void OnLongHold(GroupItem groupItem, Button element)
            {
                var btn = CreateGroupBtnForShortcut(_context, _options.Name, groupItem);
                var path = CreateChildPath(groupItem.Name) + "/";
                _context.RuntimeOverlay?.Shortcuts.StartPlacementOf(btn, element.worldBound, _options, path);
            }

            string CreateChildPath(string itemName)
            {
                if(_stack.Count > 1) return string.Join("/", _stack.Skip(1).Select(s => s.Name))+"/"+itemName;
                return itemName;
            }

            VisualElement CreateItemAndPath(OptionItem optionItem, string path)
            {
                var horizontal = new VisualElement();
                horizontal.style.flexDirection = FlexDirection.Row;
                horizontal.style.alignItems = Align.Center;
                AddItem(optionItem, horizontal);
                horizontal.Add(new Label($"@ {path}/"));
                return horizontal;
            }

            void GotoParent(int index = -1)
            {
                if (_stack.Count <= 1)
                {
                    return;
                }
                if (index < 0) index = _stack.Count - 1; 
                else index = Math.Min(index + 1, _stack.Count);
                _stack.RemoveRange(index, _stack.Count - index);
                Populate();
            }

            void SetGroup(GroupItem groupItem)
            {
                _stack.RemoveRange(1, _stack.Count - 1);
                _stack.Add(groupItem);
                Populate();
            }

            void GotoChildGroup(GroupItem groupItem)
            {
                _stack.Add(groupItem);
                Populate();
            }

            void OnHistoryClicked()
            {
                if (_searchOrHistoryMode == 2)
                {
                    CloseHistoryView();
                    Populate();
                }
                else
                {
                    CloseSearchMode();
                    PopulateHistoryMode();
                }
            }

            void PopulateHistoryMode()
            {
                _searchOrHistoryMode = 2;
                _scrollView.Clear();
                
                _historyBtn.AddToClassList("logs-menus-item-expanded");
                _historyBtn.AddToClassList("filter-active");
                var count = 0;

                var historyItems = _options.History.Items;
                for (var index = historyItems.Count - 1; index >= 0; index--)
                {
                    var historyItem = historyItems[index];
                    var item = _options._root.FindItem(historyItem);
                    count++;
                    if (item != null)
                    {
                        var slashIndex = historyItem.LastIndexOf("/", StringComparison.InvariantCulture);
                        
                        var visual = CreateItemAndPath(item, slashIndex >= 0 ? historyItem.Substring(0, slashIndex) : "");
                        _scrollView.Add(visual);
                    }
                }
                if (_scrollView.childCount > 0)
                {
                    return;
                }
                Label lbl = null;
                if (historyItems.Count == 0)
                {
                    lbl = new Label("Nothing in history yet.");
                }
                else if(count > 0)
                {
                    lbl = new Label("Items in history are not currently active");
                }
                if (lbl != null)
                {
                    lbl.AddToClassList("warning");
                    _scrollView.Add(lbl);
                }
            }

            void CloseHistoryView()
            {
                _searchOrHistoryMode = 0;
                _historyBtn.RemoveFromClassList("logs-menus-item-expanded");
                _historyBtn.RemoveFromClassList("filter-active");
            }

            void OnSearchValueChanged(ChangeEvent<string> evt)
            {
                UpdateSearchResult(evt.newValue, true);
                _context.Storage.SetString(StandardStorageKeys.OptionsSearch, evt.newValue);
            }
        }
    }
}
#endif