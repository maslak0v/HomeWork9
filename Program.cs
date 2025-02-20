using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotApp
{
    public record CatFactDto(string Fact, int Length);

    class Program
    {
        private static readonly CancellationTokenSource _cts = new();

        static async Task Main(string[] args)
        {
            var botToken = "*******"; // ТУТ НАШ ТОКЕН ТГ БОТА
            var botClient = new TelegramBotClient(botToken);

            try
            {
                Console.WriteLine("Бот запущен. Нажмите клавишу A для выхода.");

                // Получаем информацию о боте
                var me = await botClient.GetMeAsync();
                Console.WriteLine($"{me.FirstName} (@{me.Username}) запущен!");

                // цикл обработки обновлений
                await ProcessUpdatesAsync(botClient);
            }
            finally
            {
                _cts.Cancel();
                Console.WriteLine("Приложение завершается...");
            }

            if (_cts.IsCancellationRequested)
            {
                Console.WriteLine("Отмена операций завершена.");
                return;
            }

            var botInfo = await botClient.GetMeAsync();
            Console.WriteLine($"Информация о боте: {botInfo.Username} ({botInfo.FirstName})");
        }

        private static async Task ProcessUpdatesAsync(ITelegramBotClient botClient)
        {
            var offset = 0; // смещение для получения новых обновлений

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var updates = await botClient.GetUpdatesAsync(offset, timeout: 10, cancellationToken: _cts.Token);

                    foreach (var update in updates)
                    {
                        offset = update.Id + 1; // обновляем смещение

                        if (update.Type == UpdateType.Message && update.Message != null)
                        {
                            await HandleUpdateAsync(botClient, update, _cts.Token);
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

                await Task.Delay(1000, _cts.Token); // ожидание 1 секунды перед следующим запросом
            }
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;

            Console.WriteLine($"Получено сообщение: {message.Text}");

            if (message.Text == "/cat")
            {
                try
                {
                    var catFact = await GetRandomCatFact(cancellationToken);
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Факт о кошках: {catFact.Fact}",
                        cancellationToken: cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    // сообщение об ошибке для пользователя
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Не удалось получить факт о кошках. Пожалуйста, попробуйте еще раз позже.",
                        cancellationToken: cancellationToken
                    );

                    // ошибка в консоль
                    Console.WriteLine($"Ошибка при обработке команды /cat: {ex.Message}");
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Сообщение успешно принято",
                    cancellationToken: cancellationToken
                );
            }
        }

        private static async Task<CatFactDto> GetRandomCatFact(CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10); // установка таймаута для HTTP-запроса

            try
            {
                var response = await client.GetAsync("https://catfact.ninja/fact", cancellationToken);
                response.EnsureSuccessStatusCode(); // проверка успешно выполненного запроса

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                // проверка на то, что содержимое не пустое
                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new Exception("Получен пустой ответ от API.");
                }

                // десериализация ответа
                var catFact = JsonSerializer.Deserialize<JsonElement>(content);

                // проверка наличия ключа "fact" и его значения
                if (!catFact.TryGetProperty("fact", out var factProperty) || string.IsNullOrWhiteSpace(factProperty.GetString()))
                {
                    throw new Exception("Ключ 'fact' отсутствует или содержит пустое значение в ответе API.");
                }

                
                if (!catFact.TryGetProperty("length", out var lengthProperty) || lengthProperty.ValueKind != JsonValueKind.Number)
                {
                    throw new Exception("Ключ 'length' отсутствует или имеет недопустимое значение в ответе API.");
                }

                // Создаем объект CatFactDto, передавая оба параметра
                return new CatFactDto(
                    Fact: factProperty.GetString(),
                    Length: lengthProperty.GetInt32()
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения факта о кошках: {ex.Message}");
                throw; 
            }
        }
    }
}