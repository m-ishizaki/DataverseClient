using DataverseClientShared;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection.Emit;
using System.Text;

if(args.Length < 2)
{
    Console.WriteLine("Usage: DataverseAddAttributes <EntityDefinition URL> <Path to JSON file>");
    Console.WriteLine("Example");
    Console.WriteLine("<EntityDefinition URL> : https://orgxxxxxxxx.crmx.dynamics.com/api/data/v9.2/EntityDefinitions(LogicalName='xxxxx_xxxxxxxx')");
    Console.WriteLine("<Path to JSON file> : C:\example\example.json");

}

string s = args[0] + "/Attributes";
string d = args[1];;
HttpHelper http = (await HttpHelper.Instance.Init(s, new HttpClient())).Item1;

Newtonsoft.Json.Linq.JObject originJson = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText(d))!;
var value = originJson.SelectToken("value");
if (value != null)
    foreach (var attribute in value)
    {
        attribute.SelectToken("MetadataId")?.Parent?.Remove();
        attribute.SelectToken("CreatedOn")?.Parent?.Remove();
        attribute.SelectToken("ModifiedOn")?.Parent?.Remove();
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(attribute);
        Console.WriteLine(await PostAsync(http, json));
    }

async Task<string> PostAsync(HttpHelper http, string json)
{
    string? jsonContent = "";
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    if (http.HttpClient == null) return jsonContent;
    var response = await http.HttpClient.PostAsync(http.Path, content);
    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Success: {response.StatusCode}");
        jsonContent = await response.Content.ReadAsStringAsync();
    }
    else
    {
        Console.WriteLine($"Error: {response.StatusCode}");
        jsonContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(jsonContent);
    }
    return jsonContent ?? "";
}