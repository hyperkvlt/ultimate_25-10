#if !NJCONSOLE_DISABLE
using System;
using Ninjadini.Console.Internal;
using UnityEngine;
using UnityEngine.Profiling;

namespace Ninjadini.Console.UI
{
    public class MemoryMonitorElement : ConsoleGraphingElement
    {
        public MemoryMonitorElement() : base(80, intervalMs:1000)
        {
            style.height = 60;

            ValueSuffix = "<size=80%>mb</size>";
            
            var managed = new IConsoleGraphDataProvider.Simple(ConsoleUIStrings.GraphMemManaged, new Color(1f, 1f, 0.2f), GetManaged);
            Add(managed);
            
            var native = new IConsoleGraphDataProvider.Simple(ConsoleUIStrings.GraphMemNative, new Color(0.2f, 0.9f, 1f), GetNative);
            Add(native);
        }

        float GetManaged()
        {
            return Mathf.Round(GC.GetTotalMemory(false) / (1024f * 1024f));
        }

        float GetNative()
        {
            return Mathf.Round(Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
        }
    }
}
#endif