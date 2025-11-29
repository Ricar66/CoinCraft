using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CoinCraft.Services
{
    public sealed class ExportService
    {
        public void ExportToCsv<T>(IEnumerable<T> data, string filePath)
        {
            var list = data?.ToList() ?? new List<T>();
            if (list.Count == 0)
            {
                File.WriteAllText(filePath, string.Empty);
                return;
            }

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            using var sw = new StreamWriter(filePath, false);
            sw.WriteLine(string.Join(",", props.Select(p => Escape(p.Name))));
            foreach (var item in list)
            {
                var values = props.Select(p => Escape(Convert.ToString(p.GetValue(item) ?? string.Empty)));
                sw.WriteLine(string.Join(",", values));
            }
        }

        private static string Escape(string? s)
        {
            var v = s ?? string.Empty;
            if (v.Contains('"') || v.Contains(',') || v.Contains('\n'))
            {
                v = v.Replace("\"", "\"\"");
                return $"\"{v}\"";
            }
            return v;
        }
    }
}
