using System.Text.Json;

namespace Nimbus.Framework.Api
{
    /// <summary>
    /// Instance-based service class for interacting with the ReqRes API.
    /// Demonstrates using the reusable ApiClient to send GET and POST requests.
    /// </summary>
    public sealed class ReqResService
    {
        private readonly ApiClient _apiClient;

        /// <summary>
        /// Create a ReqResService with a provided ApiClient.
        /// </summary>
        public ReqResService(ApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// GET https://reqres.in/api/users?page=2
        /// Returns the raw JSON response as string (parity with Java).
        /// </summary>
        public string FetchUsers(int page = 1) => _apiClient.Get($"https://reqres.in/api/users?page={page}");

        public Task<string> FetchUsersAsync(int page = 1, CancellationToken ct = default)
        {
            return _apiClient.GetAsync($"https://reqres.in/api/users?page={page}", ct);
        }

        /// <summary>
        /// POST https://reqres.in/api/users  { "name": ..., "job": ... }
        /// Returns the raw JSON response as string (parity with Java).
        /// </summary>
        public string CreateUser(string name, string job)
        {
            var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["name"] = name,
                ["job"] = job
            });
            return _apiClient.PostJson("https://reqres.in/api/users", payload);
        }

        public Task<string> CreateUserAsync(string name, string job, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["name"] = name,
                ["job"] = job
            });
            return _apiClient.PostJsonAsync("https://reqres.in/api/users", payload, ct);
        }

        /// <summary>
        /// POST https://reqres.in/api/login  { "email": ..., "password": ... }
        /// Returns the raw JSON response as string (parity with Java).
        /// </summary>
        public string Login(string email, string password)
        {
            var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["email"] = email,
                ["password"] = password
            });
            return _apiClient.PostJson("https://reqres.in/api/login", payload);
        }

        public Task<string> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["email"] = email,
                ["password"] = password
            });
            return _apiClient.PostJsonAsync("https://reqres.in/api/login", payload, ct);
        }
    }
}
