using Microsoft.EntityFrameworkCore;

namespace WFRP_Character_Companion.Data
{
    /// <summary>
    /// Ensures schema matches the model when migrations were not applied.
    /// </summary>
    public static class CharacterSchemaFix
    {
        public static void ApplyIfNeeded(ApplicationDbContext db)
        {
            if (TableExists(db, "CharacterTalents") && !ColumnExists(db, "CharacterTalents", "Specialization"))
            {
                db.Database.ExecuteSqlRaw(
                    "ALTER TABLE CharacterTalents ADD COLUMN Specialization TEXT NULL");
            }

            if (!TableExists(db, "Characters"))
                return;

            EnsureIntColumn(db, "Characters", "ExperienceEarned");
            EnsureIntColumn(db, "Characters", "ExperienceSpent");
            EnsureIntColumn(db, "Characters", "CorruptionPoints");
        }

        private static void EnsureIntColumn(ApplicationDbContext db, string table, string column)
        {
            if (ColumnExists(db, table, column)) return;
            db.Database.ExecuteSqlRaw(
                $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" INTEGER NOT NULL DEFAULT 0");
        }

        private static bool TableExists(ApplicationDbContext db, string tableName)
        {
            var connection = db.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose) connection.Open();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
                var param = command.CreateParameter();
                param.ParameterName = "$name";
                param.Value = tableName;
                command.Parameters.Add(param);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
            finally
            {
                if (shouldClose) connection.Close();
            }
        }

        private static bool ColumnExists(ApplicationDbContext db, string tableName, string columnName)
        {
            var connection = db.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose) connection.Open();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
            finally
            {
                if (shouldClose) connection.Close();
            }
        }
    }
}
