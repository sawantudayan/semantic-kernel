// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Step04b.Steps;

/// <summary>
/// This steps defines actions for the group chat in which to agents collaborate in
/// response to input from the primary agent.
/// </summary>
public class InnerChatStep : KernelProcessStep
{
    public static class ServiceKeys
    {
        public const string ManagerAgent = $"{nameof(InnerChatStep)}:{nameof(ManagerAgent)}";
        public const string AgentGroup1 = $"{nameof(InnerChatStep)}:{nameof(AgentGroup1)}";
        public const string AgentGroup2 = $"{nameof(InnerChatStep)}:{nameof(AgentGroup2)}";
        public const string AgentGroup3 = $"{nameof(InnerChatStep)}:{nameof(AgentGroup3)}";
        public const string Summarizer = $"{nameof(InnerChatStep)}:{nameof(Summarizer)}";
    }

    private readonly IChatHistoryProvider _historyProvider;
    private readonly ChatCompletionAgent _managerAgent;
    private readonly ChatHistorySummarizationReducer _summarizer;
    private readonly Dictionary<string, AgentGroupChat> _agentGroups;

    public static class Functions
    {
        public const string InvokeInnerChat = nameof(InvokeInnerChat);
    }

    public InnerChatStep(
        IChatHistoryProvider historyProvider,
        [FromKeyedServices(ServiceKeys.ManagerAgent)] ChatCompletionAgent managerAgent,
        [FromKeyedServices(ServiceKeys.Summarizer)] ChatHistorySummarizationReducer summarizer,
        [FromKeyedServices(ServiceKeys.AgentGroup1)] AgentGroupChat agentGroup1,
        [FromKeyedServices(ServiceKeys.AgentGroup2)] AgentGroupChat agentGroup2,
        [FromKeyedServices(ServiceKeys.AgentGroup3)] AgentGroupChat agentGroup3)
    {
        this._historyProvider = historyProvider;
        this._managerAgent = managerAgent;
        this._summarizer = summarizer;
        this._agentGroups = new() {
            { AgentDefinitions.Agent1.Name, agentGroup1 },
            { AgentDefinitions.Agent2.Name, agentGroup2 },
            { AgentDefinitions.Agent3.Name, agentGroup3 },
        };
    }

    [KernelFunction(Functions.InvokeInnerChat)]
    public async Task InvokeInnerChatAsync(KernelProcessStepContext context, Kernel kernel, string input)
    {
        string sessionId =
            (string?)kernel.Data[IChatHistoryProvider.SessionId] ??
            throw new InvalidOperationException("No session defined");
        ChatHistory history = await this._historyProvider.GetHistoryAsync(sessionId);
        ChatMessageContent userInput = new(AuthorRole.User, input);
        history.Add(userInput);

        await context.EmitEventAsync(new() { Id = AgentGroupOrchestrationEvents.ManagerMessage, Data = userInput });

        while (true)
        {
            ManagerResponse response = await this.InvokeManagerAsync(context, history);

            if (!this._agentGroups.TryGetValue(response.AgentName, out AgentGroupChat? agentGroup))
            {
                break; // Manager has specified "none" for the next agent
            }

            agentGroup.IsComplete = false;
            ChatMessageContent groupInput = new(AuthorRole.User, response.AgentInput);
            agentGroup.AddChatMessage(new ChatMessageContent(AuthorRole.User, response.AgentInput));
            await context.EmitEventAsync(new() { Id = AgentGroupOrchestrationEvents.InnerMessage, Data = groupInput });
            await foreach (ChatMessageContent message in agentGroup.InvokeAsync())
            {
                await context.EmitEventAsync(new() { Id = AgentGroupOrchestrationEvents.InnerMessage, Data = message });
            }

            // Note: History is provided in decending order, so the last message is the most recent.
            ChatMessageContent[] groupHistory = await agentGroup.GetChatMessagesAsync().ToArrayAsync();
            // Capturing the final researcher message.  Summarization could be performed.
            history.Add(groupHistory[1]);
        }

        IEnumerable<ChatMessageContent>? summarized = await this._summarizer.ReduceAsync(history);
        ChatMessageContent summary = summarized?.FirstOrDefault() ?? history.Last();
        await context.EmitEventAsync(new() { Id = AgentGroupOrchestrationEvents.ChatCompleted, Data = summary });
    }

    private async Task<ManagerResponse> InvokeManagerAsync(KernelProcessStepContext context, ChatHistory history)
    {
        ChatMessageContent response = await this._managerAgent.InvokeAsync(history).SingleAsync();
        await context.EmitEventAsync(new() { Id = AgentGroupOrchestrationEvents.ManagerMessage, Data = response });
        return JsonSerializer.Deserialize<ManagerResponse>(response.Content!)!;
    }
}
