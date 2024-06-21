public class DatabaseBatchInserter : IDatabaseBatchInserter
{
    public async ValueTask<int> BulkInsert<T>(List<T> dataList, string connectionString, string tableName, int batchSize)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var batches = SplitList(dataList, batchSize);

            foreach (var batch in batches)
            {
                DataTable data = batch.ToDataTable();

                bool isSuccess = await TryInsertAsync(connection, data, tableName, batchSize);

                if (!isSuccess)
                {
                    throw new SystemException(HttpStatusCode.BadRequest, "Insert data fail!");
                }
            }
        }
        return dataList.Count();
    }

    private async ValueTask<bool> TryInsertAsync(SqlConnection connection, DataTable data, string tableName, int batchSize)
    {
        bool isSuccess = false;
        int retryAttempts = 3;

        while (!isSuccess && retryAttempts > 0)
        {
            using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = tableName;
                    bulkCopy.BatchSize = batchSize;

                    try
                    {
                        await bulkCopy.WriteToServerAsync(data);
                        await transaction.CommitAsync();
                        isSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        retryAttempts--;
                        throw new SystemException(HttpStatusCode.BadRequest, ex.Message);
                    }
                }
            }
        }

        return isSuccess;
    }

    private IEnumerable<List<T>> SplitList<T>(List<T> data, int nSize = 30)
    {
        for (int i = 0; i < data.Count; i += nSize)
        {
            yield return data.GetRange(i, Math.Min(nSize, data.Count - i));
        }
    }
}