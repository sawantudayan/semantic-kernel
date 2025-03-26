// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Assistants;

namespace Magentic.Framework;

/// <summary>
/// Extensions for registering agents with a <see cref="IAgentRuntime"/>.
/// </summary>
public static class RuntimeExtensions
{
    /// <summary>
    /// Register a <see cref="ChatCompletionAgent"/> with an <see cref="IAgentRuntime"/>.
    /// </summary>
    /// <param name="runtime">A process runtime.</param>
    /// <param name="agent">A <see cref="ChatCompletionAgent"/> to register.</param>
    /// <param name="topics">The topics to which the agent is subscribed.</param>
    /// <returns></returns>
    public static async Task<ChatCompletionAgent> RegisterAgentAsync(this IAgentRuntime runtime, ChatCompletionAgent agent, params string[] topics)
    {
        string agentType = agent.Name ?? agent.Id;
        await runtime.RegisterAgentFactoryAsync(
            agent.Name ?? agent.Id,
            (agentId, runtime) => ValueTask.FromResult<IHostableAgent>(new ChatAgent(agentId, runtime, agent))).ConfigureAwait(false);

        await runtime.RegisterTopics(agentType, topics).ConfigureAwait(false);

        return agent;
    }

    /// <summary>
    /// Register a <see cref="OpenAIAssistantAgent"/> with an <see cref="IAgentRuntime"/>.
    /// </summary>
    /// <param name="runtime">A process runtime.</param>
    /// <param name="agent">A <see cref="ChatCompletionAgent"/> to register.</param>
    /// <param name="client">// %%% COMMENT</param>
    /// <param name="topics">The topics to which the agent is subscribed.</param>
    /// <returns></returns>
    public static async Task<OpenAIAssistantAgent> RegisterAgentAsync(this IAgentRuntime runtime, OpenAIAssistantAgent agent, AssistantClient client, params string[] topics)
    {
        string agentType = agent.Name ?? agent.Id;
        await runtime.RegisterAgentFactoryAsync(
            agent.Name ?? agent.Id,
            (agentId, runtime) => ValueTask.FromResult<IHostableAgent>(new AssistantAgent(agentId, runtime, agent, client))).ConfigureAwait(false);

        await runtime.RegisterTopics(agentType, topics).ConfigureAwait(false);

        return agent;
    }

    /// <summary>
    /// Register agent (by type) with a set of topics.
    /// </summary>
    /// <param name="runtime">A process runtime.</param>
    /// <param name="agentType">The target agent-type</param>
    /// <param name="topics">A list of topics</param>
    /// <returns></returns>
    public static async Task RegisterTopics(this IAgentRuntime runtime, string agentType, params string[] topics)
    {
        for (int index = 0; index < topics.Length; ++index)
        {
            await runtime.AddSubscriptionAsync(new Subscription(topics[index], agentType)).ConfigureAwait(false);
        }
    }
}
