using System;
using System.Collections.Generic;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Random = UnityEngine.Random;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace Ninjadini.Console.Demo
{
    public class DemoNjConsole : MonoBehaviour
    {
        /*
         * Hello, and welcome to NjConsole.
         * Please refer to Documentation/GettingStarted.pdf for more details
         * For even more examples and latest tips and tricks, please check out https://ninjadini.github.io/njconsole/
         */
        
        static readonly LogChannel MyChannel = new LogChannel("myChannel");
        static readonly LogChannel HintsChannel = new LogChannel("hints");

        public Button localDocBtn;
        public Button webDocBtn;

        void Awake()
        {
            SwitchToNewInputSystemIfRequired();
        }

        void Start()
        {
            AddBasicLogs();
            RegisterDemoMenuOptions();
            RegisterDemoCommandLines();
            
            SetUpDemo(); // Ignore... Demo purposes only.
        }

        void AddBasicLogs()
        {
            NjLogger.Log("Welcome to Ninjadini Console demo");
        
            NjLogger.Debug("This is a debug level text - they get auto excluded in release builds");
            NjLogger.Info("This is an info level text");
            NjLogger.Warn("This is a warning level text");
            NjLogger.Error("This is an error level text - an alert shows when an error is logged.");
        
            NjLogger.Info("Mix argument types without allocation... integer:",123," float:", 123.45f," bool:", true);

            var playerObj = GetTestPlayerObj();
            NjLogger.Info("Here is a log with a link to ", playerObj.AsLogRef(), " - you can inspect it");
            
            // For cases where you just have a string and an object, you can directly call in this format - but it only works for 2 params as there are no overloads for other combination.
            // NjLogger.Info("Here is a log with a link to ", aTestObj);
            
            NjLogger.Info("If you don't want a link, this is how... ", playerObj.AsString());

            NjLogger.Info("Paths will automatically produce a link in details, e.g. Assets/Ninjadini.Console/README.txt.");
            NjLogger.Info("Paths for Resources.Load() can produce links using this format: Res(Fonts/JetBrainsMono-Regular)");

            MyChannel.Info("A log in `myChannel`");
            MyChannel.Warn("A warning in `myChannel`");

            NjLogger.Info( Color.cyan, "Passing a Unity Color as the first param will auto color the log to that color.");

            Debug.Log("Logs from Unity’s Debug.Log() automatically appear in NjConsole");
            NjLogger.Info("\u26a0 Now that you have seen the basic logs, you should check out the Options panel");
            
            HintsChannel.Info("<b>Some hints:");
            HintsChannel.Info("> Press and hold any empty area of the sidebar to temporarily peek behind the runtime console.");
            HintsChannel.Info("> Right-click a log to pin it. Pinned logs remain visible regardless of filters.");
            HintsChannel.Info("> Go directly to Command Line prompt by using SHIFT + ` key.");

            var hintColor = new Color(0.44f, 0.94f, 0.63f);
            HintsChannel.Info(hintColor, "\u26a0 Now that you have seen the basic logs, you should check out the <b>Options</b> panel");
            HintsChannel.Info(hintColor, "<color=#70F0A0>⌨ If you want to try the command line, you can type any key to show the prompt UI (or press the `Logs` button at the top if you don't have a physical keyboard). `/help` to list all options.");
        }

        void RegisterDemoMenuOptions()
        {
            // There are 2 ways to create options.
            
            //
            // Method 1: Use [ConsoleOption] attribute in fields/properties/methods in a class and register that class
            // Check out the documentation for the supported formats
            NjConsole.Options.CreateCatalogFrom(this); 
            // ^ This will scan your class for [ConsoleOption] and add options in console.
            
            // To add static members, you need to call by type.
            // The separation exists because the static nature means it can persist without having an instance alive.
            NjConsole.Options.CreateCatalogFrom(GetType()); 
            
            //
            // Method 2: Register the options to catalog manually
            // (This is more performant as it doesn't need to do any reflection)
            // Check out the documentation for the supported formats and examples.
            var catalog = NjConsole.Options.CreateCatalog();
            
            catalog.AddButton("Log Out", ShowDemoMessage)
                .SetHeader("App");
            
            var restartItem = catalog.AddButton("Restart", () => ShowDemoMessage("Restart"))
                .SetHeader("App");
              //.AutoCloseOverlay();  // This auto closes the console when you press the button in the overlay. if in editor window, no change.
#if ENABLE_LEGACY_INPUT_MANAGER
            restartItem.BindToKeyboard(KeyCode.R, ConsoleKeyBindings.Modifier.Ctrl);
#elif ENABLE_INPUT_SYSTEM
            restartItem.BindToKeyboard(UnityEngine.InputSystem.Key.R, ConsoleKeyBindings.Modifier.Ctrl);
#endif
            
            catalog.AddEnumChoice("Preferred Orientation", (v) => PreferredOrientation = v, () => PreferredOrientation).SetHeader("App");

            var scale = 50f;
            catalog.AddNumberPrompt("HealthModifier", f => scale = f, () => scale, 0.1f)
                .SetHeader("Player");
            
            catalog.AddToggle("Invincible", b => InvincibilityActive = b, () => InvincibilityActive)
                .SetHeader("Player");
            
            // sub group examples...
            catalog.AddNumberPrompt("Levels/Go to Level X", (int num) => ShowToast($"You entered to go to level: {num}..."));
            for (var i = 1; i < 100; i++)
            {
                catalog.AddButton($"Levels/Go to Level {i}", ShowDemoMessage);
            }
            for (var i = 1; i < 10; i++)
            {
                catalog.AddButton($"Levels/Tutorials/Go to tutorial {i}", ShowDemoMessage);
            }
            for (var i = 1; i < 10; i++)
            {
                catalog.AddButton($"Levels/Secret Level/Go to secret lvl {i}", ShowDemoMessage);
            }
            catalog.AddButton("Visuals/Hide all", () => { });
            
            catalog.AddButton("Visuals/Vfxs/Enable", () => { });
            catalog.AddButton("Visuals/Vfxs/Disable", () => { });
            var vfxIntensity = 60f;
            catalog.AddNumberPrompt("Visuals/Vfxs/Intensity %", (v) => vfxIntensity = Mathf.Clamp(v, 0, 100), () => vfxIntensity);
            catalog.AddButton("Visuals/Vfxs/Test all", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Delete all", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Filters/Warm", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Filters/Cold", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Filters/Dynamic", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Filters/Vivid", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Filters/Preset 1", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Filters/Preset 2", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Filters/Preset 3", ShowDemoMessage);
            catalog.AddButton("Visuals/Vfxs/Filters/Preset 4", ShowDemoMessage);
            catalog.AddButton("Visuals/Models/Hide all", ShowDemoMessage);
            catalog.AddButton("Visuals/Models/Detail 1", ShowDemoMessage);
            catalog.AddButton("Visuals/Models/Detail 2", ShowDemoMessage);
            catalog.AddButton("Visuals/Models/Detail 3", ShowDemoMessage);
            catalog.AddButton("Visuals/UI/Hide all", ShowDemoMessage);
            catalog.AddButton("Visuals/UI/Detail 1", ShowDemoMessage);
            catalog.AddButton("Visuals/UI/Detail 2", ShowDemoMessage);
            catalog.AddButton("Visuals/UI/Detail 3", ShowDemoMessage);

            catalog.AddButton("Check out `Logs Demo` to stress test some logs", () => ShowToast("Please find the `Logs Demo` folder at the upper area of Options panel."))
                .SetHeader("Demo");

            catalog.AddButton("Check out `Panels Demo` folder for custom panels", () => ShowToast("Please find `Panels Demo` folder at the upper area of Options panel."))
                .SetHeader("Demo");
        }

        void RegisterDemoCommandLines()
        {
            // For command line usage examples, please refer to online doc. https://ninjadini.github.io/njconsole/
            
            var commandsDemo = new CommandLineDemo();
            NjConsole.CommandLineOptions.CreateCatalogFrom(commandsDemo); // read comment below v
            // ^ For demo purposes, we register the commandline options to a dedicated `CommandLineOptions` module.
            // This is optional. You can just register to `NjConsole.Options.CreateCatalogFrom(commandsExample)` instead and it will just appear mixed with other options.
            
            // Just like OptionMenus, to add static members, you need to call by type.
            // The separation exists because the static nature means it can persist without having an instance alive.
            NjConsole.CommandLineOptions.CreateCatalogFrom(typeof(CommandLineDemo)); 
        
            // DEMO PURPOSE CODE ONLY FROM HERE
            // Below are just so the same commands show up inside Options > CommandsDemo as buttons in UI.
            NjConsole.Options.CreateCatalogFrom(commandsDemo,"CommandsDemo");
            NjConsole.Options.CreateCatalogFrom(typeof(CommandLineDemo),"CommandsDemo");
            // By default all Option menus shows up in commandline's root level.
            // But for demo purposes it gets really busy, so we moved the items inside option menus inside `Options` in commandline.
            NjConsole.Options.CommandLinePath = "Options"; // OPTIONAL. read comment above for info ^
        }

        [ConsoleOption]
        [Multiline]
        string IntroText = "Welcome to options panel for cheats and debug controls\nThis is a demo, so the buttons doesn't do anything.";

        [ConsoleOption(header:"Player")]
        public static bool InfiniteLives { get; set; } // This is a static field and it is picked up by scanning the type e.g. NjConsole.Options.CreateCatalogFrom(GetType()); 
        
        [ConsoleOption(increments:0.1f, header: "Player")]
        [Range(0.1f, 5f)]
        public float DamageScale = 1;
        
        bool InvincibilityActive { get; set; } // This is changed via catalog.AddToggle()
        DeviceOrientation PreferredOrientation { get; set; }  // This is changed via catalog.AddEnumChoice()

#if ENABLE_LEGACY_INPUT_MANAGER
        [ConsoleOption("Win Level", key:KeyCode.W, keyModifier:ConsoleKeyBindings.Modifier.Shift)]
#elif ENABLE_INPUT_SYSTEM
        [ConsoleOption("Win Level", key:UnityEngine.InputSystem.Key.W, keyModifier:ConsoleKeyBindings.Modifier.Shift)]
#endif
        public void WinLevelCheat()
        {
            ShowDemoMessage("Win Level Cheat");
        }

        bool _continuousRandomLog;

        [ConsoleOption("Logs demo/Add random log every 200ms")] // < this will show up as a toggle in console > options, inside debug section
        public bool ContinuousRandomLog
        {
            get => _continuousRandomLog;
            set
            {
                _continuousRandomLog = value;
                if (value)
                {
                    ShowToastToLogsPanel("Started logging random texts every 200ms.\nWarning: This includes error logs which will show up at the top bar");
                }
                else
                {
                    ShowToastToLogsPanel("Stopped logging random texts.");
                }
            }
        }
        
        void AddRandomLog()
        {
            var r = Random.value;
            var level = logLevels[Random.Range(0, logLevels.Length)];
            if (r < 0.1f)
            {
                NjLogger.Add("A hello log @ ", DateTime.UtcNow, options: level);
            }
            else if (r < 0.6f)
            {
                var str = loremIpsum[Random.Range(0, loremIpsum.Length)];
                if (level == NjLogger.Options.Warn)
                {
                    Debug.LogWarning(str);
                }
                else if (level == NjLogger.Options.Error)
                {
                    Debug.LogError("(Test Error) " + str);
                }
                else
                {
                    Debug.Log(str);
                }
            }
            else if (r < 0.9f)
            {
                var channel = channels[Random.Range(0, channels.Length)];
                if (level == NjLogger.Options.Error)
                {
                    channel.Add("(Test Error) ", loremIpsum[Random.Range(0, loremIpsum.Length)], " ",loremIpsum[Random.Range(0, loremIpsum.Length)], options: level);
                }
                else
                {
                    channel.Add(loremIpsum[Random.Range(0, loremIpsum.Length)], " ",loremIpsum[Random.Range(0, loremIpsum.Length)], options: level);
                }
            }
            else
            {
                if (level == NjLogger.Options.Error)
                {
                    NjLogger.Add("(Test Error) ",loremIpsum[Random.Range(0, loremIpsum.Length)], " ", loremIpsum[Random.Range(0, loremIpsum.Length)], options: level);
                }
                else
                {
                    NjLogger.Add(loremIpsum[Random.Range(0, loremIpsum.Length)], " ", loremIpsum[Random.Range(0, loremIpsum.Length)], options: level);
                }
            }
        }

        [ConsoleOption("Logs demo/Add a random log")]
        void AddRandomLogMenu()
        {
            AddRandomLog();
            ShowToastToLogsPanel("Added a random log.");
        }

        [ConsoleOption("Logs demo/ThrowException")]
        void ThrowException()
        {
            if (!ErrorBehaviourEnabled)
            {
                ShowToastToLogsPanel("Thrown an exception.");
            }
            throw new Exception("Here is an exception");
        }

        [ConsoleOption("Logs demo/LogException")]
        void LogException()
        {
            NjLogger.Exception(new Exception("Here is an exception to log"));
            if (!ErrorBehaviourEnabled)
            {
                ShowToastToLogsPanel("Added an exception log.");
            }
        }

        [ConsoleOption("Logs demo/LogError")]
        void LogError()
        {
            NjLogger.Error(loremIpsum[Random.Range(0, loremIpsum.Length)], " ", loremIpsum[Random.Range(0, loremIpsum.Length)]);
            if (!ErrorBehaviourEnabled)
            {
                ShowToastToLogsPanel("Added an error log.");
            }
        }

        [ConsoleOption("Logs demo/Debug.Log")]
        void Debug_Log()
        {
            Debug.Log(loremIpsum[Random.Range(0, loremIpsum.Length)], this);
            ShowToastToLogsPanel("Added a random Debug.Log().");
        }
        [ConsoleOption("Logs demo/Debug.LogWarning")]
        void Debug_LogWarning()
        {
            Debug.LogWarning(loremIpsum[Random.Range(0, loremIpsum.Length)], this);
            ShowToastToLogsPanel("Added a random Debug.LogWarning().");
        }

        [ConsoleOption("Logs demo/LogWarning")]
        void LogWarning()
        {
            NjLogger.Warn(loremIpsum[Random.Range(0, loremIpsum.Length)], " ", loremIpsum[Random.Range(0, loremIpsum.Length)]);
            ShowToastToLogsPanel("Added a random warning log.");
        }

        [ConsoleOption("Logs demo/Debug.LogError")]
        void Debug_LogError()
        {
            Debug.LogError(loremIpsum[Random.Range(0, loremIpsum.Length)], this);
            if (!ErrorBehaviourEnabled)
            {
                ShowToastToLogsPanel("Added a random error log.");
            }
        }

        [ConsoleOption("Logs demo/Debug.LogException")]
        void Debug_LogException()
        {
            Debug.LogException(new Exception("Here is an exception for `Debug.LogException()`"), this);
            if (!ErrorBehaviourEnabled)
            {
                ShowToastToLogsPanel("Added a Debug.LogException().");
            }
        }

        [ConsoleOption("Panels Demo/Ai Panel Attached")]
        bool AiPanelAttached
        {
            get => NjConsole.Modules.GetModule<DemoAiPanel>(false) != null;
            set => ToggleModule<DemoAiPanel>(value ? () => new DemoAiPanel() : null);
        }

        [ConsoleOption("Panels Demo/Systems Panel Attached")]
        bool SystemsPanelAttached
        {
            get => NjConsole.Modules.GetModule<DemoSystemsPanel>(false) != null;
            set => ToggleModule<DemoSystemsPanel>(value ? () => new DemoSystemsPanel() : null);
        }
        
            
            
        [ConsoleOption("Panels Demo/OnGui() Panel Attached")]
        bool OnGuiPanelAttached
        {
            get => NjConsole.Modules.GetModule<DemoOnGUIPanel>(false) != null;
            set => ToggleModule<DemoOnGUIPanel>(value ? () => new DemoOnGUIPanel() : null);
        }

        [ConsoleOption("Panels Demo/Custom Options Panel Attached")]
        bool CustomOptionsAttached
        {
            get => NjConsole.Modules.GetModule<CustomOptionsModule>(false) != null;
            set => ToggleModule<CustomOptionsModule>(value ? () => new CustomOptionsModule() : null);
        }
    
        [ConsoleOption("CommandsDemo/Print Help")]
        void PrintClHelp()
        {
#if !NJCONSOLE_DISABLE
            var clElement = NjConsole.Overlay.Window.OpenAndFocusOnCommandLine();
            clElement.Runner.TryRun("/help");
            clElement.SetInputAndSelectEnd("/help");
#endif
        }
        
        //
        //
        // End of demo API code
        //
        //
        void ToggleModule<T>(Func<T> constructorIfAdding) where T : IConsolePanelModule
        {
            if (constructorIfAdding != null)
            {
                if (NjConsole.Modules.GetModule(typeof(T)) == null)
                {
                    var module = constructorIfAdding();
                    NjConsole.Modules.AddModule(module);
                }
            }
            else
            {
                NjConsole.Modules.RemoveModule(typeof(T));
            }
        }
        static void ShowDemoMessage(string msg)
        {
            var txt = $"<b>{msg}:</b> Demo only — this action doesn’t do anything.";
            ShowToast(txt); // < you can use ConsoleToasts in your own code for debug purposes, it shows a toast message at the bottom.
        }
        
        static void ShowDemoMessage()
        {
            var txt = "Demo only — this action doesn’t do anything.";
            ShowToast(txt); // < you can use ConsoleToasts in your own code for debug purposes, it shows a toast message at the bottom.
        }

        void ShowToastToLogsPanel(string message)
        {
            ShowToast(message+"\nSee in <b>Logs</b> panel.", () =>
            {
                NjConsole.Overlay.SetActivePanel("Logs");
            }, "See");
        }
        
        void SetUpDemo()
        {
#if !NJCONSOLE_DISABLE
            localDocBtn?.onClick.AddListener(ConsoleUIUtils.GotoEditorLocalDoc);
            localDocBtn?.gameObject.SetActive(Application.isEditor);
            webDocBtn?.onClick.AddListener(() => Application.OpenURL(ConsoleSettings.WebDocsURL));
            
            // Normally when we log an error (which we did in `AddBasicLogs()`) we show an error at the top of the screen.
            // But because we are doing a demo, we don't want you to see that as the first thing when you play the scene.
            NjConsole.Overlay.Instance?.HideErrorDisplayedAtTop(); // For demo cleanliness purpose only.
#endif
        }

        float _nextLogTime;
        void Update()
        {
            if (_continuousRandomLog)
            {
                if (Time.time >= _nextLogTime)
                {
                    _nextLogTime = Time.time + 0.2f;
                    AddRandomLog();
                }
            }
        }
        
        readonly LogChannel[] channels = new LogChannel[]
        {
            "comms", "AI", "UI", "assets", "logic", "loading", "perf", "app", "system", "platform", "alerts", "player controls", "multiplayer"
        };

        readonly NjLogger.Options[] logLevels = new[]
        {
            NjLogger.Options.Debug,
            NjLogger.Options.Debug,
            NjLogger.Options.Debug,
            NjLogger.Options.Info,
            NjLogger.Options.Info,
            NjLogger.Options.Info,
            NjLogger.Options.Info,
            NjLogger.Options.Info,
            NjLogger.Options.Info,
            NjLogger.Options.Warn,
            NjLogger.Options.Warn,
            NjLogger.Options.Error
        };

        readonly string[] loremIpsum = new[]
        {
            "Lorem ipsum dolor sit amet",
            "Maecenas a condimentum ipsum",
            "Nullam efficitur faucibus purus.",
            "Vivamus convallis placerat ante efficitur",
            "In porttitor nisl quis neque venenatis tempus.",
            "Maecenas orci urna, congue purus quis, vestibulum pharetra.",
            "Proin purus mauris, feugiat sit felis et, maximus rutrum sapien",
            "Vivamus egestas justo id augue eleifend, quis placerat purus sagittis",
            "Phasellus in iaculis libero. Nunc turpis risus, finibus id velit at, tempus elementum ante.",
            "Nunc sollicitudin varius leo imperdiet luctus. Fusce lacinia sapien quis metus tincidunt gravida. Nullam mattis aliquet lacus sit amet posuere. Fusce in vestibulum arcu",
            "Aliquam interdum lectus eget pretium efficitur. Donec aliquet pretium varius. Nunc a quam urna. Nullam sit amet enim est. Vivamus eleifend lectus et libero ultricies, ac tempus lorem molestie. Aliquam erat volutpat.",
            "Maecenas pharetra sapien tincidunt sapien maximus semper.\nNulla scelerisque sodales risus et luctus.\nDuis non tortor sed metus viverra posuere quis finibus est.\nProin sit amet consequat felis.\nVestibulum eget ipsum eu urna tristique rhoncus."
        };
        
        
        PlayerProfile _testPlayerObj;
        PlayerProfile GetTestPlayerObj()
        {
            if (_testPlayerObj != null) return _testPlayerObj;
            _testPlayerObj = PlayerProfile.CreateTestPlayer();
            return _testPlayerObj;
        }

        static void ShowToast(string message, Action ctaCallback = null, string ctaBtnName = null)
        {
#if !NJCONSOLE_DISABLE
            ConsoleToasts.TryShow(message, ctaCallback, ctaBtnName);
#endif
        }

        static bool ErrorBehaviourEnabled =>
#if !NJCONSOLE_DISABLE
            NjConsole.Overlay.Instance.ErrorBehaviourEnabled;
#else
            false;
#endif

        class DemoAiPanel : IConsolePanelModule
        {
            public string Name => "AI";

            public float SideBarOrder => 10;

            public VisualElement CreateElement(ConsoleContext context)
            {
                return new BasicDemoPanel("AI");
            }
        }

        class DemoSystemsPanel : IConsolePanelModule
        {
            public string Name => "Systems";

            public float SideBarOrder => 11;

            public VisualElement CreateElement(ConsoleContext context)
            {
                return new BasicDemoPanel("systems");
            }
        }

        class BasicDemoPanel : VisualElement
        {
            public BasicDemoPanel(string panelName)
            {
                AddToClassList("panel"); // just adds a background
                Add(new Label($"This is a demo custom `{panelName}` panel using UI Toolkit's VisualElement to render the panel." +
                              $"\nYou can add panels/modules to console at runtime OR you can create an IConsoleExtension and add it from project settings to auto start." +
                              $"\nSee documentation for more details"));
            }
        }

        class CustomOptionsModule : ConsoleOptions
        {
            public override string Name => "Custom\nOptions";

            public override float SideBarOrder => 14;

            [ConsoleOption] 
            public bool AToggle;
                
            [ConsoleOption]
            public float ANumber;

            public CustomOptionsModule()
            {
                var catalog = CreateCatalogFrom(this);
                catalog.AddButton("A Button", ShowDemoMessage);
            }
        }

        class DemoOnGUIPanel : IConsoleIMGUIPanelModule
        {
            public string Name => "OnGUI";

            public float SideBarOrder => 13;

            public IConsoleIMGUI CreateIMGUIPanel(ConsoleContext context)
            {
                var txt = $"This is a demo custom IMGUI/OnGUI panel using OnGUI() to render the panel." +
                          $"\nYou can add panels/modules to console at runtime OR you can create an IConsoleExtension and add it from project settings to auto start." +
                          $"\nSee documentation for more details";
                return new BasicOnGUIDemoPanel(txt);
            }
        }

        class BasicOnGUIDemoPanel : IConsoleIMGUI
        {
            string _text;
            public BasicOnGUIDemoPanel(string text)
            {
                _text = text;
            }

            public void OnGUI()
            {
                GUILayout.Label(_text);
            }
        }
        
        

        void SwitchToNewInputSystemIfRequired()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (!eventSystem)
            {
                return;
            }
            var oldInputModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (!oldInputModule)
            {
                return;
            }
            Destroy(oldInputModule);

        // if you are seeing an error here, it means you have enabled the new input system but haven't installed the package.
        // Install InputSystem package in package manager.
        // Alternatively, go to Project Settings > Player > Other Settings > Active Input Handling > choose Input Manager (old)
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#endif
        }
    }
    
    
    public class PlayerProfile
    {
        public string Name;
        public int Age;
        public DateTime CreationDate;
        public string Location;
        public string Comments;
        public int Level;
        public List<PlayerInventory> Inventory = new ();

        public string GetPlayerInfo()
        {
            return $"{Name} [{Age}] @{Location}";
        }

        public void GiveInventoryItem(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            foreach (var inventory in Inventory)
            {
                if (inventory.Name == name)
                {
                    inventory.Count++;
                    return;
                }
            }
            Inventory.Add(new PlayerInventory()
            {
                Name = name,
                Count = 1
            });
        }
        
        public static PlayerProfile CreateTestPlayer()
        {
            var result = new PlayerProfile()
            {
                Name = "Test Player",
                Age = 25,
                CreationDate = new DateTime(2025, 1, 2, 13, 14, 5),
                Location = "Planet Earth",
                Comments = "Ninjadini Console is awesome!",
                Level = 60
            };
            result.GiveInventoryItem("Mail Armor");
            result.GiveInventoryItem("Cloth Leggings");
            result.GiveInventoryItem("Basic Axe");
            return result;
        }
    }

    public class PlayerInventory
    {
        public string Name;
        public int Count;
    }
    
    public class CommandLineDemo
    {
        [ConsoleOption]
        string FavoriteFood  = "Ramen";
        
        [ConsoleOption]
        bool GodMode;

        [ConsoleOption]
        float PlayerSpeed = 5.0f;
        
        [ConsoleOption]
        [Tooltip("Example param: \"Master Chiefy\" 30")]
        static string Introduce(string name, int age)
        {
            return $"Hello, my name is {name}, I am {age} years old";
        }

        [ConsoleOption("math / vector magnitude")]
        [Tooltip("Example param: 1 2 3  OR  (1,2,3)")]
        static float Magnitude(Vector3 a) => a.magnitude;

        [ConsoleOption("math / vector multiply")]
        [Tooltip("Example param: (1 2 3) 1  OR  (1,2,3),1")]
        static Vector3 MultiplyV(Vector3 a, float b) => a * b;
        
        [ConsoleOption("GuessTheNumber (input prompt demo)")] // advanced demo where you can take over the input prompt.
        public IConsoleCommandlineModule StartGuessNumberGame()
        {
            var game = new GuessNumberCommandLineGame();
            game.Init();
            return game;
        }
    }

    public class GuessNumberCommandLineGame : IConsoleCommandlineModule
    {
        int _numAttempts;
        int _goalNum;

        public void Init()
        {
            _goalNum = Random.Range(0, 101);
            NjLogger.Info("===========");
            NjLogger.Info("||");
            NjLogger.Info("||");
            NjLogger.Info("||");
            NjLogger.Info("||");
            NjLogger.Warn("Welcome to Guess the Number — a simple command line prompt challenge / demo");
            NjLogger.Info(Instruction);
        }

        public bool TryRun(IConsoleCommandlineModule.Context ctx)
        {
            if (int.TryParse(ctx.Input, out var num))
            {
                _numAttempts++;
                if (num == _goalNum)
                {
                    ctx.Output.Info($"Correct! You guessed the number in {_numAttempts} tries! Good bye...");
                    // no need to set result to this here because we are existing the module.
                }
                else if (num < _goalNum)
                {
                    ctx.Output.Info(LowStrs[Random.Range(0, LowStrs.Length)] + (_numAttempts));
                    ctx.Result = this;
                }
                else
                {
                    ctx.Output.Info(HighStrs[Random.Range(0, HighStrs.Length)] + (_numAttempts));
                    ctx.Result = this;
                }
            }
            else if (ctx.Input.ToLowerInvariant().Trim() == "exit")
            {
                ctx.Output.Info("Better luck next time. Good bye!");
                return true;
            }
            else
            {
                ctx.Output.Warn("That was not a number... " + Instruction);
                ctx.Result = this;
            }
            return true;
        }
        
        public void FillAutoCompletableHints(IConsoleCommandlineModule.HintContext ctx)
        {
#if !NJCONSOLE_DISABLE
            var remaining = StringParser.GetRemainingPartialMatch(ctx.Input, "exit");
            if (remaining != null)
            {
                ctx.Add(remaining, "<alpha=#44> Exit the demo game");
                return;
            }
            if (int.TryParse(ctx.Input, out var num) && num >= 0 && num <= 100)
            {
                ctx.Add("", "<0 - 100>", -ctx.Input.Length);
            }
            else
            {
                ctx.Add("", "\u26a0 Requires between 0 and 100. or type exit.", -ctx.Input.Length);
            }
#endif
        }
        
        const string Instruction = "Type a number between 0-100 below \u2193";

        static readonly string[] LowStrs = new[]
        {
            "Try a higher number. Attempts so far: ",
            "Go higher. Attempts: ",
            "Not quite — the target is higher. Tries: "
        };
        static readonly string[] HighStrs = new[]
        {
            "Try a lower number. Attempts so far: ",
            "Go lower. Attempts: ",
            "Not yet — the target is lower. Tries: "
        };

    }
}