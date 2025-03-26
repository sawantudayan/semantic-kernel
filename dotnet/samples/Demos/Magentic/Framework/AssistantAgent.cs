// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.Agents.OpenAI.Internal;
using OpenAI.Assistants;

namespace Magentic.Framework;

/// <summary>
/// A <see cref="RuntimeAgent"/> built around a <see cref="ChatCompletionAgent"/>.
/// </summary>
public sealed class AssistantAgent : ManagedAgent
{
    private readonly OpenAIAssistantAgent _agent;
    private readonly AssistantClient _client;
    private string _threadId = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAgent"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="agent">A <see cref="OpenAIAssistantAgent"/>.</param>
    /// <param name="client">The assistant client.</param>
    public AssistantAgent(AgentId id, IAgentRuntime runtime, OpenAIAssistantAgent agent, AssistantClient client)
        : base(id, runtime, agent.Description ?? throw new ArgumentException($"The agent description must be defined (#{agent.Name ?? agent.Id})."))
    {
        this._agent = agent;
        this._client = client;
    }

    /// <inheritdoc/>
    protected override async ValueTask RecieveMessageAsync(ChatMessageContent message)
    {
        await this.CreateThreadAsync().ConfigureAwait(false);
        await this._client.CreateMessageAsync(
            this._threadId,
            message.Role.ToMessageRole(),
            AssistantMessageFactory.GetMessageContents(message)).ConfigureAwait(false);
    }

    private async Task CreateThreadAsync()
    {
        if (string.IsNullOrWhiteSpace(this._threadId))
        {
            AssistantThread thread = await this._client.CreateThreadAsync().ConfigureAwait(false);
            this._threadId = thread.Id;
        }
    }

    /// <inheritdoc/>
    protected override async ValueTask ResetAsync()
    {
        if (!string.IsNullOrWhiteSpace(this._threadId))
        {
            await this._client.DeleteThreadAsync(this._threadId).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    protected override async ValueTask<ChatMessageContent> SpeakAsync()
    {
        await this.CreateThreadAsync().ConfigureAwait(false);
        ChatMessageContent[] responses = await this._agent.InvokeAsync(this._threadId).ToArrayAsync().ConfigureAwait(false);
        return
            new ChatMessageContent(responses[0].Role, string.Join("\n\n", responses.Select(r => r.Content)))
            {
                AuthorName = responses[0].AuthorName
            };
    }
}
