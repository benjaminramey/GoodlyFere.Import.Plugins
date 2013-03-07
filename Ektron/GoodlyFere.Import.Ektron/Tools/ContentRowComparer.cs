#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContentRowComparer.cs">
// GoodlyFere.Import.Ektron
// 
// Copyright (C) 2013 Benjamin Ramey
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
// http://www.gnu.org/licenses/lgpl-2.1-standalone.html
// 
// You can contact me at ben.ramey@gmail.com.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

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