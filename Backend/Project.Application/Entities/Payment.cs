namespace Project.Application.Entities;

public enum PaymentMethod
{
    Card = 0,
    PayPal = 1,
    Google = 2,
    Apple = 3
}

public enum PaymentStatus
{
    StubSucceeded = 0
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.StubSucceeded;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
