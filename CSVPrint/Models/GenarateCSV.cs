using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSVPrint.Models
{
    public static class GenarateCSV
    {
        public static string ToCsv<T>(this IEnumerable<T> list, Dictionary<string, string> columns)
        {
            return ToCsv(list, null, columns);
        }

        public static string ToCsv<T>(this IEnumerable<T> list, string searchString, Dictionary<string, string> columns)
        {
            var sb = new StringBuilder();

            if (!list.Any())
            {
                sb.AppendLine("No results");
                return sb.ToString();
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                sb.AppendLine("Search by Title: " + searchString);
                sb.AppendLine();
            }
            
            // write table headers
            string joined = string.Join(",", columns.Select(kvp => kvp.Value));
            sb.AppendLine(joined);

            var emptyStringReplacement = "=\"\"";

            // write table rows
            foreach (var obj in list)
            {
                var type = obj.GetType();
                var properties = type.GetProperties();

                var s = string.Empty;

                foreach (var k in columns.Keys)
                {
                    string formattedValue;
                    var key = k;
                    var property = properties.FirstOrDefault(pi => pi.Name == key);
                    if (property == null)
                    {
                        formattedValue = emptyStringReplacement;
                    }
                    else
                    {
                        var value = property.GetValue(obj, null);
                        formattedValue = value == null ? emptyStringReplacement : value.ToString();
                        if (value != null)
                        {
                            var valueType = value.GetType();

                            if (valueType == typeof(string))
                            {
                                formattedValue = "\"" + formattedValue.Replace(@"""", @"""""") + "\"";
                            }
                            else if (valueType == typeof(DateTime) || valueType == typeof(DateTime?))
                            {
                                formattedValue = ((DateTime?)value).Value.ToShortDateString();
                            }
                            else if (valueType == typeof(bool) || valueType == typeof(bool?))
                            {
                                formattedValue = ((bool?)value ?? false) ? "Yes" : "No";
                            }
                        }
                    }

                    s = s.Length > 0 ? s + "," + formattedValue : formattedValue;
                }

                sb.AppendLine(s);
            }

            return sb.Replace(emptyStringReplacement, string.Empty).ToString();
        }
    }
}
