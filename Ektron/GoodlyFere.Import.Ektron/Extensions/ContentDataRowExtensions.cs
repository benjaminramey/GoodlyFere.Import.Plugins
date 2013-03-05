#region Usings

using System;
using System.Data;
using System.Linq;

#endregion

namespace GoodlyFere.Import.Ektron.Extensions
{
    public static class ContentDataRowExtensions
    {
        #region Public Methods

        public static bool IsNew(this DataRow contentRow)
        {
            return contentRow["contentId"] == DBNull.Value
                   || (long)contentRow["contentId"] <= 0;
        }

        #endregion
    }
}