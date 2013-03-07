#region Usings

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using Common.Logging;
using Ektron.Cms;
using Ektron.Cms.Content;
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
            foreach (DataRow row in Data.Rows.Cast<DataRow>())
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