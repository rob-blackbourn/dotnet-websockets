using System;
using System.Security.Cryptography;
using System.Text;

namespace WebSockets.Core
{
    public enum ConnectionState
    {
        Handshake,
        Connected,
        Closing,
        Closed,
        Faulted
    }

    public class Protocol
    {

        private protected static byte[] HTTP_EOM = "\r\n\r\n"u8.ToArray();
        private protected const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private protected readonly FragmentBuffer<byte> _handshakeBuffer = new FragmentBuffer<byte>();
        private protected readonly MessageReader _messageReader = new MessageReader();
        private protected readonly MessageWriter _messageWriter;
        private protected readonly string[] _subProtocols;
        private protected readonly IDateTimeProvider _dateTimeProvider;
        private protected bool _isClient;

        public Protocol(
            bool isClient,
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider,
            INonceGenerator nonceGenerator)
        {
            _isClient = isClient;
            _subProtocols = subProtocols;
            _dateTimeProvider = dateTimeProvider;

            _messageWriter = new MessageWriter(nonceGenerator);
        }

        public ConnectionState State {get; protected set; } = ConnectionState.Handshake;

        public void SubmitMessage(Message message)
        {
            switch (State)
            {
                case ConnectionState.Connected:
                    _messageWriter.SubmitMessage(message, _isClient, Reserved.AllFalse);
                    break;
                case ConnectionState.Closing:
                    if (message.Type != MessageType.Close)
                        throw new InvalidOperationException("can only send a close message when closing.");
                    _messageWriter.SubmitMessage(message, _isClient, Reserved.AllFalse);
                    break;
                case ConnectionState.Handshake:
                case ConnectionState.Closed:
                case ConnectionState.Faulted:
                    throw new InvalidOperationException($"cannot send a message in state {State}.");
            }
        }

        public void WriteHandshakeData(byte[] buffer, long offset, long length)
        {
            _handshakeBuffer.Write(buffer, offset, length);
        }

        public long ReadHandshakeData(byte[] buffer)
        {
            return _handshakeBuffer.Read(buffer);
        }

        public void SubmitData(byte[] buffer, long offset, long length)
        {
            switch (State)
            {
                case ConnectionState.Handshake:
                    throw new InvalidOperationException("handshake not complete");
                case ConnectionState.Connected:
                case ConnectionState.Closing:
                    _messageReader.SubmitData(buffer, offset, length);
                    break;
                case ConnectionState.Closed:
                    throw new InvalidOperationException("cannot receive data when closed");
                case ConnectionState.Faulted:
                    throw new InvalidOperationException("cannot receive data when faulted");
                default:
                    throw new InvalidOperationException("invalid internal state");
            }
        }

        private protected static string CreateResponseKey(string requestKey)
        {
            var nonce = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(requestKey + WebSocketResponseGuid));
            return Convert.ToBase64String(nonce);
        }
    }
}