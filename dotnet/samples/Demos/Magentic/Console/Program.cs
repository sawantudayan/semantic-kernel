// Copyright (c) Microsoft. All rights reserved.

using Magentic.Agents;
using Magentic.Console.Services;
using Magentic.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace Magentic.Console;

internal static class Program
{
    public static async Task<int> Main()
    {
        ConsoleServices console = new();

        console.DisplayProgress("Initializing...");

        IConfigurationRoot configuration = ConfigurationServices.ReadSettings();
        Kernel kernel = KernelServices.CreateKernel(configuration, useOpenAI: true);
        InProcessRuntime runtime = new();

        AgentTeam team = await runtime.RegisterMagenticTeamAsync(kernel, console).ConfigureAwait(false);
        await runtime.RegisterOrchestratorAsync(kernel, team).ConfigureAwait(false);

        console.DisplayProgress("Ready!");

        string? input = console.ReadInput("Task");
        if (string.IsNullOrWhiteSpace(input))
        {
            return 0;
        }

        console.DisplayProgress("Initiating task...");

        await runtime.StartAsync().ConfigureAwait(false);

        AgentId orchestratorId = await runtime.GetAgentAsync(OrchestratorAgent.TypeId).ConfigureAwait(false);
        Messages.Task message = new() { Input = input };
        await runtime.SendMessageAsync(message, orchestratorId).ConfigureAwait(false);
        await runtime.RunUntilIdleAsync().ConfigureAwait(false);

        console.DisplayProgress("Completed task!");
        console.DisplayTotalUsage();

        return 0;
    }
}
