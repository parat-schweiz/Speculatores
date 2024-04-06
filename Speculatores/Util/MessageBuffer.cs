using System;
using System.Collections.Generic;
using Npgsql;

namespace Speculatores
{
    public class MessageBuffer
    {
        private readonly NpgsqlConnection _db;
        private readonly string _tableName;
        private readonly string _facilityName;

        public MessageBuffer(NpgsqlConnection db, string tableName, string facilityName)
        {
            _db = db;
            _tableName = tableName;
            _facilityName = facilityName;
            var createTable = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS " + _tableName + " (id uuid NOT NULL, facility varchar(64), message text NOT NULL, PRIMARY KEY (id, facility));", _db);
            createTable.ExecuteNonQuery();
        }

        public bool Contains(Guid id)
        {
            var query = new NpgsqlCommand("SELECT count(*) FROM " + _tableName + " WHERE id = @id and facility = @facility", _db);
            query.Parameters.Add(new NpgsqlParameter("id", id));
            query.Parameters.Add(new NpgsqlParameter("facility", _facilityName));
            return (long)query.ExecuteScalar() == 1;
        }

        public long Count()
        {
            var query = new NpgsqlCommand("SELECT count(*) FROM " + _tableName + " WHERE facility = @facility", _db);
            query.Parameters.Add(new NpgsqlParameter("facility", _facilityName));
            return (long)query.ExecuteScalar();
        }

        public void Delete(Guid id)
        {
            var query = new NpgsqlCommand("DELETE FROM " + _tableName + " WHERE id = @id and facility = @facility", _db);
            query.Parameters.Add(new NpgsqlParameter("id", id));
            query.Parameters.Add(new NpgsqlParameter("facility", _facilityName));
            query.ExecuteNonQuery();
        }

        public void Insert(Message message)
        {
            var query = new NpgsqlCommand("INSERT INTO " + _tableName + " (id, facility, message) VALUES (@id, @facility, @message)", _db);
            query.Parameters.Add(new NpgsqlParameter("id", message.Id));
            query.Parameters.Add(new NpgsqlParameter("facility", _facilityName));
            query.Parameters.Add(new NpgsqlParameter("message", message.Text));
            query.ExecuteNonQuery();
        }

        public IEnumerable<Message> Select(int limit)
        {
            var query = new NpgsqlCommand("SELECT id, message FROM " + _tableName + " WHERE facility = @facility LIMIT @limit", _db);
            query.Parameters.Add(new NpgsqlParameter("facility", _facilityName));
            query.Parameters.Add(new NpgsqlParameter("limit", limit));
            using (var reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetGuid(0);
                    var message = reader.GetString(1);
                    yield return new Message(id, message);
                }
            }
        }
    }
}
