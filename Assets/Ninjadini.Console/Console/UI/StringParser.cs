using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

#if !NJCONSOLE_DISABLE
namespace Ninjadini.Console.Internal
{
    public static class StringParser
    {
        public static bool FindNextWord(string input, int startIndex, out int wordStart, out int wordLength)
        {
            int i = startIndex;

            // 1. Skip leading whitespace and delimiters
            while (i < input.Length && (char.IsWhiteSpace(input[i]) || input[i] == '/'))
            {
                i++;
            }

            if (i >= input.Length)
            {
                wordStart = wordLength = -1;
                return false;
            }

            // 2. Found start of word
            wordStart = i;

            // 3. Find end of word (stop at delimiter or end of string)
            while (i < input.Length && input[i] != '/')
                i++;

            // 4. Trim trailing whitespace from the word
            var wordEnd = i - 1;
            while (wordEnd > wordStart && char.IsWhiteSpace(input[wordEnd]))
            {
                wordEnd--;
            }
            wordLength = wordEnd - wordStart + 1;
            return true;
        }

        public static bool SubRangeEquals(string path, int start, int length, string expected)
        {
            if (expected.Length != length)
            {
                return false;
            }
            for (var i = 0; i < length; i++)
            {
                if (path[start + i] != expected[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool SubRangeEqualsNonCase(string path, int start, int length, string expected)
        {
            if (expected.Length != length)
            {
                return false;
            }
            for (var i = 0; i < length; i++)
            {
                if (char.ToLowerInvariant(path[start + i]) != char.ToLowerInvariant(expected[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static string GetRemainingPartialMatch(string path, string expected, int start = 0, int length = int.MaxValue)
        {
            length = Math.Min(expected.Length, Math.Min(length, path.Length - start));
            for (var i = 0; i < length; i++)
            {
                if (path[start + i] != expected[i])
                {
                    return null;
                }
            }
            return expected.Substring(length);
        }

        public static string GetRemainingPartialMatchNonCase(string path, string expected, int start = 0, int length = int.MaxValue)
        {
            length = Math.Min(expected.Length, Math.Min(length, path.Length - start));
            for (var i = 0; i < length; i++)
            {
                if (char.ToLowerInvariant(path[start + i]) != char.ToLowerInvariant(expected[i]))
                {
                    return null;
                }
            }
            return expected.Substring(length);
        }
        
        public static bool IsWhiteSpace(string input, int index, int length = -1)
        {
            length = Math.Min(input.Length, (length < 0 ? int.MaxValue : (index + length)));
            for (var i = index; i < length; i++)
            {
                if (!char.IsWhiteSpace(input[i]))
                {
                    return false;
                }
            }
            return true;
        }
        
        // Splits at any space or comma or parentheses (), it keeps the contents of quotes and parentheses together.
        public static List<string> SplitParams(string input)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;
            var parenDepth = 0;
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (c == '"' && (i == 0 || input[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                }
                else if (c == '(' && !inQuotes)
                {
                    parenDepth++;
                    sb.Append(c);
                }
                else if (c == ')' && !inQuotes)
                {
                    parenDepth--;
                    sb.Append(c);
                }
                else if ((c == ' ' || c == ',') && !inQuotes && parenDepth == 0)
                {
                    if (sb.Length > 0)
                    {
                        result.Add(UnescapeGroup(sb.ToString().Trim()));
                        sb.Clear();
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
            {
                result.Add(UnescapeGroup(sb.ToString().Trim()));
            }
            return result;
        }
        
        static string UnescapeGroup(string str)
        {
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                return str
                    .Substring(1, str.Length - 2)
                    .Replace("\\\"", "\"");
            }
            if (str.StartsWith("(") && str.EndsWith(")"))
            {
                return str.Substring(1, str.Length - 2).Trim();
            }
            return str;
        }
        
        public static List<string> SplitParams(string input, int expectedParamCount)
        {
            var split = SplitParams(input);
            if (split.Count == 0)
            {
                return null;
            }
            if (split.Count != expectedParamCount)
            {
                throw new ConsoleCommandException($"Invalid argument count — expected {expectedParamCount}, but received {split.Count}. Separate parameters with spaces or comma.");
            }
            return split;
        }

        public static object[] ParseParams(string input, Type[] argTypes, ConsoleObjReferenceStorage storedRefs = null)
        {
            var numArgs = argTypes.Length;
            var argStrs = SplitParams(input);
            if (argStrs.Count != numArgs)
            {
                if (numArgs == 1)
                {
                    if (argTypes[0] == typeof(string))
                    {
                        var i = input.TrimStart();
                        return new object[] { TryParseStored(i, storedRefs, typeof(string)) ??  i};
                    }
                    if (!argTypes[0].IsPrimitive)
                    {
                        return new object[] { TryCreateFromParams(argStrs, argTypes[0], storedRefs) };
                    }
                }
                throw new ConsoleCommandException($"Invalid argument count — expected {numArgs}, but received {argStrs.Count}. {ParamGroupingInfo}");
            }
            var result = new object[numArgs];
            for(var i = 0; i < numArgs; i++)
            {
                try
                {
                    result[i] = Parse(argStrs[i], argTypes[i], storedRefs);
                }
                catch (Exception err)
                {
                    throw new ConsoleCommandException($"Failed to parse ‘<noparse>{argStrs[i]}</noparse>’ to type: {argTypes[i]}. {err.Message} Help Info: {ParamGroupingInfo}");
                }
            }
            return result;
        }

        public const string ParamGroupingInfo = "Use spaces or commas to separate parameters. Wrap strings in quotes <i>\"a b c\"</i> and group values in parentheses for constructor calls: <i>(1 2 3)</i> for Vector3.";

        public static object Parse(string input, Type type, ConsoleObjReferenceStorage storedRefs = null)
        {
            var stored = TryParseStored(input, storedRefs, type);
            if (stored != null)
            {
                return stored;
            }
            if (type == typeof(string))
            {
                return input;
            }
            var tt = Nullable.GetUnderlyingType(type);
            if (tt != null)
            {
                if (tt != type && IsNullLiteral(input))
                {
                    return null;
                }
                type = tt;
            }
            if (type == typeof(bool))
            {
                switch (input.ToLowerInvariant())
                {
                    case "true":
                    case "yes":
                    case "y":
                    case "1":
                    case "on":
                        return true;
                    case "false":
                    case "no":
                    case "n":
                    case "0":
                    case "off":
                        return false;
                }
            }
            if (type.IsEnum)
            {
                try
                {
                    return Enum.Parse(type, input, ignoreCase: true);
                }
                catch (Exception)
                {
                    throw new ConsoleCommandException($"Possible values: <noparse>{string.Join(" | ", Enum.GetNames(type))}</noparse>");
                }
            }
            if (type.IsPrimitive)
            {
                return Convert.ChangeType(input, type);
            }
            return TryCreateFromParams(SplitParams(input), type, storedRefs);
        }
        
        static bool CanParse(string argText, Type type, ConsoleObjReferenceStorage storedRefs)
        {
            if (TryParseStored(argText, storedRefs, type) != null) return true;

            var u = Nullable.GetUnderlyingType(type);
            var T = u ?? type;

            if (IsNullLiteral(argText))
            {
                return !T.IsValueType || u != null;
            }
            if (IsPrimitiveLike(T)) return true;

            // For complex types, the arg must itself contain inner params we can match recursively.
            var inner = SplitParams(argText);
            var ctors = T.GetConstructors();
            foreach (var ctor in ctors)
            {
                var ps = ctor.GetParameters();
                if (ps.Length != inner.Count) continue;

                var ok = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (!CanParse(inner[i], ps[i].ParameterType, storedRefs))
                    {
                        ok = false; 
                        break;
                    }
                }
                if (ok)
                {
                    return true;
                }
            }

            return false;
        }
        
        public static bool IsPrimitiveLike(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(bool);
        }

        static bool IsNullLiteral(string s) => string.Equals(s, "null", StringComparison.Ordinal);
        
        public static object TryCreateFromParams(List<string> paramStrs, Type type, ConsoleObjReferenceStorage storedRefs = null)
        {
            if (paramStrs.Count == 1 && IsNullLiteral(paramStrs[0]))
            {
                return null;
            }
            var constructors = type.GetConstructors();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();

                if (parameters.Length != paramStrs.Count)
                    continue; // Wrong param count

                var parsedArgs = new object[parameters.Length];
                var allParsed = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var parsed = Parse(paramStrs[i], parameters[i].ParameterType, storedRefs);
                    if (parsed == null && parameters[i].ParameterType.IsValueType)
                    {
                        allParsed = false; // Failed to parse value type
                        break;
                    }
                    parsedArgs[i] = parsed;
                }

                if (allParsed)
                {
                    try
                    {
                        return ctor.Invoke(parsedArgs);
                    }
                    catch
                    {
                        // Constructor matched but failed to invoke (unlikely if parsed right)
                        continue;
                    }
                }
            }
            if (paramStrs.Count == 1)
            {
                var stored = TryParseStored(paramStrs[0], storedRefs, type);
                if (stored != null)
                {
                    return stored;
                }
                var argStrs = SplitParams(paramStrs[0]);
                if (argStrs.Count > 0 && argStrs[0] != paramStrs[0])
                {
                    return TryCreateFromParams(argStrs, type);
                }
            }
            throw new ConsoleCommandException($"No matching constructors found to create {type} from {paramStrs.Count} params ‘<noparse>{string.Join(", ", paramStrs)}</noparse>’");
        }

        static object TryParseStored(string input, ConsoleObjReferenceStorage storedRefs, Type expectedType)
        {
            if (input.StartsWith("$") && storedRefs != null)
            {
                var stored = storedRefs.GetStored(input.Substring(1));
                if (stored != null && expectedType.IsInstanceOfType(stored))
                {
                    return stored;
                }
            }
            return null;
        }

        public static object Invoke(MethodInfo methodInfo, object target, string paramsString, ConsoleObjReferenceStorage storedRefs = null)
        {
            var parametersInfo = methodInfo.GetParameters();
            var args = ParseParams(paramsString, parametersInfo.Select(p => p.ParameterType).ToArray(), storedRefs);
            return methodInfo.Invoke(target, args);
        }

        public static object CallMember(object target, string memberName, string paramsString, ConsoleObjReferenceStorage storedRefs = null)
        {
            var binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            var type = target as Type ?? target.GetType();
            var members = type.GetMembers(binding);
            List<MethodInfo> methodInfos = null;
            foreach (var member in members)
            {
                if (!member.Name.Equals(memberName))
                {
                    continue;
                }
                if (member is FieldInfo f)
                {
                    if (string.IsNullOrWhiteSpace(paramsString))
                    {
                        return f.GetValue(target);
                    }
                    var value = Parse(paramsString, f.FieldType, storedRefs);
                    f.SetValue(target, value);
                    return value;
                }
                if (member is PropertyInfo p && p.GetIndexParameters().Length == 0)
                {
                    if (string.IsNullOrWhiteSpace(paramsString))
                    {
                        if (p.CanRead)
                        {
                            return p.GetValue(target);
                        }
                        throw new ConsoleCommandException($"Property `{memberName}` is write-only.");
                    }
                    if (!p.CanWrite)
                    {
                        throw new ConsoleCommandException($"Property `{memberName}` is read-only.");
                    }
                    var value = Parse(paramsString, p.PropertyType, null);
                    p.SetValue(target, value);
                    return value;
                }
                if (member is MethodInfo methodInfo)
                {
                    methodInfos ??= new List<MethodInfo>();
                    methodInfos.Add(methodInfo);
                }
            }

            if (methodInfos != null)
            {
                var argsText = string.IsNullOrWhiteSpace(paramsString) ? new List<string>() : SplitParams(paramsString);

                var viable = methodInfos
                    .Where(mi =>
                    {
                        var ps = mi.GetParameters();
                        if (ps.Length != argsText.Count) return false;
                        for (int i = 0; i < ps.Length; i++)
                        {
                            if (!CanParse(argsText[i], ps[i].ParameterType, null))
                                return false;
                        }
                        return true;
                    })
                    .OrderByDescending(mi => mi.GetParameters().Count(p => IsPrimitiveLike(p.ParameterType)))
                    .ToList();

                if (viable.Count > 0)
                {
                    var best = viable[0];
                    var ps = best.GetParameters();
                    var args = new object[ps.Length];
                    for (var i = 0; i < ps.Length; i++)
                    {
                        args[i] = Parse(argsText[i], ps[i].ParameterType, null);
                    }
                    return best.Invoke(best.IsStatic ? null : target, args);
                }
            }
            throw new ConsoleCommandException($"No field/property/method named `{memberName}` exists in `{type.Name}`.");
        }

        public static ConsoleContext.IEditorBridge.StackFrame[] ExtractLinesFromStacktrace(object stackTraceObj, StringBuilder nameStringBuilder = null)
        {
            if (stackTraceObj is StackTrace stackTrace)
            {
                return ExtractLinesFromStacktrace(stackTrace, nameStringBuilder);
            }
            if (stackTraceObj is string str && !string.IsNullOrEmpty(str))
            {
                return ExtractLinesFromStacktrace(str, nameStringBuilder);
            }
            return Array.Empty<ConsoleContext.IEditorBridge.StackFrame>();
        }
        
        public static ConsoleContext.IEditorBridge.StackFrame[] ExtractLinesFromStacktrace(StackTrace stackTrace, StringBuilder nameStringBuilder = null)
        {
            var frameCount = stackTrace.FrameCount;
            var result = new ConsoleContext.IEditorBridge.StackFrame[frameCount];
            for (var i = 0; i < frameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                if (nameStringBuilder != null)
                {
                    nameStringBuilder.Length = 0;
                    var method = frame.GetMethod();
                    if (method.DeclaringType != null)
                    {
                        nameStringBuilder.Append(method.DeclaringType.Name);
                        nameStringBuilder.Append(".");
                    }
                    nameStringBuilder.Append(method.Name);
                    nameStringBuilder.Append("(");
                    nameStringBuilder.Append(string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name)));
                    nameStringBuilder.Append(");");
                }
                var fileName = frame.GetFileName();
                result[i] = new ConsoleContext.IEditorBridge.StackFrame()
                {
                    Name = nameStringBuilder?.ToString() ?? fileName,
                    FilePath = frame.GetFileName(),
                    LineNumber = frame.GetFileLineNumber()
                };
            }
            return result;
        }
        
        
        static readonly Regex MethodLineRegex = new Regex(@"^\s*(?:at\s+)?(\S+?)[.:](\w+)\s*\(([^)]*)\)\s*(\(at (.+):(\d+)\))?", RegexOptions.Multiline);
        static readonly Regex PathAndLineReg = new Regex(@"((?:at\s+[\w\.]+\.(\w+)\s*\(.*?\)\s*\[.*?\]\s*)?\s*in\s+)?((?:[A-Za-z]:\\|.?\/|Assets[\\\/]).*?\.\w+)(?:\s*:\s*(\d+)|\s*\((\d+),\s*(\d+)\))?", RegexOptions.Multiline);
        // ^ Can add ` | RegexOptions.Compiled` but it made the compile time slower.
        
        public static List<ConsoleContext.IEditorBridge.StackFrame> ExtractPaths(string textBlock)
        {
            var matches = PathAndLineReg.Matches(textBlock);
            var result = new List<ConsoleContext.IEditorBridge.StackFrame>();
            
            int i = 0;
            while (i < matches.Count)
            {
                var current = matches[i];
                var currentGrp = current.Groups;
                var lineGrp = currentGrp;
                var merged = currentGrp[3].Value;
                var end = current.Index + current.Length;

                // Look ahead to next match
                while (i + 1 < matches.Count)
                {
                    var next = matches[i + 1];
        
                    // Check if next match starts immediately after current
                    var adjacent = next.Index == end || next.Index == end + 1;
                    if (!adjacent) break;

                    var startsWithSlash = next.Value.StartsWith("/") || next.Value.StartsWith("\\");
                    if (!startsWithSlash) break;

                    merged += next.Groups[3].Value;
                    end = next.Index + next.Length;
                    i++;
                    lineGrp = next.Groups;
                }
                string lineStr = null;
                if (lineGrp[4].Success) lineStr = lineGrp[4].Value;
                else if(lineGrp[5].Success) lineStr = lineGrp[5].Value;
                result.Add(new ConsoleContext.IEditorBridge.StackFrame()
                {
                    Name = currentGrp[2].Success ? currentGrp[2].Value : null,
                    FilePath = merged.Trim(),
                    LineNumber = !string.IsNullOrEmpty(lineStr) && int.TryParse(lineStr, out var v) ? v : 0
                });
                i++;
            }
            
            return result;
        }

        public static ConsoleContext.IEditorBridge.StackFrame? ExtractSimpleCsPath(string textBlock)
        {
            var match = Regex.Match(textBlock, @"(.*\.cs)\((\d+),");
            if (match.Success)
            {
                return new ConsoleContext.IEditorBridge.StackFrame()
                {
                    FilePath = match.Groups[1].Value,
                    LineNumber = int.Parse(match.Groups[2].Value)
                };
            }
            return null;
        }

        public static ConsoleContext.IEditorBridge.StackFrame[] ExtractLinesFromStacktrace(string stackTrace, StringBuilder nameStringBuilder = null)
        {
            var matches = MethodLineRegex.Matches(stackTrace);
            var frameCount = matches.Count;
            if (frameCount == 0)
            {
                return Array.Empty<ConsoleContext.IEditorBridge.StackFrame>();
            }
            var result = new ConsoleContext.IEditorBridge.StackFrame[frameCount];
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var match = matches[frameIndex];
                if (nameStringBuilder != null)
                {
                    var fullClass = match.Groups[1].Value;
                    var index = fullClass.LastIndexOf('.');
                    var className = index >= 0 ? fullClass.Substring(index + 1) : fullClass;
                    var methodName = match.Groups[2].Value;
                    var paramList = match.Groups[3].Value;
                    nameStringBuilder.Length = 0;
                    nameStringBuilder.Append(className)
                        .Append(".")
                        .Append(methodName)
                        .Append("(");
                    if (!string.IsNullOrWhiteSpace(paramList) && !paramList.StartsWith("at ") && paramList != "void")
                    {
                        var parts = paramList.Split(',');
                        for (var i = 0; i < parts.Length; i++)
                        {
                            var trimmed = parts[i].Trim();
                            var lastDot = trimmed.LastIndexOf('.');
                            if (lastDot >= 0)
                            {
                                trimmed = trimmed.Substring(lastDot + 1);
                            }
                            var spaceIndex = trimmed.IndexOf(' ');
                            if (spaceIndex >= 0)
                            {
                                trimmed = trimmed.Substring(0, spaceIndex);
                            }
                            if (i > 0)
                            {
                                nameStringBuilder.Append(", ");
                            }
                            nameStringBuilder.Append(trimmed);
                        }
                    }
                    nameStringBuilder.Append(")");
                }
                var filePath = match.Groups[5].Value;
                var lineNum = match.Groups[6].Value;
                result[frameIndex] = new ConsoleContext.IEditorBridge.StackFrame()
                {
                    Name = nameStringBuilder?.ToString() ?? match.Groups[4].Value,
                    FilePath = filePath,
                    LineNumber = string.IsNullOrEmpty(lineNum) ? 0 : int.Parse(lineNum)
                };
            }
            return result;
        }
    }
}
#endif