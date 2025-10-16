using System;
using System.Collections.Generic;
using Ninjadini.Console.Internal;
using Ninjadini.Console.UI;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console
{
#if !NJCONSOLE_DISABLE
    public partial class ConsoleScreenShortcuts : VisualElement, ConsoleUIUtils.ILongHoldListener
    {
        const int NumSaveSlots = 3;
        const int MaxItems = 30; // a magic number for now but its just so the storage doesn't get too big in player pref.
        
        readonly ConsoleOverlay _overlay;
        VisualElement _editingUnderlay;
        VisualElement _editingWindow;
        Label _cancelArea;
        VisualElement _previewItem;
        VisualElement _previewContainer;
        VisualElement _draggingItem;
        Vector2? _draggingOffset;
        int _pointerId;
        (StyleLength, StyleLength) _draggingItemSize;
        StoredItem _addingItem;
        Button _clearBtn;
        Button[] _saveBtns;
        bool _started;
        bool _editing;
        VisualElement[] _shortcutContainers;
        
        readonly Dictionary<string, ChangeTracking> _changeTracking = new Dictionary<string, ChangeTracking>();

        public bool UserHidden { get; private set; }
        
        public ConsoleScreenShortcuts(ConsoleOverlay overlay)
        {
            _overlay = overlay;
            pickingMode = PickingMode.Ignore;
            AddToClassList("fullscreen");
            
            RegisterCallback<DetachFromPanelEvent>(OnDetached);
        }

        public void Show()
        {
            UserHidden = false;
            EnsureInit();
        }

        public void ShowEditWindow()
        {
            UserHidden = false;
            EnsureInit();
            EnsureElementsExist();
            StartEditMode();
        }

        public bool HasItemsInCurrentGroup()
        {
            return GetCurrentGroup()?.Items?.Count > 0;
        }

        public bool SearchIfItemsInAnyGroup()
        {
            for (var i = 0; i < NumSaveSlots; i++)
            {
                var json = PlayerPrefs.GetString(StandardStorageKeys.ShortcutSlotPrefix + i);
                if (json != null && json.Length > 15)
                {
                    return true;
                }
            }
            return false;
        }

        void EnsureInit()
        {
            if (_started)
            {
                return;
            }
            _started = true;
            schedule.Execute(OnSlowUpdate).Every(1000);
            RefreshItems();
        }

        void OnSlowUpdate()
        {
            if (_draggingItem != null || _editing)
            {
                return;
            }
            var group = GetCurrentGroup();
            if (group.Items.Count == 0)
            {
                return;
            }
            var changed = false;
            foreach (var (key, tracking) in _changeTracking)
            {
                tracking.Reference.TryGetTarget(out var moduleBefore);
                var moduleNow = FindRestorable(key);
                if (moduleNow != moduleBefore)
                {
                    changed = true;
                    break;
                }
                if (moduleNow != null && moduleNow.ChangeIndex != tracking.DrawnChange)
                {
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                RefreshItems();
            }
        }

        IRestorable FindRestorable(string featureName)
        {
            foreach (var kv in _overlay.Context.Modules.AllModules)
            {
                if (kv.Value is IRestorable restorable && restorable.FeatureName == featureName)
                {
                    return restorable;
                }
            }
            return null;
        }

        public void RefreshItems()
        {
            var group = GetCurrentGroup();
            _changeTracking.Clear();
            ResetClearBtn();
            UpdateSelectedSaveSlot();
            ClearShortcutContainers();
            if (group.Items.Count == 0)
            {
                return;
            }
            EnsureElementsExist();
            
            var items = group.Items;
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var target = GetTargetDrop(item.loc);
                if (target == null) continue;
                IRestorable mod;
                if (_changeTracking.TryGetValue(item.fun, out var tracking))
                {
                    tracking.Reference.TryGetTarget(out mod);
                }
                else
                {
                    mod = FindRestorable(item.fun);
                    tracking = new ChangeTracking()
                    {
                        Reference = new WeakReference<IRestorable>(mod),
                        DrawnChange = mod?.ChangeIndex
                    };
                    _changeTracking[item.fun] = tracking;
                }
                if (mod == null)
                {
                    if(item.fail == 0) StoreFailedDate(group.Items, index, false);
                    continue;
                }
                var element = mod.TryRestore(_overlay.Context, item.pth);
                if (element == null)
                {
                    if(item.fail == 0) StoreFailedDate(group.Items, index, false);
                    continue;
                }
                if (item.fail != 0) StoreFailedDate(group.Items, index, true);
                target.Add(element);
                element.userData = item;
                ConsoleUIUtils.ListenForLongHold(element, null, this);
            }
        }

        void UpdateSelectedSaveSlot()
        {
            if (_saveBtns == null)
            {
                return;
            }
            var selectedIndex = CurrentSlotNumber;
            for (var i = _saveBtns.Length - 1; i >= 0; i--)
            {
                var btn = _saveBtns[i];
                btn.SetEnabled(i != selectedIndex);
                if (i == selectedIndex)
                {
                    btn.AddToClassList("selected");
                }
                else
                {
                    btn.RemoveFromClassList("selected");
                }
            }
        }
        
        float? ConsoleUIUtils.ILongHoldListener.GetHoldDuration() => _editing ? 0f : ConsoleUIUtils.DefaultLongHoldDelaySecs;
        
        VisualElement ConsoleUIUtils.ILongHoldListener.GetCaptureTarget(VisualElement btn) => this;
        
        void ConsoleUIUtils.ILongHoldListener.OnPointerDown(PointerDownEvent evt, VisualElement btn)
        {
            if (_editing)
            {
                evt.StopImmediatePropagation();
            }
        }

        void StoreFailedDate(List<StoredItem> list, int index, bool wasSuccess)
        {
            var item = list[index];
            if (wasSuccess)
            {
                item.fail = 0;
            }
            else
            {
                item.fail = (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;
            }
            list[index] = item;
        }

        void EnsureElementsExist()
        {
            if (_editingUnderlay != null)
            {
                return;
            }
            _editingUnderlay = new VisualElement();
            _editingUnderlay.AddToClassList("underlay");
            _editingUnderlay.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            _editingWindow = new VisualElement();
            _editingWindow.AddToClassList("shortcuts-edit-window");
            _editingUnderlay.Add(_editingWindow);
            
            var closeBtn = new Button(CloseEditingMode)
            {
                text = "Done"
            };
            closeBtn.style.marginBottom = 10;
            closeBtn.AddToClassList("shortcuts-edit-btn");
            closeBtn.AddToClassList("nav-btn");
            closeBtn.style.alignSelf = Align.FlexEnd;
            _editingWindow.Add(closeBtn);

            var saveBtns = new VisualElement();
            saveBtns.AddToClassList("horizontal");
            _saveBtns = new Button[NumSaveSlots];
            for (var i = 0; i < NumSaveSlots; i++)
            {
                var localI = i;
                var btn = new Button(() => OnSaveSlotClicked(localI))
                {
                    text = "Save " + (i + 1)
                };
                btn.style.flexGrow = 1f;
                btn.style.height = 50;
                btn.AddToClassList("shortcuts-edit-btn");
                saveBtns.Add(btn);
                _saveBtns[i] = btn;
            }
            UpdateSelectedSaveSlot();
            _editingWindow.Add(saveBtns);

            var autoStart = new Toggle("Auto Show at start");
            autoStart.value = AutoShow;
            autoStart.AddToClassList("shortcuts-edit-btn");
            autoStart.RegisterValueChangedCallback(OnAutoStartToggleChanged);
            _editingWindow.Add(autoStart);
            
            var addBtn = new Button(ShowAllOptionsClicked)
            {
                text = "Show all options"
            };
            addBtn.AddToClassList("shortcuts-edit-btn");
            addBtn.style.alignSelf = Align.Stretch;
            _editingWindow.Add(addBtn);

            _clearBtn = new Button(ClearShortcutsBtnClicked);
            _clearBtn.text = ClearBtnTxt1;
            _clearBtn.AddToClassList("shortcuts-edit-btn");
            _clearBtn.style.alignSelf = Align.Stretch;
            _editingWindow.Add(_clearBtn);
            
            var hideBtn = new Button(HideClicked)
            {
                text = "Hide shortcuts"
            };
            hideBtn.AddToClassList("shortcuts-edit-btn");
            hideBtn.style.alignSelf = Align.Stretch;
            _editingWindow.Add(hideBtn);
            
            _cancelArea = new Label();
            _cancelArea.AddToClassList("shortcuts-edit-window");
            
            CreateShortcutContainers();
        }

        const string ClearBtnTxt1 = "Clear shortcuts";
        const string ClearBtnTxt2 = "Clear shortcuts for sure?";

        void ResetClearBtn()
        {
            if (_clearBtn != null && _clearBtn.text != ClearBtnTxt1)
            {
                _clearBtn.text = ClearBtnTxt1;
                _clearBtn.RemoveFromClassList("red-btn");
            }
        }

        void ClearShortcutsBtnClicked()
        {
            if (_clearBtn.text == ClearBtnTxt1)
            {
                _clearBtn.text = ClearBtnTxt2;
                _clearBtn.AddToClassList("red-btn");
            }
            else if (_clearBtn.text == ClearBtnTxt2)
            {
                ResetClearBtn();
                var saveIndex = CurrentSlotNumber;
                ClearSaveGroup(saveIndex);
                RefreshItems();
            }
        }

        void OnAutoStartToggleChanged(ChangeEvent<bool> evt)
        {
            AutoShow = evt.newValue;
        }

        void OnSaveSlotClicked(int i)
        {
            CurrentSlotNumber = i;
            RefreshItems();
        }

        void CloseEditingMode()
        {
            _editing = false;
            _editingWindow.RemoveFromHierarchy();
            _editingUnderlay.RemoveFromHierarchy();
        }

        void HideClicked()
        {
            UserHidden = true;
            RemoveFromHierarchy();
            ConsoleToasts.Show(_overlay.Context, "You can show the shortcuts again via Console > Options > Show Shortcuts", () => _overlay.ShowShortcuts(), "Revert");
        }

        void ShowAllOptionsClicked()
        {
            _overlay.ShowWithAccessChallenge();
            _overlay.Window?.SetActivePanel<ConsoleOptions>();
        }

        public VisualElement AddNewItem(IRestorable restorable, string restorePath, bool canGiveUiFeedback = true)
        {
            EnsureInit();
            EnsureElementsExist();
            var item = new StoredItem()
            {
                fun = restorable?.FeatureName ?? throw new ArgumentNullException(nameof(restorable.FeatureName)),
                pth = restorePath ?? throw new ArgumentNullException(nameof(restorePath)),
                loc = 3
            };
            var group = GetCurrentGroup();
            var existingIndex = FindIndex(group.Items, ref item);
            if (existingIndex >= 0)
            {
                if (canGiveUiFeedback)
                {
                    ConsoleToasts.Show(_overlay.Context, ConsoleUIStrings.ShortcutsAlreadyExists);
                    _overlay.ShowShortcuts();
                }
                return null;
            }
            if (group.Items.Count >= MaxItems)
            {
                CleanUpOldAnItemGroup(_loadedGroup.Items);
                if (group.Items.Count >= MaxItems)
                {
                    if(canGiveUiFeedback) ConsoleToasts.Show(_overlay.Context, ConsoleUIStrings.ShortcutsMaxReached.Replace("{MaxItems}", MaxItems.ToString()));
                    return null;
                }
            }
            var target = GetTargetDrop(item.loc);
            if (target == null) return null; // donno
            
            var element = restorable.TryRestore(_overlay.Context, restorePath);
            if (element == null)
            {
                Debug.LogWarning($"Adding new shortcut ({restorable} > {restorePath}) failed because there was no element returned.");
                return null;
            }
            
            group.Items.Add(item);
            SaveCurrentGroup();
            element.userData = item;
            ConsoleUIUtils.ListenForLongHold(element, null, this);
            target.Add(element);
            if (canGiveUiFeedback) _overlay.ShowShortcuts();
            return element;
        }

        public void StartPlacementOf(VisualElement element, Rect rect, IRestorable restorable, string restorePath)
        {
            EnsureInit();
            StartPlacementOf(element, rect, new StoredItem()
            {
                fun = restorable?.FeatureName,
                pth = restorePath
            });
        }

        void StartPlacementOf(VisualElement element, Rect rect, StoredItem item)
        {
            _overlay.ShowShortcuts();
            EnsureElementsExist();

            StopDragging();
            _editingWindow.RemoveFromHierarchy();
            Insert(0, _editingUnderlay);
            _addingItem = item;
            if (_previewItem == null)
            {
                _previewItem = new VisualElement();
                _previewItem.style.backgroundColor = Color.cyan;
            }
            var elementStyle = element.resolvedStyle;
            var previewStyle = _previewItem.style;

            rect.x -= 3;
            rect.y -= 3;
            previewStyle.width = rect.width;
            previewStyle.height = rect.height;
            
            previewStyle.paddingBottom = elementStyle.paddingBottom;
            previewStyle.paddingTop = elementStyle.paddingTop;
            previewStyle.paddingLeft = elementStyle.paddingLeft;
            previewStyle.paddingRight = elementStyle.paddingRight;
            previewStyle.marginBottom = elementStyle.marginBottom;
            previewStyle.marginTop = elementStyle.marginTop;
            previewStyle.marginLeft = elementStyle.marginLeft;
            previewStyle.marginRight = elementStyle.marginRight;
            
            _draggingItem = element;
            _draggingItemSize = (element.style.width, element.style.height);
            _draggingItem.style.position = Position.Absolute;
            element.style.width = rect.width;
            element.style.height = rect.height;
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.SetEnabled(false);
            _draggingOffset = null;
            
            Add(element);
            _cancelArea.text = item.loc == 0 ? "Drop here to cancel" : "Drop here to remove";
            pickingMode = PickingMode.Position;
            Add(_cancelArea);
            ConsoleUIUtils.SetBorderColor(_cancelArea, Color.clear);
            RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (_draggingOffset == null)
            {
                var pos = evt.localPosition;
                _draggingOffset = new Vector2(pos.x - _draggingItem.style.left.value.value, pos.y - _draggingItem.style.top.value.value);
            }
            UpdateMove(evt.position);
        }

        void UpdateMove(Vector2 mousePosition)
        {
            if (_draggingItem == null)
            {
                StopDragging();
                return;
            }
            _draggingItem.style.left = mousePosition.x - (_draggingOffset?.x ?? 0f);
            _draggingItem.style.top = mousePosition.y - (_draggingOffset?.y ?? 0f);
            
            var target = GetTargetDrop(GetTargetDrop(mousePosition));
            if (_previewContainer != null)
            {
                _previewContainer.style.backgroundColor = StyleKeyword.Initial;
            }
            _previewContainer = target;
            if (target != null)
            {
                target.Add(_previewItem);
                target.style.backgroundColor = new Color(1f, 1f, 1f, 0.25f);
                ConsoleUIUtils.SetBorderColor(_cancelArea, Color.clear);
            }
            else
            {
                _previewItem.RemoveFromHierarchy();
                ConsoleUIUtils.SetBorderColor(_cancelArea, Color.red);
            }
        }

        void StopDragging()
        {
            pickingMode = PickingMode.Ignore;
            _cancelArea?.RemoveFromHierarchy();
            _previewItem?.RemoveFromHierarchy();
            UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            _draggingItem = null;
            _addingItem = default;
            if (_previewContainer != null)
            {
                _previewContainer.style.backgroundColor = StyleKeyword.Initial;
                _previewContainer = null;
            }
            _editingUnderlay.RemoveFromHierarchy();
        }

        void OnDetached(DetachFromPanelEvent evt)
        {
            if (_draggingItem != null)
            {
                ProcessEndDrag(_addingItem.loc);
            }
        }
        
        void OnPointerUp(PointerUpEvent evt)
        {
            _pointerId = evt.pointerId;
            var targetId = GetTargetDrop(evt.position);
            ProcessEndDrag(targetId);
            evt.StopPropagation();
        }
        
        void ProcessEndDrag(int targetId)
        {
            var isNewItem = _addingItem.loc == 0;
            if (_draggingItem == null)
            {
                StopDragging();
                return;
            }
            this.ReleasePointer(_pointerId);
            _draggingItem.style.position = Position.Relative;
            _draggingItem.style.width = _draggingItemSize.Item1;
            _draggingItem.style.height = _draggingItemSize.Item2;
            _draggingItem.style.left = StyleKeyword.Null;
            _draggingItem.style.top = StyleKeyword.Null;
            var draggingItem = _draggingItem;
            _draggingItem.schedule.Execute(() => draggingItem.SetEnabled(true));
            
            if (isNewItem)
            {
                ProcessNewItemDrop(targetId);
            }
            else
            {
                ProcessExistingItemDrop(targetId);
            }
            StopDragging();
            StartEditMode();
        }

        void StartEditMode()
        {
            ResetClearBtn();
            Insert(0, _editingUnderlay);
            _editingUnderlay.Add(_editingWindow);
            _editing = true;
        }

        void ProcessNewItemDrop(int targetId)
        {
            var target = GetTargetDrop(targetId);
            if (target != null)
            {
                _addingItem.loc = targetId;
                
                var group = GetCurrentGroup();
                var existingIndex = FindIndex(group.Items, ref _addingItem);
                var hadExisting = false;
                if (existingIndex >= 0)
                {
                    hadExisting = true;
                    group.Items.RemoveAt(existingIndex);
                }
                if (group.Items.Count >= MaxItems)
                {
                    CleanUpOldAnItemGroup(_loadedGroup.Items);
                }
                if (group.Items.Count < MaxItems)
                {
                    target.Add(_draggingItem);

                    if (!string.IsNullOrEmpty(_addingItem.fun) && !string.IsNullOrEmpty(_addingItem.pth))
                    {
                        group.Items.Add(_addingItem);
                        SaveCurrentGroup();
                    }
                    var draggingItem = _draggingItem;
                    var addingItem = _addingItem;
                    draggingItem.userData = addingItem;
                    ConsoleUIUtils.ListenForLongHold(draggingItem, null, this);
                    ConsoleToasts.Show(_overlay.Context, "Item added to shortcut! back to options menu?", _overlay.ShowWithoutAccessChallenge, "Back");
                    if (hadExisting)
                    {
                        _draggingItem.RemoveFromHierarchy();
                        RefreshItems();
                    }
                }
                else
                {
                    ConsoleToasts.Show(_overlay.Context, ConsoleUIStrings.ShortcutsMaxReached.Replace("{MaxItems}", MaxItems.ToString()));
                    _draggingItem.RemoveFromHierarchy();
                }
            }
            else
            {
                _draggingItem.RemoveFromHierarchy();
                _overlay.ShowWithoutAccessChallenge();
            }
        }

        void ProcessExistingItemDrop(int targetId)
        {
            var group = GetCurrentGroup();

            var tempItem = _addingItem;
            tempItem.loc = targetId;
            var hadExisting = false;
            var existingIndex = FindIndex(group.Items, ref tempItem);
            if (existingIndex >= 0)
            {
                hadExisting = true;
                group.Items.RemoveAt(existingIndex);
            }
            var target = GetTargetDrop(targetId);
            var itemIndex = FindIndex(group.Items, ref _addingItem);
            if (itemIndex >= 0)
            {
                group.Items.RemoveAt(itemIndex);
            }
            if (target != null)
            {
                target.Add(_draggingItem);
                if (!string.IsNullOrEmpty(_addingItem.fun) && !string.IsNullOrEmpty(_addingItem.pth))
                {
                    _addingItem.loc = targetId;
                    group.Items.Add(_addingItem);
                }
                _draggingItem.userData = _addingItem;
            }
            else
            {
                _draggingItem.RemoveFromHierarchy();
            }
            SaveCurrentGroup();
            if (hadExisting)
            {
                _draggingItem.RemoveFromHierarchy();
                RefreshItems();
            }
        }

        void ConsoleUIUtils.ILongHoldListener.OnLongHoldTriggered(VisualElement element, int pointerId)
        {
            _pointerId = pointerId;
            if (element.userData is not StoredItem item)
            {
                return;
            }
            if (_editing)
            {
                StartPlacementOf(element, element.worldBound, item);
            }
            else
            {
                this.ReleasePointer(_pointerId);
                ConsoleUIUtils.SendPointerCancelEvent(element);
                StartEditMode();
            }
        }

        VisualElement GetTargetDrop(int loc)
        {
            return loc > 0 && loc <= _shortcutContainers.Length ? _shortcutContainers[loc - 1] : null;
        }

        static readonly Vector2[] SnapPositions = new[]
        {
            new Vector2(0.25f, 0f),
            new Vector2(0.75f, 0f),
            new Vector2(1f, 0.25f),
            new Vector2(1f, 0.75f),
            new Vector2(0.75f, 1f),
            new Vector2(0.25f, 1f),
            new Vector2(0f, 0.75f),
            new Vector2(0f, 0.25f),
        };

        void CreateShortcutContainers()
        {
            _shortcutContainers = new VisualElement[8];
            var topLeftToRight = new VisualElement
            {
                style =
                {
                    left = 0,
                    top = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.FlexStart,
                    alignSelf = Align.FlexStart,
                    //flexWrap = Wrap.Wrap
                }
            };
            Add(topLeftToRight);
            _shortcutContainers[0] = topLeftToRight;
            
            var topLeftToDown = new VisualElement
            {
                style =
                {
                    alignItems = Align.FlexStart,
                    top = Length.Percent(100),
                    paddingTop = 500,
                    translate = new Translate(0, -500, 0f),
                    // ^ hack to show the white highlight all the way to the top
                    marginLeft = -ContainerPadding,
                }
            };
            _shortcutContainers[7] = topLeftToDown;
            
            var topRightToLeft = new VisualElement
            {
                style =
                {
                    right = 0,
                    top = 0,
                    flexDirection = FlexDirection.RowReverse,
                    alignItems = Align.FlexStart,
                    alignSelf = Align.FlexEnd,
                    //flexWrap = Wrap.Wrap
                }
            };
            Add(topRightToLeft);
            _shortcutContainers[1] = topRightToLeft;
            
            var topRightToDown = new VisualElement
            {
                style =
                {
                    alignItems = Align.FlexEnd,
                    top = Length.Percent(100),
                    paddingTop = 500,
                    translate = new Translate(0, -500, 0f),
                    // ^ hack to show the white highlight all the way to the top
                    marginRight = -ContainerPadding,
                }
            };
            _shortcutContainers[2] = topRightToDown;
            
            var bottomRightToLeft = new VisualElement
            {
                style =
                {
                    right = 0,
                    bottom = 0,
                    flexDirection = FlexDirection.RowReverse,
                    alignItems = Align.FlexEnd,
                    //flexWrap = Wrap.Wrap
                }
            };
            Add(bottomRightToLeft);
            _shortcutContainers[4] = bottomRightToLeft;
            
            var bottomRightToUp = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.ColumnReverse,
                    alignItems = Align.FlexEnd,
                    bottom = Length.Percent(100),
                    paddingBottom = 500,
                    translate = new Translate(0, 500, 0f),
                    // ^ hack to show the white highlight all the way to the top
                    marginRight = -ContainerPadding,
                }
            };
            _shortcutContainers[3] = bottomRightToUp;
            
            var bottomLeftToRight = new VisualElement
            {
                style =
                {
                    left = 0,
                    bottom = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.FlexStart,
                    //flexWrap = Wrap.Wrap
                }
            };
            Add(bottomLeftToRight);
            _shortcutContainers[5] = bottomLeftToRight;
            
            var bottomLeftToUp = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.ColumnReverse,
                    alignItems = Align.FlexStart,
                    bottom = Length.Percent(100),
                    paddingBottom = 500,
                    translate = new Translate(0, 500, 0f),
                    // ^ hack to show the white highlight all the way to the top
                    marginLeft = -ContainerPadding,
                }
            };
            _shortcutContainers[6] = bottomLeftToUp;
            foreach (var element in _shortcutContainers)
            {
                StandardContainerStyle(element);
            }
            ClearShortcutContainers(); // < this ensures the parenting is correct straight away.
        }
        
        const int ContainerPadding = 2;
        
        void StandardContainerStyle(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            var sty = element.style;
            sty.position = Position.Absolute;
            //if(sty.flexWrap.keyword == StyleKeyword.Null) sty.flexWrap = Wrap.Wrap;
            if(sty.paddingTop.keyword == StyleKeyword.Null) sty.paddingTop = ContainerPadding;
            if(sty.paddingBottom.keyword == StyleKeyword.Null) sty.paddingBottom = ContainerPadding;
            if(sty.paddingLeft.keyword == StyleKeyword.Null) sty.paddingLeft = ContainerPadding;
            if(sty.paddingRight.keyword == StyleKeyword.Null) sty.paddingRight = ContainerPadding;
        }

        int GetTargetDrop(Vector2 mousePos)
        {
            var rootRect = _overlay.Context.RootElement.contentRect;

            var xPercent = Mathf.Clamp01(mousePos.x / rootRect.width);
            var yPercent = Mathf.Clamp01(mousePos.y / rootRect.height);
            if (xPercent is > 0.35f and < 0.65f && yPercent is > 0.35f and < 0.65f)
            {
                return 0;
            }
            var pos = new Vector2(xPercent, yPercent);
            var bestDistSq = float.MaxValue;
            var bestIndex = 0;
            for (var i = SnapPositions.Length - 1; i >= 0; i--)
            {
                var distSq = (SnapPositions[i] - pos).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }
            return bestIndex + 1;
        }

        void ClearShortcutContainers()
        {
            if (_shortcutContainers == null) return;
            foreach (var element in _shortcutContainers)
            {
                element?.Clear();
            }
            _shortcutContainers[0].Add(_shortcutContainers[7]);
            _shortcutContainers[1].Add(_shortcutContainers[2]);
            _shortcutContainers[4].Add(_shortcutContainers[3]);
            _shortcutContainers[5].Add(_shortcutContainers[6]);
        }

        int _loadedSlotNumber = int.MinValue;
        StoredGroup _loadedGroup;
        StoredGroup GetCurrentGroup()
        {
            var slotNum = CurrentSlotNumber;
            if (_loadedGroup != null && _loadedSlotNumber == slotNum)
            {
                return _loadedGroup;
            }
            _loadedGroup = null;
            var json = PlayerPrefs.GetString(StandardStorageKeys.ShortcutSlotPrefix + slotNum);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    _loadedGroup = JsonUtility.FromJson<StoredGroup>(json);
                }
                catch (Exception e)
                {
                    NjLogger.Warn("There was an error trying to load previous shortcut saves", e);
                }
            }
            _loadedGroup ??= new StoredGroup();
            _loadedGroup.Items ??= new List<StoredItem>();
            _loadedSlotNumber = slotNum;
            return _loadedGroup;
        }

        public void ClearSaveGroup(int saveIndex)
        {
            if (saveIndex == CurrentSlotNumber)
            {
                _loadedGroup = null;
                _loadedSlotNumber = int.MinValue;
            }
            PlayerPrefs.DeleteKey(StandardStorageKeys.ShortcutSlotPrefix + saveIndex);
        }

        int FindIndex(List<StoredItem> items, ref StoredItem item)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var other = items[i];
                if (other.fun == item.fun && other.pth == item.pth && other.loc == item.loc)
                {
                    return i;
                }
            }
            return -1;
        }

        void SaveCurrentGroup()
        {
            if(_loadedGroup == null) return;
            var json = JsonUtility.ToJson(_loadedGroup);
            PlayerPrefs.SetString(StandardStorageKeys.ShortcutSlotPrefix + CurrentSlotNumber, json);
        }

        void CleanUpOldAnItemGroup(List<StoredItem> items)
        {
            if (items.Count < MaxItems) return;
            int bestIndex = -1;
            int bestDay = int.MaxValue;
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var failDay = items[i].fail;
                if (failDay > 0 && failDay < bestDay)
                {
                    bestDay = failDay;
                    bestIndex = i;
                }
            }
            if (bestIndex >= 0)
            {
                items.RemoveAt(bestIndex);
            }
        }

        int CurrentSlotNumber
        {
            get => PlayerPrefs.GetInt(StandardStorageKeys.ShortcutLastSlot);
            set => PlayerPrefs.SetInt(StandardStorageKeys.ShortcutLastSlot, value);
        }

        public bool AutoShow
        {
            get => PlayerPrefs.GetInt(StandardStorageKeys.AutoShowPrevious, 1) != 0;
            set => PlayerPrefs.SetInt(StandardStorageKeys.AutoShowPrevious, value ? 1 : 0);
        }

        struct ChangeTracking
        {
            public WeakReference<IRestorable> Reference;
            public uint? DrawnChange;
        }

        class StoredGroup
        {
            public List<StoredItem> Items;
        }
        
        [Serializable]
        struct StoredItem
        {
            public string fun;
            public int loc;
            public string pth;
            public int fail;
        }
    }
#endif

    public partial class ConsoleScreenShortcuts
    {
        public interface IRestorable
        {
            string FeatureName { get; }
            VisualElement TryRestore(ConsoleContext context, string path);

            /// Increment this number if you need the shortcuts system to refresh the content.
            uint ChangeIndex => 1;
        }
    }
}