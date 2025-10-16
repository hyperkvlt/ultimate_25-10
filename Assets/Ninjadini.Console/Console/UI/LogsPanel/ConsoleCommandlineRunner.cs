#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using Ninjadini.Console.UI;
using Ninjadini.Logger;

namespace Ninjadini.Console.Internal
{
    public class ConsoleCommandlineRunner
    {
        readonly ConsoleContext _context;

        public const string NormalPrompt = ">";
        public const string LockedPrompt = "!:";
        
        public IConsoleCommandlineModule.Context CurrentContext { get; private set; }
        public IConsoleCommandlineModule LockedModule { get; internal set; }
        public event Action<string, bool, object> RanAction;

        public ConsoleCommandlineRunner(ConsoleContext context)
        {
            _context = context;
        }

        public bool TryRun(string cmdString)
        {
            return TryRun(new IConsoleCommandlineModule.Context(cmdString, IConsoleCommandlineModule.Channel, Storage));
        }

        public bool TryRun(IConsoleCommandlineModule.Context ctx)
        {
            CurrentContext = ctx;
            try
            {
                if (LockedModule != null)
                {
                    var module = LockedModule;
                    LockedModule = null;
                    if (module.TryRun(ctx))
                    {
                        OnResult(ctx);
                        return true;
                    }
                    LockedModule ??= module;
                    ctx.Output.Warn(IConsoleCommandlineModule.FailPrefix, $"Your input was not handled by `{LockedModule.GetType().Name}`. If you are stuck, press cancel to exit input prompt mode.");
                    RanAction?.Invoke(ctx.Input, false, ctx.Result);
                    return false;
                }
                else if (RunFromModules(ctx))
                {
                    return true;
                }
            }
            catch (ConsoleCommandException err)
            {
                ctx.Output.Warn(IConsoleCommandlineModule.FailPrefix, err.Message);
                RanAction?.Invoke(ctx.Input, false, ctx.Result);
                return true;
            }
            catch (Exception err)
            {
                if (err.InnerException is ConsoleCommandException innerE)
                {
                    ctx.Output.Warn(IConsoleCommandlineModule.FailPrefix, innerE.Message);
                }
                else
                {
                    ctx.Output.Warn(IConsoleCommandlineModule.FailPrefix, err);
                }
                RanAction?.Invoke(ctx.Input, false, ctx.Result);
                return true;
            }
            finally
            {
                CurrentContext = null;
            }
            ctx.Output.Warn(IConsoleCommandlineModule.FailPrefix, $"Undefined command “<noparse>{ctx.Input}</noparse>”... Try <b><u>/help</u></b> for more info with list of commands.");
            RanAction?.Invoke(ctx.Input, false, ctx.Result);
            return false;
        }

        bool RunFromModules(IConsoleCommandlineModule.Context ctx)
        {
            var slashStartIndex = GetStartingSlashIndex(ctx.Input);
            if (slashStartIndex >= 0)
            {
                ctx.Index = slashStartIndex + 1;
                if (OptionCommandsModule.TryRun(ctx, GetBuildInGroup(), true))
                {
                    OnResult(ctx);
                    return true;
                }
            }
            foreach (var kv in _context.Modules.AllModules)
            {
                if (kv.Value is IConsoleCommandlineModule commandlineModule && commandlineModule.Enabled)
                {
                    ctx.Index = 0;
                    if (commandlineModule.TryRun(ctx))
                    {
                        OnResult(ctx);
                        return true;
                    }
                }
            }
            var basicInput = ctx.Input.ToLowerInvariant().Trim();
            if (basicInput == "help" || basicInput == "-help") //just safety fallback
            {
                RunHelp(ctx);
                return true;
            }
            if (basicInput.Length > 1 && basicInput.StartsWith("$"))
            {
                ctx.Result = Retrieve(basicInput.Substring(1));
                OnResult(ctx);
                return true;
            }
            return false;
        }

        public void CancelLockedModule()
        {
            LockedModule = null;
        }

        void OnResult(IConsoleCommandlineModule.Context ctx)
        {
            if (ctx.Result != null)
            {
                SetLastResult(ctx.Result);
                if (IsAutoScopeObject(ctx.Result))
                {
                    SetScope(ctx.Result);
                }
                if (ctx.Result is IConsoleCommandlineModule newLock)
                {
                    // the lock is auto removed as soon as you run something so it only force set if it new.
                    LockedModule = newLock;
                }
            }
            RanAction?.Invoke(ctx.Input, true, ctx.Result);
        }

