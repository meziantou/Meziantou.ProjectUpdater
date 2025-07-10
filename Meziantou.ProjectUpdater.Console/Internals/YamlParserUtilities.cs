using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Meziantou.ProjectUpdater.Console.Internals;

internal static class YamlParserUtilities
{
    public static YamlStream? LoadYamlDocument(string text)
    {
        using var textReader = new StringReader(text);
        return LoadYamlDocument(textReader);
    }

    public static YamlStream? LoadYamlDocument(Stream stream)
    {
        using var textReader = new StreamReader(stream, leaveOpen: true);
        return LoadYamlDocument(textReader);
    }

    public static YamlStream? LoadYamlDocument(TextReader textReader)
    {
        try
        {
            var reader = new MergingParser(new Parser(textReader));
            var yaml = new YamlStream();
            yaml.Load(reader);
            return yaml;
        }
        catch
        {
            return null;
        }
    }

    public static YamlNode? GetPropertyValue(YamlNode node, string propertyName, StringComparison stringComparison)
    {
        return GetProperty(node, propertyName, stringComparison)?.Value;
    }
    
    public static KeyValuePair<YamlNode, YamlNode>? GetProperty(YamlNode node, string propertyName, StringComparison stringComparison)
    {
        if (node is YamlMappingNode mapping)
        {
            foreach (var child in mapping.Children)
            {
                if (child.Key is YamlScalarNode scalar && string.Equals(scalar.Value, propertyName, stringComparison))
                    return child;
            }
        }

        return null;
    }

    public static string? GetScalarValue(YamlNode? node)
    {
        if (node is YamlScalarNode scalar && scalar.Value is not null)
            return scalar.Value;

        return null;
    }
}