using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Library_Brider_2.Generic_Classes
{


    public static class Filter
    {
        private static readonly List<string> BlacklistedWords = new List<string>
        {
            "ft.", "feat.", "featuring", "#", "lyrics"
        };

        public static string CleanStringForSearch(string stringToFilter)
        {
            if (stringToFilter == null)
                return null;
            else
            {
                stringToFilter = FilterStringWithBlacklist(stringToFilter);
                stringToFilter = RemoveParenthesisFromString(stringToFilter);
                return stringToFilter;
            }
        }

        private static string FilterStringWithBlacklist(string stringToFilter)
        {
            BlacklistedWords.ForEach(i => stringToFilter = RemoveWordFromString(stringToFilter, i));
            return stringToFilter;
        }

        private static string RemoveWordFromString(string input, string search)
        {
            return Regex.Replace(
                input,
                Regex.Escape(search),
                "".Replace("$", "$$"),
                RegexOptions.IgnoreCase
            );
        }

        private static string RemoveParenthesisFromString(string stringToFilter)
        {
            if (!(IsRemix(stringToFilter)))
                stringToFilter = RemoveWordFromString(stringToFilter, "(\\[.*\\])|(\\(.*\\))");
            return stringToFilter;
        }

        private static bool IsRemix(string stringToCheck)
        {
            return CheckStringForWord(stringToCheck, "remix", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CheckStringForWord(string source, string toCheck, StringComparison comp)
        {
            return source != null && toCheck != null && source.IndexOf(toCheck, comp) >= 0;
        }
    }
}