        public static bool IsAutoScopeObject(object obj)
        {
            return obj != null 
                   && obj is not string 
                   && obj is not IConsoleCommandlineModule 
                   && obj.GetType().IsClass; // !StringParser.IsPrimitiveLike(obj.GetType());
        }

        public void ClearLastResult()
        {
            //Storage.StoreAsLastResult(null);
        }

        public void SetLastResult(object obj)
        {
            Storage.StoreAsLastResult(obj);
        }

        public void SetScope(object obj, ILogChannel channelToReport = null)
        {
            Storage.StoreAsScope(obj);
            channelToReport ??= IConsoleCommandlineModule.Channel;
            if (obj == null)
            {
                channelToReport?.Info(IConsoleCommandlineModule.FailPrefix, "Scope <u>$@</u> set to null.");
            }
            else
            {
                channelToReport?.Info(IConsoleCommandlineModule.ResultPrefix, (obj is string str ? $"‘<noparse>{str}</noparse>’" : obj).AsLogRef(), "<alpha=#99>; Scope <u>$@</u> set.");
            }
        }

        public void RevertToPreviousScope()
        {
            var lastObj = Storage.GetPreviousScope();
            if (lastObj != null)
            {
                SetScope(lastObj);
            }
            else
            {
                throw new ConsoleCommandException("Last scope is null");
            }
        }

        ConsoleObjReferenceStorage Storage => _context.Modules.GetOrCreateModule<ConsoleObjReferenceStorage>();
        
        void RunHelp(IConsoleCommandlineModule.Context ctx)
        {
            var lines = new List<string>();
            lines.Add("<b>Built-in Commands: </b>");
            OptionCommandsModule.PopulateHelpSuggestions(lines, GetBuildInGroup(), "> /");
            var numModules = 0;
            var startingIndex = lines.Count;
            foreach (var kv in _context.Modules.AllModules)
            {
                if (kv.Value is not IConsoleCommandlineModule commandlineModule || !commandlineModule.Enabled) continue;
                numModules++;
                commandlineModule.PopulateHelpSuggestions(lines);
            }
            if (numModules == 0)
            {
                ctx.Output.Warn($"\u274c No <u>{nameof(IConsoleCommandlineModule)}</u> found — modules may be disabled or not registered.");
            }
            else if (lines.Count <= startingIndex)
            {
                ctx.Output.Warn($"\u274c {numModules} <u>{nameof(IConsoleCommandlineModule)}</u> registered, but no help suggestions were generated.");
            }
            else
            {
                foreach (var line in lines)
                {
                    ctx.Output.Info(line);
                }
                ctx.Output.Info("\ufffd See NjConsole online doc for latest and best examples. Use \u2191/\u2193 to navigate command history. Tab to accept autocomplete. Shift+\u2191/\u2193 to navigate autocomplete suggestions. Esc to hide command line. Shift+<activation key> to jump to commandline.");
            }
        }
        
        public void FillAutoCompletableHints(IConsoleCommandlineModule.HintContext hintContext)
        {
            if (LockedModule != null)
            {
                LockedModule.FillAutoCompletableHints(hintContext);
                return;
            }
            var slashStartIndex = GetStartingSlashIndex(hintContext.Input);
            if (slashStartIndex >= 0)
            {
                hintContext.Index = slashStartIndex + 1;
                OptionCommandsModule.FillAutoCompletableHints(hintContext, GetBuildInGroup(), true);
            }
            foreach (var kv in _context.Modules.AllModules)
            {
                if (kv.Value is IConsoleCommandlineModule commandlineModule && commandlineModule.Enabled)
                {
                    hintContext.Index = 0;
                    commandlineModule.FillAutoCompletableHints(hintContext);
                }
            }
        }

        int GetStartingSlashIndex(string input)
        {
            var index = input.IndexOf("/", StringComparison.InvariantCulture);
            if (index >= 0)
            {
                if (StringParser.IsWhiteSpace(input, 0, index))
                {
                    return index;
                }
            }
            return -1;
        }
        
