using System;
using System.Collections.Generic;
using System.Linq;

namespace SwimSharp.Extensions
{
    public static class ListExtensions
    {
        private static Random rng = new Random();

        public static IList<T> Shuffle<T>(this IList<T> list) where T : ICloneable
        {
            var clonedList = list.Clone();
            int n = clonedList.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = clonedList[k];
                clonedList[k] = clonedList[n];
                clonedList[n] = value;
            }
            return clonedList;
        }

        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }
    }
}
