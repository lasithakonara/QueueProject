namespace SupportReqeustAcceptor.RabbitMQ
{
    public interface IMessagePublisher
    {
        void SendMessage<T>(T message);
        uint GetMessageCount(string queueName);
    }
}
