// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace Magentic.Agents.Internal;

internal static class JsonSchemaGenerator
{
    /// <summary>
    /// Wrapper for generating a JSON schema as string from a .NET type.
    /// </summary>
    public static string FromType<TSchemaType>()
    {
        JsonSerializerOptions options = new(JsonSerializerOptions.Default)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        AIJsonSchemaCreateOptions config = new()
        {
            IncludeSchemaKeyword = false,
            DisallowAdditionalProperties = true,
        };

        return KernelJsonSchemaBuilder.Build(typeof(TSchemaType), "Intent Result", config).AsJson();
    }
}

internal static class KernelJsonSchemaBuilder
{
    private static JsonSerializerOptions? s_options;
    internal static readonly AIJsonSchemaCreateOptions s_schemaOptions = new()
    {
        IncludeSchemaKeyword = false,
        IncludeTypeInEnumSchemas = true,
        RequireAllProperties = false,
        DisallowAdditionalProperties = false,
    };

    private static readonly JsonElement s_trueSchemaAsObject = JsonDocument.Parse("{}").RootElement;
    private static readonly JsonElement s_falseSchemaAsObject = JsonDocument.Parse("""{"not":true}""").RootElement;

    public static KernelJsonSchema Build(Type type, string? description = null, AIJsonSchemaCreateOptions? configuration = null)
    {
        return Build(type, GetDefaultOptions(), description, configuration);
    }

    public static KernelJsonSchema Build(
        Type type,
        JsonSerializerOptions options,
        string? description = null,
        AIJsonSchemaCreateOptions? configuration = null)
    {
        configuration ??= s_schemaOptions;
        JsonElement schemaDocument = AIJsonUtilities.CreateJsonSchema(type, description, serializerOptions: options, inferenceOptions: configuration);
        switch (schemaDocument.ValueKind)
        {
            case JsonValueKind.False:
                schemaDocument = s_falseSchemaAsObject;
                break;
            case JsonValueKind.True:
                schemaDocument = s_trueSchemaAsObject;
                break;
        }

        return KernelJsonSchema.Parse(schemaDocument.GetRawText());
    }

    private static JsonSerializerOptions GetDefaultOptions()
    {
        if (s_options is null)
        {
            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                Converters = { new JsonStringEnumConverter() },
            };
            options.MakeReadOnly();
            s_options = options;
        }

        return s_options;
    }
}
