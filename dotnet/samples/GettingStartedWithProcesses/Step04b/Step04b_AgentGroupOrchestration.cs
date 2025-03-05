// Copyright (c) Microsoft. All rights reserved.

using Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SharedSteps;
using Step04b.Steps;

namespace Step04b;

/// <summary>
/// Demonstrate creation of a <see cref="KernelProcess"/> that invokes an "InnerChat" step.
/// </summary>
public class Step04b_AgentGroupOrchestration(ITestOutputHelper output) : BaseTest(output, redirectSystemConsoleOutput: true)
{
    /// <summary>
    /// Demonstrate the "inner-chat" step.
    /// </summary>
    [Fact]
    public async Task RunInnerChatAsync()
    {
        // Define process
        KernelProcess process = SetupAgentProcess<BasicAgentChatUserInput>(nameof(RunInnerChatAsync));

        // Execute process
        await RunProcessAsync(process);
    }

    private sealed class BasicAgentChatUserInput : ScriptedUserInputStep
    {
        public BasicAgentChatUserInput()
        {
            this.SuppressOutput = true;
        }

        public override void PopulateUserInputs(UserInputState state)
        {
            state.UserInputs.Add("Plan a trip to go to the Eiffel tower.  I'd like to spend at least 3 nights in Paris and see other sites also.");
        }
    }

    private async Task RunProcessAsync(KernelProcess process)
    {
        // Initialize services as part of the kernel.
        // This includes the agents and the chat history provider.
        Kernel kernel = SetupKernel();

        string sessionId = Guid.NewGuid().ToString("N");
        kernel.Data[IChatHistoryProvider.SessionId] = sessionId;

        // Execute process
        using LocalKernelProcessContext localProcess =
            await process.StartAsync(
                kernel,
                new KernelProcessEvent()
                {
                    Id = AgentGroupOrchestrationEvents.StartProcess,
                });

        // Demonstrate history is maintained independent of process state
        //this.WriteHorizontalRule();
        //ChatHistory history = await kernel.GetRequiredService<IChatHistoryProvider>().GetHistoryAsync(sessionId);
        //foreach (ChatMessageContent message in history)
        //{
        //    RenderMessageStep.Render(message);
        //}
    }

