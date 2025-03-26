// Copyright (c) Microsoft. All rights reserved.

using Magentic.Framework;
using Microsoft.SemanticKernel;

namespace Magentic.Agents;

/// <summary>
/// Defines a common interface for user interaction.
/// </summary>
public interface IUXServices
{
    /// <summary>
    /// Defines a signature for soliciting textual input.
    /// </summary>
    /// <returns>The input text.</returns>
    ValueTask<string?> ReadInputAsync();

    /// <summary>
    /// Defines a signature for displaying the inner agent chat.
    /// </summary>
    ValueTask DisplayChatAsync(ChatMessageContent message);

    /// <summary>
    /// Defines a signature for displaying the messages to the user.
    /// </summary>
    ValueTask DisplayOutputAsync(ChatMessageContent message);

    /// <summary>
    /// Defines a signature for displaying task progress.
    /// </summary>
    ValueTask DisplayProgressAsync(Messages.Progress message);
}
