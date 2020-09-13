using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AquaShine.ApiHub.Tests
{
    public static class SystemExtension
    {
        public static T Clone<T>(this T original)
        {
            var serialized = JsonSerializer.Serialize(original);
            return JsonSerializer.Deserialize<T>(serialized);
        }

        private static readonly Random Rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}