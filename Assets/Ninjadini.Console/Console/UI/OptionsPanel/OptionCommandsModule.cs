using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Console.Internal;
using Ninjadini.Console.UI;
using UnityEngine;

namespace Ninjadini.Console
{
    public class ConsoleOptionsForCommands : ConsoleOptions, IConsolePanelModule
    {
        public override string Name => "CommandLine";

        public override float SideBarOrder => 20;

        bool IConsolePanelModule.PanelFeatureEnabled => false;
    }
    
    public class OptionCommandsModule : IConsoleCommandlineModule
    {
        public bool Enabled { get; set; } = true;
        
#if !NJCONSOLE_DISABLE
        ConsoleModules _modules;
        
        bool IConsoleModule.PersistInEditMode => true;
        
        void IConsoleModule.OnAdded(ConsoleModules modules)
        {
            _modules = modules;
        }

        void IConsoleCommandlineModule.PopulateHelpSuggestions(List<string> helpLines)
        {
            var startingIndex = helpLines.Count;
            foreach (var (type, module) in _modules.AllModules)
            {
                if (module is not ConsoleOptions opts || opts.CommandLinePath == null)
                {
                    continue;
                }
                if (opts.GetType() == typeof(ConsoleOptions))
                {
                    helpLines.Add("<b>Option Commands: </b>");
                }
                else
                {
                    helpLines.Add($"<b>[{opts.GetType().Name}] Commands: </b>");
                }
                startingIndex++;
                var prefix = string.IsNullOrEmpty(opts.CommandLinePath) ? "> " : $"> {opts.CommandLinePath}/";
                PopulateHelpSuggestions(helpLines, opts.CommandLineRoot, prefix);
            }
            if (helpLines.Count == startingIndex)
            {
                helpLines.Add($"{IConsoleCommandlineModule.FailPrefix}No option commands set up... {nameof(OptionCommandsModule)} uses the same paths as Options menu.");
                if (Application.isEditor)
                {
                    foreach (var str in ConsoleUIStrings.OptsHowToSetup.Split("\n"))
                    {
                        if (!string.IsNullOrEmpty(str))
                        {
                            helpLines.Add("| "+str);
                        }
                    }
                }
            }
            else
            {
                helpLines.Add("\ufffd "+ StringParser.ParamGroupingInfo);
            }
        }

        public static void PopulateHelpSuggestions(List<string> helpLines, ConsoleOptions.GroupItem group, string prefixStr)
        {
            if (group.ChildGroups != null)
            {
                foreach (var child in group.ChildGroups)
                {
                    PopulateHelpSuggestions(helpLines, child, prefixStr);
                }
            }
            if (group.ChildItems != null)
            {
                foreach (var item in group.ChildItems)
                {
                    var tooltip = GetArgsAndTooltip(item, out var hasArg);
                    if (string.IsNullOrEmpty(tooltip))
                    {
                        helpLines.Add(prefixStr+item.Path);
                    }
                    else if(hasArg)
                    {
                        helpLines.Add($"{prefixStr}{item.Path} {tooltip}");
                    }
                    else
                    {
                        helpLines.Add($"{prefixStr}{item.Path}{tooltip}");
                    }
                }
            }
        }
        
        public void FillAutoCompletableHints(IConsoleCommandlineModule.HintContext hintContext)
        {
            foreach (var kv in _modules.AllModules)
            {
                if (kv.Value is ConsoleOptions opts && opts.CommandLinePath != null)
                {
                    FillAutoCompletableHints(hintContext, opts.CommandLineRoot, true);
                }
            }
            hintContext.Result.Sort((a, b) => a.cmd.Length - b.cmd.Length);
        }

