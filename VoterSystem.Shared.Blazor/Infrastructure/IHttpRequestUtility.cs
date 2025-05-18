using VoterSystem.Shared.Dto;
using VoterSystem.Shared.Blazor.ViewModels;

namespace VoterSystem.Shared.Blazor.Infrastructure;

public interface IHttpRequestUtility
{
    Task<HttpResponseWrapper<T>> ExecuteGetHttpRequestAsync<T>(string uri);
    Task<TU?> ExecutePostHttpRequestAsync<T, TU>(string uri, T requestDto) where T : class;
    Task ExecutePostHttpRequestAsync<T>(string uri, T? requestDto = null) where T : class;
    Task ExecutePostHttpRequestAsync(string uri);
    Task<TU?> ExecutePutHttpRequestAsync<T, TU>(string uri, T requestDto) where TU : class;
    Task ExecutePutHttpRequestAsync<T>(string uri, T requestDto);
    Task<TU?> ExecutePatchHttpRequestAsync<T, TU>(string uri, T requestDto) where TU : class;
    Task ExecutePatchHttpRequestAsync(string uri);
    Task ExecuteDeleteHttpRequestAsync(string uri);
    Task<TokensDto> RedeemTokenAsync(CancellationToken cancellationToken = default);
    bool IsAccessTokenExpired(string token);
}