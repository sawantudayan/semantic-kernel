// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

namespace Magentic.Agents.Internal;

internal static class ObjectExtensions
{
    private static readonly JsonSerializerOptions s_jsonOptionsCache = new() { WriteIndented = true };

    public static string AsJson(this object obj)
    {
        return JsonSerializer.Serialize(obj, s_jsonOptionsCache);
    }
}
