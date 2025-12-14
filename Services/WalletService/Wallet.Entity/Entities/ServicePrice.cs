using Wallet.Entity.Common;
using Wallet.Entity.Enums;

namespace Wallet.Entity.Entities;

public class ServicePrice : BaseEntity
{
    // Örn: "ImageGeneration"
    public string ServiceCategory { get; set; } = default!;

    // Örn: "fal-ai/flux-pro" (Benzersiz olmalı)
    public string ModelSystemName { get; set; } = default!;

    // Örn: "Flux Pro Ultra"
    public string DisplayName { get; set; } = default!;

    // Birim maliyet (Örn: 5.00)
    public decimal UnitPrice { get; set; }

    public Currency Currency { get; set; } = Currency.Credit;

    public bool IsActive { get; set; } = true;
}