using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using TBIGDocumentGenerator.Application.Interfaces.Handlers;

namespace DataIntegration.Application.Services.Modules.MessageBroker
{
	public class RabbitMqConsumer : BackgroundService
	{
		private readonly IConfiguration _config;
		private readonly IDictionary<string, IMessageHandler> _handlers;

		public RabbitMqConsumer(IConfiguration config, IDictionary<string, IMessageHandler> handlers)
		{
			_config = config;
			_handlers = handlers;
		}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"],
                UserName = _config["RabbitMQ:Username"],
                Password = _config["RabbitMQ:Password"]
            };

            var connection = await factory.CreateConnectionAsync(stoppingToken);
            var channel = await connection.CreateChannelAsync();

            var queues = _config.GetSection("RabbitMQ:ConsumerQueues").Get<string[]>();

            foreach (var queue in queues)
            {
                await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    try
                    {
                        if (_handlers.TryGetValue(queue, out var handler))
                        {
                            await handler.HandleAsync(message);

                            // ✅ Acknowledge manually after successful processing
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            Console.WriteLine($"No handler for queue: {queue}");
                            // ❌ Optionally reject message if no handler
                            await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                        // ❌ Reject and requeue the message for retry
                        await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                // ❗ Set autoAck to false for manual control
                await channel.BasicConsumeAsync(queue, autoAck: false, consumer);
            }
        }
    }
}
