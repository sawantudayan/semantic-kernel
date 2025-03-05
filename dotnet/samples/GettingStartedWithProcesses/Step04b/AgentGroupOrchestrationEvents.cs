// Copyright (c) Microsoft. All rights reserved.
namespace Step04b;

/// <summary>
/// Processes events used in <see cref="Step04b_AgentGroupOrchestration"/> samples
/// </summary>
public static class AgentGroupOrchestrationEvents
{
    public static readonly string StartProcess = nameof(StartProcess);

    public static readonly string ChatInput = nameof(ChatInput);
    public static readonly string ManagerMessage = nameof(ManagerMessage);
    public static readonly string InnerMessage = nameof(InnerMessage);
    public static readonly string ChatCompleted = nameof(ChatCompleted);
}
