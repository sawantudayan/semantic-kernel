// Copyright (c) Microsoft. All rights reserved.

namespace Magentic.Agents.Internal;

internal static class Topics
{
    public const string CoderAgentTopic = $"{nameof(Agents.CoderAgent)}Topic";
    public const string ResearchAgentTopic = $"{nameof(Agents.ResearchAgent)}Topic";
    public const string IllustratorAgentTopic = $"{nameof(IllustratorAgent)}Topic";
    public const string UserProxyTopic = $"{nameof(UserProxyAgent)}Topic";
}
