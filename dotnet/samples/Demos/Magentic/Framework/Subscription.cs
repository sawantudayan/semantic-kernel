// Copyright (c) Microsoft. All rights reserved.

namespace Magentic.Framework;

internal sealed class Subscription(string topicType, string agentType, string? id = null) : ISubscriptionDefinition
{
    /// <inheritdoc/>
    public string Id { get; } = id ?? Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the topic associated with the subscription.
    /// </summary>
    public string TopicType { get; } = topicType;

    /// <inheritdoc/>
    public bool Equals(ISubscriptionDefinition? other) => this.Id == other?.Id;

    /// <inheritdoc/>
    public override int GetHashCode() => this.Id.GetHashCode();

    /// <inheritdoc/>
    public AgentId MapToAgent(TopicId topic)
    {
        if (!this.Matches(topic))
        {
            throw new InvalidOperationException("Topic does not match the subscription.");
        }

        return new AgentId(agentType, topic.Source);
    }

    /// <inheritdoc/>
    public bool Matches(TopicId topic) => topic.Type == this.TopicType;
}
