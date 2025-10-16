using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Ninjadini.Logger
{
    public static partial class LoggerUtils
    {
        static string NegativeSign => CultureInfo.InvariantCulture.NumberFormat.NegativeSign;
        static string DecimalSeparator => CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
        static string GroupSeparator => CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator;


        [ThreadStatic]
        static StringBuilder _stringBuilder;
        public static StringBuilder TempStringBuilder
        {
            get
            {
                _stringBuilder ??= new StringBuilder(128);
                return _stringBuilder;
            }
        }
        
        public static StringBuilder AppendNum(StringBuilder stringBuilder, int num, bool group = false)
        {
            if (num < 0)
            {
                stringBuilder.Append(NegativeSign);
                num = -num;
            }
            return AppendNum(stringBuilder, (uint)num, group);
        }
        
        public static void AppendNumWithZeroPadding(StringBuilder stringBuilder, int num, int padding)
        {
            if (num < 0)
            {
                stringBuilder.Append("-");
                AppendNumWithZeroPadding(stringBuilder, -num, padding);
                return;
            }
            int count;
            if (num > 0)
            {
                count = 0;
                var tempNum = num;
                while (tempNum > 0)
                {
                    tempNum /= 10;
                    count++;
                }
            }
            else
            {
                count = 1;
            }
            while (count < padding)
            {
                count++;
                stringBuilder.Append("0");
            }
            AppendNum(stringBuilder, (uint)num, false);
        }
        
        public static StringBuilder AppendNum(StringBuilder stringBuilder, uint num, bool group = false)
        {
            if (num == 0)
            {
                return stringBuilder.Append('0');
            }
            var startIndex = stringBuilder.Length;
            var count = 0;
            while (num > 0)
            {
                stringBuilder.Append((char)(num % 10 + '0')); 
                num /= 10;
                count++;
                if (group && count % 3 == 0 && num > 0)
                {
                    stringBuilder.Append(GroupSeparator);
                }
            }
            ReverseLast(stringBuilder, startIndex);
            return stringBuilder;
        }
        
        public static StringBuilder AppendNum(StringBuilder stringBuilder, long num, bool group = false)
        {
            if (num < 0)
            {
                stringBuilder.Append(NegativeSign);
                num = -num;
            }
            return AppendNum(stringBuilder, (ulong)num, group);
        }
        
        public static StringBuilder AppendNum(StringBuilder stringBuilder, ulong num, bool group = false)
        {
            if (num == 0)
            {
                return stringBuilder.Append('0');
            }
            var startIndex = stringBuilder.Length;
            var count = 0;
            while (num > 0)
            {
                stringBuilder.Append((char)(num % 10 + '0')); 
                num /= 10;
                count++;
                if (group && count % 3 == 0 && num > 0)
                {
                    stringBuilder.Append(GroupSeparator);
                }
            }
            ReverseLast(stringBuilder, startIndex);
            return stringBuilder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ReverseLast(StringBuilder stringBuilder, int startIndex)
        {
            var endIndex = stringBuilder.Length - 1;
            var l = (stringBuilder.Length - startIndex) / 2;
            for (var i = 0; i < l; i++)
            {
                (stringBuilder[endIndex - i], stringBuilder[startIndex + i]) = (stringBuilder[startIndex + i], stringBuilder[endIndex - i]);
            }
        }

        public static void AppendNum(StringBuilder stringBuilder, float num, int maxDecimalPlaces = 8, int minDecimalPlaces = 0, bool group = false)
        {
            if (num < 0)
            {
                stringBuilder.Append(NegativeSign);
                num = -num;
            }
            if (num > 999999899999999999999f)
            {
                // TODO need to start adding e##
                stringBuilder.Append(num);
                return;
            }
            var wholeNum = (uint)num;
            AppendNum(stringBuilder, wholeNum, group);
            AppendDecimal(stringBuilder, num - wholeNum, maxDecimalPlaces, minDecimalPlaces);
        }

        static void AppendDecimal(StringBuilder stringBuilder, float decimalValue, int maxDecimalPlaces, int minDecimalPlaces)
        {
            if (maxDecimalPlaces > 0 && decimalValue > 0.000001f)
            {
                stringBuilder.Append(DecimalSeparator);
                var remainingInt = (uint)Math.Round(decimalValue * 100000f);
                
                Span<char> list = stackalloc char[6];
                var count = 0;
                var start = -1;
                while (remainingInt > 0)
                {
                    var d = remainingInt % 10;
                    if (start == -1 && d != 0)
                    {
                        start = count;
                    }
                    list[count++] = (char)(d + '0'); 
                    remainingInt /= 10;
                }
                if (start >= 0)
                {
                    for (var i = count - 1; i >= start && maxDecimalPlaces > 0; i--)
                    {
                        maxDecimalPlaces--;
                        stringBuilder.Append(list[i]);
                    }
                }
                count -= start;
                while (count < minDecimalPlaces)
                {
                    count++;
                    stringBuilder.Append('0');
                }
            }
            else if (minDecimalPlaces > 0)
            {
                stringBuilder.Append(DecimalSeparator);
                while (minDecimalPlaces > 0)
                {
                    minDecimalPlaces--;
                    stringBuilder.Append('0');
                }
            }
        }

        public static void AppendNum(StringBuilder stringBuilder, double num, int maxDecimalPlaces = 8, int minDecimalPlaces = 0, bool group = false)
        {
            if (num < 0)
            {
                stringBuilder.Append(NegativeSign);
                num = -num;
            }
            if (num > 999999899999999999999f)
            {
                // TODO need to start adding e##
                stringBuilder.Append(num);
                return;
            }
            var wholeNum = (uint)num;
            AppendNum(stringBuilder, wholeNum, group);
            AppendDecimal(stringBuilder, num - wholeNum, maxDecimalPlaces, minDecimalPlaces);
        }

        static void AppendDecimal(StringBuilder stringBuilder, double decimalValue, int maxDecimalPlaces, int minDecimalPlaces)
        {
            if (maxDecimalPlaces > 0 && decimalValue > 0.000001)
            {
                stringBuilder.Append(DecimalSeparator);
                var remainingInt = (uint)Math.Round(decimalValue * 100000);
                
                Span<char> list = stackalloc char[6];
                var count = 0;
                var start = -1;
                while (remainingInt > 0)
                {
                    var d = remainingInt % 10;
                    if (start == -1 && d != 0)
                    {
                        start = count;
                    }
                    list[count++] = (char)(d + '0'); 
                    remainingInt /= 10;
                }
                if (start >= 0)
                {
                    for (var i = count - 1; i >= start && maxDecimalPlaces > 0; i--)
                    {
                        maxDecimalPlaces--;
                        stringBuilder.Append(list[i]);
                    }
                }
                count -= start;
                while (count < minDecimalPlaces)
                {
                    count++;
                    stringBuilder.Append('0');
                }
            }
            else if (minDecimalPlaces > 0)
            {
                stringBuilder.Append(DecimalSeparator);
                while (minDecimalPlaces > 0)
                {
                    minDecimalPlaces--;
                    stringBuilder.Append('0');
                }
            }
        }

        static readonly Regex BasicRichTextRegex = new(@"<[^>]+>", RegexOptions.Compiled);
        static readonly Regex RichTextRegex = new (@"<\/?[^>]+?>", RegexOptions.Compiled);
         
        public static bool IsPotentiallyRichText(string str)
        {
            return !string.IsNullOrEmpty(str) && str.Contains("<") && BasicRichTextRegex.IsMatch(str);
        }
        
        public static string StripRichText(string str)
        {
            return RichTextRegex.Replace(str, "");
        }

        public static string StripNonNumberString(string input, bool isInterger)
        {
            var result = Regex.Replace(input, @"[^0-9\-\.]", "");
            if (isInterger)
            {
                result = result.Replace(".", "");
            }
            return result;
        }

        static readonly char[] LineBreaks = new[] { '\n', '\r' };
        public static int GetFirstLineBreakIndex(string input)
        {
            return input?.IndexOfAny(LineBreaks) ?? -1;
        }

        public static string GetFirstLine(string input)
        {
            var index = GetFirstLineBreakIndex(input);
            return index >= 0 ? input.Substring(0, index) : input;
        }

        public static string GetSingleShortenedLine(string input, int maxLen = 0)
        {
            if (GetFirstLineBreakIndex(input) >= 0)
            {
                input = input.Replace("\r", "\\r").Replace("\n", "\\n").Trim();
            }
            if (maxLen > 4 && input.Length > maxLen)
            {
                input = input.Substring(0, maxLen - 4) + "...";
            }
            return input;
        }
    }
}