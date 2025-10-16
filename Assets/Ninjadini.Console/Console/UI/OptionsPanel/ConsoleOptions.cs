using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Console.Internal;
using Ninjadini.Console.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Component = UnityEngine.Component;

namespace Ninjadini.Console
{
    public partial class ConsoleOptions : IConsolePanelModule, ConsoleScreenShortcuts.IRestorable
    {
        /// <summary>
        /// Create a new options menu catalog.<br/>
        /// Catalogs are useful because when you no longer need a set of option menus, you can just call catalog.RemoveAll().
        /// Example code:
        /// <code>
        /// var catalog = NjConsole.Options.CreateCatalog();
        /// catalog.AddButton("My First Button", () => Debug.Log("Clicked my first button"));
        /// catalog.AddButton("A Directory / Child Directory / Child Button", () => Debug.Log("Child button was clicked"))
        ///        .SetHeader("My header");
        /// // ^ Put the button inside a header sub-group.
        /// catalog.AddButton("My Space Bound Button", () => Debug.Log("Clicked my space bound button"))
        ///        .BindToKeyboard(KeyCode.Space)
        ///        .AutoCloseOverlay();
        /// // ^ Here we also bind to keyboard and set it to auto close the overlay on click (if it's a button)
        
        /// catalog.RemoveAll();
        /// // ^ Call to remove both buttons when no longer needed.
        /// </code>
        /// </summary>
        public Catalog CreateCatalog()
        {
#if NJCONSOLE_DISABLE
            return new Catalog();
#else
            return new Catalog(_root, _modules);
#endif
        }
        
        /// <summary>
        /// Create a catalog using object reflection<br/>
        /// This is the alternative path to creating menus. Using [ConsoleOption] attribute.
        /// To add static fields/properties/methods, you need to pass the type instead `CreateCatalogFrom(typeof(MyStaticOptionsClass))`
        ///
        /// Declare some example options in your code:
        ///<code>
        /// 
        /// public class TestOptions : MonoBehaviour {
        ///
        /// void Start() {
        ///     NjConsole.Options.CreateCatalogFrom(this, "TestOptions");
        ///     // ^ second param `TestOptions` is optional, it puts all the items inside the `TestOptions` group in this example.
        ///     // If the item being created from is a MonoBehaviour, it will automatically remove the options when the monobehaviour is destroyed.
        /// }
        /// 
        /// [ConsoleOption]
        /// void SayHello() {
        ///     Debug.Log("Hello");
        /// }
        ///
        /// [ConsoleOption("Set Player Name")]
        /// public void SetName(string newName) {
        ///     Debug.Log("SetName caleld: " + newName);
        /// }
        ///
        /// [ConsoleOption(key:UnityEngine.InputSystem.Key.I, header:"Player")] // bind to keyboard key I and put inside header sub-group "Player"
        /// bool InfiniteLives;
        ///
        /// [ConsoleOption(increments:1, header:"Player")]
        /// int Health {get; set;}
        ///
        /// [ConsoleOption(increments:1, header:"Player")]
        /// [Range(1, 10)]
        /// float Speed;
        ///
        /// }
        /// 
        /// </code>
        /// </summary>
        public Catalog CreateCatalogFrom(object obj, string parentPath = null, bool autoRemoveOnMonoBehaviourDestroy = true)
        {
#if NJCONSOLE_DISABLE
            return new Catalog();
#else
            var catalog = new Catalog(_root, _modules);
            if (obj != null)
            {
                if (obj is Component component && autoRemoveOnMonoBehaviourDestroy)
                {
                    var autoRemove = component.gameObject.GetComponent<ConsoleOptionsCatalogAutoRemove>();
                    if (!autoRemove)
                    {
                        autoRemove = component.gameObject.AddComponent<ConsoleOptionsCatalogAutoRemove>();
                    }
                    autoRemove.Add(component, catalog);
                }
                AddByReflection(obj, catalog, parentPath);
            }
            return catalog;
#endif
        }

        public virtual string Name => "Options";
        public virtual float SideBarOrder => 2;
        
        public virtual bool PersistInEditMode => false;
        
#if !NJCONSOLE_DISABLE
        
        ConsoleModules _modules;
        readonly GroupItem _root = new()
        {
            Name = string.Empty
        };
        GroupItem _clRoot;
        
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GroupItem Root => _root;

        public bool PanelFeatureEnabled { get; protected set; } = true;

        void IConsoleModule.OnAdded(ConsoleModules modules)
        {
            _modules = modules;
            if (GetType() == typeof(ConsoleOptions))
            {
                PanelFeatureEnabled = Application.isEditor || modules.Settings.inPlayerOptionsPanel;
            }
        }
        
        VisualElement IConsolePanelModule.CreateElement(ConsoleContext context)
        {
            return new PanelElement(context, this);
        }

        string _shortCutFeatureName;
        string ConsoleScreenShortcuts.IRestorable.FeatureName
        {
            get
            {
                _shortCutFeatureName ??= "opts:" + Name;
                return _shortCutFeatureName;
            }
        }

        /// Path prefix for commandline.<br/>
        /// Default value is empty, all options will show up at root level of CommandLine paths.
        /// Setting to null will hide all options under this module in CommandLine
        public string CommandLinePath
        {
            get => _root.Name;
            set => _root.Name = value;
        }

        public GroupItem CommandLineRoot
        {
            get
            {
                if (string.IsNullOrEmpty(_root.Name))
                {
                    return _root;
                }
                if(_clRoot == null)
                {
                    _clRoot = new GroupItem();
                    _clRoot.AddChildGroup(_root);
                }
                return _clRoot;
            }
        }

        VisualElement ConsoleScreenShortcuts.IRestorable.TryRestore(ConsoleContext context, string path)
        {
            if (path.EndsWith("/"))
            {
                var group = _root.FindGroup(path);
                if (group != null)
                {
                    return PanelElement.CreateGroupBtnForShortcut(context, Name, group);
                }
                return null;
            }
            var item = _root.FindItem(path);
            var btn = item?.CreateElement(this, context);
            PanelElement.SetupBtn(btn, item);
            return btn;
        }

        uint ConsoleScreenShortcuts.IRestorable.ChangeIndex => _root.ChangeIndex;

        ConsoleUIUtils.PlayerPrefHistoryStrings _history;
        public ConsoleUIUtils.PlayerPrefHistoryStrings History
        {
            get
            {
                if (_history == null)
                {
                    _history = new ConsoleUIUtils.PlayerPrefHistoryStrings($"{StandardStorageKeys.OptionsHistory}_{GetType().Name}", 12, "/ /");
                }
                return _history;
            }
        }
 
        void IConsoleModule.Dispose()
        {
            _root.ChildGroups?.Clear();
            _root.ChildItems.Clear();
        }
#else
        bool IConsolePanelModule.PanelFeatureEnabled => false;
        VisualElement IConsolePanelModule.CreateElement(ConsoleContext context) => null;
        VisualElement ConsoleScreenShortcuts.IRestorable.TryRestore(ConsoleContext context, string path) => null;
        string ConsoleScreenShortcuts.IRestorable.FeatureName => null;
        public string CommandLinePath { get; set; }
#endif
    }

    public sealed class ConsoleOptionsForEditMode : ConsoleOptions, IConsolePanelModule
    {
        public override string Name => "Editor\nOptions";
        public override float SideBarOrder => 5;
        public override bool PersistInEditMode => true;
    }
}