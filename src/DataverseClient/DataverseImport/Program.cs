using DataverseClientShared;
using System.Net.Http.Json;
using System.Text;

string s = args[0];
var jsons = DataverseClientShared.CSV.Instance.ToJosnArrayFromFile(args[1]);

HttpHelper http = (await HttpHelper.Instance.Init(s, new HttpClient())).Item1;

foreach (var json in jsons)
{
    var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
    var response = await http.HttpClient.PostAsync(http.Path, content);
    var jsonContent = await response.Content.ReadAsStringAsync();
    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Success: {response.StatusCode} {response.ReasonPhrase} {jsonContent}");
    }
    else
    {
        Console.WriteLine($"Error: {response.StatusCode} {response.ReasonPhrase} {jsonContent}");
    }
}