// File: src/Nimbus.Framework/Api/ApiClient.cs
using System.Net.Http.Headers;
using System.Text;

namespace Nimbus.Framework.Api
{
    /// <summary>
    /// Lightweight reusable API client. Optional x-api-key header.
    /// Can disable system proxy to avoid corporate gateways for public APIs.
    /// </summary>
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _client;
        private readonly string? _apiKey;

        /// <summary>
        /// Default: no API key, no proxy change.
        /// </summary>
        public ApiClient() : this(apiKey: null, disableProxy: false) { }

        /// <summary>
        /// Build an ApiClient.
        /// </summary>
        /// <param name="apiKey">If provided, sent as x-api-key.</param>
        /// <param name="disableProxy">If true, bypasses system proxy (useful for public test APIs).</param>
        public ApiClient(string? apiKey, bool disableProxy = false)
        {
            var handler = new HttpClientHandler();
            if (disableProxy) handler.UseProxy = false;  // <-- bypass corporate proxy/gateway
            _client = new HttpClient(handler, disposeHandler: true);

            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
        }

        public async Task<string> GetAsync(string uri, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (_apiKey is not null) req.Headers.TryAddWithoutValidation("x-api-key", _apiKey);

            using var resp = await _client.SendAsync(req, ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return body;
        }

        public async Task<string> PostJsonAsync(string uri, string json, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, uri);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (_apiKey is not null) req.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
            req.Content = new StringContent(json ?? string.Empty, Encoding.UTF8, "application/json");

            using var resp = await _client.SendAsync(req, ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return body;
        }

        public string Get(string uri) => GetAsync(uri).GetAwaiter().GetResult();
        public string PostJson(string uri, string json) => PostJsonAsync(uri, json).GetAwaiter().GetResult();

        public void Dispose() => _client.Dispose();
    }
}
