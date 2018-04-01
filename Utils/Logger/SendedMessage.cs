namespace Utils.Logger
{
    public class SendedMessage
    {
        public readonly string Type = MessageType.Outgoing.ToString();
        public string Message { get; set; }
        public string To { get; set; }
    }
}