    private KernelProcess SetupAgentProcess<TUserInputStep>(string processName) where TUserInputStep : ScriptedUserInputStep
    {
        ProcessBuilder process = new(processName);

        ProcessStepBuilder userInputStep = process.AddStepFromType<TUserInputStep>();
        ProcessStepBuilder innerChatStep = process.AddStepFromType<InnerChatStep>();
        ProcessStepBuilder renderMessageStep = process.AddStepFromType<RenderMessageStep>();

        // Entry point
        process.OnInputEvent(AgentGroupOrchestrationEvents.StartProcess)
            .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep));

        process
            .OnError()
            .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderError, "error"));

        userInputStep
            .OnEvent(CommonEvents.UserInputReceived)
            .SendEventTo(new ProcessFunctionTargetBuilder(innerChatStep));

        innerChatStep
            .OnEvent(AgentGroupOrchestrationEvents.ManagerMessage)
            .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderMessage, "message"));

        innerChatStep
            .OnEvent(AgentGroupOrchestrationEvents.InnerMessage)
            .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderInnerMessage, "message"));

        innerChatStep
            .OnEvent(AgentGroupOrchestrationEvents.ChatCompleted)
            .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderResult, "message"));

        KernelProcess kernelProcess = process.Build();

        return kernelProcess;
    }

    private Kernel SetupKernel()
    {
        IKernelBuilder builder = Kernel.CreateBuilder();

        // Add Chat Completion to Kernel
        this.AddChatCompletionToKernel(builder);

        // Capture simple kernel for agent initialization
        Kernel agentKernel = builder.Build();

        // Inject agents into service collection
        SetupAgents(builder, agentKernel);

        // Inject history provider into service collection
        builder.Services.AddSingleton<IChatHistoryProvider>(new ChatHistoryProvider());

        builder.Services.AddKeyedSingleton(InnerChatStep.ServiceKeys.Summarizer, SetupReducer(agentKernel, AgentDefinitions.Manager.Summary));

        // NOTE: Uncomment to see process logging
        //builder.Services.AddSingleton<ILoggerFactory>(this.LoggerFactory);

        return builder.Build();
    }

    private static ChatHistorySummarizationReducer SetupReducer(Kernel kernel, string instructions) =>
         new(kernel.GetRequiredService<IChatCompletionService>(), 1)
         {
             SummarizationInstructions = instructions
         };

    private static void SetupAgents(IKernelBuilder builder, Kernel agentKernel)
    {
        // Create manager agent and inject into service collection
        ChatCompletionAgent managerAgent = CreateAgent(AgentDefinitions.Manager.Name, AgentDefinitions.Manager.Instructions, agentKernel.Clone(), ManagerResponse.Schema);
        builder.Services.AddKeyedSingleton(InnerChatStep.ServiceKeys.ManagerAgent, managerAgent);

        // Create agent group #1 and inject into service collection
        builder.Services.AddKeyedSingleton(InnerChatStep.ServiceKeys.AgentGroup1, SetupAgentGroup(agentKernel));
        // Create agent group #2 and inject into service collection
        builder.Services.AddKeyedSingleton(InnerChatStep.ServiceKeys.AgentGroup2, SetupAgentGroup(agentKernel));
        // Create agent group #3 and inject into service collection
        builder.Services.AddKeyedSingleton(InnerChatStep.ServiceKeys.AgentGroup3, SetupAgentGroup(agentKernel));
    }

    private static AgentGroupChat SetupAgentGroup(Kernel kernel)
    {
        ChatCompletionAgent researchAgent = CreateAgent(AgentDefinitions.ResearchAgent.Name, AgentDefinitions.ResearchAgent.Instructions, kernel.Clone());

        ChatCompletionAgent reviewerAgent = CreateAgent(AgentDefinitions.ReviewerAgent.Name, AgentDefinitions.ReviewerAgent.Instructions, kernel.Clone());

        KernelFunction selectionFunction =
            AgentGroupChat.CreatePromptFunctionForStrategy(
                $$$"""
                Determine which participant takes the next turn in a conversation based on the the most recent participant.
                State only the name of the participant to take the next turn.
                No participant should take more than one turn in a row.
                
                Choose only from these participants:
                - {{{researchAgent.Name}}}
                - {{{reviewerAgent.Name}}}
                
                Always follow these rules when selecting the next participant:
                - After user input, it is {{{researchAgent.Name}}}'s turn.
                - After {{{researchAgent.Name}}}, it is {{{reviewerAgent.Name}}}'s turn.
                - After {{{reviewerAgent.Name}}}, it is {{{researchAgent.Name}}}'s turn.

                History:
                {{$history}}
                """,
                safeParameterNames: "history");

        KernelFunction terminationFunction =
            AgentGroupChat.CreatePromptFunctionForStrategy(
                """
                When the reviewer is satisfied with the research agent's response, respond with a single word: yes

                History:
                {{$history}}
                """,
                safeParameterNames: "history");

        AgentGroupChat group =
            new(researchAgent, reviewerAgent)
            {
                // NOTE: Replace logger when using outside of sample.
                // Use `this.LoggerFactory` to observe logging output as part of sample.
                LoggerFactory = NullLoggerFactory.Instance,
                ExecutionSettings = new()
                {
                    SelectionStrategy =
                        new KernelFunctionSelectionStrategy(selectionFunction, kernel)
                        {
                            HistoryVariableName = "history",
                            HistoryReducer = new ChatHistoryTruncationReducer(1),
                            ResultParser =
                                (result) =>
                                {
                                    ChatMessageContent? message = result.GetValue<ChatMessageContent>();
                                    return message?.Content ?? researchAgent.Name!;
                                },
                        },
                    TerminationStrategy =
                        new KernelFunctionTerminationStrategy(terminationFunction, kernel)
                        {
                            Agents = [reviewerAgent],
                            HistoryVariableName = "history",
                            HistoryReducer = new ChatHistoryTruncationReducer(3),
                            MaximumIterations = 12,
                            ResultParser =
                                (result) =>
                                {
                                    return result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false;
                                }
                        }
                }
            };

        return group;
    }

    private static ChatCompletionAgent CreateAgent(string name, string instructions, Kernel kernel, object? responseFormat = null) =>
        new()
        {
            Name = name,
            Instructions = instructions,
            Kernel = kernel.Clone(),
            Arguments =
                new KernelArguments(
                    new OpenAIPromptExecutionSettings
                    {
                        Temperature = 0,
                        ResponseFormat = responseFormat,
                    }),
        };
}
