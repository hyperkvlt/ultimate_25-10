#if !NJCONSOLE_DISABLE
using System;
using System.Diagnostics;
using System.IO;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ninjadini.Console.Editor
{
    public class ConsoleEditorBridge : ConsoleContext.IEditorBridge
    {
        public bool HasEditorFeatures => true;

        /*
         * Assign your custom function to skip showing certain stack frames in details view.s
         */
        public static Func<ConsoleContext.IEditorBridge.StackFrame, ConsoleContext.IEditorBridge.StackSkipType> CustomStackTraceFrameSkip;

        bool ConsoleContext.IEditorBridge.ValidFilePath(string path)
        {
            return File.Exists(path);
        }

        bool ConsoleContext.IEditorBridge.GoToFile(object stackTraceObj, int stackIndex)
        {
            if (stackTraceObj is string str)
            {
                if (str.StartsWith(ConsoleContext.IEditorBridge.ResPathPrefix))
                {
                    var asset = Resources.Load(str.Substring(ConsoleContext.IEditorBridge.ResPathPrefix.Length));
                    return TryGoToAsset(asset);
                }
                try
                {
                    if (File.Exists(str))
                    {
                        return GoToFile(str, stackIndex);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
                var frames = StringParser.ExtractPaths(str);
                if (frames != null)
                {
                    foreach (var frame in frames)
                    {
                        if (GoToFile(frame.FilePath, frame.LineNumber))
                        {
                            return true;
                        }
                    }
                }
                var simpleCs = StringParser.ExtractSimpleCsPath(str);
                if (simpleCs != null && GoToFile(simpleCs.Value.FilePath, simpleCs.Value.LineNumber))
                {
                    return true;
                }
            }
            var lines = StringParser.ExtractLinesFromStacktrace(stackTraceObj);
            for (int i = stackIndex, l = Math.Min(stackIndex + 3, lines.Length); i < l; i++)
            {
                var frame = lines[i];
                if (!string.IsNullOrEmpty(frame.FilePath) && ShouldSkipStackFrame(frame) == ConsoleContext.IEditorBridge.StackSkipType.DoNotSkip && GoToFile(frame.FilePath, frame.LineNumber))
                {
                    return true;
                }
            }
            return false;
        }

        public ConsoleContext.IEditorBridge.StackSkipType ShouldSkipStackFrame(ConsoleContext.IEditorBridge.StackFrame frame)
        {
            /*
            // this felt too expensive
            var script = LoadAssetAtPath(frame.FilePath) as MonoScript;
            if (script)
            {
                var assembly = script.GetClass()?.Assembly;
                if (assembly != null && assembly == typeof(NjLogger).Assembly)
                {
                    return ConsoleContext.IEditorBridge.StackSkipType.SkipEarly;
                }
            }*/
            if (frame.FilePath?.Contains("Ninjadini.Console/Logger/") == true)
            {
                return ConsoleContext.IEditorBridge.StackSkipType.SkipEarly;
            }
            // ^ this is cheaper version.
            
            var customSkip = CustomStackTraceFrameSkip?.Invoke(frame);
            return customSkip ?? ConsoleContext.IEditorBridge.StackSkipType.DoNotSkip;
        }

        void ConsoleContext.IEditorBridge.PingObject(UnityEngine.Object obj)
        {
            //Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        public static bool GoToFile(string path, int lineNumber)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var obj = LoadAssetAtPath(path);
            if (TryGoToAsset(obj, lineNumber))
            {
                return true;
            }
            if (!File.Exists(path))
            {
                return false;
            }
            if (Path.GetExtension(path).ToLower() != "cs")
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(path));
                return true;
            }
            try
            {
                var editorPath = EditorPrefs.GetString("kScriptsDefaultApp");
                var startInfo = new ProcessStartInfo();
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    startInfo.FileName = editorPath;
                    startInfo.Arguments = $"\"{path}\" /command \"edit.goto {lineNumber}\"";
                }
                else
                {
                    startInfo.FileName = "open";
                    startInfo.Arguments = $"-a \"{editorPath}\" \"{path}\" --args -r -g {lineNumber}";
                }
                Process.Start(startInfo);
                return true;
            }
            catch (Exception e)
            {
                NjLogger.Warn(e);
            }
            return false;
        }

        public static Object LoadAssetAtPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (path.StartsWith("Assets/"))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj)
                {
                    return obj;
                }
            }
            if (!File.Exists(path))
            {
                return null;
            }
            var assetsPath = Path.GetFullPath(Application.dataPath);
            path = Path.GetFullPath(path);
            if (path.StartsWith(assetsPath))
            {
                var projPath = "Assets" + path.Substring(assetsPath.Length);
                var obj = AssetDatabase.LoadAssetAtPath<Object>(projPath);
                if (obj)
                {
                    return obj;
                }
            }
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var package in packages)
            {
                var root = Path.GetFullPath(package.resolvedPath).Replace('\\', '/');

                if (path.StartsWith(root))
                {
                    var packagePath = $"Packages/{package.name}/{path.Substring(root.Length).TrimStart('/')}";
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(packagePath);
                    if (obj)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }
        
        static bool TryGoToAsset(Object asset, int lineNumber = 0)
        {
            if (!asset) return false;
            if (asset is MonoScript)
            {
                return AssetDatabase.OpenAsset(asset, lineNumber);
            }
            EditorApplication.ExecuteMenuItem("Window/General/Project");
            EditorApplication.delayCall += () =>
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            };
            return true;
        }
    }
}
#endif