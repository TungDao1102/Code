public class CsvParserService : ICsvParserService
{
    public IEnumerable<T> GetData<T>(string path) where T : new()
    {
        using var reader = new StreamReader(path, Encoding.UTF8);

        var header = reader.ReadLine() ?? throw new SystemException(HttpStatusCode.NoContent, "CSV file is empty.");

        var headers = header.Split(',');

        var results = new List<T>();

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = line.Split(',');
            var obj = new T();

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var index = Array.IndexOf(headers, prop.Name.ToSnakeCase());
                if (index >= 1 && index < values.Length)
                {
                    var value = values[index];
                    SetPropertyValue(obj, prop, value);
                }
            }

            results.Add(obj);
        }

        return results;
    }

    private void SetPropertyValue<T>(T obj, PropertyInfo prop, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SetDefaultValue(obj, prop);
            return;
        }

        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        object? convertedValue = null;

        try
        {
            if (targetType == typeof(DateOnly))
            {
                if (DateOnly.TryParse(value, out var dateOnlyValue))
                {
                    convertedValue = dateOnlyValue;
                }
            }
            else if (targetType == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out var dateTimeValue))
                {
                    convertedValue = dateTimeValue;
                }
            }
            else if (targetType == typeof(bool))
            {
                if (value == "1")
                {
                    convertedValue = true;
                }
                else
                {
                    convertedValue = false;
                }
            }
            else
            {
                convertedValue = Convert.ChangeType(value, targetType);
            }

            prop.SetValue(obj, convertedValue);
        }
        catch
        {
            SetDefaultValue(obj, prop);
        }
    }

    private void SetDefaultValue<T>(T obj, PropertyInfo prop)
    {
        bool isNullable = Nullable.GetUnderlyingType(prop.PropertyType) != null;
        if (isNullable)
        {
            prop.SetValue(obj, null);
        }
        else if (prop.PropertyType == typeof(DateOnly))
        {
            prop.SetValue(obj, DateOnly.FromDateTime(DateTime.UtcNow));
        }
        else if (prop.PropertyType == typeof(bool))
        {
            prop.SetValue(obj, true);
        }
        else if (prop.PropertyType == typeof(DateTime))
        {
            prop.SetValue(obj, DateTime.UtcNow);
        }
    }
}