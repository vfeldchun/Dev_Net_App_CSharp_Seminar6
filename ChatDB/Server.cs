using System.Net;
using ChatDB.Models;
using ChatDB.Abstraction;

namespace ChatDB
{
    public class Server
    {
        private IMessageSource _messageSource;

        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct;
        private Dictionary<string, IPEndPoint> _userDict = new Dictionary<string, IPEndPoint>();

        public Server(IMessageSource messageSource, IPEndPoint serverEndPoint)
        {
            _messageSource = messageSource;                 
        }

        async Task Register(Message message, IPEndPoint fromep)
        {
            Console.WriteLine("Message Register, name = " + message.SenderName);

            if (!_userDict.ContainsKey(message.SenderName))
            {
                _userDict.Add(message.SenderName, fromep);

                using (var ctx = new ChatDbContext())
                {
                    if (ctx.Users.FirstOrDefault(x => x.Name == message.SenderName) != null) 
                    {
                        await _messageSource.SendMessageAsync(new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} зарегестрирован!", message.SenderName), fromep);                        
                        return;
                    }                    

                    ctx.Add(new User { Name = message.SenderName });
                    ctx.SaveChanges();
                }                
            }
            else
            {
                _userDict[message.SenderName] = fromep;                
            }

            await _messageSource.SendMessageAsync(new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} зарегестрирован!", message.SenderName), fromep);
        }

        async Task Unregister(Message message)
        {
            IPEndPoint fromep = _userDict[message.SenderName];

            Console.WriteLine("Message Unegister, name = " + message.SenderName);

            if (_userDict.ContainsKey(message.SenderName))
            {
                _userDict.Remove(message.SenderName);

                using (var ctx = new ChatDbContext())
                {
                    if (ctx.Users.FirstOrDefault(x => x.Name == message.SenderName) == null)
                    {
                        await _messageSource.SendMessageAsync(new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} не был зарегестрирован и не может быть удален!", message.SenderName), fromep);                        
                        return;
                    }                    

                    ctx.Users.Remove(ctx.Users.FirstOrDefault(x => x.Name == message.SenderName));
                    ctx.SaveChanges();
                }

                await _messageSource.SendMessageAsync(new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} удален!", message.SenderName), fromep);
            }
            else
            {
                await _messageSource.SendMessageAsync(new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} не был зарегестрирован и не может быть удален!", message.SenderName), fromep);                
            }
        }

        void ConfirmMessageReceived(int? id)
        {
            Console.WriteLine("Message confirmation id=" + id);

            using (var ctx = new ChatDbContext())
            {
                var msg = ctx.Messages.FirstOrDefault(x => x.Id == id);

                if (msg != null)
                {
                    msg.Received = true;
                    ctx.SaveChanges();
                }
            }
        }

        async Task RelyMessage(Message message)
        {
            int? id = null;           

            if (_userDict.TryGetValue(message.RecipientName!, out IPEndPoint ep))
            {
                using (var ctx = new ChatDbContext())
                {
                    var fromUser = ctx.Users.First(x => x.Name == message.SenderName);
                    var toUser = ctx.Users.First(x => x.Name == message.RecipientName);

                    var msg = new Models.Message { FromUser = fromUser, ToUser = toUser, Received = false, Text = message.MessageText! };
                    ctx.Messages.Add(msg);

                    ctx.SaveChanges();

                    id = msg.Id;
                }

                var forwardMessage = message.Clone() as Message;
                forwardMessage.Id = id;
                await _messageSource.SendMessageAsync(forwardMessage, ep);
                
                Console.WriteLine($"Message Relied, from = {message.SenderName} to = {message.RecipientName}");
            }
            else
            {
                Console.WriteLine("Пользователь не найден.");
            }
        }

        async Task ForwardAllNotReceivedMessagesToUser(string userName, IPEndPoint ep)
        {
            using (var ctx = new ChatDbContext())
            {
                int userId = ctx.Users.Where(r => r.Name == userName).Select(x => x.Id).SingleOrDefault();
                var notReceivedMessages = ctx.Messages.Where(r => r.Received == false && r.ToUserId == userId).Select(x => x).ToList();

                if (notReceivedMessages.Count > 0)
                {
                    await _messageSource.SendMessageAsync(new Message(GlobalVariables.SERVER_NAME, $"У вас есть не полученные сообщения"), ep);                    
                    
                    foreach (var msg in notReceivedMessages)
                    {                        
                        var fromUser = ctx.Users.Where(r => r.Id == msg.FromUserId).Select(x => x.Name).SingleOrDefault();
                        var newMessage = new Message() { Id = msg.Id, Command = Command.Message, MessageText = msg.Text, SenderName = fromUser, RecipientName = userName };
                        newMessage.MessageTime = DateTime.Now;
                        await _messageSource.SendMessageAsync(newMessage, ep);
                    }
                }
            }
        }

