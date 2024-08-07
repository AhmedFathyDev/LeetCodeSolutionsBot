using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

const string botToken = "6566902039:AAE79sRie1JhP_Zep6wbFFanVHS06Mn8EO4";
const string telegramApiUrl = "https://api.telegram.org";

await GetMe();

var botClient = new TelegramBotClient(botToken);

using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = [] // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

return;

async static Task GetMe()
{
    using var client = new HttpClient();
    using var request = new HttpRequestMessage(HttpMethod.Get, $"{telegramApiUrl}/bot{botToken}/{nameof(GetMe)}");

    using var response = await client.SendAsync(request);
    
    Console.WriteLine(await response.Content.ReadAsStringAsync());
}

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;
    
    Console.WriteLine($"Received a \"{messageText}\" message in chat {message.Chat.Id}.");

    // Echo received message text
    await botClient.SendTextMessageAsync(
        chatId: message.Chat.Id,
        parseMode: ParseMode.MarkdownV2,
        disableWebPagePreview: true,
        text: "```cpp " +
              messageText +
              "```",
        cancellationToken: cancellationToken);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var errorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
}