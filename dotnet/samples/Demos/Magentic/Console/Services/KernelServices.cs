// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;
using Azure.Identity;
using Magentic.Agents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using OpenAI;
using OpenAI.Assistants;

namespace Magentic.Console.Services;

internal static class KernelServices
{
    public static Kernel CreateKernel(IConfigurationRoot configuration, bool useOpenAI)
    {
        Settings.ChatCompletionSettings chatSettings = configuration.GetChatCompletionSettings();
        Settings.TextToImageSettings imageSettings = configuration.GetTextToImageSettings();
        Settings.OpenAISettings openAISettings = configuration.GetOpenAISettings();

        IKernelBuilder builder = Kernel.CreateBuilder();

        if (useOpenAI)
        {
            builder.AddOpenAIChatCompletion(
                openAISettings.ChatModel,
                openAISettings.ApiKey,
                serviceId: AgentServices.ChatService);

            builder.AddOpenAIChatCompletion(
                openAISettings.ReasoningModel,
                openAISettings.ApiKey,
                serviceId: AgentServices.ReasoningService);

            builder.AddOpenAIChatCompletion(
                openAISettings.SearchModel,
                openAISettings.ApiKey,
                serviceId: AgentServices.SearchService);

            builder.Services.AddSingleton(openAISettings.ChatModel);
            OpenAIClient client = new(openAISettings.ApiKey);
            builder.Services.AddSingleton<AssistantClient>(client.GetAssistantClient());
        }
        else
        {
            builder.AddAzureOpenAIChatCompletion(
                chatSettings.DeploymentName,
                chatSettings.Endpoint,
                new AzureCliCredential(),
                serviceId: AgentServices.ChatService);

            builder.Services.AddSingleton(chatSettings.DeploymentName);
            AzureOpenAIClient client = new(new Uri(chatSettings.Endpoint), new AzureCliCredential());
            builder.Services.AddSingleton<AssistantClient>(client.GetAssistantClient());
        }

        // Always use the azure service for image creation
        builder.AddAzureOpenAITextToImage(
            imageSettings.DeploymentName,
            imageSettings.Endpoint,
            new AzureCliCredential());

        // Create a new kernel instance
        return builder.Build();
    }
}
