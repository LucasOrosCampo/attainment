using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace attainment.Models;

/// <summary>
/// Represents an application setting stored as key/value.
/// Key is the primary key.
/// </summary>
public class Setting
{
    /// <summary>
    /// Setting key (primary key)
    /// </summary>
    [Key]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value
    /// </summary>
    public string? Value { get; set; }
}

/// <summary>
/// Static container for all supported setting keys.
/// </summary>
public static class SettingKeys
{
    public const string OpenAIKey = "openai.key";
    public const string OpenAIModel = "openai.model";

    public static string? DefaultValue(string key) => key switch
    {
        OpenAIModel => "gpt-4o-mini",
        _ => null
    };

    /// <summary>
    /// Returns all keys defined as public const string on this class.
    /// </summary>
    public static IEnumerable<string> All()
    {
        return typeof(SettingKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
    }
}
