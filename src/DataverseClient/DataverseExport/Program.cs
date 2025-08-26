using System.Text;

string s = args[0];
string r = await new DataverseHttpClient(s).GetAsync();
Console.WriteLine(ToCsv.Convert(r));

class DataverseHttpClient
{
    public string Resource { get; init; }
    public Uri BaseAddress { get; init; }
    public string Path { get; init; }
    public static string ClientId { get; } = "51f81489-12ee-4a9e-aaae-a2591f45987d";
    public static string RedirectUri { get; } = "http://localhost";

    Microsoft.Identity.Client.AuthenticationResult Token { get; set; }
    HttpClient HttpClient { get; init; }

    public DataverseHttpClient(string resource)
    {
        var uri = new Uri(resource);
        Resource = uri.Scheme + "://" + uri.Host;
        BaseAddress = new Uri(Resource + "/api/data/v9.2/");
        Path = new string(resource.Skip(BaseAddress.AbsoluteUri.Length).ToArray());
        Authentication().GetAwaiter().GetResult();
        HttpClient = BuildHttpClient();
    }

    async Task<Microsoft.Identity.Client.AuthenticationResult> Authentication()
    {
        var authBuilder = Microsoft.Identity.Client.PublicClientApplicationBuilder.Create(ClientId)
                       .WithAuthority(Microsoft.Identity.Client.AadAuthorityAudience.AzureAdMultipleOrgs)
                       .WithRedirectUri(RedirectUri)
                       .Build();
        string[] scopes = { Resource + "/user_impersonation" };
        Microsoft.Identity.Client.AuthenticationResult token = await authBuilder.AcquireTokenInteractive(scopes).ExecuteAsync();
        return Token = token;
    }

    HttpClient BuildHttpClient()
    {
        HttpClient client = new HttpClient() { BaseAddress = BaseAddress, Timeout = new TimeSpan(0, 2, 0) };
        System.Net.Http.Headers.HttpRequestHeaders headers = client.DefaultRequestHeaders;
        headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token.AccessToken);
        return client;
    }

    public async Task<string> GetAsync(string path = null)
    {
        var response = await HttpClient.GetAsync(path ?? Path);
        string jsonContent = null;
        if (response.IsSuccessStatusCode)
        {
            jsonContent = await response.Content.ReadAsStringAsync();
        }
        return jsonContent ?? "";
    }
}

class ToCsv
{
    static string CellValue(string value) => "\"" + value?.Replace("\"", "\"\"") + "\"";
    public static string Convert(string json)
    {
        if (string.IsNullOrEmpty(json)) return string.Empty;
        var csvBuilder = new System.Text.StringBuilder();
        var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(json);
        if (jsonObject.GetValue("value") is Newtonsoft.Json.Linq.JArray jsonArray)
        {
            bool isFirst = true;
            foreach (var item in jsonArray)
            {
                if (item is Newtonsoft.Json.Linq.JObject itemObject)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        var titleLine = string.Join(",", itemObject.Properties().Select(property => CellValue(property.Name)).ToArray());
                        csvBuilder.AppendLine(titleLine);
                    }
                    var dataLine = string.Join(",", itemObject.Properties().Select(property => CellValue(property.Value.ToString())).ToArray());
                    csvBuilder.AppendLine(dataLine);
                }
            }
        }
        return csvBuilder.ToString();
    }
}

