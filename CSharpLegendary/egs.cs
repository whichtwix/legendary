using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Web;

namespace CSharpLegendary
{
    public class EpicApi
    {
        private string _userAgent = "UELauncher/11.0.1-14907503+++Portal+Release-Live Windows/10.0.19041.1.256.64bit";

        private string _userBasic = "34a02cf8f4414e29b15921876da36f9a";
        private string _pwBasic = "daafbccc737745039dffe53d94fc76cf";

        private const string OauthHost = "account-public-service-prod03.ol.epicgames.com";

        private readonly HttpClient _httpClient;
        private JsonNode? _userSession; 
        public string? AccessToken { get; private set; }
        public string? AccountId { get; private set; }

        public EpicApi()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        }

        public string GetAuthUrl()
        {
            string baseUrl = "https://www.epicgames.com/id/login?redirectUrl=";
            string redirectUrl = $"https://www.epicgames.com/id/api/redirect?clientId={_userBasic}&responseType=code";
            return baseUrl + HttpUtility.UrlEncode(redirectUrl);
        }

        private void SetAuthHeader()
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }
        }

        public async Task<JsonNode> StartSessionAsync(string authCode = "", string refreshToken = "", string exchangeCode = "")
        {
            var parameters = new List<KeyValuePair<string, string>>();

            if (!string.IsNullOrEmpty(refreshToken))
            {
                parameters.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
                parameters.Add(new KeyValuePair<string, string>("refresh_token", refreshToken));
                parameters.Add(new KeyValuePair<string, string>("token_type", "eg1"));
            }
            else if (!string.IsNullOrEmpty(exchangeCode))
            {
                parameters.Add(new KeyValuePair<string, string>("grant_type", "exchange_code"));
                parameters.Add(new KeyValuePair<string, string>("exchange_code", exchangeCode));
                parameters.Add(new KeyValuePair<string, string>("token_type", "eg1"));
            }
            else if (!string.IsNullOrEmpty(authCode))
            {
                parameters.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
                parameters.Add(new KeyValuePair<string, string>("code", authCode));
                parameters.Add(new KeyValuePair<string, string>("token_type", "eg1"));
            }
            else
            {
                throw new ArgumentException("At least one token type must be specified!");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{OauthHost}/account/api/oauth/token");
            
            var basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_userBasic}:{_pwBasic}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            request.Content = new FormUrlEncodedContent(parameters);

            var response = await _httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(jsonString);

            if (jsonNode["errorCode"] != null)
            {
                string code = jsonNode["errorCode"].ToString();
                throw new Exception($"Login to EGS API failed with errorCode: {code}. Response: {jsonString}");
            }
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"API Error {response.StatusCode}: {jsonString}");
            }

            _userSession = jsonNode;
            
            AccessToken = jsonNode["access_token"]?.ToString();
            AccountId = jsonNode["account_id"]?.ToString();
            
            SetAuthHeader();

            return _userSession;
        }

        public async Task<JsonNode> ResumeSessionAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.GetAsync($"https://{OauthHost}/account/api/oauth/verify");
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(jsonString);

            if (jsonNode["errorMessage"] != null)
            {
                throw new Exception($"Session verify failed: {jsonNode["errorCode"]}");
            }
            
            if (!response.IsSuccessStatusCode) throw new Exception($"Session verify failed - error code {response.StatusCode}");

            AccessToken = accessToken;
            _userSession = jsonNode;
            AccountId = jsonNode["account_id"]?.ToString();

            return _userSession;
        }

        public async Task<string> GetGameTokenAsync()
        {
            var response = await _httpClient.GetAsync($"https://{OauthHost}/account/api/oauth/exchange");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var node = JsonNode.Parse(json);
            
            return node["code"]?.ToString();
        }
    }
}