        ConsoleOptions.GroupItem _buildInGroup;
        ConsoleOptions.GroupItem GetBuildInGroup()
        {
            if (_buildInGroup == null)
            {
                _buildInGroup = new ConsoleOptions.GroupItem();
                var catalog = new ConsoleOptions.Catalog(_buildInGroup, _context.Modules);
                
                catalog.AddButton("help", () => RunHelp(CurrentContext ?? new IConsoleCommandlineModule.Context("help", IConsoleCommandlineModule.Channel, Storage)))
                    .SetTooltip("List all possible commands");
                    
                catalog.AddButton("filter cmds", FilterToCommands)
                    .SetTooltip("Set logs filtering to only show logs from Command Line");
                
                catalog.AddButton("filter reset", FilterReset)
                    .SetTooltip("Clear all filters");
                    
                catalog.AddButton("clear logs", NjLogger.LogsHistory.Clear)
                    .SetTooltip("Clear logs");
                    
                catalog.AddCommand("store", (Action<string>)Store)
                    .SetTooltip("Store the last result object <u>$_</u> to provided name");
                    
                catalog.AddCommand("retrieve", (Func<string, object>)Retrieve)
                    .SetTooltip("Retrieve stored object by name");
                    
                catalog.AddButton("list stored", ListStorage)
                    .SetTooltip("List all stored objects");
                    
                catalog.AddButton("clear stored", ClearStorage)
                    .SetTooltip("Clear all stored objects");
                    
                catalog.AddCommand("scope", (Action<string>)SetScopeCmd)
                    .SetTooltip("Set Scope <u>$@</u> to last result object <u>$_</u>. Optionally you may pass the name of the stored object <u>$</u>** to set to that instead.");
                    
                catalog.AddButton("rescope",RevertToPreviousScope)
                    .SetTooltip($"Set Scope <u>$@</u> back to previous scope <u>${ConsoleObjReferenceStorage.PreviousScopeName}</u>");
                
                catalog.AddCommand("inspect", (Action<string>)Inspect)
                    .SetTooltip("Inspect: the last returned item OR the saved item if name param is passed.");
                    
                catalog.AddCommand("destroy", (Action<string>)DestroyObj)
                    .SetTooltip("Destroy the last returned item OR the saved item if name param is passed.");
                    
                catalog.AddButton("find type", FindType)
                    .SetTooltip("Find types in assembly using the type search prompt UI");
                    
                catalog.AddCommand("call", (Func<string, object>)CallMember)
                    .SetTooltip("Call a field, property or method of the scope <u>$@</u>. e.g. scope.MyMethod(v1, v2) => `MyMethod v1 v2`");

                if (_context.IsRuntimeOverlay)
                {
                    catalog.AddButton("close", () => _context.RuntimeOverlay?.Hide()).SetTooltip("Close this console window.");
                }
            }
            return _buildInGroup;
        }

        void SetScopeCmd(string storedName = null)
        {
            SetScope(string.IsNullOrEmpty(storedName) ? Storage.GetLastResult() : Storage.GetStored(storedName));
        }

        public void FilterReset()
        {
            if (_context.Window?.ActivePanelElement is ConsoleLogsPanel logsPanel)
            {
                ((IConsolePanelModule.IElement)logsPanel).OnReselected();
            }
        }

        public void FilterToCommands()
        {
            if (_context.Window?.ActivePanelElement is ConsoleLogsPanel logsPanel)
            {
                ((IConsolePanelModule.IElement)logsPanel).OnReselected();
                logsPanel.Filters.SetActiveChannels(new[] { IConsoleCommandlineModule.Channel.Name });
                IConsoleCommandlineModule.Channel.Info(IConsoleCommandlineModule.FeedbackPrefix, "Set logs filter to ", IConsoleCommandlineModule.Channel.Name, ". Call `filter reset` to reset (or clear the filter via' Channels' button in UI at the top)");
            }
        }

        void Store(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ConsoleCommandException("Name required");
            }
            var obj = Storage.GetLastResult();
            if (obj == null)
            {
                throw new ConsoleCommandException("Last result <u>$_</u> is null.");
            }
            Storage.Store(name, obj);
            IConsoleCommandlineModule.Channel.Info(IConsoleCommandlineModule.FeedbackPrefix + "Stored ", obj.AsLogRef(), " to $" + name);
        }

