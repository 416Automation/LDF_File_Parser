using System.Collections.Generic;
using System.Linq;

namespace LDF_File_Parser.Extension
{
    public static class IEnumerableExtension
    {
        /// <summary>Joins the collection by specified seperator.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="seperator">The seprator.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public static string Join<T>(this IEnumerable<T> collection, string seperator = " ; ")
        {
            return string.Join(seperator, collection.ToArray());
        }
    }
}
