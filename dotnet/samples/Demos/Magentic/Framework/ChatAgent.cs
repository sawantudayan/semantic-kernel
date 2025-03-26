// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Magentic.Framework;

/// <summary>
/// A <see cref="RuntimeAgent"/> built around a <see cref="ChatCompletionAgent"/>.
/// </summary>
public sealed class ChatAgent : ManagedAgent
{
    private readonly ChatCompletionAgent _agent;
    private readonly ChatHistory _chat;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAgent"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="agent">A <see cref="ChatCompletionAgent"/>.</param>
    public ChatAgent(AgentId id, IAgentRuntime runtime, ChatCompletionAgent agent)
        : base(id, runtime, agent.Description ?? throw new ArgumentException($"The agent description must be defined (#{agent.Name ?? agent.Id})."))
    {
        this._agent = agent;
        this._chat = [];
    }

    /// <inheritdoc/>
    protected override ValueTask RecieveMessageAsync(ChatMessageContent message)
    {
        this._chat.Add(message);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask ResetAsync()
    {
        this._chat.Clear();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async ValueTask<ChatMessageContent> SpeakAsync()
    {
        ChatMessageContent response = await this._agent.InvokeAsync(this._chat).SingleAsync().ConfigureAwait(false);
        this._chat.Add(response);
        return response;
    }
}
