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
using Ektron.Cms.Common;
using Ektron.Cms.Content;
using Ektron.Cms.Framework.Organization;
using GoodlyFere.Import.Ektron.Extensions;
using GoodlyFere.Import.Ektron.Tools;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public class ContentDestination : DestinationBase
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<ContentDestination>();
        private readonly Dictionary<string, long> _folderIds;

        #endregion

        #region Constructors and Destructors

        public ContentDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
            _folderIds = new Dictionary<string, long>();
        }

        #endregion

        #region Public Methods

        public override bool Receive(DataTable data)
        {
            Log.InfoFormat("Received data table with name '{0}'", data.TableName);

            Data = data;
            ValidateTable();
            SaveOrUpdateContentItems();

            return true;
        }

        #endregion

        #region Methods

        protected static FolderData GetFolderData(string folderName)
        {
            Log.InfoFormat("Getting folder data for folder with name '{0}'", folderName);

            FolderManager fm = new FolderManager();
            FolderCriteria folderCrit = new FolderCriteria();
            folderCrit.AddFilter(
                FolderProperty.FolderName,
                CriteriaFilterOperator.EqualTo,
                folderName);

            FolderData folder = fm.GetList(folderCrit).FirstOrDefault();
            return folder;
        }

        protected override void GetExistingContentFilters(ContentCriteria criteria)
        {
            base.GetExistingContentFilters(criteria);

            foreach (DataRow row in Data.Rows.Cast<DataRow>().Where(dr => dr.IsNew()))
            {
                CriteriaFilterGroup<ContentProperty> group = new CriteriaFilterGroup<ContentProperty>();
                group.Condition = LogicalOperation.And;

                group.AddFilter(ContentProperty.Title, CriteriaFilterOperator.EqualTo, row["title"].ToString());
                group.AddFilter(
                    ContentProperty.FolderName, CriteriaFilterOperator.EqualTo, row["folderName"].ToString());

                criteria.FilterGroups.Add(group);
            }
        }

        protected long GetFolderId(DataRow row)
        {
            Log.InfoFormat("Getting folder id for folder with name '{0}'", row["folderName"]);

            string folderName = row["folderName"].ToString();
            if (_folderIds.ContainsKey(folderName))
            {
                Log.DebugFormat("Folder id was cached: {0}", _folderIds[folderName]);
                return _folderIds[folderName];
            }

            var folder = GetFolderData(folderName);
            if (folder != null)
            {
                _folderIds.Add(folderName, folder.Id);
                Log.DebugFormat("Folder id was found and added to cache: {0}", folder.Id);
                return folder.Id;
            }

            Log.DebugFormat("Did not find folder with name '{0}'", folderName);
            return -1;
        }

        /// <summary>
        ///     Saves a new content item with the values from the data row.
        ///     The 'title' and 'html' fields are saved. The item is placed
        ///     in the folder found using the 'folderName' column value in the data row.
        /// </summary>
        /// <param name="row">Row with content data.</param>
        protected virtual void SaveContent(DataRow row)
        {
            Log.InfoFormat("Saving content with title '{0}' in folder '{1}'", row["title"], row["folderName"]);
            long folderId = GetFolderId(row);

            if (folderId <= 0)
            {
                return;
            }

            ContentData content = new ContentData();
            SetContentFields(row, content);
            content.FolderId = folderId;

            try
            {
                ContentManager.Add(content);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to add content with title '{0}'", ex, row["title"]);
            }
        }

        protected virtual void SetContentFields(DataRow row, ContentData contentItem)
        {
            contentItem.Title = row["title"].ToString();
            contentItem.Html = row["html"].ToString();
        }

        protected override bool TableHasValidSchema()
        {
            bool tableHasValidSchema = DestinationHelper.HasColumn(Data, "html", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "folderName", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "title", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "contentId", typeof(long));
            Log.DebugFormat("Table has valid schema: {0}", tableHasValidSchema);
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
            Log.InfoFormat("Updating content with title '{0}'", row["title"]);
            SetContentFields(row, existingItem);

            try
            {
                ContentManager.Update(existingItem);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat(
                    "Failed to update content with title '{0}' and id {1}", ex, row["title"], row["contentId"]);
            }
        }

        private static ContentData CheckForExistingItem(DataRow row, IEnumerable<ContentData> existingItems)
        {
            if (row.IsNew())
            {
                string title = row["title"].ToString();
                string folderName = row["folderName"].ToString();

                return existingItems.FirstOrDefault(ei => ei.Title == title && ei.FolderName == folderName);
            }

            long id = (long)row["contentId"];
            return existingItems.FirstOrDefault(ei => ei.Id == id);
        }

        private void SaveOrUpdateContentItems()
        {
            List<ContentData> existingItems = GetExistingContent();
            foreach (var row in Data.Rows.Cast<DataRow>().Distinct(new ContentRowComparer()))
            {
                var existingItem = CheckForExistingItem(row, existingItems);
                if (existingItem != null)
                {
                    UpdateContent(row, existingItem);
                }
                else
                {
                    SaveContent(row);
                }
            }
        }

        #endregion
    }
}