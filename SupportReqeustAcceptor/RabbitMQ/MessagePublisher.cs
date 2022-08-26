using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace SupportReqeustAcceptor.RabbitMQ
{
    public class MessagePublisher : IMessagePublisher
    {
        /// <summary>
        /// Adds message to the queue to be processed by the consumer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
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

        /// <summary>
        /// Retrieves current message count in the queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns>Message count</returns>
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
