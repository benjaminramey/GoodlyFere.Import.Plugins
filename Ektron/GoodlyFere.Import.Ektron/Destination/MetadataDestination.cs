#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetadataDestination.cs">
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
using Common.Logging;
using Ektron.Cms;
using Ektron.Cms.Content;
using GoodlyFere.Import.Ektron.Extensions;
using GoodlyFere.Import.Ektron.Tools;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public class MetadataDestination : DestinationBase
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<MetadataDestination>();

        #endregion

        #region Constructors and Destructors

        public MetadataDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
        }

        #endregion

        #region Public Methods

        public override bool Receive(DataTable data)
        {
            Log.InfoFormat("Received data table with name '{0}'", data.TableName);

            Data = data;
            ValidateTable();
            UpdateMetaData();

            return true;
        }

        #endregion

        #region Methods

        protected override void GetExistingContentFilters(ContentCriteria criteria)
        {
            criteria.ReturnMetadata = true;
            base.GetExistingContentFilters(criteria);
        }

        protected override bool TableHasValidSchema()
        {
            bool tableHasValidSchema = DestinationHelper.HasColumn(Data, "contentId", typeof(long));
            Log.DebugFormat("Table has valid schema: {0}", tableHasValidSchema);
            return tableHasValidSchema;
        }

        private void UpdateMetaData()
        {
            Log.InfoFormat("Updating metadata", Data.TableName);

            List<ContentData> contentItems = GetExistingContent();
            foreach (DataRow row in Data.Rows.Cast<DataRow>().Where(dr => !dr.IsNew()))
            {
                ContentData item = contentItems.Single(ci => (long)row["contentId"] == ci.Id);

                foreach (DataColumn column in Data.Columns.Cast<DataColumn>().Where(dc => dc.ColumnName != "contentId"))
                {
                    ContentMetaData metaData = item.MetaData.SingleOrDefault(cmd => cmd.Name == column.ColumnName);
                    if (metaData == null)
                    {
                        Log.WarnFormat(
                            "Metadata named '{0}' not found for content item {1}", column.ColumnName, item.Id);
                        continue;
                    }

                    metaData.Text = row[column].ToString();
                }

                ContentManager.Update(item);
            }
        }

        #endregion
    }
}