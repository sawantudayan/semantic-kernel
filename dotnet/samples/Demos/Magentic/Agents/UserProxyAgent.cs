// Copyright (c) Microsoft. All rights reserved.

using Magentic.Framework;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Magentic.Agents;

/// <summary>
/// A <see cref="RuntimeAgent"/> that proxies user interactions.
/// </summary>
public sealed class UserProxyAgent : RuntimeAgent
{
    /// <summary>
    /// A common description for <see cref="UserProxyAgent"/>.
    /// </summary>
    public const string Description = "The user. Provides input or clarification.";

    /// <summary>
    /// The well-known <see cref="AgentType"/> for <see cref="UserProxyAgent"/>.
    /// </summary>
    public const string TypeId = nameof(UserProxyAgent);

    private readonly IUXServices _uxService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProxyAgent"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="uxService">Services for UX interactions.</param>
    public UserProxyAgent(AgentId id, IAgentRuntime runtime, IUXServices uxService)
        : base(id, runtime, Description)
    {
        this._uxService = uxService;

        this.RegisterHandler<Messages.Group>(this.OnGroupMessageAsync);
        this.RegisterHandler<Messages.Result>(this.OnUserMessageAsync);
        this.RegisterHandler<Messages.Progress>(this.OnProgressMessageAsync);
        this.RegisterHandler<Messages.Speak>(this.OnSpeakMessageAsync);
    }

    private async ValueTask OnGroupMessageAsync(Messages.Group message, MessageContext context)
    {
        // User input already visible, only output assistant responses.
        if (message.Message.Role != AuthorRole.User) // %%% MANAGER AS USER
        {
            await this._uxService.DisplayChatAsync(message.Message).ConfigureAwait(false);
        }
    }

    private async ValueTask OnProgressMessageAsync(Messages.Progress message, MessageContext context)
    {
        await this._uxService.DisplayProgressAsync(message).ConfigureAwait(false);
    }

    private async ValueTask OnUserMessageAsync(Messages.Result message, MessageContext context)
    {
        await this._uxService.DisplayOutputAsync(message.Message).ConfigureAwait(false);
    }

    private async ValueTask OnSpeakMessageAsync(Messages.Speak message, MessageContext context)
    {
        string? userInput = null;

        do
        {
            userInput = await this._uxService.ReadInputAsync().ConfigureAwait(false);
        }
        while (string.IsNullOrWhiteSpace(userInput));

        await this.PublishMessageAsync(new ChatMessageContent(AuthorRole.User, userInput).ToGroup(), ChatAgent.GroupChatTopic).ConfigureAwait(false);
    }
}
