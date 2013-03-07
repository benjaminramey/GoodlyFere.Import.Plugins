#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SmartFormDestination.cs">
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
using System.Data;
using System.Linq;
using Common.Logging;
using Ektron.Cms;
using GoodlyFere.Import.Ektron.Tools;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public class SmartFormDestination : ContentDestination
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<SmartFormDestination>();

        #endregion

        #region Constructors and Destructors

        public SmartFormDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
        }

        #endregion

        #region Methods

        protected override void SetContentFields(DataRow row, ContentData contentItem)
        {
            base.SetContentFields(row, contentItem);
            contentItem.ContType = 1;
            contentItem.XmlConfiguration = new XmlConfigData { Id = (long)row["smartFormId"] };
        }

        protected override bool TableHasValidSchema()
        {
            bool hasValidSchema = base.TableHasValidSchema()
                                  && DestinationHelper.HasColumn(Data, "smartFormId", typeof(long));

            Log.DebugFormat("Data table has valid schema: {0}", hasValidSchema);
            return hasValidSchema;
        }

        #endregion
    }
}