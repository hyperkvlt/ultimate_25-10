using System;
using Ninjadini.Console.UI;
using Ninjadini.Logger;
using Ninjadini.Logger.Internal;
using UnityEngine;

namespace Ninjadini.Console.Internal
{
    static class ConsoleAutoStart
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnAfterAssembliesLoaded()
        {
            try
            {
#if !NJCONSOLE_DISABLE  
                ConsoleUtilitiesModule.LocalTimeAtStart = DateTime.Now;
#endif

                if (Application.isEditor)
                {
                    return;
                }
                var settings = ConsoleSettings.Get();
                UnityToNjLogger.Start(settings.inPlayerLogMode, settings.maxLogsHistory);
                NjLogger.SetMinSetTraceLevel((int)settings.stackTraceMinLevel);
                
                /*
                if (settings.writeToSystemLogs && settings.inPlayerLogMode == UnityToNjLogger.Modes.PassThroughToNjLogger)
                {
                    NjLogger.AddHandler(new LoggerToConsoleHandler());
                }*/
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to start NjLogger: " + e);
            }
        }
        
#if !NJCONSOLE_DISABLE    

        static bool _started;
        
        // This could live in another class to be more obvious I guess. I'll forget in 3 months time, where this is.
        static readonly (Type, Func<IConsoleModule>)[] DefaultRuntimeModules = 
        {
            // I am doing this via Func callback to make sure the constructor doesn't get stripped if someone decides to make the lib into a dll.
            (typeof(ConsoleOptions), () => new ConsoleOptions()),
            (typeof(GraphMonitoringModule), () => new GraphMonitoringModule()),
            (typeof(ConsoleHierarchyModule), () => new ConsoleHierarchyModule()),
            (typeof(ConsoleUtilitiesModule), () => new ConsoleUtilitiesModule()),
            (typeof(OptionCommandsModule), () => new OptionCommandsModule()),
        };
  
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SubsystemRegistration()
        {
            // if you have domain reload off, this is needed.
            _started = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            if (_started)
            {
                return;
            }
            _started = true;
            var settings = ConsoleSettings.Get();
            var modules = NjConsole.Modules;
            
            foreach (var module in settings.modules)
            {
                if (module != null && modules.GetModule(module.GetType()) == null)
                {
                    NjConsole.Modules.AddModule(module);
                }
            }
            modules.EnsureModulesExist(DefaultRuntimeModules);
            if (settings.autoStartOverlay)
            {
                ConsoleOverlay.GetOrCreateInstance().WaitForTriggersToShow();
            }
        }
#endif
    }
}