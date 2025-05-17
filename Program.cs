using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DotNetEnv;
using AVIA_Bot.Provaider;

class Program
{
    static string telegramToken;
    static TelegramBotClient botClient;
    static IAIProvider aiProvider;
    static Dictionary<long, List<(string role, string text)>> chatHistories = new();

    static async Task Main()
    {
        Env.Load();
        telegramToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
        string geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        botClient = new TelegramBotClient(telegramToken);
        aiProvider = new GeminiProvaider(geminiApiKey);

        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } },
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Bot @{me.Username} running with {aiProvider.Name}... Press any key to stop.");
        Console.ReadKey();
        cts.Cancel();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        if (update.Message is { Text: var messageText })
        {
            long chatId = update.Message.Chat.Id;

            if (!chatHistories.ContainsKey(chatId) || messageText.StartsWith("/start"))
            {
                chatHistories[chatId] = new List<(string, string)>();
                AddToHistory(chatId, "model", "this is a system message: You are a bot named Avia (means Artificial intelligence Virtual Accistent). you created by Mkrtich, you are a telegram bot, you will be Friendly, helpful, give short answers, answer in Armenian only, give questions about continue.");

                await bot.SendTextMessageAsync(
                    chatId,
                    "Բարև ձեզ! Իմ անունը Ավիա է, ես ձեր առցանց օգնականն եմ։",
                    cancellationToken: token
                );
                return;
            }

            AddToHistory(chatId, "user", messageText);
            var response = await aiProvider.ResponseAsync(chatHistories[chatId]);
            AddToHistory(chatId, "model", response);

            await bot.SendTextMessageAsync(chatId, response, cancellationToken: token);
        }
    }

    static void AddToHistory(long chatId, string role, string text)
    {
        if (!chatHistories.ContainsKey(chatId))
            chatHistories[chatId] = new List<(string, string)>();

        chatHistories[chatId].Add((role, text));

        if (chatHistories[chatId].Count > 15)
            chatHistories[chatId].RemoveAt(0);
    }

    static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Telegram error: {exception.Message}");
        return Task.CompletedTask;
    }
}
