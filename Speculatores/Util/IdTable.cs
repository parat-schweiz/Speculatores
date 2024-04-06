using System;
using Npgsql;

namespace Speculatores
{
    public class IdTable
    {
        private readonly NpgsqlConnection _db;
        private readonly string _tableName;

        public IdTable(NpgsqlConnection db, string tableName)
        {
            _db = db;
            _tableName = tableName;
            var createTable = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS " + _tableName + " (id uuid NOT NULL, PRIMARY KEY (id));", _db);
            createTable.ExecuteNonQuery();
        }

        public bool NotContained(Guid id)
        {
            var query = new NpgsqlCommand("SELECT count(*) FROM " + _tableName + " WHERE id = @id", _db);
            query.Parameters.Add(new NpgsqlParameter("id", id));
            return (long)query.ExecuteScalar() == 0;
        }

        public void Insert(Guid id)
        {
            var insert = new NpgsqlCommand("INSERT INTO " + _tableName + " (id) VALUES (@id)", _db);
            insert.Parameters.Add(new NpgsqlParameter("id", id));
            insert.ExecuteNonQuery();
        }
    }
}
