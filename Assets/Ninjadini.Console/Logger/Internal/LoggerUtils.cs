using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Ninjadini.Logger
{
    public static partial class LoggerUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrValue AsLogRef<T>(this T obj) where T : class
        {
            return StrValue.FromObject(obj);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrValue AsString(this object obj)
        {
            return obj?.ToString();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrValue AsStrongLogRef<T>(this T obj) where T : class
        {
            return StrValue.AsStrongRef(obj);
        }

        public static void RemoveSwapBack<T>(List<T> list, T item)
        {
            var index = list.IndexOf(item);
            if (index < 0) return;
            var lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
        }

        public static StrValue.WeakRef BorrowWeakRef(object obj)
        {
            var weakRef  = ThreadLocalPool<StrValue.WeakRef>.Borrow();
            weakRef.Type = obj?.GetType();
            if (weakRef.Ref == null)
            {
                weakRef.Ref = new WeakReference(obj);
            }
            else
            {
                weakRef.Ref.Target = obj;
            }
            return weakRef;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(this StrValue.WeakRef weakRef)
        {
            weakRef.Ref.Target = null;
            weakRef.Type = null;
            ThreadLocalPool<StrValue.WeakRef>.Return(weakRef);
        }
        
        public static class ThreadLocalPool<T> where T : class, new()
        {
            public static int MaxPerThread = 16;
            public static Func<T> Constructor;

            [ThreadStatic]
            static List<T> _pool;
            
            public static T Borrow()
            {
                _pool ??= new List<T>(MaxPerThread);

                int count = _pool.Count;
                if (count > 0)
                {
                    var index = count - 1;
                    var item = _pool[index];
                    _pool.RemoveAt(index);
                    return item;
                }
                return Constructor?.Invoke() ?? new T();
            }

            public static void Return(T item)
            {
                _pool ??= new List<T>(MaxPerThread);
                if (_pool.Count < MaxPerThread)
                {
                    _pool.Add(item);
                }
            }

            public static int Count => _pool?.Count ?? 0;

            public static void Clear()
            {
                _pool?.Clear();
            }
        }
    }
}