        public static void FillAutoCompletableHints(IConsoleCommandlineModule.HintContext hintContext, ConsoleOptions.GroupItem group, bool isRoot)
        {
            if (!StringParser.FindNextWord(hintContext.Input, hintContext.Index, out var start, out var length))
            {
                if (isRoot && hintContext.Input.Length > hintContext.Index)
                {
                    // we'll only hint the ones that start without a slash or whitespace etc.
                    return;
                }
                if (group.ChildGroups != null)
                {
                    foreach (var child in group.ChildGroups)
                    {
                        hintContext.Add(ShouldAddSlash(hintContext.Input) ? $"/{child.Name}/" : (child.Name + "/"), null);
                    }
                }
                if (group.ChildItems != null)
                {
                    foreach (var child in group.ChildItems)
                    {
                        var n = ShouldAddSlash(hintContext.Input) ? ("/" + child.Name) : child.Name;
                        var tooltip = GetArgsAndTooltip(child, out var hasArg);
                        if(hasArg) hintContext.Add(n + " ", tooltip);
                        else hintContext.Add(n, tooltip);
                    }
                }
                return;
            }
            if (isRoot && hintContext.Index != start)
            {
                // we'll only hint the ones that start without a slash or whitespace etc.
                return;
            }
            if (StringParser.IsWhiteSpace(hintContext.Input, start + length)) // last content, take all the rest as is, in case we have whitespace in input.
            {
                length = hintContext.Input.Length - start;
            }
            var needsGroupMatch = hintContext.Input.Length > start + length && hintContext.Input[start + length] =='/';
            if (group.ChildGroups != null)
            {
                foreach (var child in group.ChildGroups)
                {
                    if (length > child.Name.Length) continue;
                    var txt = StringParser.GetRemainingPartialMatchNonCase(hintContext.Input, child.Name, start, length);
                    if (txt == null)
                    {
                        // failed
                    }
                    else if (txt.Length == 0) // exact match
                    {
                        var prevIndex = hintContext.Index;
                        hintContext.Index = start + length + 1;
                        FillAutoCompletableHints(hintContext, child, false);
                        hintContext.Index = prevIndex;
                    }
                    else if(!needsGroupMatch)
                    {
                        hintContext.Add(txt + "/", null);
                    }
                }
            }
            if (group.ChildItems != null && !needsGroupMatch)
            {
                foreach (var child in group.ChildItems)
                {
                    var txt = StringParser.GetRemainingPartialMatchNonCase(hintContext.Input, child.Name, start, length);
                    if (txt == null) continue; // No match.
                    if(txt.Length == 0 && length > child.Name.Length && !char.IsWhiteSpace(hintContext.Input[start + child.Name.Length])) continue; // the input overflows the name.
                    var tooltip = GetArgsAndTooltip(child, out var hasArg);
                    var startOfArgsAdjustment = child.Name.Length - length;
                    if (hasArg)
                    {
                        hintContext.Add(txt + " ", tooltip, startOfArgsAdjustment);
                    }
                    else if(hintContext.Input.Length - start <= child.Name.Length)
                    {
                        hintContext.Add(txt, tooltip, startOfArgsAdjustment);
                    }
                    // else there is no reason to show this one as its invalid, there are no args expected
                }
            }
        }

        static bool ShouldAddSlash(string input)
        {
            for (var i = input.Length - 1; i >= 0; i--)
            {
                var c = input[i];
                if (char.IsWhiteSpace(c))
                {
                    continue;
                }
                return c != '/';
            }
            return false;
        }

