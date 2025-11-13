using System.Text.RegularExpressions;
using Humanizer;

namespace OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class StringExtensions
{
    /// <summary>
    ///     Convert a string to snake case
    /// </summary>
    /// <param name="text">The string to convert</param>
    /// <returns>The string converted to snake case</returns>
    public static string ToSnakeCase(this string text)
    {
        return new string(Convert(text.GetEnumerator()).ToArray());
        static IEnumerable<char> Convert(CharEnumerator e)
        {
            if (!e.MoveNext()) yield break;
            yield return char.ToLower(e.Current);
            while (e.MoveNext())
                if (char.IsUpper(e.Current))
                {
                    yield return '_';
                    yield return char.ToLower(e.Current);
                }
                else
                {
                    yield return e.Current;
                }
        }
    }

    /// <summary>
    ///     Pluralize a string
    /// </summary>
    /// <param name="text">The string to convert</param>
    /// <returns>The string converted to plural</returns>
    public static string ToPlural(this string text)
    {
        return text.Pluralize(false);
    }

    /// <summary>
    ///     Convert a string to kebab-case
    /// </summary>
    /// <param name="text">The string to convert</param>
    /// <returns>The string converted to kebab-case</returns>
    public static string ToKebabCase(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        
        text = Regex.Replace(text, "([A-Z]+)([A-Z][a-z])", "$1-$2");
        
        text = Regex.Replace(text, "([a-z0-9])([A-Z])", "$1-$2");
        
        return text.ToLower();
    }
}