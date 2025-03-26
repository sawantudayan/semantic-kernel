// Copyright (c) Microsoft. All rights reserved.

using Magentic.Framework;
using Microsoft.SemanticKernel;

namespace Magentic.Agents;

public sealed partial class OrchestratorAgent
{
    private sealed class State(AgentTeam team)
    {
        public string Team { get; } = team.FormatList();
        public string Names { get; } = team.FormatNames();

        public ChatMessageContent? Facts { get; set; }
        public ChatMessageContent? Plan { get; set; }
        public ChatMessageContent? Ledger { get; set; }
    }
}
