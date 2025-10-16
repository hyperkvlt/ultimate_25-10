using System.Runtime.CompilerServices;

namespace Ninjadini.Logger
{
    public static partial class LoggerUtils
    {
        public const int LevelsMask = 1 | 1 << 1 | 1 << 2;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NjLogger.Level GetLevel(this NjLogger.Options options)
        {
            return (NjLogger.Level)((int)options & LevelsMask);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLevelInLevelsMask(NjLogger.Level level, int levels)
        {
            var lvlMask = 1 << (int)level;
            return (levels & lvlMask) != 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLevelInLevelsMask(this NjLogger.Options options, int levels)
        {
            var lvlMask = 1 << ((int)options & LevelsMask);
            return (levels & lvlMask) != 0;
        }
        
        public static int AddLevelToLevelsMask(NjLogger.Level level, int levels)
        {
            return levels | (1 << (int)level);
        }
        
        public static int RemoveLevelFromLevelsMask(NjLogger.Level level, int levels)
        {
            return levels & ~(1 << (int)level);
        }
        
        public static bool Contains(this NjLogger.Options options, NjLogger.Options mask)
        {
            return (options & mask) == mask;
        }
        
        public static bool NotContains(this NjLogger.Options options, NjLogger.Options mask)
        {
            return (options & mask) == 0;
        }
    }
}