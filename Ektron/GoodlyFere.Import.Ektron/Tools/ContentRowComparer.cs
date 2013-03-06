#region Usings

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using GoodlyFere.Import.Ektron.Extensions;

#endregion

namespace GoodlyFere.Import.Ektron
{
    internal class ContentRowComparer : IEqualityComparer<DataRow>
    {
        #region Public Methods

        public bool Equals(DataRow x, DataRow y)
        {
            string xTitle = x["title"].ToString();
            string yTitle = y["title"].ToString();
            string xFolder = x["folderName"].ToString();
            string yFolder = y["folderName"].ToString();

            if (!x.IsNew() && !y.IsNew())
            {
                long xId = (long)x["contentId"];
                long yId = (long)y["contentId"];
                return xId == yId;
            }

            return xTitle.Equals(yTitle) && xFolder == yFolder;
        }

        public int GetHashCode(DataRow obj)
        {
            string title = obj["title"].ToString();
            string folder = obj["folderName"].ToString();

            if (!obj.IsNew())
            {
                long id = (long)obj["contentId"];
                return id.GetHashCode();
            }

            return title.GetHashCode() | folder.GetHashCode();
        }

        #endregion
    }
}