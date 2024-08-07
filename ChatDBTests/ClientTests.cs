using ChatDB.Abstraction;
using ChatDB;
using System.Net;

namespace ChatDBTests
{
    public class ClientTests
    {
        IMessageSource _source;
        IPEndPoint _peer;

        [SetUp]
        public void Setup()
        {
            _peer = new IPEndPoint(IPAddress.Any, 0);
        }

        [Test]
        public void ReceiveMessageTest()
        {
            _source = new MockClientMessageSource(_peer);
            var result = _source.ReceiveMessageAsync();

            Assert.IsNotNull(result.Result);
            Assert.IsNull(result.Result.MessageText);
            Assert.IsNotNull(result.Result.SenderName);
            Assert.IsNotNull(result.Result.RecipientName);
            Assert.That(Command.Register, Is.EqualTo(result.Result.Command));
            Assert.That("Vlad", Is.EqualTo(result.Result.SenderName));
            Assert.That("Server", Is.EqualTo(result.Result.RecipientName));

            result = _source.ReceiveMessageAsync();
            Assert.IsNotNull(result.Result);
            Assert.IsNull(result.Result.MessageText);
            Assert.IsNotNull(result.Result.SenderName);
            Assert.IsNotNull(result.Result.RecipientName);
            Assert.That(Command.GetExchangeType, Is.EqualTo(result.Result.Command));
            Assert.That("Vlad", Is.EqualTo(result.Result.SenderName));
            Assert.That("Server", Is.EqualTo(result.Result.RecipientName));

            result = _source.ReceiveMessageAsync();
            Assert.IsNotNull(result.Result);
            Assert.IsNotNull(result.Result.MessageText);
            Assert.IsNotNull(result.Result.SenderName);
            Assert.IsNotNull(result.Result.RecipientName);
            Assert.That(Command.Message, Is.EqualTo(result.Result.Command));
            Assert.That("Vlad", Is.EqualTo(result.Result.SenderName));
            Assert.That("Alena", Is.EqualTo(result.Result.RecipientName));
            Assert.That("Привет!", Is.EqualTo(result.Result.MessageText));
        }
    }
}