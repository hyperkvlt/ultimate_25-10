#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Console.Internal;

namespace Ninjadini.Console
{
    public partial class ConsoleOptions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class GroupItem
        {
            public string Name { get; internal set; }
            public uint ChangeIndex { get; private set; }
            
            List<GroupItem> _childGroups;
            readonly List<OptionItem> _childItems = new List<OptionItem>();

            public List<GroupItem> ChildGroups => _childGroups;
            public List<OptionItem> ChildItems => _childItems;
            
            public void Add(string path, OptionItem item)
            {
                var group = GetGroupAndWord(path, true, out var nameStart, out var nameLength);
                if (group != null)
                {
                    item.Name = path.Substring(nameStart, nameLength);
                    item.Path = path;
                    ChangeIndex++;
                    group.AddChildItem(item);
                }
            }

            void AddChildItem(OptionItem item)
            {
                for (var index = _childItems.Count - 1; index >= 0; index--)
                {
                    if (string.Equals(_childItems[index].Name, item.Name, StringComparison.Ordinal))
                    {
                        _childItems[index].OnRemoved();
                        _childItems.RemoveAt(index);
                        break;
                    }
                }
                ChangeIndex++;
                _childItems.Add(item);
            }

            GroupItem GetGroupAndWord(string path, bool canCreate, out int nameStart, out int nameLength, List<GroupItem> parentsChain = null)
            {
                // a little overengineered :D because I didn't want to use split / allocate
                if (StringParser.FindNextWord(path, 0, out var wordStart, out var wordLength))
                {
                    return GetGroupAndWord(path, canCreate, wordStart, wordLength, out nameStart, out nameLength, parentsChain);
                }
                nameStart = nameLength = 0;
                return null;
            }
            
            GroupItem GetGroupAndWord(string path, bool canCreate, int wordStart, int wordLength, out int nameStart, out int nameLength, List<GroupItem> parentsChain = null)
            {
                if (StringParser.FindNextWord(path, wordStart + wordLength, out var nextWordStart, out var nextWordLength))
                {
                    if (_childGroups != null)
                    {
                        foreach (var otherGroup in _childGroups)
                        {
                            if (StringParser.SubRangeEquals(path, wordStart, wordLength, otherGroup.Name))
                            {
                                parentsChain?.Add(otherGroup);
                                return otherGroup.GetGroupAndWord(path, canCreate, nextWordStart, nextWordLength, out nameStart, out nameLength, parentsChain);
                            }
                        }
                    }
                    if (!canCreate)
                    {
                        nameStart = nameLength = 0;
                        return null;
                    }
                    _childGroups ??= new List<GroupItem>();
                    var childGroup = new GroupItem()
                    {
                        Name = path.Substring(wordStart, wordLength)
                    };
                    _childGroups.Add(childGroup);
                    parentsChain?.Add(childGroup);
                    ChangeIndex++;
                    return childGroup.GetGroupAndWord(path, true, nextWordStart, nextWordLength, out nameStart, out nameLength);
                }
                nameStart = wordStart;
                nameLength = wordLength;
                return this;
            }

            static readonly List<GroupItem> _tempChain = new List<GroupItem>();
            
            public void Remove(string path, Catalog requiredCatalog = null)
            {
                _tempChain.Clear();
                var group = GetGroupAndWord(path, false, out var nameStart, out var nameLength, _tempChain);
                if (group == null)
                {
                    _tempChain.Clear();
                    return;
                }
                for (var index = group._childItems.Count - 1; index >= 0; index--)
                {
                    if (StringParser.SubRangeEquals(path, nameStart, nameLength, group._childItems[index].Name))
                    {
                        if (requiredCatalog != null && group._childItems[index].Catalog != requiredCatalog)
                        {
                            break;
                        }
                        group._childItems[index].OnRemoved();
                        group._childItems.RemoveAt(index);
                        group.ChangeIndex++;
                        CleanOutEmptyGroups(_tempChain);
                        break;
                    }
                }
                _tempChain.Clear();
                ChangeIndex++;
            }

            public OptionItem FindItem(string path)
            {
                var group = GetGroupAndWord(path, false, out var nameStart, out var nameLength);
                if (group == null)
                {
                    return null;
                }
                foreach (var childItem in group._childItems)
                {
                    if (StringParser.SubRangeEquals(path, nameStart, nameLength, childItem.Name))
                    {
                        return childItem;
                    }
                }
                return null;
            }

            public GroupItem FindGroup(string path)
            {
                var group = GetGroupAndWord(path, false, out var nameStart, out var nameLength);
                if (group?._childGroups != null)
                {
                    foreach (var childGroup in group._childGroups)
                    {
                        if (StringParser.SubRangeEquals(path, nameStart, nameLength, childGroup.Name))
                        {
                            return childGroup;
                        }
                    }
                }
                return null;
            }

            void CleanOutEmptyGroups(List<GroupItem> parentsChain)
            {
                if (parentsChain.Count == 0)
                {
                    return;
                }
                var group = parentsChain[^1];
                if (group._childGroups is { Count: > 0 })
                {
                    return;
                }
                if (group._childItems.Count > 0)
                {
                    return;
                }
                parentsChain.RemoveAt(parentsChain.Count - 1);
                var parent = parentsChain.Count > 0 ? parentsChain[^1] : this;
                parent._childGroups.Remove(group);
                parent.ChangeIndex++;
                CleanOutEmptyGroups(parentsChain);
            }

            internal void AddChildGroup(GroupItem item)
            {
                _childGroups ??= new List<GroupItem>();
                _childGroups.Add(item);
            }
        }
    }
}
#endif