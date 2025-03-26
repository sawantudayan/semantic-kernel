// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Magentic.Framework;
using Microsoft.SemanticKernel;
using ChatTokenUsage = OpenAI.Chat.ChatTokenUsage;

namespace Magentic.Agents;

/// <summary>
/// Extension methods for <see cref="ChatMessageContent"/>.
/// </summary>
public static class ChatMessageContentExtensions
{
    /// <summary>
    /// Get the usage metadata from a message.
    /// </summary>
    public static ChatTokenUsage? GetUsage(this ChatMessageContent message)
    {
        if (message.Metadata?.TryGetValue("Usage", out object? usage) ?? false)
        {
            if (usage is ChatTokenUsage chatUsage)
            {
                return chatUsage;
            }
        }
        return null;
    }

    /// <summary>
    /// Convert message content to a specific type.
    /// </summary>
    /// <remarks>
    /// This is used for structured output where an extremely strong expectation exists
    /// around the expected message structure.
    /// </remarks>
    internal static TValue GetValue<TValue>(this ChatMessageContent message) where TValue : class
    {
        if (string.IsNullOrWhiteSpace(message.Content))
        {
            throw new InvalidDataException("Message content is empty.");
        }

        return
            JsonSerializer.Deserialize<TValue>(message.Content) ??
            throw new InvalidDataException($"Message content does not align with requested type: {typeof(TValue).Name}.");
    }

    /// <summary>
    /// Transform chat message to a progress message.
    /// </summary>
    internal static Messages.Progress ToProgress(this ChatMessageContent message, string label)
    {
        ChatTokenUsage? usage = message.GetUsage();
        return
            new Messages.Progress
            {
                Label = label,
                TotalTokens = usage?.TotalTokenCount,
                InputTokens = usage?.InputTokenCount,
                OutputTokens = usage?.OutputTokenCount,
            };
    }
}
