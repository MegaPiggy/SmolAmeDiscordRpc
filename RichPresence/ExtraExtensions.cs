using System;
using System.Collections.Generic;
using System.Linq;
using SALT.Extensions;
public interface ShallowICloneable<T>
{
    T ShallowClone();
}
public interface DeepICloneable<T>
{
    T DeepClone();
}
public interface Cloneable<T> : DeepICloneable<T>, ShallowICloneable<T>, ICloneable
{

}

namespace RichPresence
{

    public static class ExtraExtensions
    {
        public static int PercentageOf(this int of, int num) => (int)(((float)of/num)*100);
        public static T[] ShallowCopy<T>(this T[] array) where T : ShallowICloneable<T> => array.Select(a => a.ShallowClone()).ToArray();
        public static T[] DeepCopy<T>(this T[] array) where T : DeepICloneable<T> => array.Select(a => a.DeepClone()).ToArray();

        /// <summary>
        /// Reverses a array
        /// </summary>
        public static T[] Reverse<T>(this T[] array)
        {
            Array.Reverse(array);
            return array;
        }

        /// <summary>
        /// Splits a string and then reverses the array it creates.
        /// </summary>
        public static string[] ReverseSplit(this string str, string seperator = " ")
        {
            return str.Split(seperator.ToCharArray()).Reverse();
        }

        /// <summary>
        /// Splits a string, reverses the array it creates, and then joins them back together.
        /// </summary>
        public static string Reverse(this string str, string seperator = " ", string seperatorJoin = " ")
        {
            return str.ReverseSplit(seperator).Join(seperatorJoin);
        }

        /// <summary>
        /// Swap two elements in array
        /// </summary>
        public static T[] Swap<T>(this T[] array, int a, int b)
        {
            T x = array[a];
            array[a] = array[b];
            array[b] = x;
            return array;
        }

        /// <summary>
        /// Joins a string array
        /// </summary>
        public static string Join(this string[] array, string seperator) => string.Join(seperator, array);
    }
}
