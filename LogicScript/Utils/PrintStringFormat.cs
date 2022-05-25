using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogicScript.Data;

namespace LogicScript.Utils
{
    public static class PrintStringFormat
    {
        public static string Format(string str, IDictionary<string, BitsValue> locals)
        {
            return Regex.Replace(str, @"\$([a-zA-Z_][a-zA-Z0-9_]*)(:(?<base>b|x))?", m =>
            {
                if (!locals.TryGetValue(m.Groups[1].Value, out var value))
                    throw new Exception($"Local variable ${m.Value} not found in string interpolation");

                var nBase = m.Groups["base"].Success ? m.Groups["base"].Value : null;

                return nBase == "x" ? value.ToStringHex()
                    : nBase == "b" ? value.ToStringBinary()
                    : value.ToString();
            });
        }
    }
}