using DataverseClientShared;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;

if (args.Length < 2)
{
    Console.WriteLine("Usage: DataverseImport <EntityCollection URL> <Path to CSV file>");
    Console.WriteLine("Example");
    Console.WriteLine("<Data URL> : https://orgxxxxxxxx.crmx.dynamics.com/api/data/v9.2/xxxxxxxxs");
    Console.WriteLine("<Definitions URL> : \"https://orgxxxxxxxx.crmx.dynamics.com/api/data/v9.2/EntityDefinitions(LogicalName='xxxxxxxx')\"");
    Console.WriteLine("<Path to CSV file> : C:\\example\\example.csv");
    return;
}
string s1 = args[0];
string s2 = $"{args[1]}/Attributes";

var jsons = DataverseClientShared.CSV.Instance.ToJosnArrayFromFile(args[2]);

HttpHelper http = (await HttpHelper.Instance.Init(s2, new HttpClient())).Item1;

string uniqueidentifierName = null;
Dictionary<string, string> attributeTypes = new();
{
    var attributesJsonString = await http.GetAsync();
    var attributesJsonObject = Newtonsoft.Json.Linq.JObject.Parse(attributesJsonString);
    uniqueidentifierName = (attributesJsonObject["value"] as Newtonsoft.Json.Linq.JArray).Where(attr => attr["AttributeType"].ToString() == "Uniqueidentifier").Select(attr => attr["SchemaName"].ToString().ToLower()).FirstOrDefault();
    var attributes = (attributesJsonObject["value"] as Newtonsoft.Json.Linq.JArray).Select(attr => (attr["SchemaName"], attr["@odata.type"])).ToArray();
    foreach (var attr in attributes.Where(attr => attr.Item1 != null && attr.Item2 != null))
        attributeTypes[attr.Item1.ToString().ToLower()] = attr.Item2.ToString();
}

foreach (var json in jsons)
    foreach (var attr in json.Properties().ToArray())
    {
        if (attributeTypes.TryGetValue(attr.Name.ToString(), out var type))
        {
            var value = attr.Value.ToString(); if (string.IsNullOrWhiteSpace(value)) { attr.Value = null; continue; }
            switch (type)
            {
                case "#Microsoft.Dynamics.CRM.DecimalAttributeMetadata":
                case "#Microsoft.Dynamics.CRM.DoubleAttributeMetadata":
                    if (string.IsNullOrWhiteSpace(value)) attr.Value = null; else attr.Value = decimal.Parse(value);
                    break;
                case "#Microsoft.Dynamics.CRM.IntegerAttributeMetadata":
                case "#Microsoft.Dynamics.CRM.BigIntAttributeMetadata":
                    if (string.IsNullOrWhiteSpace(value)) attr.Value = null; else attr.Value = long.Parse(value);
                    break;
                case "#Microsoft.Dynamics.CRM.DateTimeAttributeMetadata":
                    if (string.IsNullOrWhiteSpace(value)) attr.Value = null; else attr.Value = DateTime.Parse(value);
                    break;
                default:
                    break;
            }
        }
        else if (attr.Name?.ToLower() == uniqueidentifierName?.ToLower())
        {
            continue;
        }
        else
            attr.Remove();
    }

http.InitPath(s1);
foreach (var json in jsons)
{
    if (uniqueidentifierName != null && !string.IsNullOrWhiteSpace(json[uniqueidentifierName]?.ToString()))
    {
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
        var response = await http.HttpClient.PatchAsync(http.Path + $"({json[uniqueidentifierName]?.ToString()})", content);
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
    else
    {
        json[uniqueidentifierName]?.Parent?.Remove();
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
}