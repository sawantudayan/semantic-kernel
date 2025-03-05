// Copyright (c) Microsoft. All rights reserved.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Step04b;

/// <summary>
/// Provider based access to the chat history.
/// </summary>
/// <remarks>
/// While the in-memory implementation is trivial, this abstraction demonstrates how one might
/// allow for the ability to access chat history from a remote store for a distributed service.
/// </remarks>
public interface IChatHistoryProvider
{
    /// <summary>
    /// The key used to store the session id in the <see cref="Kernel.Data"/> dictionary.
    /// </summary>
    const string SessionId = nameof(SessionId);

    /// <summary>
    /// Provides access to the chat history.
    /// </summary>
    Task<ChatHistory> GetHistoryAsync(string sessionId);

    /// <summary>
    /// Commits any updates to the chat history.
    /// </summary>
    Task CommitAsync(string sessionId, IEnumerable<ChatMessageContent> messages);
}

/// <summary>
/// In memory based specialization of <see cref="IChatHistoryProvider"/>.
/// </summary>
internal sealed class ChatHistoryProvider() : IChatHistoryProvider
{
    private readonly Dictionary<string, ChatHistory> _chatHistories = [];

    /// <inheritdoc/>
    public Task<ChatHistory> GetHistoryAsync(string sessionId)
    {
        if (!this._chatHistories.TryGetValue(sessionId, out ChatHistory? sessionHistory))
        {
            sessionHistory = [];
            this._chatHistories[sessionId] = sessionHistory;
        }
        return Task.FromResult(sessionHistory);
    }

    /// <inheritdoc/>
    public async Task CommitAsync(string sessionId, IEnumerable<ChatMessageContent> messages)
    {
        ChatHistory history = await this.GetHistoryAsync(sessionId);
        history.AddRange(messages);
    }
}
