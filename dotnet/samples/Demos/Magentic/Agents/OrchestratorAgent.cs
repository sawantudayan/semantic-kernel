// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using Magentic.Agents.Internal;
using Magentic.Framework;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Magentic.Agents;

/// <summary>
/// The Magentic team orchestrator.
/// </summary>
public sealed partial class OrchestratorAgent : ManagerAgent
{
    private const int DefaultMaximumRetryCount = 2;
    private const int DefaultMaximumStallCount = 3;

    /// <summary>
    /// The well-known <see cref="AgentType"/> for <see cref="OrchestratorAgent"/>.
    /// </summary>
    public const string TypeId = nameof(OrchestratorAgent);
    private readonly Kernel _kernel;
    private readonly State _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestratorAgent"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="kernel">A kernel for model services</param>
    /// <param name="team">The team of agents being orchestrated</param>
    public OrchestratorAgent(AgentId id, IAgentRuntime runtime, Kernel kernel, AgentTeam team)
        : base(id, runtime, team)
    {
        this._kernel = kernel;
        this._state = new State(this.Team);

        Debug.WriteLine($"\n<NAMES>:\n{this._state.Names}\n</NAMES>\n");
        Debug.WriteLine($"<TEAM>:\n{this._state.Team}\n</TEAM>\n");
    }

    /// <summary>
    /// The maximum number of retry attempts when the task execution faulters.
    /// </summary>
    public int MaximumRetryCount { get; init; } = DefaultMaximumRetryCount;

