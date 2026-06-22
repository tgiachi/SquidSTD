using System.Collections;
using System.Text.RegularExpressions;

namespace SquidStd.Core.Extensions.Env;

/// <summary>
/// Provides extension methods for expanding environment variables in strings
/// </summary>
public static class EnvExtensions
{
    /// <summary>
    /// Expands environment variables in a string using custom $VARIABLE syntax
    /// </summary>
    /// <param name="input">The input string containing environment variable references</param>
    /// <returns>The string with environment variables expanded to their values</returns>
    public static string ExpandEnvironmentVariables(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
        {
            var key = $"${env.Key}";
            var value = env.Value?.ToString() ?? string.Empty;
            input = input.Replace(key, value);
        }

        return input;
    }

    /// <summary>
    /// Replaces "$VAR" tokens with the matching environment variable value. Unknown variables are
    /// left unchanged.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The string with known $VAR tokens substituted.</returns>
    public static string ReplaceEnv(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return EnvTokenRegex.Replace(
            input,
            match =>
            {
                var name = match.Groups[1].Value;
                var value = Environment.GetEnvironmentVariable(name);

                return value ?? match.Value;
            });
    }

    private static readonly Regex EnvTokenRegex = new(
        @"\$([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);
}
