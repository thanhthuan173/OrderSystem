namespace Inventory.Data.Entities
{
    public sealed class Reservation
    {
        public long Id { get; set; }
        public Guid OrderId { get; set; }
        public string Sku { get; set; } = null!;
        public int Quantity { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}