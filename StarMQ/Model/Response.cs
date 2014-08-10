namespace StarMQ.Model
{
    public enum ResponseType
    {
        Ack,
        Nack
    }

    public enum ResponseAction
    {
        DoNothing,
        Unsubscribe
    }

    public class Response
    {
        public ResponseType Type { get; set; }
        public ResponseAction Action { get; set; }
    }
}
