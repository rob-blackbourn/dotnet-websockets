using System;
using System.Text;

namespace WebSockets.Core
{
    public class ServerProtocol
    {
        enum State
        {
            Handshake,
            Connected,
            Closing,
            Closed
        }

        private static byte[] HTTP_EOM = Encoding.UTF8.GetBytes("\r\n\r\n");

        private readonly FragmentBuffer<byte> _handshakeBuffer = new FragmentBuffer<byte>();
        private State _state = State.Handshake;

        public void Receive(byte[] data)
        {
            switch (_state)
            {
                case State.Handshake:
                    ReceiveHandshake(data);
                    break;
                case State.Connected:
                case State.Closing:
                    ReceiveMessages(data);
                    break;
                case State.Closed:
                    throw new InvalidOperationException("cannot receive data when closed");
            }
        }

        private void ReceiveHandshake(byte[] data)
        {
            var offset = long.Max(0, _handshakeBuffer.Count - 3);
            _handshakeBuffer.Write(data);
        }

        private void ReceiveMessages(byte[] data)
        {
        }
    }
}