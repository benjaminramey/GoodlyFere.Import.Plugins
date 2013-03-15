#region Usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common.Logging;
using Ektron.Cms;
using Ektron.Cms.Common;
using Ektron.Cms.Content;
using Ektron.Cms.Organization;
using GoodlyFere.Import.Ektron.Extensions;
using GoodlyFere.Import.Ektron.Tools;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public class TaxonomyDestination : DestinationBase
    {
        #region Constants and Fields

        private static readonly string[] ExcludeColumns = new[]
            {
                "contentId", "title", "folderPath", "smartFormId", "html"
            };

        private static readonly ILog Log = LogManager.GetLogger<TaxonomyDestination>();
        private readonly ITaxonomyItemManager _taxItemManager;
        private readonly ITaxonomyManager _taxManager;

        #endregion

        #region Constructors and Destructors

        public TaxonomyDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
            _taxItemManager = ObjectFactory.GetTaxonomyItemManager();
            _taxItemManager.RequestInformation.AuthenticationToken = AuthToken;

            _taxManager = ObjectFactory.GetTaxonomyManager();
            _taxManager.RequestInformation.AuthenticationToken = AuthToken;
        }

        #endregion

        #region Properties

        private IEnumerable<DataColumn> TaxonomyColumns
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
            Log.InfoFormat("==> Beginning Ektron taxonomy update from data table '{0}'", data.TableName);

            Data = data;
            ValidateTable();
            UpdateTaxonomy();

            Log.InfoFormat("==| Ektron taxonomy update from data table '{0}' is done.", data.TableName);
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

        protected override bool TableHasValidSchema()
        {
            bool tableHasValidSchema = DestinationHelper.HasColumn(Data, "contentId", typeof(long))
                                       && DestinationHelper.HasColumn(Data, "folderPath", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "title", typeof(string));
            Log.DebugFormat("Table has valid schema: {0}", tableHasValidSchema);
            return tableHasValidSchema;
        }

        private static TaxonomyItemData CheckForExistingTaxonomyDataItem(
            List<TaxonomyItemData> existingDataItems, TaxonomyData tax, DataRow row)
        {
            TaxonomyItemData data = existingDataItems.FirstOrDefault(d => d.TaxonomyId == tax.Id);

            if (data != null)
            {
                row.LogContentInfo(
                    "Found an existing taxonomy data item for taxonomy '{0}'",
                    tax.Name);
            }
            else
            {
                data = new TaxonomyItemData();
                row.LogContentInfo(
                    "Did not find an existing taxonomy data item for taxonomy '{0}'", tax.Name);
            }
            return data;
        }

        private List<TaxonomyItemData> FindExistingTaxonomyItemsForContent(ContentData item)
        {
            TaxonomyItemCriteria itemCriteria = new TaxonomyItemCriteria();
            itemCriteria.AddFilter(TaxonomyItemProperty.ItemId, CriteriaFilterOperator.EqualTo, item.Id);
            List<TaxonomyItemData> existingDataItems = _taxItemManager.GetList(itemCriteria);
            Log.InfoFormat("{1}: found {0} existing taxonomy data items", existingDataItems.Count, item.Title);
            return existingDataItems;
        }

        private void UpdateTaxonomy()
        {
            List<ContentData> existingItems = GetExistingContent();

            foreach (DataRow row in Data.Rows.Cast<DataRow>().Distinct(new ContentRowComparer()))
            {
                ContentData item = CheckForExistingItem(row, existingItems);
                if (item != null)
                {
                    row.LogContentInfo("updating taxonomy for id {0}", item.Id);
                    List<TaxonomyItemData> existingDataItems = FindExistingTaxonomyItemsForContent(item);

                    foreach (DataColumn column in TaxonomyColumns)
                    {
                        string[] taxPaths = row[column].ToString().Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string taxPath in taxPaths)
                        {
                            row.LogContentInfo("setting taxonomy '{0}'", taxPath);
                            TaxonomyData tax = _taxManager.GetItem(taxPath);

                            if (tax == null || tax.Id <= 0)
                            {
                                Log.WarnFormat("Could not find taxonomy with path '{0}'", taxPath);
                                continue;
                            }

                            Log.InfoFormat("Found taxonomy '{0}' with id {1}", taxPath, tax.Id);
                            TaxonomyItemData data = CheckForExistingTaxonomyDataItem(existingDataItems, tax, row);
                            data.TaxonomyId = tax.Id;
                            data.ItemId = item.Id;
                            data.ItemType = EkEnumeration.TaxonomyItemType.Content;
                            _taxItemManager.Add(data);
                        }
                    }
                }
                else
                {
                    row.LogContentError("could not find existing item in path '{0}'", row["folderPath"]);
                }
            }
        }

        #endregion
    }
}