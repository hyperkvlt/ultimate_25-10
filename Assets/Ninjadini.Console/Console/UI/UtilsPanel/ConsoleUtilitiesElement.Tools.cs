#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Text;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleUtilitiesElement
    {
        public static VisualElement CreateUtilitiesPanel(ConsoleContext context)
        {
            var container = new ScrollView();
            container.AddToClassList("info-info-panel");

            var overlayTiedBtns = new List<VisualElement>();

            var horizontal = AddHorizontal(container);
            overlayTiedBtns.Add(AddButton(horizontal, "UI Scale\n<size=36>-", () => ChangeScale(-0.1f), 80));
            overlayTiedBtns.Add(AddButton(horizontal, "UI Scale\n<size=36>+", () => ChangeScale(0.1f), 80));


            var safeArea = Screen.safeArea;
            if (context.IsRuntimeOverlay && (!Mathf.Approximately(safeArea.size.x, Screen.width) || !Mathf.Approximately(safeArea.size.y, Screen.height)))
            {
                var vertical = new VisualElement();
                horizontal.Add(vertical);
                overlayTiedBtns.Add(AddButton(vertical, "SafeArea\n+", () => ChangeSafeArea(0.1f), 38));
                overlayTiedBtns.Add(AddButton(vertical, "SafeArea\n-", () => ChangeSafeArea(-0.1f), 38));
            }
            
            AddGap(container);
            horizontal = AddHorizontal(container);
            overlayTiedBtns.Add(AddButton(horizontal, ConsoleUIStrings.InfoFpsMonitor, AddFpsMonitor));
            overlayTiedBtns.Add(AddButton(horizontal, ConsoleUIStrings.InfoMemoryMonitor, AddMemoryMonitor));
            AddButton(horizontal, ConsoleUIStrings.InfoGc, GC.Collect);
            
            AddGap(container);
            horizontal = AddHorizontal(container);
            AddButton(horizontal, ConsoleUIStrings.InfoCopyTextLog, () => CopyTextLog(context));
            AddButton(horizontal, ConsoleUIStrings.InfoEmailTextLog, () => EmailTextLog(context));

            if (ConsoleUIUtils.CanExportText())
            {
                AddButton(horizontal, ConsoleUIStrings.InfoExportTextLog, () => ExportTextLog(context));
                //AddButton(horizontal, ConsoleUIStrings.InfoExportHtmlLog, () => ExportHtmlLog(context));
            }


            if (IsInspectorEnabled(context))
            {
                AddGap(container);
                AddButton(container, "Types\ninspector", () => ConsoleInspector.StartClassesInspector(container));
            }

            if (!context.IsRuntimeOverlay)
            {
                container.schedule.Execute(() =>
                {
                    var available = NjConsole.Overlay.Context != null;
                    foreach (var btn in overlayTiedBtns)
                    {
                        btn.style.opacity = available ? 1f : 0.6f;
                    }
                }).Every(100);
            }
            
            return container;
        }
        
        static VisualElement AddHorizontal(VisualElement container)
        {
            var horizontal = new VisualElement();
            horizontal.AddToClassList("horizontal");
            horizontal.style.flexWrap = Wrap.Wrap;
            container.Add(horizontal);
            return horizontal;
        }

        static void AddGap(VisualElement container)
        {
            container.Add(new VisualElement()
            {
                style = { height = 10 }
            });
        }

        static Button AddButton(VisualElement container, string text, Action callback, int customHeight = 0, int customWidth = 0)
        {
            const int width = 80;
            const int height = 40;
            var btn = new Button(callback)
            {
                text = text,
                style = {
                    width = customWidth > 0 ? customWidth : width,
                    height = customHeight > 0 ? customHeight : height
                }
            };
            container.Add(btn);
            return btn;
        }

        static void ChangeScale(float delta)
        {
            var context = GetOverlayContextOrShowError();
            if (context?.RuntimeOverlay != null)
            {
                context.RuntimeOverlay.Scale += delta;
                if (!context.RuntimeOverlay.Showing)
                {
                    context.RuntimeOverlay.ShowWithAccessChallenge();
                }
            }
        }

        static void ChangeSafeArea(float delta)
        {
            var context = GetOverlayContextOrShowError();
            if (context?.RuntimeOverlay != null)
            {
                context.RuntimeOverlay.SafeAreaScale += delta;
                if (!context.RuntimeOverlay.Showing)
                {
                    context.RuntimeOverlay.ShowWithAccessChallenge();
                }
            }
        }

        static void AddFpsMonitor()
        {
            var context = GetOverlayContextOrShowError();
            context?.Modules.GetOrCreateModule<GraphMonitoringModule>().AddFpsToShortcuts(context);
        }

        static void AddMemoryMonitor()
        {
            var context = GetOverlayContextOrShowError();
            context?.Modules.GetOrCreateModule<GraphMonitoringModule>().AddMemoryToShortcuts(context);
        }

        static ConsoleContext GetOverlayContextOrShowError()
        {
            var context = NjConsole.Overlay.Context;
            if (context == null)
            {
                ConsoleToasts.TryShow(ConsoleUIStrings.InfoNoRuntimeOverlay);
                return null;
            }
            return context;
        }

        public static void ExportTextLog(ConsoleContext context)
        {
            var logs = GenerateTextLog(context);
            ConsoleUIUtils.ExportText("logs.txt", logs);
        }

        public static void EmailTextLog(ConsoleContext context)
        {
            var logs = GenerateTextLog(context);
            var to = context.Settings.emailForTextLogButton?.Trim() ?? "";
            ConsoleUIUtils.MailTo(to, "Logs " + DateTime.Now, logs);
        }

        public static void CopyTextLog(ConsoleContext context)
        {
            var logs = GenerateTextLog(context);
            ConsoleUIUtils.CopyText(logs, context);
        }

        public static string GenerateTextLog(ConsoleContext context)
        {
            var history = NjLogger.LogsHistory;
            var stringBuilder = new StringBuilder();
            AppendHeader(context, stringBuilder);
            var formatter = FindLogFormatter(context);
            Action<LogLine, StringBuilder> customAppender = formatter != null ? formatter.AppendFormatted : null;
            history.GenerateHistoryNewestToOldest(stringBuilder, appendLogString:customAppender);
            AppendFooter(context, stringBuilder);
            return stringBuilder.ToString();
        }

        static void AppendHeader(ConsoleContext context, StringBuilder stringBuilder)
        {
            foreach (var kv in context.Modules.AllModules)
            {
                if (kv.Value is IConsoleLogExportFormatter formatter)
                {
                    formatter.AppendHeader(stringBuilder);
                }
            }
        }

        static void AppendFooter(ConsoleContext context, StringBuilder stringBuilder)
        {
            foreach (var kv in context.Modules.AllModules)
            {
                if (kv.Value is IConsoleLogExportFormatter formatter)
                {
                    formatter.AppendFooter(stringBuilder);
                }
            }
        }
        
        static IConsoleLogExportFormatter FindLogFormatter(ConsoleContext context)
        {
            foreach (var kv in context.Modules.AllModules)
            {
                if (kv.Value is IConsoleLogExportFormatter formatter && formatter.HasLogFormatter)
                {
                    return formatter;
                }
            }
            return null;
        }
        /*
         // it was working but html looked very low tech. lets put it back in later.
        public static void ExportHtmlLog(ConsoleContext context)
        {
            var history = NjLogger.LogsHistory;
            const String htmlReplacement = "{text:'HTML_REPLACEMENT'}";

            var data = new ExportData();
            var logs = data.logs = new List<ExportLog>();
            
            var stringBuilder = new StringBuilder();
            AppendHeader(context, stringBuilder);
            if (stringBuilder.Length > 0)
            {
                logs.Add(new ExportLog()
                {
                    priority = 1,
                    text = stringBuilder.ToString()
                });
            }

            var formatter = FindLogFormatter(context);
            
            for (int i = history.FirstVisibleIndex, l = history.Head; i < l; i++)
            {
                var log = history.GetLog(i);

                string line;
                if (formatter != null)
                {
                    stringBuilder.Clear();
                    formatter.AppendFormatted(log, stringBuilder);
                    line = stringBuilder.ToString();
                }
                else
                {
                    line = log.GetLineString();
                }
                logs.Add(new ExportLog()
                {
                    priority = (int)log.GetLevel(),
                    ch = log.GetChannelName(),
                    text = line
                });
            }

            stringBuilder.Clear();
            AppendFooter(context, stringBuilder);
            if (stringBuilder.Length > 0)
            {
                logs.Add(new ExportLog()
                {
                    priority = 1,
                    text = stringBuilder.ToString()
                });
            }
            
            var json = JsonUtility.ToJson(data);
            var txt = Resources.Load<TextAsset>("NjConsoleHtmlTemplate").text;
            txt = txt.Replace(htmlReplacement, json);
            ConsoleUIUtils.ExportText("logs.html", txt);
        }
            
        [Serializable]
        class ExportData
        {
            public List<ExportLog> logs;
        }
        
        [Serializable]
        class ExportLog
        {
            public string ch;
            public int priority;
            public string text;
        }*/
    }
}
#endif