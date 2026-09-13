namespace Messaging
{
    public static class PulsarTopics
    {
        public const string OrderPlaced =
            "persistent://public/default/order-placed";

        public const string ReservationSucceeded =
            "persistent://public/default/reservation-succeeded";

        public const string ReservationFailed =
            "persistent://public/default/reservation-failed";

        public const string PaymentSucceeded =
            "persistent://public/default/payment-succeeded";

        public const string PaymentFailed =
            "persistent://public/default/payment-failed";

        public const string StockReleased =
            "persistent://public/default/stock-released";
    }
}
