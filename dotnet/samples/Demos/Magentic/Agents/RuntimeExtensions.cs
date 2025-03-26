// Copyright (c) Microsoft. All rights reserved.

using Magentic.Agents.Internal;
using Magentic.Framework;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Assistants;

namespace Magentic.Agents;

/// <summary>
/// Extension methods for initializing a <see cref="IAgentRuntime"/> with the Magentic team.
/// </summary>
public static class RuntimeExtensions
{
    /// <summary>
    /// Initialize a <see cref="IAgentRuntime"/> with the Magentic team.
    /// </summary>
    /// <param name="runtime">A process runtime.</param>
    /// <param name="kernel">A kernel for use with <see cref="ChatCompletionAgent"/>.</param>
    /// <param name="uxServices">Services for UX interactions.</param>
    public static async Task<AgentTeam> RegisterMagenticTeamAsync(this IAgentRuntime runtime, Kernel kernel, IUXServices uxServices)
    {
        ChatCompletionAgent researchAgent =
            await runtime.RegisterAgentAsync(
                ResearchAgent.Create(kernel.Clone()),
                ChatAgent.GroupChatTopic.Type,
                Topics.ResearchAgentTopic).ConfigureAwait(false);

        AssistantClient client = kernel.GetRequiredService<AssistantClient>();
        OpenAIAssistantAgent coderAgent =
            await runtime.RegisterAgentAsync(
                await CoderAgent.CreateAsync(kernel, client).ConfigureAwait(false),
                client,
                ChatAgent.GroupChatTopic.Type,
                Topics.CoderAgentTopic).ConfigureAwait(false);

        ChatCompletionAgent illustratorAgent =
            await runtime.RegisterAgentAsync(
                IllustratorAgent.Create(kernel.Clone()),
                ChatAgent.GroupChatTopic.Type,
                Topics.IllustratorAgentTopic).ConfigureAwait(false);

        await runtime.RegisterUserProxyAsync(
            uxServices,
            ChatAgent.GroupChatTopic.Type,
            ChatAgent.InnerChatTopic.Type,
            Topics.UserProxyTopic).ConfigureAwait(false);

        AgentTeam team =
            new()
            {
                { researchAgent.Name ?? researchAgent.Id,  (new TopicId(Topics.ResearchAgentTopic), researchAgent.Description) },
                { coderAgent.Name ?? coderAgent.Id,  (new TopicId(Topics.CoderAgentTopic), coderAgent.Description) },
                { illustratorAgent.Name ?? illustratorAgent.Id,  (new TopicId(Topics.IllustratorAgentTopic), illustratorAgent.Description) },
                { nameof(UserProxyAgent),  (new TopicId(Topics.UserProxyTopic), UserProxyAgent.Description) },
            };

        return team;
    }

    /// <summary>
    /// Register a <see cref="ManagerAgent"/> with an <see cref="IAgentRuntime"/>.
    /// </summary>
    /// <param name="runtime">A process runtime.</param>
    /// <param name="kernel">A kernel for chat-completion services.</param>
    /// <param name="team">The team of agents being orchestrated</param>
    public static async ValueTask RegisterOrchestratorAsync(this IAgentRuntime runtime, Kernel kernel, AgentTeam team)
    {
        await runtime.RegisterAgentFactoryAsync(
            OrchestratorAgent.TypeId,
            (agentId, runtime) => ValueTask.FromResult<IHostableAgent>(new OrchestratorAgent(agentId, runtime, kernel.Clone(), team))).ConfigureAwait(false);
        await runtime.RegisterTopics(OrchestratorAgent.TypeId, ChatAgent.GroupChatTopic.Type).ConfigureAwait(false);
    }

    /// <summary>
    /// Register a <see cref="UserProxyAgent"/> with an <see cref="IAgentRuntime"/>.
    /// </summary>
    /// <param name="runtime">A process runtime.</param>
    /// <param name="uxServices">Services for UX interactions.</param>
    /// <param name="topics">The topics to which the agent is subscribed.</param>
    private static async Task RegisterUserProxyAsync(this IAgentRuntime runtime, IUXServices uxServices, params string[] topics)
    {
        await runtime.RegisterAgentFactoryAsync(
            UserProxyAgent.TypeId,
            (agentId, runtime) => ValueTask.FromResult<IHostableAgent>(new UserProxyAgent(agentId, runtime, uxServices))).ConfigureAwait(false);

        await runtime.RegisterTopics(UserProxyAgent.TypeId, topics).ConfigureAwait(false);
    }
}
