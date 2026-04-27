using Common.Dto;

namespace TelegramServer.Services;

public interface IBusinessServerClient
{
    Task<EchoResponse?> EchoAsync(EchoRequest request, CancellationToken ct = default);
}
