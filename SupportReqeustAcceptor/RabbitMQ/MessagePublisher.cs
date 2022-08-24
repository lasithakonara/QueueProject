using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace SupportReqeustAcceptor.RabbitMQ
{
    public class MessagePublisher : IMessagePublisher
    {
        public void SendMessage<T>(T message)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            var connection = connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare("sessions", exclusive: false);

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "", routingKey: "sessions", body: body);
        }

        public uint GetMessageCount(string queueName)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            using (IConnection connection = connectionFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.QueueDeclare("sessions", exclusive: false);
                return channel.MessageCount(queueName);
            }
        }
    }
}
