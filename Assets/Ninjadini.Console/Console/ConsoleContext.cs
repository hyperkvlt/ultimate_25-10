using System;
using System.Collections.Generic;
using Ninjadini.Console.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console
{
    /// <summary>
    /// ConsoleContext is the central place for a particular console window/UI
    /// While NjConsole / ConsoleModules is shared across many windows, ConsoleContext is an instance per window.
    /// This is where you want to store data specific to an individual window / UI.
    /// SetData() / GetData() will store some data in memory only.
    /// Storage.Get...() / Set...() will store it to PlayerPref for runtime window, and for editor window it'll be serialised into the window's instance as storage.
    /// </summary>
    public class ConsoleContext
    {
        public readonly ConsoleModules Modules;
        public readonly IStorage Storage;
        public readonly VisualElement RootElement;
        public ConsoleSettings Settings => Modules.Settings;
        
        readonly Dictionary<Type, object> _dataByType = new ();
        
#if !NJCONSOLE_DISABLE

        static ConsoleContext _focusedContext;

#pragma warning disable CS8632
        public readonly ConsoleOverlay? RuntimeOverlay;
        public ConsoleWindow? Window { get; internal set; }
        public UIDocument? RuntimeUIDocument => RuntimeOverlay?.UIDocument; // This can be null in editor
#pragma warning restore CS8632 // This can be null in editor
        
        public ConsoleContext(ConsoleModules modules, IStorage storage, VisualElement rootElement, ConsoleOverlay runtimeOverlay = null)
        {
            Modules = modules;
            Storage = storage;
            RootElement = rootElement;
            RuntimeOverlay = runtimeOverlay;
        }

        public bool IsRuntimeOverlay => RuntimeOverlay != null;

        public static void SetFocusedContext(ConsoleContext context)
        {
            _focusedContext = context;
        }
        public static ConsoleContext TryGetFocusedContext()
        {
            return _focusedContext ?? ConsoleOverlay.Instance?.Context;
        }

        StyleSheet _styleSheet;
        public StyleSheet StyleSheet
        {
            get
            {
                if (_styleSheet == null)
                {
                    _styleSheet = Resources.Load<StyleSheet>(ConsoleSettings.StyleSheetResName);
                }
                return _styleSheet;
            }
            set
            {
                if(value != null)
                {
                    _styleSheet = value;
                }
                else
                {
                    throw new System.ArgumentNullException(nameof(StyleSheet));
                }
            }
        }
#endif

        public void SetData(object data)
        {
            _dataByType[data.GetType()] = data;
        }

        public T GetData<T>() where T : class
        {
            _dataByType.TryGetValue(typeof(T), out var data);
            return data as T;
        }
        
        public interface IStorage
        {
            void SetString(string key, string value);
            string GetString(string key);
            void SetInt(string key, int value);
            int GetInt(string key, int defaultValue = 0);
        }

        public class PlayerPrefsStorage : IStorage
        {
            public void SetString(string key, string value)
            {
                PlayerPrefs.SetString(key, value);
            }   
            
            public string GetString(string key)
            {
                return PlayerPrefs.GetString(key);
            }
            
            public void SetInt(string key, int value)
            {
                PlayerPrefs.SetInt(key, value);
            }
            
            public int GetInt(string key, int defaultValue = 0)
            {
                return PlayerPrefs.GetInt(key, defaultValue);
            }
        }
        
        static IEditorBridge _editorBridge;
        public static IEditorBridge EditorBridge
        {
            get { return _editorBridge ??= new NonEditorBridge(); }
            set => _editorBridge = value ?? throw new ArgumentNullException(nameof(EditorBridge));
        }
        
        public interface IEditorBridge
        {
            public const string ResPathPrefix = "RES:";
            
            bool HasEditorFeatures { get; }
            bool ValidFilePath(string path);
            
            StackSkipType ShouldSkipStackFrame(StackFrame frame);
            
            bool GoToFile(object pathOrStackTraceObj, int stackIndex);
            void PingObject(UnityEngine.Object obj);

            public enum StackSkipType
            {
                DoNotSkip, // always show
                SkipEarly, // hide if it appears in early stage of stack before any is shown yet
                Hide // always hide
            }

            public struct StackFrame
            {
                public string Name;
                public string FilePath;
                public int LineNumber;
            }
        }

        public class NonEditorBridge : IEditorBridge
        {
            public bool HasEditorFeatures => false;

            public IEditorBridge.StackSkipType ShouldSkipStackFrame(IEditorBridge.StackFrame frame) => IEditorBridge.StackSkipType.DoNotSkip;

            public bool GoToFile(object stackTraceObj, int stackIndex)
            {
                return true;
            }

            public void PingObject(UnityEngine.Object obj)
            {
                
            }

            public bool ValidFilePath(string path) => false;
        }
    }
}