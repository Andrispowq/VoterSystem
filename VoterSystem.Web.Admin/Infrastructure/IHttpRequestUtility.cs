using VoterSystem.Web.Admin.Dto;
using VoterSystem.Web.Admin.ViewModels;

namespace VoterSystem.Web.Admin.Infrastructure;

public interface IHttpRequestUtility
{
    Task<HttpResponseWrapper<T>> ExecuteGetHttpRequestAsync<T>(string uri);
    Task<TU?> ExecutePutHttpRequestAsync<T, TU>(string uri, T requestDto);
    Task<TU?> ExecutePatchHttpRequestAsync<T, TU>(string uri, T requestDto);
    Task<TU?> ExecutePostHttpRequestAsync<T, TU>(string uri, T requestDto);
    Task ExecutePostHttpRequestAsync(string uri);
    Task ExecuteDeleteHttpRequestAsync(string uri);
    Task<TokensDto> RedeemTokenAsync(CancellationToken cancellationToken = default);
}