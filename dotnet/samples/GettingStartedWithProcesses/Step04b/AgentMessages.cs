// Copyright (c) Microsoft. All rights reserved.
using System.ComponentModel;
using ChatResponseFormat = OpenAI.Chat.ChatResponseFormat;

namespace Step04b;

/// <summary>
/// Structred response from the manager agent.
/// </summary>
public sealed record ManagerResponse(
    [property:Description("The selected agent")]
    string AgentName,
    [property:Description("The instructions for the selected agent")]
    string AgentInput,
    [property:Description("The reason for selecting the agent")]
    string Reason)
{
    public static readonly ChatResponseFormat Schema =
        ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "manager_response",
            jsonSchema: BinaryData.FromString(JsonSchemaGenerator.FromType<ManagerResponse>()),
            jsonSchemaIsStrict: true);
}