        object Retrieve(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ConsoleCommandException("Name required");
            }
            var obj = Storage.GetStored(name);
            if (obj == null)
            {
                IConsoleCommandlineModule.Channel.Warn(IConsoleCommandlineModule.FailPrefix, "Failed to retrieve $"+name, ". Call <b><u>/list stored</u></b> to list all stored objects");
            }
            else
            {
                IConsoleCommandlineModule.Channel.Info(IConsoleCommandlineModule.FeedbackPrefix, "Retrieved $"+name+": ",obj.AsLogRef());
            }
            return obj;
        }

        public void ListStorage()
        {
            IConsoleCommandlineModule.Channel.Info(Storage._saves.Count, " stored objects:");
            foreach (var kv in Storage._saves)
            {
                var midText = ": ";
                if (kv.Key == ConsoleObjReferenceStorage.LastResultName)
                {
                    midText = " <alpha=#99>(last returned object)<alpha=#FF>: ";
                }
                else if (kv.Key == ConsoleObjReferenceStorage.ScopeName)
                {
                    midText = " <alpha=#99>(command scope)<alpha=#FF>: ";
                }
                else if (kv.Key == ConsoleObjReferenceStorage.PreviousScopeName)
                {
                    midText = " <alpha=#99>(previous scope)<alpha=#FF>: ";
                }
                IConsoleCommandlineModule.Channel.Info("$"+kv.Key, midText, kv.Value.AsLogRef());
            }
        }

        void ClearStorage()
        {
            var count = Storage._saves.Count;
            Storage.Clear();
            IConsoleCommandlineModule.Channel.Info(IConsoleCommandlineModule.FeedbackPrefix + "Cleared ", count, " objects");
        }

        void Inspect(string storeName = null)
        {
            var obj = string.IsNullOrEmpty(storeName) ? Storage.GetLastResult() : Storage.GetStored(storeName);
            if (obj == null)
            {
                throw new ConsoleCommandException("Object to inspect is null.");
            }
            ConsoleInspector.Show(GetWindowOrThrow(), obj);
        }

        void DestroyObj(string storeName = null)
        {
            var obj = string.IsNullOrEmpty(storeName) ? Storage.GetLastResult() : Storage.GetStored(storeName);
            if (obj == null)
            {
                throw new ConsoleCommandException("Object to destroy is null.");
            }
            if (obj is UnityEngine.Object unityObj)
            {
                var sb = LoggerUtils.TempStringBuilder.Clear();
                sb.Append(IConsoleCommandlineModule.FeedbackPrefix);
                sb.Append("Destroyed ");
                StrValue.FillObject(sb, unityObj, unityObj.GetType());
                var str = sb.ToString();
                UnityEngine.Object.Destroy(unityObj);
                CurrentContext?.Output?.Info(str);
            }
            else
            {
                throw new ConsoleCommandException($"Object to destroy is not a Unity Object but {obj.GetType().FullName}");
            }
        }

        void FindType()
        {
            var window = GetWindowOrThrow();
            ConsoleInspector.ShowClassSearchPrompt(GetWindowOrThrow(), (result) =>
            {
                if (result != null)
                {
                    SetScope(result);
                    SetLastResult(result);
                    window.OpenAndFocusOnCommandLine();
                }
            });
        }

        object CallMember(string memberAndParams)
        {
            var obj = GetScopeOrThrow();
            if (string.IsNullOrWhiteSpace(memberAndParams))
            {
                throw new ConsoleCommandException("Requires name of field, property or method, followed by any params.");
            }
            var spaceIndex = memberAndParams?.IndexOf(" ", StringComparison.InvariantCulture) ?? -1;
            if (spaceIndex < 0)
            {
                return StringParser.CallMember(obj, memberAndParams, "", Storage);
            }
            var member = memberAndParams.Substring(0, spaceIndex);
            var argsStr = memberAndParams.Substring(spaceIndex + 1).Trim();
            return StringParser.CallMember(obj, member, argsStr, Storage);
        }

        object GetScopeOrThrow()
        {
            var obj = Storage.GetScope();
            if (obj == null)
            {
                throw new ConsoleCommandException("Current $@ is null.");
            }
            return obj;
        }

        ConsoleWindow GetWindowOrThrow()
        {
            var window = _context.Window ?? ConsoleContext.TryGetFocusedContext()?.Window;
            if (window == null)
            {
                throw new ConsoleCommandException("No active NjConsole window found to show the NjConsole inspector.");
            }
            return window;
        }
    }
}
#endif