namespace Wallet.Application.Abstractions;

/// API katmanında JWT'den userId okuyup Business'a iletmek için basit bir sözleşme.
public interface ICurrentUserAccessor
{
    Guid? GetUserId();
}
