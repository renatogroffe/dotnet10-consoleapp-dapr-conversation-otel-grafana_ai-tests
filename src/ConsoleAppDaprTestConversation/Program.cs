using Dapr.AI.Conversation;
using Dapr.AI.Conversation.ConversationRoles;
using Dapr.AI.Conversation.Extensions;
using Grafana.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Text.Json;

const string SERVICE_NAME = "ConsoleAppDaprTestConversation";
const string CONVERSATION_COMPONENT_ID = "grok-llm";
var SERVICE_VERSION = typeof(Program).Assembly.GetName().Version!.ToString();

Console.WriteLine("**** Testes com DaprConversationClient ****");

var builder = Host.CreateApplicationBuilder(args);

#pragma warning disable DAPR_CONVERSATION // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddDaprConversationClient();
#pragma warning restore DAPR_CONVERSATION // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(SERVICE_NAME);
using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource(SERVICE_NAME, SERVICE_VERSION)
    .AddSource("ModelContextProtocol.Client")
    .AddGrpcClientInstrumentation()
    .UseGrafana()
    .Build();

using var host = builder.Build();

using var activitySource = new ActivitySource(SERVICE_NAME, SERVICE_VERSION);
using var activityTest = activitySource.StartActivity("TestesDaprConversation")!;
activityTest.SetTag("conversation.component.id", CONVERSATION_COMPONENT_ID);

#pragma warning disable DAPR_CONVERSATION // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var conversationClient = host.Services.GetRequiredService<DaprConversationClient>();
#pragma warning restore DAPR_CONVERSATION // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Send a message to the conversation service
var response = await conversationClient.ConverseAsync(
    [
        new ConversationInput(
            new List<IConversationMessage>
            {
                new UserMessage
                {
                    Name = "Test User",
                    Content =
                    [
                        new MessageContent(
                            "Explique em poucas palavras o que é o Dapr.")
                    ]
                }
            }
        )
    ],
    new ConversationOptions(CONVERSATION_COMPONENT_ID)
);

var oldColor = Console.ForegroundColor;
int responseNumber = 0;
Console.WriteLine();
Console.WriteLine("Respostas geradas:");
foreach (var resp in response.Outputs)
{
    Console.WriteLine();
    Console.WriteLine($"* {resp.GetType().FullName}.Outputs[{responseNumber++}]");
    Console.WriteLine($"Model: {resp.Model}");
    Console.WriteLine("Conteudo:");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(JsonSerializer.Serialize(resp, new JsonSerializerOptions() { WriteIndented = true }));
    Console.ForegroundColor = oldColor;
    int choiceNumber = 0;
    foreach (var choice in resp.Choices)
    {
        Console.WriteLine();
        Console.WriteLine($"{resp.GetType().FullName}.Choices[{choiceNumber++}].Message:");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(choice.Message.Content);
        Console.ForegroundColor = oldColor;
    }
}

Console.WriteLine();
Console.WriteLine("**** Fim dos testes ****");
Console.WriteLine();