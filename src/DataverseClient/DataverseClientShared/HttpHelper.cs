namespace DataverseClientShared;

public class HttpHelper
{
    public static HttpHelper Instance { get; } = new HttpHelper();

    public string? Resource { get; private set; }
    public Uri? BaseAddress { get; private set; }
    public string? Path { get; private set; }
    public static string ClientId { get; } = "51f81489-12ee-4a9e-aaae-a2591f45987d";
    public static string RedirectUri { get; } = "http://localhost";

    private HttpHelper() { }

    public Microsoft.Identity.Client.AuthenticationResult? Token { get; private set; }
    public HttpClient? HttpClient { get; private set; }

    public HttpHelper InitPath(string fullPath)
    {
        var uri = new Uri(fullPath);
        Resource = uri.Scheme + "://" + uri.Host;
        BaseAddress = new Uri(Instance.Resource + string.Join("", uri.Segments.Take(4)));
        Path = new string(uri.PathAndQuery.Skip(BaseAddress.AbsolutePath.Length).ToArray());
        return Instance;
    }

    public async Task<Microsoft.Identity.Client.AuthenticationResult> AuthenticateAsync()
    {
        var authBuilder = Microsoft.Identity.Client.PublicClientApplicationBuilder.Create(ClientId)
           .WithAuthority(Microsoft.Identity.Client.AadAuthorityAudience.AzureAdMultipleOrgs)
           .WithRedirectUri(RedirectUri)
           .Build();
        string[] scopes = { Resource + "/user_impersonation" };
        Microsoft.Identity.Client.AuthenticationResult token = await authBuilder.AcquireTokenInteractive(scopes).ExecuteAsync();
        return Token = token;
    }

    public HttpClient SetHttpClient(HttpClient httpClient)
    {
        var client = HttpClient = httpClient ?? HttpClient ?? new HttpClient();
        client.BaseAddress = BaseAddress;
        client.Timeout = new TimeSpan(0, 2, 0);
        System.Net.Http.Headers.HttpRequestHeaders headers = client.DefaultRequestHeaders;
        headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token?.AccessToken);
        headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public async Task<(HttpHelper, HttpClient)> Init(string fullPath, HttpClient httpClient)
    {
        InitPath(fullPath);
        await AuthenticateAsync();
        SetHttpClient(httpClient);
        return (Instance!, HttpClient!);
    }
}
