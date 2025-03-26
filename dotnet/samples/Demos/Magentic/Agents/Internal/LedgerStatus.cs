// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using OpenAI.Chat;

namespace Magentic.Agents.Internal;

/// <summary>
/// Structured response for the ledger evaluation.
/// </summary>
internal sealed record LedgerStatus(
    [property:Description("The name of who is selected to respond.")]
    string Name,
    [property:Description("Direction to the selected responder that is ALWAYS phrased in the 2nd person.")]
    string Instruction,
    [property:Description("The reason for selecting the agent and its instruction.")]
    string Reason,
    [property:Description("Is the task completed?")]
    LedgerState IsTaskComplete,
    [property:Description("Is the task making progress, but not complete?")]
    LedgerState IsTaskProgressing,
    [property:Description("Is the task stuck in a loop?")]
    LedgerState IsTaskInLoop)
{
    public static readonly ChatResponseFormat Schema =
        ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "ledger_status",
            jsonSchema: BinaryData.FromString(JsonSchemaGenerator.FromType<LedgerStatus>()),
            jsonSchemaIsStrict: true);
}

internal sealed record LedgerState(
    [property:Description("The result for the property being evaluated")]
    bool Result,
    [property:Description("The reason for the result")]
    string Reason)
{
    public static implicit operator bool(LedgerState state) => state.Result;
}

//internal sealed record LedgerExpression( // %%% REMOVE
//    [property:Description("The text expression for the evaluated property")]
//    string Text,
//    [property:Description("The reason for the defined text")]
//    string Reason)
//{
//    public static implicit operator string(LedgerExpression expression) => expression.Text;
//}
