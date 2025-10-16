#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using Ninjadini.Console.Internal;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Ninjadini.Console.UI
{
    public partial class ConsoleUtilitiesElement
    {
        public class PlayerPrefDebugger : VisualElement
        {
            const int MaxHistory = 20;
            const string DefaultHistoryTxt = "history";
            
            ConsoleContext _context;
            
            TextField _keyField;
            DropdownField _historyDropdown;
            EnumField _typeDropdown;

            VisualElement _resultContainer;
            Label _resultLbl;
            IntegerField _intField;
            FloatField _floatField;
            TextField _stringField;
            Button _deleteAllBtn;
            int _deleteConformCount;
            
            enum StoreType
            {
                Int,
                Float,
                String
            }

            public PlayerPrefDebugger(ConsoleContext context)
            {
                _context = context;
                AddToClassList("monoFont");
                style.flexWrap = Wrap.Wrap;

                var lbl = new Label("Enter the PlayerPref key below to view and modify the values");
                lbl.style.whiteSpace = WhiteSpace.Normal;
                lbl.style.marginTop = 5;
                lbl.style.marginBottom = 10;
                lbl.style.marginLeft = 5;
                Add(lbl);

                var horizontal = new VisualElement();
                horizontal.AddToClassList("horizontal");
                horizontal.style.flexWrap = Wrap.Wrap;
                _keyField = new TextField("PlayerPrefKey");
                _keyField.style.width = 250;
                _keyField.labelElement.style.minWidth = 50;
                ConsoleUIUtils.SetSubmissionCallback(_keyField, OnPlayerPrefKeyChanged);
                _keyField.AddToClassList("info-info-field");
                horizontal.Add(_keyField);

                _historyDropdown = new DropdownField();
                _historyDropdown.choices = History;
                _historyDropdown.SetValueWithoutNotify(DefaultHistoryTxt);
                _historyDropdown.style.width = 120;
                _historyDropdown.AddToClassList("monoFont");
                _historyDropdown.RegisterValueChangedCallback(OnHistoryDropDownChanged);
                ConsoleUIUtils.FixDropdownFieldPopupSize(_historyDropdown, context);
                horizontal.Add(_historyDropdown);

                Add(horizontal);

                _resultContainer = new VisualElement();
                _resultContainer.style.display = DisplayStyle.None;
                Add(_resultContainer);

                _resultLbl = new Label();
                _resultLbl.AddToClassList("monoFont");
                _resultLbl.style.marginLeft = 5;
                _resultLbl.style.marginTop = 5;
                _resultLbl.style.marginBottom = 10;
                _resultContainer.Add(_resultLbl);
                
                _typeDropdown = new EnumField(StoreType.Int);
                _typeDropdown.RegisterValueChangedCallback(OnTypeDropDownChanged);
                _typeDropdown.AddToClassList("monoFont");
                ConsoleUIUtils.FixDropdownFieldPopupSize(_typeDropdown, context);
                _resultContainer.Add(_typeDropdown);

                _intField = new IntegerField("IntegerValue");
#if UNITY_2022_3_OR_NEWER
                _intField.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
#endif
                _intField.AddToClassList("info-info-field");
                _resultContainer.Add(_intField);

                _floatField = new FloatField("FloatValue");
#if UNITY_2022_3_OR_NEWER
                _floatField.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
#endif
                _floatField.AddToClassList("info-info-field");
                _resultContainer.Add(_floatField);

                _stringField = new TextField("StringValue");
                _stringField.multiline = true;
                _stringField.AddToClassList("info-info-field");
                _resultContainer.Add(_stringField);

                horizontal = new VisualElement();
                horizontal.AddToClassList("horizontal");
                horizontal.style.marginTop = 10;
                horizontal.style.flexGrow = 1;
                _resultContainer.Add(horizontal);

                var btn = new Button(DeleteBtnClicked);
                btn.text = "Delete Key";
                btn.style.minWidth = 100;
                btn.style.height = 30;
                horizontal.Add(btn);
                
                btn = new Button(OnSaveClicked);
                btn.text = "Save";
                btn.style.minWidth = 100;
                btn.style.height = 30;
                horizontal.Add(btn);
                
                btn = new Button(DeleteAllBtnClicked);
                _deleteAllBtn = btn;
                btn.text = "Delete All";
                btn.AddToClassList("red-btn");
                btn.style.minWidth = 100;
                btn.style.height = 30;
                btn.style.alignSelf = Align.FlexStart;
                btn.style.marginTop = 20;
                Add(btn);
            }

            void OnTypeDropDownChanged(ChangeEvent<Enum> evt)
            {
                RefreshFields((StoreType)evt.newValue);
            }

            void OnHistoryDropDownChanged(ChangeEvent<string> evt)
            {
                _historyDropdown.SetValueWithoutNotify(DefaultHistoryTxt);
                _keyField.SetValueWithoutNotify(evt.newValue);
                RefreshFields();
            }

            void OnPlayerPrefKeyChanged(string newValue)
            {
                RefreshFields();
            }

            void RefreshFields(StoreType? type = null)
            {
                var key = _keyField.value;
                
                _deleteAllBtn.text = ConsoleUIStrings.PlayerPrefsDeleteAll;
                _deleteConformCount = 0;
                
                if (string.IsNullOrEmpty(key))
                {
                    _resultContainer.style.display = DisplayStyle.None;
                    return;
                }

                var hasKey = PlayerPrefs.HasKey(key);
                _resultLbl.text = hasKey ? $"\u2713 PlayerPref key found" : $"\u26a0 PlayerPref key not found.\nYou can still set the values below...";
                
                if (hasKey)
                {
                    AddToHistory(key);
                    if (!type.HasValue)
                    {
                        type = DetermineType(key);
                    }
                }
                type ??= StoreType.String;
                
                _resultContainer.style.display = DisplayStyle.Flex;

                _typeDropdown.SetValueWithoutNotify(type.Value);

                VisualElement field = null;
                switch (type.Value)
                {
                    case StoreType.Int:
                        field = _intField;
                        _intField.value = PlayerPrefs.GetInt(key);
                        break;
                    case StoreType.Float:
                        field = _floatField;
                        _floatField.value = PlayerPrefs.GetFloat(key);
                        break;
                    case StoreType.String:
                        field = _stringField;
                        _stringField.value = PlayerPrefs.GetString(key);
                        break;
                }
                _intField.style.display = field == _intField ? DisplayStyle.Flex : DisplayStyle.None;
                _floatField.style.display = field == _floatField ? DisplayStyle.Flex : DisplayStyle.None;
                _stringField.style.display = field == _stringField ? DisplayStyle.Flex : DisplayStyle.None;
            }

            StoreType DetermineType(string key)
            {
                if (PlayerPrefs.GetInt(key, 1) != 1) return StoreType.Int;
                if (PlayerPrefs.GetInt(key, 2) != 2) return StoreType.Int;
                
                if (!Mathf.Approximately(PlayerPrefs.GetFloat(key, 1f), 1f)) return StoreType.Float;
                if (!Mathf.Approximately(PlayerPrefs.GetFloat(key, 2f), 2f)) return StoreType.Float;
                
                return StoreType.String;
            }


            void OnSaveClicked()
            {
                var key = _keyField.value;
                
                PlayerPrefs.DeleteKey(key);
                var storeType = (StoreType)_typeDropdown.value;
                
                if (storeType == StoreType.Int)
                {
                    PlayerPrefs.SetInt(key, _intField.value);
                }
                if (storeType == StoreType.Float)
                {
                    PlayerPrefs.SetFloat(key, _floatField.value);
                }
                if (storeType == StoreType.String)
                {
                    PlayerPrefs.SetString(key, _stringField.value);
                }

                if (_context != null)
                {
                    ConsoleToasts.Show(_context,  $"Saved {storeType} player pref key `{key}`");
                }

                AddToHistory(key);
                
                PlayerPrefs.Save();
                RefreshFields();
            }

            void DeleteBtnClicked()
            {
                var key = _keyField.value;
                PlayerPrefs.DeleteKey(key);
                if (_context != null)
                {
                    ConsoleToasts.Show(_context,  $"Deleted player pref key `{key}`");
                }
                RefreshFields();
            }

            void DeleteAllBtnClicked()
            {
                if (_deleteConformCount == 0)
                {
                    _deleteAllBtn.text = ConsoleUIStrings.PlayerPrefsDeleteAll1;
                    _deleteConformCount++;
                }
                else if (_deleteConformCount == 1)
                {
                    _deleteAllBtn.text = ConsoleUIStrings.PlayerPrefsDeleteAll2;
                    _deleteConformCount++;
                }
                else
                {
                    PlayerPrefs.DeleteAll();
                    RefreshFields();

                    if (_context != null)
                    {
                        ConsoleToasts.Show(_context,  "Deleted all player prefs, good luck!");
                    }
                }
            }

            static HistoryStorage _history;
            static List<string> History
            {
                get
                {
                    if (_history != null) return _history.keys;
                    try
                    {
                        var json = PlayerPrefs.GetString(StandardStorageKeys.PlayerPrefHistory);
                        if (!string.IsNullOrEmpty(json))
                        {
                            _history = JsonUtility.FromJson<HistoryStorage>(json);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    _history ??= new HistoryStorage();
                    _history.keys ??= new ();
                    if (_history.keys.Count == 0)
                    {
                        _history.keys.Add(DefaultHistoryTxt); // < a bug in android where it crashes if dropdown list is empty.
                    }
                    return _history.keys;
                }
                set
                {
                    _history ??= new HistoryStorage();
                    _history.keys = value;
                    var json = JsonUtility.ToJson(_history);
                    PlayerPrefs.SetString(StandardStorageKeys.PlayerPrefHistory, json);
                }
            }

            void AddToHistory(string key)
            {
                var history = History;
                if (history.Count == 1 && history[0] == DefaultHistoryTxt)
                {
                    history.RemoveAt(0); // < a bug in android where it crashes if dropdown list is empty.
                }
                if (!history.Remove(key))
                {
                    while (history.Count >= MaxHistory)
                    {
                        history.RemoveAt(MaxHistory - 1);
                    }
                }
                history.Insert(0, key);
                History = history;
            }

            [Serializable]
            class HistoryStorage
            {
                public List<string> keys;
            }
        }
    }
}
#endif