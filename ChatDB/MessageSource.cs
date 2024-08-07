
using ChatDB.Abstraction;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatDB
{
    public class MessageSource : IMessageSource
    {
        private UdpClient _udpClient;
        private Converter _converter;

        public IPEndPoint ReceiveEndPoint { get; set; }

        public MessageSource(int port)
        {
            _udpClient = new UdpClient(port);
            if (GlobalVariables.SerializingFormat == "XML")
                _converter = new XmlConverter();
            else
                _converter = new JsonConverter();

        }

        public async Task<Message> ReceiveMessageAsync()
        {            
            var receivedResult = await _udpClient.ReceiveAsync();
            string message = Encoding.UTF8.GetString(receivedResult.Buffer);
            this.ReceiveEndPoint = receivedResult.RemoteEndPoint;

            return _converter.Deserialize(message)!;
        }

        public async Task SendMessageAsync(Message message, IPEndPoint endPoint)
        {
            string sendingMessage = _converter.Serialize(message);
            byte[] sendingBytes = Encoding.UTF8.GetBytes(sendingMessage);
            await _udpClient.SendAsync(sendingBytes, sendingBytes.Length, endPoint);
        }

        public async Task SendMessageExchangeTypeAsync(Message message, IPEndPoint endPoint)
        {            
            Converter converter = new JsonConverter();
            string sendingMessage = converter.Serialize(message);
            byte[] sendingBytes = Encoding.UTF8.GetBytes(sendingMessage);
            await _udpClient.SendAsync(sendingBytes, sendingBytes.Length, endPoint);
            converter = null;
        }

        public async Task ReceiveMessageExchangeTypeAsync()
        {
            Converter converter = new JsonConverter();
            var receivedResult = await _udpClient.ReceiveAsync();
            string message = Encoding.UTF8.GetString(receivedResult.Buffer);

            var result = converter.Deserialize(message)!;
            if (result.MessageText == "XML")
            {
                _converter = new XmlConverter();
                GlobalVariables.SerializingFormat = "XML";
            }                
            else
            {
                _converter = new JsonConverter();
                GlobalVariables.SerializingFormat = "JSON";
            }

            GlobalVariables.IsExchangeFormatSync = true;
            converter = null;            
        }
    }
}
