#region Usings

using System;
using System.Data;
using System.Linq;

#endregion

namespace GoodlyFere.Import.Ektron.Tools
{
    public static class DestinationHelper
    {
        #region Public Methods

        public static bool HasColumn(DataTable data, string columnName, Type columnType)
        {
            return data.Columns[columnName] != null
                   && data.Columns[columnName].DataType == columnType;
        }

        #endregion
    }
}