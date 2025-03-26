// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace Magentic.Framework;

/// <summary>
/// A <see cref="RuntimeAgent"/> built around a <see cref="ChatCompletionAgent"/>.
/// </summary>
public sealed class AgentProxy : ManagedAgent
{
    private readonly Agent _agent;
    private readonly List<ChatMessageContent> _cache;
    private AgentThread? _thread;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentProxy"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="agent">A <see cref="ChatCompletionAgent"/>.</param>
    public AgentProxy(AgentId id, IAgentRuntime runtime, Agent agent)
        : base(id, runtime, agent.Description ?? throw new ArgumentException($"The agent description must be defined (#{agent.Name ?? agent.Id})."))
    {
        this._agent = agent;
        this._cache = [];
    }

    /// <inheritdoc/>
    protected override ValueTask RecieveMessageAsync(ChatMessageContent message)
    {
        this._cache.Add(message);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async ValueTask ResetAsync()
    {
        if (this._thread is not null)
        {
            await this._thread.DeleteAsync().ConfigureAwait(false);
            this._thread = null;
        }
    }

    /// <inheritdoc/>
    protected override async ValueTask<ChatMessageContent> SpeakAsync()
    {
        AgentResponseItem<ChatMessageContent>[] responses = await this._agent.InvokeAsync(this._cache, this._thread).ToArrayAsync().ConfigureAwait(false);
        AgentResponseItem<ChatMessageContent> response = responses.First();
        this._thread ??= response.Thread;
        this._cache.Clear();
        return
            new ChatMessageContent(response.Message.Role, string.Join("\n\n", responses.Select(response => response.Message)))
            {
                AuthorName = response.Message.AuthorName,
            };
    }
}
