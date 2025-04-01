using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class BotHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly CatFactService _catFactService;

    // События для начала и завершения обработки сообщений
    public event EventHandler<MessageEventArgs> OnMessageProcessingStarted;
    public event EventHandler<MessageEventArgs> OnMessageProcessingCompleted;

    public BotHandler(ITelegramBotClient botClient, CatFactService catFactService)
    {
        _botClient = botClient;
        _catFactService = catFactService;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message == null)
            return;

        var message = update.Message;

        // Вызов события начала обработки сообщения
        OnMessageProcessingStarted?.Invoke(this, new MessageEventArgs(message.Text));

        try
        {
            if (message.Text == "/cat")
            {
                var catFact = await _catFactService.GetRandomCatFactAsync();
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Факт о кошках: {catFact.Fact}", cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Сообщение успешно принято", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка при обработке запроса.", cancellationToken: cancellationToken);
            Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
        }
        finally
        {
            // Вызов события завершения обработки сообщения
            OnMessageProcessingCompleted?.Invoke(this, new MessageEventArgs(message.Text));
        }
    }
}

public class MessageEventArgs : EventArgs
{
    public string Message { get; }

    public MessageEventArgs(string message)
    {
        Message = message;
    }
}