        async Task ListUsers(Message message)
        {
            IPEndPoint ep = _userDict[message.SenderName];

            string userList = "[ ";
            foreach (var key in _userDict.Keys)
                userList += key + ", ";
            userList += "]";

            await _messageSource.SendMessageAsync(new Message(GlobalVariables.SERVER_NAME, $"Список зарегестрированных пользователей\n{userList}", message.SenderName), ep);            
        }

        async Task ProcessMessage(Message message, IPEndPoint fromep)
        {
            Console.WriteLine($"Получено сообщение от {message.SenderName} для {message.RecipientName} с командой {message.Command}:");
            
            if (message.Command == Command.GetExchangeType)
            {
                var cloneMessage = message.Clone() as Message;
                cloneMessage.MessageText = GlobalVariables.SerializingFormat;
                cloneMessage.RecipientName = message.SenderName;
                cloneMessage.SenderName = message.RecipientName;

            }

                if (message.Command == Command.Register)
                await Register(message, new IPEndPoint(fromep.Address, fromep.Port));

            if (message.Command == Command.Unregister)
                await Unregister(message);

            if (message.Command == Command.List)
                await ListUsers(message);

            if (message.Command == Command.Confirmation && message.SenderName != GlobalVariables.SERVER_NAME)
            {
                Console.WriteLine("Confirmation received");
                ConfirmMessageReceived(message.Id);
            }

            if (message.Command == Command.Message)
                await RelyMessage(message);

        }
        private void LoadUsersFromDb()
        {
            using (var ctx = new ChatDbContext())
            {
                var userList = ctx.Users.Select(x => x.Name).ToList();

                foreach (var user in userList)
                {
                    _userDict.Add(user, null);
                }
            }
        }        

        public async Task UdpRecieverAsync()
        {    
            LoadUsersFromDb();

            Console.WriteLine(GlobalVariables.SERVER_START_MESSAGE);

            ct = cts.Token;

            new Task(() =>
            {
                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)                           
                        break;
                }

                // Отправка сообщения о завершении работы в консоль сервера                         
                Message escapeMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.SERVER_ESC_MESSAGE);
                Console.WriteLine("x" + escapeMessage);
                Environment.Exit(0);                
            }).Start();               

            while (ct.IsCancellationRequested != true)
            {
                try
                {            
                    var message = await _messageSource.ReceiveMessageAsync();

                    // Обрабатываем формат запроса формата обмена сообщениями от клиента
                    if (message.Command == Command.GetExchangeType)
                    {
                        var newMessage = message.Clone() as Message;
                        newMessage.MessageText = GlobalVariables.SerializingFormat;
                        newMessage.SenderName = GlobalVariables.SERVER_NAME;
                        newMessage.RecipientName = message.SenderName;

                        await _messageSource.SendMessageExchangeTypeAsync(newMessage, _messageSource.ReceiveEndPoint);

                        // Запрашиваем имя пользователя у клиента для последующей отправки ему всех не прочитанных сообщений
                        await _messageSource.SendMessageAsync(new Message { Command = Command.RequestUserName, SenderName = GlobalVariables.SERVER_NAME, RecipientName = message.SenderName }, _messageSource.ReceiveEndPoint);

                    }
                    // Если есть не полученные сообщения у коиента то отправляем их все клиенту - ДЗ Семинар 5
                    else if (message.Command == Command.RequestUserName)
                    {                        
                        if (message?.MessageText != null)
                        {
                            var userName = message.MessageText.Trim();

                            // Если имя есть в списке зарегестрированных полтзователей то оправляем сообщения - ДЗ Семинар 5
                            if (_userDict.ContainsKey(userName))
                            {
                                await ForwardAllNotReceivedMessagesToUser(userName, _messageSource.ReceiveEndPoint);
                            }
                        }
                    }
                    else
                    {
                        await Task.Run(async () =>
                        {
                            if (message?.MessageText?.ToLower() == GlobalVariables.USER_SERVER_SHUTDOWN_COMMAND)
                            {
                                cts.Cancel();

                                // Отправка подтверждения получения сообщения завершения работы сервера
                                Message acceptMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.SERVER_SHUTDOWN_MESSAGE);
                                await _messageSource.SendMessageAsync(acceptMessage, _messageSource.ReceiveEndPoint);
                                Console.WriteLine(acceptMessage);
                                Thread.Sleep(500);
                            }
                            else
                            {
                                if (message != null)
                                {  
                                    await ProcessMessage(message, _messageSource.ReceiveEndPoint);

                                    Console.WriteLine(message);

                                    // Отправка подтверждения получения сообщения
                                    Message acceptMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.CONFIRMATION_MESSAGE, Command.Confirmation);
                                    acceptMessage.RecipientName = message.SenderName;
                                    await _messageSource.SendMessageAsync(acceptMessage, _messageSource.ReceiveEndPoint);
                                }
                                else
                                    Console.WriteLine(GlobalVariables.SERVER_COMMON_ERROR_MESSAGE);
                            }
                        });
                    }                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


            }
        }
    }
}
