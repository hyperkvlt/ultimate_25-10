using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ninjadini.Console.Tests")]
[assembly: InternalsVisibleTo("Ninjadini.Console.Editor")]
[assembly: InternalsVisibleTo("Ninjadini.Console.Experimental")]
namespace Ninjadini.Console
{
    /// Central place where all console features/modules are registered.
    /// Generally there is only 1 instance of ConsoleModules even if you have multiple console windows open such as in editor window or at runtime.
    /// Module are central place to store the features, but not the state of UI
    /// If you are looking to store/minipulate the state of a particular window, see ConsoleContext or store directly in the view element.
    public class ConsoleModules
    {
        /// <summary>
        /// Access to run time console settings.<br/>
        /// This is direct instance to the asset in resources directory. If you change the values in editor, it may get saved into the asset.
        /// </summary>
        public readonly ConsoleSettings Settings;
        
        readonly Dictionary<Type, IConsoleModule> _modules;
        
        /// All registered modules as type and instance dictionary.
        /// This is direct access to core of Console, so please do not modify the dictionary.
        public readonly IReadOnlyDictionary<Type, IConsoleModule> AllModules;
        
        /// Event dispatched when a module is added.
        /// Please remember to unregister the event. ConsoleModules static instance is shared by multiple console windows and play instances, so it can easily start leaking fast.
        public event Action<IConsoleModule> ModuleAdd;
        
        /// Event dispatched when a module is removed.
        /// Please remember to unregister the event. ConsoleModules static instance is shared by multiple console windows and play instances, so it can easily start leaking fast.
        public event Action<IConsoleModule> ModuleRemoved;

        public ConsoleModules(ConsoleSettings settings)
        {
            Settings = settings;
            AllModules = _modules = new Dictionary<Type, IConsoleModule>();
        }

        /// <summary>
        /// Find a registered module by type.
        /// </summary>
        /// <param name="includeSubClasses">Set to true if you want to match subtypes of T also</param>
        public T GetModule<T>(bool includeSubClasses)
        {
            if (includeSubClasses)
            {
                foreach (var kv in _modules)
                {
                    if(kv.Value is T t)
                    {
                        return t;
                    }
                }
                return default;
            }
            if (_modules.TryGetValue(typeof(T), out var result))
            {
                return (T)result;
            }
            return default;
        }

        /// <summary>
        /// Find a registered module by type or create and add it.
        /// WARNING: It matches by exact class type (not between base and subclasses).
        /// May trigger ModuleAdd event if it was created.
        /// </summary>
        public T GetOrCreateModule<T>() where T : class, IConsoleModule, new()
        {
            var module = _modules.GetValueOrDefault(typeof(T));
            if (module == null)
            {
                module = new T();
                AddModule(module);
            }
            return (T)module;
        }

        /// <summary>
        /// Find a registered module by type - It will not match between a base class and sub class unless you set includeSubClasses to true.
        /// </summary>
        /// <param name="type">Concrete IConsoleModule type to find</param>
        /// <param name="includeSubClasses">Should this search for subclass matches?</param>
        public IConsoleModule GetModule(Type type, bool includeSubClasses = false)
        {
            if (_modules.TryGetValue(type, out var result))
            {
                return result;
            }
            if (includeSubClasses)
            {
                foreach (var kvp in _modules)
                {
                    if (kvp.Key.IsAssignableFrom(type))
                        return kvp.Value;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Add a module. If a module with the same exact type is found, it will throw an error. It is ok to add different subclasses.
        /// Triggers ModuleAdd event.
        /// </summary>
        /// <param name="module"></param>
        /// <exception cref="ArgumentNullException">Thown if module is null</exception>
        /// <exception cref="Exception">Thrown if module is already added OR a module with the same type already exists.</exception>
        public void AddModule(IConsoleModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }
            var type = module.GetType();
            var existingModule = _modules.GetValueOrDefault(type);
            if (existingModule != null)
            {
                if (existingModule == module)
                {
                    throw new Exception($"Module '{module}' already added");
                }
                throw new Exception($"Module of the same type already exists, existing: '{existingModule}' (hashCode: {existingModule.GetHashCode()}), new: '{module}' (hashCode: {module.GetHashCode()})");
            }
            _modules[type] = module;
            //ModulesChangeCount++;
            module.OnAdded(this);
            ModuleAdd?.Invoke(module);
        }
        
        /// <summary>
        /// Remove a module by type.
        /// Triggers ModuleRemoved event if module was removed.
        /// </summary>
        /// <param name="type">Concrete IConsoleModule type to remove</param>
        /// <returns>Returns true if successfully removed.</returns>
        public bool RemoveModule(Type type)
        {
            if (_modules.Remove(type, out var module))
            {
                //ModulesChangeCount++;
                module.Dispose();
                ModuleRemoved?.Invoke(module);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Test if a module is registered.
        /// </summary>
        public bool HasModule(IConsoleModule module)
        {
            return module != null && _modules.GetValueOrDefault(module.GetType()) == module;
        }

        /// Internal feature. Do not use.
        public void RemovePlayModeOnlyModules()
        {
            foreach (var module in _modules.Values.ToList())
            {
                if (!module.PersistInEditMode)
                {
                    RemoveModule(module.GetType());
                }
            }
        }

        /// Internal feature. Do not use.
        public void EnsureModulesExist((Type type, Func<IConsoleModule> constructor)[] mapping)
        {
            foreach (var item in mapping)
            {
                if (item.type != null && GetModule(item.type, includeSubClasses:true) == null)
                {
                    AddModule(item.constructor());
                }
            }
        }
    }
}