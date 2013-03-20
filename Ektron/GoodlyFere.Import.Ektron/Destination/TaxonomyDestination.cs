#region Usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading;
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

        private const string TaxonomySeparator = ";;";

        private static readonly string[] ExcludeColumns = new[]
            {
                "contentId", "title", "folderPath", "smartFormId", "html"
            };

        private static readonly ILog Log = LogManager.GetLogger<TaxonomyDestination>();

        private readonly Dictionary<string, TaxonomyData> _taxonomyData = new Dictionary<string, TaxonomyData>();
        private readonly object _taxonomyDataLock = new object();

        #endregion

        #region Constructors and Destructors

        public TaxonomyDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
        }

        #endregion

        #region Properties

        private ITaxonomyItemManager TaxItemManager
        {
            get
            {
                ITaxonomyItemManager mgr = ObjectFactory.GetTaxonomyItemManager();
                mgr.RequestInformation.AuthenticationToken = AuthToken;
                return mgr;
            }
        }

        private ITaxonomyManager TaxManager
        {
            get
            {
                ITaxonomyManager mgr = ObjectFactory.GetTaxonomyManager();
                mgr.RequestInformation.AuthenticationToken = AuthToken;
                return mgr;
            }
        }

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
            SaveOrUpdateContentItems();

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

        protected override void RowGroupSaveOrUpdate(List<DataRow> dataRows, List<ContentData> existingItems)
        {
            foreach (DataRow row in dataRows)
            {
                ContentData item = CheckForExistingItem(row, existingItems);
                if (item != null)
                {
                    row.LogContentInfo("updating taxonomy for id {0}", item.Id);
                    UpdateItemTaxonomies(item, row);
                }
                else
                {
                    row.LogContentError("could not find existing item in path '{0}'", row["folderPath"]);
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

        private static TaxonomyItemData CheckForExistingTaxonomyDataItem(
            IEnumerable<TaxonomyItemData> existingDataItems, TaxonomyData tax, DataRow row)
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

        private void DoUpdate(
            TaxonomyItemData data, DataRow row, ContentData existingItem, short timeouts = 0, bool failOnFault = false)
        {
            try
            {
                if (HasAuthentication)
                {
                    TaxItemManager.Add(data);
                    row.LogContentInfo("updated successfully with id {0}.", existingItem.Id);
                }
                else
                {
                    row.LogContentWarn("does not have authentication.");
                }
            }
            catch (TimeoutException te)
            {
                if (timeouts < 10)
                {
                    row.LogContentInfo("update timed out.  Trying again.");
                    Thread.Sleep(1000);
                    DoUpdate(data, row, existingItem, ++timeouts);
                }
                else
                {
                    LogUpdateError(row, te);
                }
            }
            catch (FaultException fe)
            {
                if (!failOnFault
                    && fe.Message.Contains("The current user does not have permission to carry out this request"))
                {
                    Authenticate();
                    DoUpdate(data, row, existingItem, failOnFault: true);
                }
                else
                {
                    LogUpdateError(row, fe);
                }
            }
            catch (Exception ex)
            {
                LogUpdateError(row, ex);
            }
        }

        private List<TaxonomyItemData> FindExistingTaxonomyItemsForContent(ContentData item)
        {
            TaxonomyItemCriteria itemCriteria = new TaxonomyItemCriteria();
            itemCriteria.AddFilter(TaxonomyItemProperty.ItemId, CriteriaFilterOperator.EqualTo, item.Id);
            List<TaxonomyItemData> existingDataItems = TaxItemManager.GetList(itemCriteria);
            Log.InfoFormat("{1}: found {0} existing taxonomy data items", existingDataItems.Count, item.Title);
            return existingDataItems;
        }

        private TaxonomyData GetTaxonomy(string taxPath)
        {
            lock (_taxonomyDataLock)
            {
                if (_taxonomyData.ContainsKey(taxPath))
                {
                    Log.InfoFormat("Taxonomy is cached: {0}", taxPath);
                    return _taxonomyData[taxPath];
                }

                Log.InfoFormat("Taxonomy not cached: {0}", taxPath);
                TaxonomyData taxData = TaxManager.GetItem(taxPath);
                _taxonomyData.Add(taxPath, taxData);
                return taxData;
            }
        }

        private void UpdateItemTaxonomies(ContentData item, DataRow row)
        {
            List<TaxonomyItemData> existingDataItems = FindExistingTaxonomyItemsForContent(item);

            foreach (DataColumn column in TaxonomyColumns)
            {
                string[] taxPaths = row[column].ToString()
                                               .Split(
                                                   new[] { TaxonomySeparator }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string taxPath in taxPaths)
                {
                    row.LogContentInfo("setting taxonomy '{0}'", taxPath);
                    TaxonomyData tax = GetTaxonomy(taxPath);

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
                    DoUpdate(data, row, item);
                }
            }

            row.LogContentInfo("updated successfully with taxonomies.");
        }

        #endregion
    }
}