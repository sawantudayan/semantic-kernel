// Copyright (c) Microsoft. All rights reserved.
namespace Step04b;

internal static class AgentDefinitions
{
    public static class Manager
    {
        public const string Name = "";
        public const string Description = "";
        public const string Instructions =
            $"""
            You are coordinating a team of agents to plan a trip based on user input.
            Determine which agent should respond next and provide clear, concise, and complete instructions for each agent.

            At a minimum, ensure each agent is involved in the conversation at least once.

            When the plan is complete, specify a agent name of: "{NoAgent.Name}".

            The available agents are:
            - {Agent1.Name}: {Agent1.Description}
            - {Agent2.Name}: {Agent2.Description}        
            - {Agent3.Name}: {Agent3.Description}
            """;
        public const string Summary =
            """
            Summarize all of the responses to the user input in markdown format without acknowledging this instruction.
            """;
    }

    public static class NoAgent
    {
        public const string Name = "none";
    }

    public static class Agent1
    {
        public const string Name = "Agent1";
        public const string Description = "Create a site seeing itinerary";
    }

    public static class Agent2
    {
        public const string Name = "Agent2";
        public const string Description = "Suggest hotels for the trip";
    }

    public static class Agent3
    {
        public const string Name = "Agent3";
        public const string Description = "Checks the weather forecast";
    }

    public static class ResearchAgent
    {
        public const string Name = "Researcher";
        //public const string Description = "Provides research based on the user input";
        public const string Instructions = "Respond to the user direction";
    }

    public static class ReviewerAgent
    {
        public const string Name = "Reviewer";
        //public const string Description = "Reviews the research";
        public const string Instructions = "Determine if the response satisfies the user request.  If not, provide clear direction on what is to be addressed.  Note: The response need not be perfect.";
    }
}
