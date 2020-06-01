﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Text;

namespace Darwin.Utilities
{
    public static class SQLiteReaderExtensions
    {
        public static int SafeGetInt(this SQLiteDataReader rdr, string columnName)
        {
            var colIndex = rdr.GetOrdinal(columnName);

            if (rdr.IsDBNull(colIndex))
                return default(int);

            return rdr.GetInt32(colIndex);
        }

        public static int? SafeGetNullableInt(this SQLiteDataReader rdr, string columnName)
        {
            var colIndex = rdr.GetOrdinal(columnName);

            if (rdr.IsDBNull(colIndex))
                return null;

            return rdr.GetInt32(colIndex);
        }

        // TODO: This one might not be necessary
        public static string SafeGetString(this SQLiteDataReader rdr, string columnName)
        {
            var colIndex = rdr.GetOrdinal(columnName);

            if (rdr.IsDBNull(colIndex))
                return null;

            return rdr.GetString(colIndex);
        }
    }
}
