namespace WebSockets.Core.Http
{
    abstract class BodyParser
    {
        public abstract bool NeedsData { get; }
        public abstract bool HasBody { get; }

        public abstract byte[] ReadBody();

        public abstract void WriteData(byte[] array, long offset, long length);
        public abstract void ProcessData();
    }
}
