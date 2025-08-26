namespace DataverseClientShared;

public class CSV
{
    public static CSV Instance { get; } = new CSV();
    private CSV() { }

    public IEnumerable<Newtonsoft.Json.Linq.JObject> ToJosnArrayFromFile(string path)
    {
        var csvString = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
        var result = ToJsonArray(csvString);
        return result;
    }

    public IEnumerable<Newtonsoft.Json.Linq.JObject> ToJsonArray(string csvString)
    {
        var (header, data) = CsvReader.ReadHeaderAndData(csvString);
        var list = new List<Newtonsoft.Json.Linq.JObject>();
        foreach (var row in data)
        {
            var obj = new Newtonsoft.Json.Linq.JObject();
            for (int i = 0; i < header.Length && i < row.Length; i++)
            {
                obj[RemoveTailCR(header[i])] = RemoveTailCR(row[i]);
            }
            list.Add(obj);
        }
        return list;
    }

    string RemoveTailCR(string s) => s.EndsWith("\r") ? s[..^1] : s;
}

public static class CsvReader
{
    public static string[][] Read(string csvString) => _Read(csvString);

    public static (string[] header, string[][] data) ReadHeaderAndData(string csvString)
    {
        var values = Read(csvString);
        return (values.FirstOrDefault() ?? Array.Empty<string>(), values.Skip(1).ToArray());
    }

    private static string[][] _Read(string csvString)
    {
        List<List<List<char>>> data = new();
        (bool isLineHead, bool isQuoted, char quote, bool isEscaped) = (true, false, '"', false);
        foreach (var c in csvString)
        {
            if (isLineHead) { data.Add(new List<List<char>>()); data.LastOrDefault()?.Add(new List<char>()); isLineHead = false; }
            if (data.LastOrDefault()?.LastOrDefault()?.Count == 0 && !isQuoted && (c == '"' || c == '\''))
            {
                (isQuoted, quote, isEscaped) = (true, c, false);
                continue;
            }
            if (!isEscaped && isQuoted && c == quote) { isEscaped = true; continue; }
            if (c == ',' && (!isQuoted || isEscaped)) { data.LastOrDefault()?.Add(new List<char>()); (isQuoted, isEscaped) = (false, false); continue; }
            if (c == '\n' && (!isQuoted || isEscaped)) { isLineHead = true; (isQuoted, isEscaped) = (false, false); continue; }
            isEscaped = false;
            data.LastOrDefault()?.LastOrDefault()?.Add(c);
        }
        return data.Select(line => line.Select(cell => new string(cell.ToArray())).ToArray()).ToArray();
    }
}
