public static class DataTableExtensions
{
    public static DataTable ToDataTable<T>(this IEnumerable<T> data)
    {
        DataTable dataTable = new DataTable();
        if (data == null || data.Count() == 0)
        {
            return dataTable;
        }

        var properties = typeof(T).GetProperties().Select(p => new
        {
            Property = p,
            ColumnAttribute = p.GetCustomAttributes(typeof(ColumnAttribute), true)
                                    .FirstOrDefault() as ColumnAttribute
        }).ToList();

        foreach (var prop in properties)
        {
            Type columnType = prop.Property.PropertyType;
            if (columnType.IsGenericType && columnType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                columnType = Nullable.GetUnderlyingType(columnType) ?? columnType;
            }
            string columnName = prop.ColumnAttribute?.Name ?? prop.Property.Name;
            dataTable.Columns.Add(columnName, columnType);
        }

        foreach (T item in data)
        {
            DataRow row = dataTable.NewRow();
            foreach (var prop in properties)
            {
                string columnName = prop.ColumnAttribute?.Name ?? prop.Property.Name;
                row[columnName] = prop.Property.GetValue(item, null) ?? DBNull.Value;
            }

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }
}