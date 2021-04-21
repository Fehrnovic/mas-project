using System;
using System.Collections.Generic;

namespace MultiAgent.SearchClient.Utils
{
    public class Extensions
    {
        public static Dictionary<TKey, TValue> CloneDictionary<TKey, TValue>
            (Dictionary<TKey, TValue> original) where TValue : ICloneable
        {
            var ret = new Dictionary<TKey, TValue>(original.Count, original.Comparer);
            foreach (var entry in original)
            {
                ret.Add(entry.Key, (TValue) entry.Value.Clone());
            }

            return ret;
        }
    }
}
