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

        private static readonly string[] ExcludeColumns = new[]
            {
                "contentId", "title", "folderPath", "smartFormId", "html"
            };

        private static readonly ILog Log = LogManager.GetLogger<MetadataDestination>();

        #endregion

        #region Constructors and Destructors

        public MetadataDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
        }

        #endregion

        #region Properties

        private IEnumerable<DataColumn> MetadataColumns
        {
            get
            {
                return Data.Columns.Cast<DataColumn>().Where(dc => !ExcludeColumns.Contains(dc.ColumnName));
            }
        }

        #endregion

        #region Public Methods

        public override bool Receive(DataTable data)
        {
            Log.InfoFormat("==> Beginning Ektron metadata update from data table '{0}'", data.TableName);

            Data = data;
            ValidateTable();
            SaveOrUpdateContentItems();

            Log.InfoFormat("==| Ektron metadata update from data table '{0}' is done.", data.TableName);
            return true;
        }

        #endregion

        #region Methods

        protected override void GetExistingContentFilters(ContentCriteria criteria)
        {
            criteria.ReturnMetadata = true;
            base.GetExistingContentFilters(criteria);
            DestinationHelper.BuildTitleAndPathGroups(Data, criteria);
        }

        protected override void RowGroupSaveOrUpdate(List<DataRow> dataRows, List<ContentData> existingItems)
        {
            foreach (var row in dataRows)
            {
                ContentData item = CheckForExistingItem(row, existingItems);
                if (item != null)
                {
                    row.LogContentInfo("updating metadata for id {0}", item.Id);
                    UpdateItemMetadata(item, row);
                }
                else
                {
                    row.LogContentError(
                        "could not find existing item in path '{0}'",
                        row["folderPath"]);
                }
            }
        }

        protected override bool TableHasValidSchema()
        {
            bool tableHasValidSchema = DestinationHelper.HasColumn(Data, "contentId", typeof(long))
                                       && DestinationHelper.HasColumn(Data, "folderPath", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "title", typeof(string));
            Log.InfoFormat("Table has valid schema: {0}", tableHasValidSchema);
            return tableHasValidSchema;
        }

        private void UpdateItemMetadata(ContentData item, DataRow row)
        {
            foreach (DataColumn column in MetadataColumns)
            {
                ContentMetaData metaData = item.MetaData.SingleOrDefault(cmd => cmd.Name == column.ColumnName);
                if (metaData == null)
                {
                    row.LogContentWarn(
                        "metadata named '{0}' not found",
                        column.ColumnName);
                    continue;
                }

                row.LogContentInfo("setting metadata field '{0}' to '{1}'", metaData.Name, row[column]);
                metaData.Text = row[column].ToString();
            }

            DoContentUpdate(row, item);
        }

        #endregion
    }
}