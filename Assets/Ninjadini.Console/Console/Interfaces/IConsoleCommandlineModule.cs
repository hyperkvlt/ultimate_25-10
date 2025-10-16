using System;
using System.Collections.Generic;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;

namespace Ninjadini.Console
{
    /// <summary>
    /// This allows you to have your own custom command line execution module<br/>
    /// You an also return an object which implements this interface to accept input prompts (see doc / example)<br/>
    /// 1. First create a class that implements both IConsoleCommandlineModule and IConsoleExtension.<br/>
    /// 2. Add [Serializable] attribute to the class.<br/>
    /// 3. Go to project settings > NjConsole > Extension Modules > add your new class in the list.<br/>
    /// See online documentation for more info.
    /// </summary>
    public interface IConsoleCommandlineModule : IConsoleModule
    {
        public bool Enabled => true;
        void PopulateHelpSuggestions(List<string> helpLines) { }
        void FillAutoCompletableHints(HintContext ctx);
        bool TryRun(Context ctx);
        
        public static LogChannel Channel { get; } = new LogChannel("cmd");
        public const string FeedbackPrefix = "<color=#80eaff>";
        public const string ResultPrefix = "<color=#7FFFB0>> ";
        public const string FailPrefix = "\u26a0 ";
        
        public class Context
        {
            public readonly string Input;
            public readonly ILogChannel Output;
            public ConsoleObjReferenceStorage StoredRefs;
            public int Index;
            public object Result;

            public Context(string cmdInput, ILogChannel logOutput, ConsoleObjReferenceStorage storedRefs)
            {
                Input = cmdInput;
                Output = logOutput;
                StoredRefs = storedRefs;
            }
        }
        
        public class HintContext
        {
            public readonly string Input;
            public readonly List<(string cmd, string tooltip, int startOfCmdAdjustment)> Result = new List<(string, string, int)>();
            public int Index;
            
            public HintContext(string cmdInput)
            {
                Input = cmdInput;
            }

            public void Add(string command, string tooltip, int startOfCmdAdjustment = 0)
            {
                if (!Result.Exists(c => c.cmd == command))
                {
                    Result.Add((command, tooltip, startOfCmdAdjustment));
                }
            }
        }
    }

    public class ConsoleObjReferenceStorage : IConsoleModule
    {
        public const string LastResultName = "_";
        public const string ScopeName = "@";
        public const string PreviousScopeName = "@prev";
        
        public Dictionary<string, object> _saves = new Dictionary<string, object>();
        public object GetStored(string name)
        {
            if (_saves.TryGetValue(name, out var result))
            {
                return result;
            }
            if (name.Length > 1 && name.StartsWith("$"))
            {
                return _saves.GetValueOrDefault(name.Substring(1));
            }
            return null;
        }

        public void Store(string name, object value)
        {
            if (value != null)
            {
                _saves[name] = value;
            }
            else
            {
                _saves.Remove(name);
            }
        }

        public object GetLastResult() => GetStored(LastResultName);

        public void StoreAsLastResult(object value)
        {
            Store(LastResultName, value);
        }
        
        public object GetScope() => GetStored(ScopeName);
        
        public object GetPreviousScope() => GetStored(PreviousScopeName);

        public void StoreAsScope(object value)
        {
            var prevScope = GetScope();
            if (prevScope != value)
            {
                Store(PreviousScopeName, prevScope);
            }
            Store(ScopeName, value);
        }

        public void Clear()
        {
            _saves.Clear();
        }
    }

    public class ConsoleCommandException : Exception
    {
        public ConsoleCommandException(string message) : base(message)
        {
            
        }
    }
}