using System.Text.Json.Serialization;

namespace SquidStd.Tests.Support;

/// <summary>
/// Source-generated JSON serializer context exposing the test DTO types.
/// </summary>
[JsonSerializable(typeof(SampleDto)), JsonSerializable(typeof(OtherDto))]
public partial class TestJsonContext : JsonSerializerContext { }
