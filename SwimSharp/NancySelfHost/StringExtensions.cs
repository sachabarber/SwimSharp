using System;
using System.Collections.Generic;
using System.Text;

namespace SwimSharp.Extensions
{
    public static class StringExtensions
    {
        public static (string, string) SplitAt(this string input, int index)
        {
            return (input.Substring(0, index), input.Substring(index));
        }
    }
}
