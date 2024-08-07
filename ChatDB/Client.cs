using System.Net.Sockets;
using System.Net;
using System.Text;
using ChatDB.Abstraction;

namespace ChatDB
{
    public class Client
    {
        private readonly IPEndPoint _udpServerEndPoint;
        private readonly IMessageSource _messageSource;
        private readonly string _clientName;

        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct;

        public Client(IMessageSource messageSource, IPEndPoint serverEndPoint, string name)
        {
            _messageSource = messageSource;            
            _clientName = name;            
            _udpServerEndPoint = serverEndPoint;
        }

        private async Task UdpClientRecieverAsync()
        {
            while (ct.IsCancellationRequested != true)
            {
                try
                {
                    var receivedMessage = await _messageSource.ReceiveMessageAsync();                    

                    if (receivedMessage.Command == Command.RequestUserName)
                    {
                        var newMessage = receivedMessage.Clone() as Message;
                        newMessage.MessageText = _clientName; 
                        await _messageSource.SendMessageAsync(newMessage, _udpServerEndPoint);                        
                    }
                    else                    
                    {                      

                        if (receivedMessage?.MessageText == GlobalVariables.SERVER_SHUTDOWN_MESSAGE)
                        {
                            Console.WriteLine(receivedMessage);
                            cts.Cancel();
                        }
                        else if (receivedMessage?.Command == Command.Confirmation) continue;
                        else
                        {
                            Console.WriteLine(receivedMessage);                            

                            var confirmationMessage = receivedMessage?.Clone() as Message;
                            if (confirmationMessage != null)
                            {
                                confirmationMessage.Command = Command.Confirmation;
                                await _messageSource.SendMessageAsync(confirmationMessage, _udpServerEndPoint);
                            }

                            Console.WriteLine(GlobalVariables.CLIENT_INPUT_MESSAGE);
                        }
                    }                                        
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private async Task UserRegistration()
        {
            var registrationMessage = new Message { Command = Command.Register, SenderName = _clientName, RecipientName = GlobalVariables.SERVER_NAME};
            await _messageSource.SendMessageAsync(registrationMessage, _udpServerEndPoint);
        }        

        public async Task UdpSenderAsync()
        {
            Message newMessage;       
            ct = cts.Token;

            // Запрос типа сериализации у сервера и ждем ответа                       
            var exchangeTypeMessage = new Message { Command = Command.GetExchangeType, SenderName = _clientName, RecipientName = GlobalVariables.SERVER_NAME };
            await _messageSource.SendMessageExchangeTypeAsync(exchangeTypeMessage, _udpServerEndPoint);
            await _messageSource.ReceiveMessageExchangeTypeAsync();                    

            while (!GlobalVariables.IsExchangeFormatSync) { }
            Console.WriteLine($"{GlobalVariables.SERIALIZATION_FORMAT_MESSAGE}: {GlobalVariables.SerializingFormat}");

            // Запускаем локальный получатель сообщений клиента
            new Task(async () => { await UdpClientRecieverAsync(); }).Start();            

            // Регестрируем пользователя на сервере
            await UserRegistration();

            // Запускаем цикл отправки сообщений от клиента
            while (ct.IsCancellationRequested != true)
            {
                Console.WriteLine(GlobalVariables.CLIENT_INPUT_MESSAGE);
                string? messageText = Console.ReadLine();

                if (messageText?.ToLower() == GlobalVariables.CLIENT_EXIT_COMMAND)
                {
                    cts.Cancel();
                    Message exitMessage = new Message(_clientName, messageText);                    
                    await _messageSource.SendMessageAsync(exitMessage, _udpServerEndPoint);
                }
                else if (messageText == null) continue;
                else
                {
                    if (messageText.ToLower().Contains(GlobalVariables.USER_LIST_COMMAND.ToLower()))
                    {
                        newMessage = new Message(_clientName, messageText, Command.List);
                        newMessage.RecipientName = GlobalVariables.SERVER_NAME;
                    }
                    else
                    {
                        newMessage = new Message(_clientName, messageText);
                    }
                    await _messageSource.SendMessageAsync(newMessage, _udpServerEndPoint);
                }                                
            }
        }
    }
}
