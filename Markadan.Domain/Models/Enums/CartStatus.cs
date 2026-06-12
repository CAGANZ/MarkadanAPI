namespace Markadan.Domain.Models.Enums
{
    public enum CartStatus : byte
    {
        Active = 0,
        Ordered = 1,       // ödeme alındı, sipariş onaylandı
        Cancelled = 2,     // iptal edildi
        PaymentPending = 3, // ödeme başlatıldı, tamamlanmadı
        Preparing = 4,     // hazırlanıyor (admin tarafından)
        Shipped = 5,       // kargoya verildi
        Delivered = 6      // teslim edildi
    }
}
