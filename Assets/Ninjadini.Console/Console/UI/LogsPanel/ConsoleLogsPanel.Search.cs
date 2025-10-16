#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleLogsPanel
    {
        public class TextSearch : FilteringPanel
        {
            VisualElement _activeSearchesElement;
            Toggle _searchToggle;
            ScrollView _searchScroll;
            TextField _termField;
            EnumField _typeField;
            EnumField _matchField;
            VisualElement _saveArea;
            TextField _saveNameField; 
            DropdownField _loadField;
            DropdownField _deleteField;
            SaveSet _searchSet;

            ConsoleLogsPanel Panel => Filtering.Panel;
            ConsoleContext Context => Panel._context;

            public TextSearch(Filtering filtering) : base(filtering)
            {
                AddToClassList("logs-search-container");
            }

            protected override Button CreateMenuButton()
            {
                var btn = new Button(ToggleShowHide)
                {
                    text = ConsoleUIStrings.LogsSearch
                };
                btn.AddToClassList("nav-btn");
                return btn;
            }

            public override void Reset()
            {
                if (_searchToggle != null && _searchToggle.value)
                {
                    _searchToggle.SetValueWithoutNotify(false);
                    UpdateHasSearchesStatus();
                }
                base.Reset();
            }

            protected override void EnsureElementsExists()
            {
                base.EnsureElementsExists();
                
                if (_searchToggle != null)
                {
                    return;
                }
                _searchSet = LoadCurrent();
                _searchToggle = new Toggle(ConsoleUIStrings.LogsSearchEnabled);
                _searchToggle.AddToClassList("logs-search-toggle");
                _searchToggle.RegisterValueChangedCallback(OnActivateSearchesToggleChanged);
                Add(_searchToggle);
                
                _searchScroll = new ScrollView();
                _searchScroll.mode = ScrollViewMode.Vertical;
                Add(_searchScroll);

                var horizontal = new VisualElement();
                horizontal.AddToClassList("horizontal");
                horizontal.AddToClassList("logs-search-item");
                
                _typeField = new EnumField(OperatorType.And);
                _typeField.style.width = 68;
                ConsoleUIUtils.FixDropdownFieldPopupSize(_typeField, Context);
                _typeField.RegisterValueChangedCallback(OnTypeValueChanged);
                
                _matchField = new EnumField(MatchType.IgnoreCase);
                _matchField.style.width = 130;
                ConsoleUIUtils.FixDropdownFieldPopupSize(_matchField, Context);
                _matchField.RegisterValueChangedCallback(OnMatchValueChanged);
                
                _termField = new TextField();
                _termField.style.width = 160;
                _termField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        AddSearchBtnClicked();
                        evt.StopPropagation();
                        evt.StopImmediatePropagation();
                    }
                }, TrickleDown.TrickleDown);
                _termField.RegisterValueChangedCallback(OnTermValueChanged);
                
                var addBtn = new Button(AddSearchBtnClicked)
                {
                    text = ConsoleUIStrings.LogsAddTerm
                };

                var searchArea = new VisualElement();
                searchArea.AddToClassList("logs-search-area");
                
                horizontal.Add(_typeField);
                horizontal.Add(_termField);
                horizontal.Add(_matchField);
                horizontal.Add(addBtn);
                searchArea.Add(horizontal);
                
                _activeSearchesElement = new VisualElement();
                _activeSearchesElement.AddToClassList("logs-search-items");
                searchArea.Add(_activeSearchesElement);
                
                _searchScroll.Add(searchArea);
               
                _saveArea = new VisualElement();
                _saveArea.AddToClassList("horizontal");
                _searchScroll.Add(_saveArea);
                
                _saveNameField = new TextField();
                _saveNameField.style.minWidth = 100;
                _saveNameField.maxLength = 12;
                _saveNameField.RegisterValueChangedCallback(evt =>
                {
                    var filtered = new string(evt.newValue.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
                    if (filtered != evt.newValue)
                    {
                        _saveNameField.value = filtered;
                    }
                });
                _saveArea.Add(_saveNameField);
                
                var saveBtn = new Button(SaveSearchBtnClicked)
                {
                    text = ConsoleUIStrings.LogsSave
                };
                _saveArea.Add(saveBtn);

                var saveLoadArea = new VisualElement();
                saveLoadArea.AddToClassList("logs-search-loadArea");
                _searchScroll.Add(saveLoadArea);

                _loadField = new DropdownField();
                _loadField.value = ConsoleUIStrings.LogsLoad;
                _loadField.style.width = 100;
                ConsoleUIUtils.FixDropdownFieldPopupSize(_loadField, Context);
                _loadField.RegisterCallback<FocusEvent, TextSearch>((e, filtering) =>
                {
                    filtering.UpdateLoadFieldNames();
                }, this);
                _loadField.RegisterValueChangedCallback(OnLoadFieldValueChanged);
                saveLoadArea.Add(_loadField);
                
                _deleteField = new DropdownField();
                _deleteField.value = ConsoleUIStrings.LogsDelete;
                _deleteField.style.width = 100;
                ConsoleUIUtils.FixDropdownFieldPopupSize(_deleteField, Context);
                _deleteField.RegisterCallback<FocusEvent, TextSearch>((_, filtering) =>
                {
                    filtering.UpdateLoadFieldNames();
                }, this);
                _deleteField.RegisterValueChangedCallback(OnDeleteSearchChanged);
                saveLoadArea.Add(_deleteField);
                
                SetSearchesActiveState(false);
                RefreshSearchEntryFields();
            }

            void OnActivateSearchesToggleChanged(ChangeEvent<bool> evt)
            {
                SetSearchesActiveState(evt.newValue);
                UpdateHasSearchesStatus();
                Filtering.UpdateFilteringResult();
            }

            void SetSearchesActiveState(bool active)
            {
                _searchToggle.SetValueWithoutNotify(active);
                _searchScroll.style.opacity = active ? 1f : 0.5f;
            }

            void AddSearchBtnClicked()
            {
                var term = _termField.value;
                if (string.IsNullOrEmpty(term))
                {
                    return;
                }
                _termField.SetValueWithoutNotify("");
                _termField.schedule.Execute(() =>
                {
                    _termField.Focus();
                });
                var searchGroup = new SearchGroup()
                {
                    active = true,
                    combo = (OperatorType)_typeField.value,
                    term = term,
                    type = (MatchType)_matchField.value
                };
                _typeField.SetValueWithoutNotify(OperatorType.And);
                _matchField.SetValueWithoutNotify(MatchType.IgnoreCase);
                CreateSearchGroupElement(searchGroup);
                _activeSearchesElement.Add(searchGroup.Element);
                _searchSet.terms.Add(searchGroup);
                UpdateHasSearchesStatus();
                OnWorkingItemChanged();
            }

            void CreateSearchGroupElement(SearchGroup searchGroup)
            {
                var str = searchGroup.combo.ToString();
                str += " (" + searchGroup.type + ")";
                str += " \"" + searchGroup.term + "\"";

                var element = new VisualElement();
                searchGroup.Element = element;
                element.AddToClassList("horizontal");
                element.AddToClassList("logs-search-item");

                var lbl = new Label(str);

                var toggleBtn = new Button();
                toggleBtn.clicked += OnActiveDeactivateClicked;
                var deleteBtn = new Button(() =>
                {
                    RemoteSearchGroup(searchGroup);
                })
                {
                    text = "X"
                };
                element.Add(deleteBtn);
                element.Add(toggleBtn);
                element.Add(lbl);
                searchGroup.Element = element;
                
                UpdateActiveState();
                return;

                void UpdateActiveState()
                {
                    lbl.style.opacity = searchGroup.active ? 1f : 0.4f;
                    toggleBtn.text = searchGroup.active ? "Ignore" : "Enable";
                }
                void OnActiveDeactivateClicked()
                {
                    searchGroup.active = !searchGroup.active;
                    UpdateActiveState();
                    OnWorkingItemChanged();
                }
            }

            void RemoteSearchGroup(SearchGroup group)
            {
                if (_searchSet.terms.Remove(group))
                {
                    group.Element.RemoveFromHierarchy();
                    OnWorkingItemChanged();
                }
            }

            void OnWorkingItemChanged()
            {
                var working = _searchSet.working ??= new SearchGroup();
                working.active = true;
                working.term = _termField.value;
                working.combo = (OperatorType)_typeField.value;
                working.type = (MatchType)_matchField.value;
                working.RegexCache = null;
                SetSearchesActiveState(true);
                SaveCurrent();
                UpdateHasSearchesStatus();
                Filtering.UpdateFilteringResult();
            }

            void SaveSearchBtnClicked()
            {
                var saveName = _saveNameField.value;
                if (string.IsNullOrEmpty(saveName))
                {
                    ConsoleToasts.Show(Context, ConsoleUIStrings.LogsNeedSaveSearchName);
                    _saveNameField.Focus();
                    return;
                }
                GetSaves().AddSaveFrom(saveName, _searchSet);
                _saveNameField.SetValueWithoutNotify("");
                ConsoleToasts.Show(Context, ConsoleUIStrings.LogsSavedSearch.Replace("{name}", saveName));
                UpdateLoadFieldNames();
                SaveSaves();
            }

            void OnDeleteSearchChanged(ChangeEvent<string> evt)
            {
                var deleteValue = evt.newValue;
                if (string.IsNullOrEmpty(deleteValue))
                {
                    return;
                }
                var set = GetSaves().DeleteSave(deleteValue);
                Action revertAction = () =>
                {
                    RevertDelete(set);
                };
                if (set == null)
                {
                    revertAction = null;
                }
                UpdateLoadFieldNames();
                ConsoleToasts.Show(Context, ConsoleUIStrings.LogsDeletedSearch.Replace("{name}", deleteValue), revertAction, "Revert");
                _deleteField.SetValueWithoutNotify(ConsoleUIStrings.LogsDelete);
                SaveSaves();
            }

            void RevertDelete(SaveSet saveSet)
            {
                if (saveSet?.name == null)
                {
                    return;
                }
                GetSaves().AddSaveFrom(saveSet.name, saveSet);
                ConsoleToasts.Show(Context, ConsoleUIStrings.LogsSavedSearch.Replace("{name}", saveSet.name));
                SaveSaves();
            }

            void UpdateLoadFieldNames()
            {
                var saves = GetSaves();
                var list = saves.saves.Select(set => set?.name).Where(n => !string.IsNullOrEmpty(n)).Reverse().ToList();
                if (list.Count == 0)
                {
                    list.Add("- no saves -"); // < this also prevents a crash in Android from having an empty dropdown.
                }
                _loadField.choices = list;
                _loadField.MarkDirtyRepaint();
                _deleteField.choices = list;
                _deleteField.MarkDirtyRepaint();
            }

            void OnLoadFieldValueChanged(ChangeEvent<string> evt)
            {
                var loadValue = evt.newValue;
                if (string.IsNullOrEmpty(loadValue))
                {
                    return;
                }
                var set = GetSaves().Get(loadValue);
                if (set == null)
                {
                    return;
                }
                ConsoleToasts.Show(Context, ConsoleUIStrings.LogsLoadedSearch.Replace("{name}", loadValue));
                Load(set);
                _loadField.SetValueWithoutNotify(ConsoleUIStrings.LogsLoad);
            }

            void Load(SaveSet set)
            {
                _termField.SetValueWithoutNotify("");
                _searchSet.working = set?.working?.GetCopyForSerialization();
                _searchSet.terms.Clear();
                if (set?.terms != null)
                {
                    _searchSet.terms.AddRange(set.terms.Select(s => s.GetCopyForSerialization()));
                }
                SaveCurrent();
                RefreshSearchEntryFields();
                OnWorkingItemChanged();
            }

            static Saves _saves;
            static Saves GetSaves()
            {
                if (_saves == null)
                {
                    try
                    {
                        var json = PlayerPrefs.GetString(StandardStorageKeys.LogsFilterSaves);
                        if (!string.IsNullOrEmpty(json))
                        {
                            _saves = JsonUtility.FromJson<Saves>(json);
                            _saves.saves ??= new ();
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    _saves ??= new Saves();
                }
                return _saves;
            }

            static void SaveSaves()
            {
                if (_saves != null)
                {
                    var json = JsonUtility.ToJson(_saves);
                    PlayerPrefs.SetString(StandardStorageKeys.LogsFilterSaves, json);
                }
            }
            
            SaveSet LoadCurrent()
            {
                SaveSet result = null;
                try
                {
                    var json = Context.Storage.GetString(StandardStorageKeys.LogsFilterCurrent);
                    if (!string.IsNullOrEmpty(json))
                    {
                        result = JsonUtility.FromJson<SaveSet>(json);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
                result ??= new SaveSet();
                result.terms ??= new List<SearchGroup>();
                return result;
            }

            void SaveCurrent()
            {
                var json = JsonUtility.ToJson(_searchSet);
                Context.Storage.SetString(StandardStorageKeys.LogsFilterCurrent, json);
            }

            void RefreshSearchEntryFields()
            {
                var working = _searchSet.working;
                _termField.SetValueWithoutNotify(working?.term ?? "");
                _typeField.SetValueWithoutNotify(working?.combo ?? OperatorType.And);
                _matchField.SetValueWithoutNotify(working?.type ?? MatchType.IgnoreCase);
                _activeSearchesElement.Clear();
                foreach (var searchGroup in _searchSet.terms)
                {
                    if (searchGroup.Element != null)
                    {
                        searchGroup.Element.RemoveFromHierarchy();
                    }
                    CreateSearchGroupElement(searchGroup);
                    _activeSearchesElement.Add(searchGroup.Element);
                }
            }
            

            void UpdateHasSearchesStatus()
            {
                var hasAnyFilter = HasFilters();
                Filtering.SetFilterButtonActive(MenuBtn, hasAnyFilter);
                _activeSearchesElement.style.display = _searchSet.terms?.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                _saveArea.style.display = hasAnyFilter ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public bool HasFilters() => _searchToggle is { value: true }
                                        && (!string.IsNullOrEmpty(_termField.value) || _searchSet.terms.Any(t => t.active));
            
            void OnMatchValueChanged(ChangeEvent<Enum> evt) => OnWorkingItemChanged();

            void OnTypeValueChanged(ChangeEvent<Enum> evt) => OnWorkingItemChanged();

            void OnTermValueChanged(ChangeEvent<string> evt) => OnWorkingItemChanged();
            
            public bool MatchesFilter(LogLine logLine)
            {
                if (_searchToggle?.value != true)
                {
                    return true;
                }
                bool? hadOrMatch = null;
                var text = logLine.GetLineString();
                if (!ProcessTerm(text, _searchSet.working, ref hadOrMatch))
                {
                    return false;
                }
                foreach (var term in _searchSet.terms)
                {
                    if (!ProcessTerm(text, term, ref hadOrMatch))
                    {
                        return false;
                    }
                }
                return hadOrMatch is null or true;
            }
            
            static bool ProcessTerm(string text, SearchGroup searchGroup, ref bool? hadOrMatch)
            {
                if (!searchGroup.active || string.IsNullOrEmpty(searchGroup.term))
                {
                    return true;
                }
                if (string.IsNullOrEmpty(text))
                {
                    return false;
                }
                if (searchGroup.combo == OperatorType.Or && hadOrMatch == true)
                {
                    return true;
                }
                var foundMatch = TestTerm(text, searchGroup);
                if (searchGroup.combo == OperatorType.Or)
                {
                    if (hadOrMatch == null) hadOrMatch = foundMatch;
                    else hadOrMatch |= foundMatch;
                    return true; // if it's an OR match, we will carry on and match later.
                }
                if (searchGroup.combo == OperatorType.Not)
                {
                    foundMatch = !foundMatch;
                }
                return foundMatch;
            }

            static bool TestTerm(string fullTextString, SearchGroup searchGroup)
            {
                switch (searchGroup.type)
                {
                    case MatchType.Loose:
                        return LooseMatch(fullTextString, searchGroup.term);
                    case MatchType.IgnoreCase:
                        return fullTextString.IndexOf(searchGroup.term, StringComparison.OrdinalIgnoreCase) >= 0;
                    case MatchType.CaseSensitive:
                        return fullTextString.Contains(searchGroup.term);
                    case MatchType.RegExp:
                        if (searchGroup.RegexCache == null)
                        {
                            try
                            {
                                searchGroup.RegexCache = new Regex(searchGroup.term, RegexOptions.Compiled);
                            }
                            catch (ArgumentException)
                            {
                                searchGroup.RegexCache = new Regex("(?!)", RegexOptions.Compiled);
                            }
                        }
                        return searchGroup.RegexCache.IsMatch(fullTextString);
                    default:
                        return false;
                }
            }
            
            static bool LooseMatch(string fullText, string term)
            {
                bool IsSimpleTerm(string s)
                {
                    for (var i = 0; i < s.Length; i++)
                    {
                        if (!char.IsLetterOrDigit(s[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                if (!IsSimpleTerm(term))
                {
                    return fullText.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                var fi = 0;
                var ti = 0;
                var fullLen = fullText.Length;
                var termLen = term.Length;
                while (fi < fullLen && ti < termLen)
                {
                    var fc = fullText[fi];
                    if (!char.IsLetterOrDigit(fc))
                    {
                        fi++;
                        continue; // skip symbols/spaces in fullText
                    }
                    var tc = term[ti];
                    var match = char.IsDigit(tc) ? (fc == tc) : char.ToLowerInvariant(fc) == char.ToLowerInvariant(tc);
                    if (match)
                    {
                        ti++;
                    }
                    fi++;
                }
                return ti == termLen;
            }

            [Serializable]
            public class SearchGroup
            {
                public bool active;
                public OperatorType combo;
                public MatchType type;
                public string term;
                
                [NonSerialized]
                public VisualElement Element;
                [NonSerialized]
                public Regex RegexCache;

                public SearchGroup GetCopyForSerialization() => new SearchGroup()
                {
                    active = active,
                    combo = combo,
                    type = type,
                    term = term
                };
            }

            [Serializable]
            public class SaveSet
            {
                public string name;
                public SearchGroup working;
                public List<SearchGroup> terms = new List<SearchGroup>();

                public void CopyFrom(SaveSet other)
                {
                    name = other.name;
                    terms ??= new List<SearchGroup>();
                    terms.Clear();

                    working = other.working == null || string.IsNullOrEmpty(other.working.term) ? null : other.working.GetCopyForSerialization();
                    foreach (var term in other.terms)
                    {
                        if (!string.IsNullOrEmpty(term.term))
                        {
                            terms.Add(term.GetCopyForSerialization());
                        }
                    }
                }
            }

            [Serializable]
            public class Saves
            {
                public List<SaveSet> saves = new List<SaveSet>();

                public SaveSet Get(string name)
                {
                    foreach (var save in saves)
                    {
                        if (save?.name == name)
                        {
                            return save;
                        }
                    }
                    return null;
                }
                
                public void AddSaveFrom(string saveName, SaveSet newSet)
                {
                    if (string.IsNullOrEmpty(saveName))
                    {
                        return;
                    }
                    DeleteSave(saveName);
                    var set = new SaveSet();
                    set.CopyFrom(newSet);
                    set.name = saveName;
                    saves.Add(set);
                }

                public SaveSet DeleteSave(string saveName)
                {
                    SaveSet match = null;
                    for (var i = saves.Count - 1; i >= 0; i--)
                    {
                        var set = saves[i];
                        if (set == null)
                        {
                            saves.RemoveAt(i);
                        }
                        else if (set.name == saveName)
                        {
                            match = saves[i];
                            saves.RemoveAt(i);
                        }
                    }
                    return match;
                }
            }
        }
        
        public enum OperatorType
        {
            And = 0,
            Or = 1,
            Not = 2,
        }
        
        public enum MatchType
        {
            IgnoreCase = 0,
            CaseSensitive = 1,
            Loose = 2,
            RegExp = 3,
        }
    }
}
#endif