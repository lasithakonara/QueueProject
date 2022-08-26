namespace SupportReqeustAcceptor.RabbitMQ
{
    public interface IMessagePublisher
    {
        /// <summary>
        /// Adds message to the queue to be processed by the consumer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        void SendMessage<T>(T message);

        /// <summary>
        /// Retrieves current message count in the queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns>Message count</returns>
        uint GetMessageCount(string queueName);
    }
}