    /// <inheritdoc/>
    protected override async Task<TopicId?> PrepareTaskAsync()
    {
        ChatHistory internalChat = [];

        await this.AnalyzeFactsAsync(internalChat).ConfigureAwait(false);
        await this.AnalyzePlanAsync(internalChat).ConfigureAwait(false);
        await this.GenerateLedgerAsync(internalChat).ConfigureAwait(false);

        return await this.SelectAgentAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override async Task<TopicId?> SelectAgentAsync()
    {
        int stallCount = 0;
        int retryCount = 0;
        bool isStalled = false;

        do
        {
            string agentName = string.Empty;
            string agentInstruction = string.Empty;
            try
            {
                LedgerStatus status = await this.AnalyzeStatusAsync().ConfigureAwait(false);

                if (status.IsTaskComplete)
                {
                    // %%% TODO: Refine
                    await this.GenerateAnswerAsync().ConfigureAwait(false);
                    return null;
                }

                isStalled = !status.IsTaskProgressing || status.IsTaskInLoop;
                agentName = status.Name;
                agentInstruction = status.Instruction;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
                isStalled = true;
            }

            if (!isStalled && this.Team.TryGetValue(agentName, out (TopicId Topic, string? Description) value))
            {
                stallCount = Math.Max(0, stallCount - 1); // %%% CONSIDER: INFINITE PING PONG

                ChatMessageContent instruction =
                    new(AuthorRole.Assistant, agentInstruction)
                    {
                        AuthorName = nameof(OrchestratorAgent),
                    };
                this.Chat.Add(instruction);
                await this.PublishMessageAsync(instruction.ToGroup(), ChatAgent.GroupChatTopic).ConfigureAwait(false);

                return value.Topic;
            }

            isStalled = true;
            ++stallCount;

            Debug.WriteLine($"TASK STALLED: #{stallCount}/{DefaultMaximumStallCount} [#{retryCount}]");

            if (stallCount >= DefaultMaximumStallCount)
            {
                if (retryCount >= this.MaximumRetryCount)
                {
                    await this.PublishMessageAsync(new ChatMessageContent(AuthorRole.User, "I've experienced multiple failures and am unable to continue.").ToGroup(), ChatAgent.GroupChatTopic).ConfigureAwait(false);
                    return null;
                }

                retryCount++;
                stallCount = 0;

                Debug.WriteLine($"TASK RESET [#{retryCount}]");

                await this.PublishMessageAsync(new Messages.Reset(), ChatAgent.GroupChatTopic).ConfigureAwait(false);
                await this.UpdateTaskAsync().ConfigureAwait(false);
            }
        }
        while (isStalled);

        return null;
    }

    private async Task UpdateTaskAsync()
    {
        ChatHistory internalChat = [.. this.Chat];
        this.Chat.Clear();

        await this.UpdateFactsAsync(internalChat).ConfigureAwait(false);
        await this.UpdatePlanAsync(internalChat).ConfigureAwait(false);
        await this.GenerateLedgerAsync(internalChat).ConfigureAwait(false);
    }

    private async Task AnalyzeFactsAsync(ChatHistory internalChat)
    {
        if (this._state.Facts != null)
        {
            return;
        }
        KernelArguments arguments =
            new()
            {
                { Prompts.Parameters.Task, this.Task.Input },
            };
        this._state.Facts = await this.GetResponseAsync(internalChat, Prompts.NewFactsTemplate, arguments).ConfigureAwait(false);
        Debug.WriteLine($"\n<FACTS>:\n{this._state.Facts}\n</FACTS>\n");
        await this.PublishMessageAsync(this._state.Facts.ToProgress("Analyzed task..."), ChatAgent.InnerChatTopic).ConfigureAwait(false);
    }

    private async Task UpdateFactsAsync(ChatHistory internalChat)
    {
        KernelArguments arguments =
            new()
            {
                { Prompts.Parameters.Task, this.Task.Input },
                { Prompts.Parameters.Facts, this._state.Facts },
            };
        this._state.Facts = await this.GetResponseAsync(internalChat, Prompts.NewFactsTemplate, arguments).ConfigureAwait(false);
        Debug.WriteLine($"\n<FACTS>:\n{this._state.Facts}\n</FACTS>\n");
        await this.PublishMessageAsync(this._state.Facts.ToProgress("Analyzed task..."), ChatAgent.InnerChatTopic).ConfigureAwait(false);
    }

    private async Task AnalyzePlanAsync(ChatHistory internalChat)
    {
        if (this._state.Plan != null)
        {
            return;
        }
        KernelArguments arguments =
            new()
            {
                { Prompts.Parameters.Team, this._state.Team },
            };
        this._state.Plan = await this.GetResponseAsync(internalChat, Prompts.NewPlanTemplate, arguments).ConfigureAwait(false);
        Debug.WriteLine($"\n<PLAN>:\n{this._state.Plan}\n</PLAN>\n");
        await this.PublishMessageAsync(this._state.Plan.ToProgress("Generated plan..."), ChatAgent.InnerChatTopic).ConfigureAwait(false);
    }

    private async Task UpdatePlanAsync(ChatHistory internalChat)
    {
        KernelArguments arguments =
            new()
            {
                { Prompts.Parameters.Team, this._state.Team },
            };
        this._state.Plan = await this.GetResponseAsync(internalChat, Prompts.NewPlanTemplate, arguments).ConfigureAwait(false);
        Debug.WriteLine($"\n<PLAN>:\n{this._state.Plan}\n</PLAN>\n");
        await this.PublishMessageAsync(this._state.Plan.ToProgress("Generated plan..."), ChatAgent.InnerChatTopic).ConfigureAwait(false);
    }

    private async Task GenerateLedgerAsync(ChatHistory internalChat)
    {
        KernelArguments arguments =
            new()
            {
                { Prompts.Parameters.Task, this.Task.Input },
                { Prompts.Parameters.Team, this._state.Team },
                { Prompts.Parameters.Facts, this._state.Facts },
                { Prompts.Parameters.Plan, this._state.Plan },
            };
        this._state.Ledger = await this.GetMessageAsync(Prompts.LedgerTemplate, arguments).ConfigureAwait(false);
        this.Chat.Add(this._state.Ledger);
        await this.PublishMessageAsync(this._state.Ledger.ToGroup(), ChatAgent.GroupChatTopic).ConfigureAwait(false);
    }

    private async Task<LedgerStatus> AnalyzeStatusAsync()
    {
        ChatHistory internalChat = [.. this.Chat];
        OpenAIPromptExecutionSettings executionSettings = new() { ResponseFormat = LedgerStatus.Schema };
        KernelArguments arguments =
            new()
            {
                { Prompts.Parameters.Task, this.Task.Input },
                { Prompts.Parameters.Team, this._state.Team },
                { Prompts.Parameters.Facts, this._state.Facts },
            };
        ChatMessageContent response = await this.GetResponseAsync(internalChat, Prompts.StatusTemplate, arguments, executionSettings).ConfigureAwait(false);
        await this.PublishMessageAsync(response.ToProgress("Evaluted status..."), ChatAgent.InnerChatTopic).ConfigureAwait(false);
        LedgerStatus status = response.GetValue<LedgerStatus>();
        Debug.WriteLine(status.AsJson());
        return status;
    }

    private async Task GenerateAnswerAsync()
    {
        KernelArguments arguments =
            new()
            {
                { Prompts.Parameters.Task, this.Task.Input },
            };
        ChatMessageContent response = await this.GetResponseAsync(this.Chat, Prompts.AnswerTemplate, arguments).ConfigureAwait(false);
        response.AuthorName = "Answer";
        await this.PublishMessageAsync(response.ToResult(), new(Topics.UserProxyTopic)).ConfigureAwait(false); // %%% TOPIC: IS PROPER ???
    }

    private async Task<ChatMessageContent> GetMessageAsync(IPromptTemplate template, KernelArguments arguments)
    {
        string input = await template.RenderAsync(this._kernel, arguments).ConfigureAwait(false);
        return new ChatMessageContent(AuthorRole.User, input);
    }

    private async Task<ChatMessageContent> GetResponseAsync(
        ChatHistory internalChat,
        IPromptTemplate template,
        KernelArguments arguments,
        PromptExecutionSettings? executionSettings = null)
    {
        ChatMessageContent message = await this.GetMessageAsync(template, arguments).ConfigureAwait(false);
        internalChat.Add(message);
        IChatCompletionService chatService = this._kernel.GetRequiredService<IChatCompletionService>(AgentServices.ReasoningService);
        ChatMessageContent response = await chatService.GetChatMessageContentAsync(internalChat, executionSettings).ConfigureAwait(false);
        internalChat.Add(response);
        return response;
    }
}
