using YamlDotNet.Serialization;

namespace SquidStd.Core.Yaml;

/// <summary>
/// Provides YAML serialization helpers.
/// </summary>
public static class YamlUtils
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
                                                     .DisableAliases()
                                                     .WithIndentedSequences()
                                                     .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
                                                         .IgnoreUnmatchedProperties()
                                                         .Build();

    /// <summary>
    /// Deserializes YAML text using reflection-based metadata.
    /// </summary>
    /// <param name="yaml">The YAML text to deserialize.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T Deserialize<T>(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        return Deserializer.Deserialize<T>(yaml) ??
               throw new InvalidDataException($"Deserialization returned null for type {typeof(T).Name}");
    }

    /// <summary>
    /// Deserializes YAML text to the specified runtime type.
    /// </summary>
    /// <param name="yaml">The YAML text to deserialize.</param>
    /// <param name="type">The target type.</param>
    /// <returns>The deserialized object.</returns>
    public static object Deserialize(string yaml, Type type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        ArgumentNullException.ThrowIfNull(type);

        return Deserializer.Deserialize(yaml, type) ??
               throw new InvalidDataException($"Deserialization returned null for type {type.Name}");
    }

    /// <summary>
    /// Deserializes YAML from a file using reflection-based metadata.
    /// </summary>
    /// <param name="filePath">The YAML file path.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T DeserializeFromFile<T>(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var yaml = File.ReadAllText(GetExistingFilePath(filePath));

        return Deserialize<T>(yaml);
    }

    /// <summary>
    /// Deserializes a top-level YAML section to the specified runtime type.
    /// </summary>
    /// <param name="yaml">The YAML document.</param>
    /// <param name="sectionName">The top-level section name.</param>
    /// <param name="type">The target section type.</param>
    /// <returns>The deserialized section, or null when the section is absent.</returns>
    public static object? DeserializeSection(string yaml, string sectionName, Type type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(type);

        var sections = Deserialize<Dictionary<string, object?>>(yaml);

        if (!sections.TryGetValue(sectionName, out var section) || section is null)
        {
            return null;
        }

        return Deserialize(Serializer.Serialize(section), type);
    }

    /// <summary>
    /// Serializes an object to YAML using reflection-based metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <typeparam name="T">The source type.</typeparam>
    /// <returns>The serialized YAML text.</returns>
    public static string Serialize<T>(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return Serializer.Serialize(obj);
    }

    /// <summary>
    /// Serializes top-level YAML sections.
    /// </summary>
    /// <param name="sections">The section map to serialize.</param>
    /// <returns>The serialized YAML document.</returns>
    public static string SerializeSections(IReadOnlyDictionary<string, object> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);

        return Serializer.Serialize(sections);
    }

    /// <summary>
    /// Serializes an object to a YAML file using reflection-based metadata.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="filePath">The output YAML file path.</param>
    /// <typeparam name="T">The source type.</typeparam>
    public static void SerializeToFile<T>(T obj, string filePath)
    {
        var yaml = Serialize(obj);

        File.WriteAllText(GetWritableFilePath(filePath), yaml);
    }

    private static string GetExistingFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var normalizedPath = Path.GetFullPath(filePath);

        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"The file '{normalizedPath}' does not exist.", normalizedPath);
        }

        return normalizedPath;
    }

    private static string GetWritableFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var normalizedPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(normalizedPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return normalizedPath;
    }
}
