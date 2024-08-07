
using ChatDB.Abstraction;
using System.Net;


namespace ChatDB
{
    internal class Program
    {
        private static void PrintHelp()
        {
            Console.WriteLine("Справка по использованию програмы:");
            Console.WriteLine($"\t{GlobalVariables.SERVER_START}\t-\tЗапуск сервера чата.");
            Console.WriteLine($"\t{GlobalVariables.CLIENT_START} <name>\t-\tЗапуск клиента чата, где name - имя пользователя чата.");
            Console.WriteLine("\nКоманды выполняемые после запуска клиента:");
            Console.WriteLine($"\t{GlobalVariables.USER_REGISTER_COMMAND}\t-\tРегистрация имени клиента в чате для отправки и получения сообщений.");
            Console.WriteLine($"\t{GlobalVariables.USER_UNREGISTER_COMMAND}\t-\tОтмена регистрация имени клиента в чате для отправки и получения сообщений.");
            Console.WriteLine($"\t{GlobalVariables.USER_LIST_COMMAND}\t-\tПолучение списка имен зарегистрированных пользователей");
            Console.WriteLine("\nФормат отправки сообщения: <имя_получателя>: текст сообщения");
            Console.WriteLine("\nДля выхода из клиента наберити exit.");
        }
        static async Task Main(string[] args)
        {
            if (args.Length == 0 || args[0] == GlobalVariables.SERVER_START)
            {
                if (args.Length > 1 && args[1] == "XML")
                {
                    GlobalVariables.SerializingFormat = args[1];
                }

                var massageSource = new MessageSource(GlobalVariables.SERVER_RECEIVER_PORT);
                var serverEndPoint = new IPEndPoint(IPAddress.Any, GlobalVariables.SERVER_RECEIVER_PORT);
                var server = new Server(massageSource, serverEndPoint);

                await server.UdpRecieverAsync();
            }
            else if (args[0] == GlobalVariables.CLIENT_START)
            {
                var messageSource = new MessageSource(GlobalVariables.CLIENT_RECEIVER_PORT);
                

                if (args.Length > 1)
                {
                    var serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), GlobalVariables.SERVER_RECEIVER_PORT);
                    var client = new Client(messageSource, serverEndPoint, $"{args[1]}");
                    await client.UdpSenderAsync();
                }
                    
                else
                    PrintHelp();
            }
            else
                PrintHelp();

            //await Client.UdpSenderAsync($"Vlad");
        }
    }
}
