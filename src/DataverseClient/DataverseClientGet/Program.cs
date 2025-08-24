using DataverseClientShared;

string s = args[0];
HttpHelper http = (await HttpHelper.Instance.Init(s, new HttpClient())).Item1;

string? jsonContent = "";
if (http.HttpClient != null)
{
    var response = await http.HttpClient.GetAsync(http.Path);
    if (response.IsSuccessStatusCode)
    {
        jsonContent = await response.Content.ReadAsStringAsync();
    }
}

Console.WriteLine(jsonContent ?? "");
