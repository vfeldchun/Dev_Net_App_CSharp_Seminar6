
using ChatDB;
using ChatDB.Abstraction;
using System.Net;

namespace ChatDBTests
{
    public class MockClientMessageSource : IMessageSource
    {        
        private Queue<Message> messages = new Queue<Message>();
        private Message message;

        public IPEndPoint ReceiveEndPoint { get; set; }

        public MockClientMessageSource(IPEndPoint endPoint)
        {
            messages.Enqueue(new Message { Command = Command.Register, SenderName = "Vlad", RecipientName = "Server" });
            messages.Enqueue(new Message { Command = Command.GetExchangeType, SenderName = "Vlad", RecipientName = "Server" });
            messages.Enqueue(new Message { Command = Command.Message, SenderName = "Vlad", RecipientName = "Alena", MessageText = "Привет!" });
            ReceiveEndPoint = endPoint;
        }        

        public Task<Message> ReceiveMessageAsync()
        {
            var task = new TaskCompletionSource<Message>();
            message = messages.Dequeue();
            task.SetResult(message);
            return task.Task;
        }

        public Task ReceiveMessageExchangeTypeAsync()
        {         
            return Task.Delay(100);
        }

        public Task SendMessageAsync(Message message, IPEndPoint endPoint)
        {
            messages.Enqueue(message);
            return Task.Delay(100);
        }

        public Task SendMessageExchangeTypeAsync(Message message, IPEndPoint endPoint)
        {            
            return Task.Delay(100);
        }
    }
}
