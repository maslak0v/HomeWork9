using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

class Program
{
    private static readonly CancellationTokenSource _cts = new();
    private static BotHandler botHandler; 

    static async Task Main(string[] args)
    {
        var botToken = "<***>"; //НАШ ТГ ТОКЕН
        var botClient = new TelegramBotClient(botToken);

        try
        {
            Console.WriteLine("Бот запущен. Нажмите клавишу A для выхода.");

            // Создаем экземпляры сервисов
            var catFactService = new CatFactService();
            botHandler = new BotHandler(botClient, catFactService); // Инициализация botHandler

            // Подписываемся на события
            botHandler.OnMessageProcessingStarted += (sender, e) => Console.WriteLine($"Началась обработка сообщения: {e.Message}");
            botHandler.OnMessageProcessingCompleted += (sender, e) => Console.WriteLine($"Завершена обработка сообщения: {e.Message}");

            // Получаем информацию о боте
            var me = await botClient.GetMeAsync();
            Console.WriteLine($"{me.FirstName} (@{me.Username}) запущен!");

            // Запускаем цикл обработки обновлений
            await ProcessUpdatesAsync(botClient, botHandler);
        }
        finally
        {
            _cts.Cancel();
            Console.WriteLine("Приложение завершается...");

            // Отписываемся от событий
            if (botHandler != null)
            {
                botHandler.OnMessageProcessingStarted -= (sender, e) => Console.WriteLine($"Началась обработка сообщения: {e.Message}");
                botHandler.OnMessageProcessingCompleted -= (sender, e) => Console.WriteLine($"Завершена обработка сообщения: {e.Message}");
            }
        }

        if (_cts.IsCancellationRequested)
        {
            Console.WriteLine("Отмена операций завершена.");
            return;
        }

        var botInfo = await botClient.GetMeAsync();
        Console.WriteLine($"Информация о боте: {botInfo.Username} ({botInfo.FirstName})");
    }

    private static async Task ProcessUpdatesAsync(ITelegramBotClient botClient, BotHandler botHandler)
    {
        var offset = 0; 

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var updates = await botClient.GetUpdatesAsync(offset, timeout: 10, cancellationToken: _cts.Token);

                foreach (var update in updates)
                {
                    offset = update.Id + 1;

                    if (update.Type == UpdateType.Message && update.Message != null)
                    {
                        await botHandler.HandleUpdateAsync(update, _cts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Операция отменена.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении обновлений: {ex.Message}");
            }

            await Task.Delay(1000, _cts.Token); 
        }
    }
}