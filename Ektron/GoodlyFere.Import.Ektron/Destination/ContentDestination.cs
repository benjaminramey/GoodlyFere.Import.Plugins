#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContentDestination.cs">
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
    public class ContentDestination : DestinationBase
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<ContentDestination>();
        private readonly FolderUtil _folderUtil;

        #endregion

        #region Constructors and Destructors

        public ContentDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
            _folderUtil = new FolderUtil();
        }

        #endregion

        #region Public Methods

        public override bool Receive(DataTable data)
        {
            Log.InfoFormat("==> Beginning Ektron content push from data table '{0}'", data.TableName);

            Data = data;
            ValidateTable();
            SaveOrUpdateContentItems();

            Log.InfoFormat("==| Ektron content push from data table '{0}' is done.", data.TableName);
            return true;
        }

        #endregion

        #region Methods

        protected override void GetExistingContentFilters(ContentCriteria criteria)
        {
            base.GetExistingContentFilters(criteria);

            DestinationHelper.BuildTitleAndPathGroups(Data, criteria);
        }

        protected virtual ContentData GetNewContentDataObject()
        {
            return new ContentData();
        }

        protected override void RowGroupSaveOrUpdate(List<DataRow> dataRows, List<ContentData> existingItems)
        {
            foreach (var row in dataRows)
            {
                var existingItem = CheckForExistingItem(row, existingItems);
                if (existingItem != null)
                {
                    row.LogContentInfo("exists, going to update.");
                    UpdateContent(row, existingItem);
                }
                else
                {
                    row.LogContentInfo("does not exist, going to save.");
                    SaveContent(row);
                }
            }
        }

        /// <summary>
        ///     Saves a new content item with the values from the data row.
        ///     The 'title' and 'html' fields are saved. The item is placed
        ///     in the folder found using the 'folderPath' column value in the data row.
        /// </summary>
        /// <param name="row">Row with content data.</param>
        protected virtual void SaveContent(DataRow row)
        {
            row.LogContentInfo("saving in folder '{0}'", row["folderPath"]);
            long folderId = _folderUtil.GetFolderId(row);

            if (folderId <= 0)
            {
                row.LogContentWarn("can't save item because no folder was found for it.");
                return;
            }

            ContentData content = GetNewContentDataObject();
            SetContentFields(row, content);
            content.FolderId = folderId;

            DoContentAdd(row, content);
        }

        protected virtual void SetContentFields(DataRow row, ContentData contentItem)
        {
            contentItem.Title = row["title"].ToString();
            contentItem.Html = row["html"].ToString();
        }

        protected override bool TableHasValidSchema()
        {
            bool tableHasValidSchema = DestinationHelper.HasColumn(Data, "html", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "folderPath", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "title", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "contentId", typeof(long));
            Log.InfoFormat("Table has valid schema: {0}", tableHasValidSchema);
            return tableHasValidSchema;
        }

        /// <summary>
        ///     Updates the existing content item as opposed to adding a new one. The
        ///     'title' and 'html' fields are updated.
        /// </summary>
        /// <param name="row">Row containing data to update in existing content item.</param>
        /// <param name="existingItem">Existing content item to update.</param>
        protected virtual void UpdateContent(DataRow row, ContentData existingItem)
        {
            row.LogContentInfo("updating content with id {0}", row["contentId"]);
            SetContentFields(row, existingItem);
            DoContentUpdate(row, existingItem);
        }

        #endregion
    }
}