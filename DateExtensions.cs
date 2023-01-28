using System;

namespace IngameScript
{
    internal static class DateExtensions
    {
        private static readonly int nil = 0;

        public static bool DateMayBeFaked(this DateTime datetime)
        {
            return datetime.Millisecond == nil && datetime.Second == nil && datetime.Minute == nil;
        }
    }
}