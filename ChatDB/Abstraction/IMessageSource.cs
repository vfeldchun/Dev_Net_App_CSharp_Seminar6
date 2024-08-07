
using System.Net;

namespace ChatDB.Abstraction
{
    public interface IMessageSource
    {
        public IPEndPoint ReceiveEndPoint { get;  set; }
        Task SendMessageAsync(Message message, IPEndPoint endPoint);
        Task<Message> ReceiveMessageAsync();
        Task SendMessageExchangeTypeAsync(Message message, IPEndPoint endPoint);
        Task ReceiveMessageExchangeTypeAsync();
    }
}
