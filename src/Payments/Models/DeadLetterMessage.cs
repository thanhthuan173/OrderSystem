namespace Payments.Models
{
    public class DeadLetterMessage
    {
        public string OriginalTopic { get; set; } = string.Empty;
        public string SubscriptionName { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public int DeliveryAttempt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime FailedAt { get; set; }
    }
}