        public bool TryRun(IConsoleCommandlineModule.Context ctx)
        {
            foreach (var kv in _modules.AllModules)
            {
                if (kv.Value is ConsoleOptions opts
                    && opts.CommandLinePath != null
                    && TryRun(ctx, opts.CommandLineRoot, true))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryRun(IConsoleCommandlineModule.Context ctx, ConsoleOptions.GroupItem groupItem, bool isRoot)
        {
            if (!StringParser.FindNextWord(ctx.Input, ctx.Index, out var start, out var length))
            {
                return false;
            }
            if (isRoot && ctx.Index != start)
            {
                // we'll only run the ones that start without a slash or whitespace etc.
                return false;
            }
            if (groupItem.ChildGroups != null)
            {
                foreach (var child in groupItem.ChildGroups)
                {
                    if (StringParser.SubRangeEqualsNonCase(ctx.Input, start, length, child.Name))
                    {
                        var prevIndex = ctx.Index;
                        ctx.Index = start + length;
                        if (TryRun(ctx, child, false))
                        {
                            return true;
                        }
                        ctx.Index = prevIndex;
                    }
                }
            }

            var childItems = groupItem.ChildItems;
            if (childItems == null || childItems.Count == 0)
            {
                return false;
            }

            foreach (var item in childItems)
            {
                if (StringParser.SubRangeEqualsNonCase(ctx.Input, start, length, item.Name))
                {
                    ctx.Index = start + length;
                    Run(item, ctx);
                    return true;
                }
            }

            if (!StringParser.IsWhiteSpace(ctx.Input, start + length))
            {
                // didn't find any match. And we have more words after this group.
                return false;
            }

            // ok we need to backtrack to see if we can break up the words to params
            var end = ctx.Input.Length;
            while (end > ctx.Index + 1)
            {
                end--;
                if (char.IsWhiteSpace(ctx.Input[end]) && !char.IsWhiteSpace(ctx.Input[end - 1]))
                {
                    foreach (var item in childItems)
                    {
                        if (StringParser.SubRangeEqualsNonCase(ctx.Input, start, end - start, item.Name))
                        {
                            ctx.Index = end;
                            Run(item, ctx);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static string GetArgsAndTooltip(ConsoleOptions.OptionItem item, out bool hasArg)
        {
            var args = GetArgsHint(item);
            hasArg = !string.IsNullOrEmpty(args);
            if (hasArg)
            {
                args = $"<alpha=#99><noparse><{args}></noparse>";
            }
            var tooltip = item.Tooltip;
            if (string.IsNullOrEmpty(tooltip))
            {
                return "<size=95%>" + args;
            }
            if (hasArg)
            {
                return $"<size=95%>{args} <alpha=#88>{tooltip}";
            }
            return $"<size=95%> <alpha=#88>{tooltip}";
        }

        static string GetArgsHint(ConsoleOptions.OptionItem item)
        {
            if (item is ConsoleOptions.ToggleItem toggleItem)
            {
                return toggleItem.Setter != null ? "true|false|1|0" : null;
            }
            if (item is ConsoleOptions.NumberItem numberItem)
            {
                return numberItem.Data.ResultCallback != null ? "number" : null;
            }
            if (item is ConsoleOptions.TextItem textItem)
            {
                return textItem.Data.ResultCallback != null ? "string" : null;
            }
            if (item is ConsoleOptions.ChoiceItem choiceItem)
            {
                if (choiceItem.List == null || choiceItem.SetSelectedIndex == null) return null;
                var list = choiceItem.List;
                if (list.Count > 10)
                {
                    return string.Join(" | ", list.Take(8)) + $" | +{list.Count - 8}more";
                }
                return string.Join(" | ", list);
            }
            if (item is ConsoleOptions.CmdItem cmdItem)
            {
                var parametersInfo = cmdItem.Method?.GetParameters();
                if (parametersInfo != null)
                {
                    return string.Join("> <", parametersInfo.Select(p => 
                        p.HasDefaultValue ? $"{p.ParameterType.Name}? {p.Name}" : $"{p.ParameterType.Name} {p.Name}"
                    ));
                }
            }
            return null;
        }

        static void Run(ConsoleOptions.OptionItem item, IConsoleCommandlineModule.Context ctx)
        {
            // normally I would have put the code in each OptionItem subclass - OOP style, but I wanted CommandLine stuff to be an extension that sit outside the core stuff.
            if (item is ConsoleOptions.ButtonItem buttonItem)
            {
                RunButtonItem(ctx, buttonItem);
            }
            else if (item is ConsoleOptions.ToggleItem toggleItem)
            {
                RunToggleItem(ctx, toggleItem);
            }
            else if (item is ConsoleOptions.TextItem textItem)
            {
                RunTextItem(ctx, textItem);
            }
            else if (item is ConsoleOptions.ChoiceItem choiceItem)
            {
                RunChoiceItem(ctx, choiceItem);
            }
            else if (item is ConsoleOptions.CmdItem cmdItem)
            {
                RunFunc(ctx, cmdItem.Method, cmdItem.Target, cmdItem.Getter);
            }
            else
            {
                var paramStr = ctx.Input.Substring(ctx.Index).Trim();
                if(string.IsNullOrEmpty(paramStr))
                {
                    throw new NotImplementedException("Unhandled option item call.");
                }
                throw new NotImplementedException("Unhandled option item call with param: " + paramStr);
            }
        }

        static void RunButtonItem(IConsoleCommandlineModule.Context ctx, ConsoleOptions.ButtonItem buttonItem)
        {
            if (StringParser.IsWhiteSpace(ctx.Input, ctx.Index))
            {
                buttonItem.Callback?.Invoke();
            }
            else
            {
                PrintInvalidArg(ctx, ctx.Input.Substring(ctx.Index).Trim());
            }
        }

        static void RunToggleItem(IConsoleCommandlineModule.Context ctx, ConsoleOptions.ToggleItem toggleItem)
        {
            var args = StringParser.SplitParams(ctx.Input.Substring(ctx.Index), 1);
            if (args != null)
            {
                if (toggleItem.Setter != null)
                {
                    toggleItem.Setter((bool) StringParser.Parse(args[0], typeof(bool)));
                    if(toggleItem.Getter != null)
                    {
                        SetReturnedResult(ctx, toggleItem.Getter());
                    }
                }
                else
                {
                    ctx.Output.Warn(OptionIsReadOnly);
                }
            }
            else if(toggleItem.Getter != null)
            {
                SetReturnedResult(ctx, toggleItem.Getter());
            }
            else
            {
                ctx.Output.Warn(OptionIsNotReadable);
            }
        }

        static void RunTextItem(IConsoleCommandlineModule.Context ctx, ConsoleOptions.TextItem textItem)
        {
            if (!StringParser.IsWhiteSpace(ctx.Input, ctx.Index))
            {
                var paramStr = ctx.Input.Substring(ctx.Index);
                if (textItem.Data.ResultCallback == null)
                {
                    ctx.Output.Warn(OptionIsReadOnly);
                }
                else if ((bool)StringParser.Invoke(textItem.Data.ResultCallback.Method, textItem.Data.ResultCallback.Target, paramStr, ctx.StoredRefs) != true)
                {
                    PrintInvalidArg(ctx, paramStr);
                }
                else if(textItem.Getter != null)
                {
                    SetReturnedResult(ctx, textItem.Getter());
                }
            }
            else if(textItem.Getter != null)
            {
                SetReturnedResult(ctx, textItem.Getter());
            }
            else
            {
                ctx.Output.Warn(OptionIsNotReadable);
            }
        }

        static void RunChoiceItem(IConsoleCommandlineModule.Context ctx, ConsoleOptions.ChoiceItem choiceItem)
        {
            var args = StringParser.SplitParams(ctx.Input.Substring(ctx.Index), 1);
            if (args != null)
            {
                if (choiceItem.SetSelectedIndex == null)
                {
                    ctx.Output.Warn(OptionIsReadOnly);
                    return;
                }
                var index = choiceItem.List.IndexOf(args[0]);
                if (index < 0)
                {
                    index = choiceItem.List.FindIndex(i => i.ToLowerInvariant() == args[0].ToLowerInvariant());
                }
                if (index < 0)
                {
                    PrintInvalidArg(ctx, args[0], string.Join(" | ", choiceItem.List));
                    return;
                }
                choiceItem.SetSelectedIndex(index);
                if(choiceItem.GetSelectedIndex != null && choiceItem.List != null)
                {
                    SetReturnedResult(ctx, GetSelected(choiceItem));
                }
            }
            else if(choiceItem.GetSelectedIndex != null && choiceItem.List != null)
            {
                SetReturnedResult(ctx, GetSelected(choiceItem));
            }
            else
            {
                ctx.Output.Warn(OptionIsNotReadable);
            }
        }

        static string GetSelected(ConsoleOptions.ChoiceItem choiceItem)
        {
            if (choiceItem.GetSelectedIndex == null || choiceItem.List == null) return null;
            var index = choiceItem.GetSelectedIndex();
            if (index >= 0 && index <= choiceItem.List.Count)
            {
                return choiceItem.List[index];
            }
            else
            {
                return $"index {index}";
            }
        }

        public static void RunFunc(IConsoleCommandlineModule.Context ctx, 
            MethodInfo methodInfo, object target, Func<object> getter = null)
        {
            if (methodInfo == null)
            {
                ctx.Output.Warn($"\u26a0 Delegate function is null");
                return;
            }
            if (getter != null && StringParser.IsWhiteSpace(ctx.Input, ctx.Index))
            {
                SetReturnedResult(ctx, getter());
                return;
            }
            var result = StringParser.Invoke(methodInfo, target, ctx.Input.Substring(ctx.Index), ctx.StoredRefs);
            if (methodInfo.ReturnType != typeof(void))
            {
                SetReturnedResult(ctx, result);
            }
        }

        public static void SetReturnedResult(IConsoleCommandlineModule.Context ctx, object obj)
        {
            ctx.Result = obj;
            if (!ConsoleCommandlineRunner.IsAutoScopeObject(obj))
            {
                ctx.Output.Info(IConsoleCommandlineModule.ResultPrefix, (obj is string str ? $"‘<noparse>{str}</noparse>’" : obj) ?? "null");
            }
        }

        public static bool TrySetReturnedResultToCurrentContext(object resultObj, ConsoleContext context = null)
        {
            context ??= ConsoleContext.TryGetFocusedContext();
            if (context?.Window?.ActivePanelElement is not ConsoleLogsPanel logsPanel)
            {
                return false;
            }
            var cmdCtx = logsPanel.CommandLineElement?.Runner?.CurrentContext;
            if (cmdCtx == null)
            {
                return false;
            }
            SetReturnedResult(cmdCtx, resultObj);
            return true;
        }
        
        public static void PrintInvalidArg(IConsoleCommandlineModule.Context ctx, string str, string validStr = null)
        {
            if (string.IsNullOrEmpty(validStr))
            {
                ctx.Output.Warn(IConsoleCommandlineModule.FailPrefix, $"Invalid argument ‘<noparse>{str}</noparse>’");
            }
            else
            {
                ctx.Output.Warn(IConsoleCommandlineModule.FailPrefix,$"Invalid argument ‘<noparse>{str}</noparse>’. Valid params: ", validStr);
            }
        }

        static readonly string OptionIsReadOnly = $"{IConsoleCommandlineModule.FailPrefix}Option is read only";
        static readonly string OptionIsNotReadable = $"{IConsoleCommandlineModule.FailPrefix}Option requires parameters.";
#else
        void IConsoleCommandlineModule.FillAutoCompletableHints(IConsoleCommandlineModule.HintContext hintContext) { }
        void IConsoleCommandlineModule.PopulateHelpSuggestions(List<string> helpLines) { }
        bool IConsoleCommandlineModule.TryRun(IConsoleCommandlineModule.Context ctx) => false;
#endif
    }
}