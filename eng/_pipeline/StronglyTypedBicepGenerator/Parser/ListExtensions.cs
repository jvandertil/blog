using System.Collections.Generic;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser
{
    internal static class ListExtensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> elements)
        {
            foreach (var item in elements)
            {
                list.Add(item);
            }
        }
    }
}
