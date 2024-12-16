using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Sockets; // For SocketException
using Microsoft.Extensions.Logging; // For logging

namespace eShop.ServiceDefaults;

public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddAuthToken(this IHttpClientBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddTransient<HttpClientAuthorizationDelegatingHandler>();

        // Ensure that the logger is passed correctly to the handler
        builder.AddHttpMessageHandler(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<HttpClientAuthorizationDelegatingHandler>>(); // Get logger from DI
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new HttpClientAuthorizationDelegatingHandler(httpContextAccessor, logger);
        });

        return builder;
    }

    private class HttpClientAuthorizationDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HttpClientAuthorizationDelegatingHandler> _logger;

        // Constructor where both httpContextAccessor and logger are injected
        public HttpClientAuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor, ILogger<HttpClientAuthorizationDelegatingHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Ensure logger is not null
        }

        // Constructor for inner handler (if needed)
        public HttpClientAuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor, ILogger<HttpClientAuthorizationDelegatingHandler> logger, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Ensure logger is not null
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if cancellation is requested early
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Request has been cancelled.");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout);
                }

                // Ensure the HTTP context is available
                if (_httpContextAccessor.HttpContext is HttpContext context)
                {
                    var accessToken = await context.GetTokenAsync("access_token");

                    if (accessToken is not null)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    else
                    {
                        _logger.LogWarning("No access token found in HTTP context.");
                    }
                }

                // Send the HTTP request
                return await base.SendAsync(request, cancellationToken);
            }
            catch (SocketException ex)
            {
                // Handle network-related issues
                _logger.LogError(ex, "Network error while sending HTTP request.");
                throw new HttpRequestException("Network error while sending HTTP request", ex);
            }
            catch (TaskCanceledException ex)
            {
                // Handle cancellation of the request
                _logger.LogError(ex, "Request was canceled while sending HTTP request.");
                throw new HttpRequestException("Request was canceled while sending HTTP request", ex);
            }
            catch (HttpRequestException ex)
            {
                // Handle other HTTP request errors
                _logger.LogError(ex, "Error while sending HTTP request.");
                throw; // Rethrow the HttpRequestException as is
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                _logger.LogError(ex, "Unexpected error while sending HTTP request.");
                throw new HttpRequestException("Unexpected error while sending HTTP request", ex);
            }
        }
    }
}
