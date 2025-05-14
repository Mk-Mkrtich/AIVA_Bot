using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DotNetEnv;

class Program
{
 static string telegramToken;
    static string geminiApiKey;
    static TelegramBotClient botClient;

    static async Task Main()
    {
        Env.Load();

        telegramToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
        geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        botClient = new TelegramBotClient(telegramToken);

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Bot @{me.Username} is running... Press any key to stop.");
        Console.ReadKey();

        cts.Cancel();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        if (update.Message is { Text: var messageText })
        {
            long chatId = update.Message.Chat.Id;

            if (messageText.StartsWith("/start"))
            {
                await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Բարև ձեզ! Իմ անունը Ավիա է, ես ձեր առցանց օգնականն եմ ամեն ինչում, մենք կարող ենք պարզապես խոսել կամ լուծել կարևոր հարցեր։",
                    cancellationToken: token
                );
                messageText = "You are a AVIA bot. You are a friendly, helpful assistant. you speak in Armenian.";
                await GetGeminiResponse(messageText);
                return;
            }else {
                string answer = await GetGeminiResponse(messageText);

                await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: answer ?? "Չհաջողվեց ստանալ պատասխան:",
                    cancellationToken: token
                );
            }
        }
    }

    static async Task<string> GetGeminiResponse(string message)
    {
        using var httpClient = new HttpClient();

        var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={geminiApiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = message }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(apiUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return $"Error API: {response.StatusCode}\n{responseBody}";
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var resultText = jsonDoc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return resultText ?? "Gemini is retun empty answer.";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}\n Answer from API:\n{responseBody}";
        }
    }

    static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}
