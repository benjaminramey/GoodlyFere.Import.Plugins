#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DestinationHelper.cs">
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Ektron.Cms.Common;
using Ektron.Cms.Content;
using GoodlyFere.Import.Ektron.Extensions;

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

        #region Methods

        internal static void BuildTitleAndPathGroups(DataTable data, ContentCriteria criteria)
        {
            foreach (DataRow row in data.Rows.Cast<DataRow>().Where(dr => dr.IsNew()))
            {
                CriteriaFilterGroup<ContentProperty> critGroup = new CriteriaFilterGroup<ContentProperty>();
                critGroup.Condition = LogicalOperation.And;

                critGroup.AddFilter(
                    ContentProperty.Title,
                    CriteriaFilterOperator.EqualTo,
                    row["title"].ToString());

                critGroup.AddFilter(
                    ContentProperty.Path,
                    CriteriaFilterOperator.EqualTo,
                    row["folderPath"].ToString());

                criteria.FilterGroups.Add(critGroup);
            }
        }

        internal static IEnumerable<List<DataRow>> MakeThreadGroups(IEnumerable<DataRow> dataRows)
        {
            IEnumerable<DataRow> distinctRows = dataRows.Distinct(new ContentRowComparer());
            IEnumerable<List<DataRow>> threadGroups = distinctRows
                .Select((r, i) => new { Index = i, Row = r })
                .GroupBy(obj => Math.Truncate(obj.Index / 10.0))
                .Select(grp => grp.Select(v => v.Row).ToList());
            return threadGroups;
        }

        #endregion
    }
}