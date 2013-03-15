#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContentDataRowExtensions.cs">
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
using GoodlyFere.Import.Ektron.Destination;

#endregion

namespace GoodlyFere.Import.Ektron.Extensions
{
    public static class ContentDataRowExtensions
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<DestinationBase>();

        #endregion

        #region Public Methods

        public static bool IsNew(this DataRow contentRow)
        {
            return contentRow["contentId"] == DBNull.Value
                   || (long)contentRow["contentId"] <= 0;
        }

        public static void LogContentInfo(this DataRow row, string message, params object[] arguments)
        {
            Log.InfoFormat(
                "{0}: {1}",
                row["title"],
                String.Format(message, arguments));
        }

        public static void LogContentWarn(this DataRow row, string message, params object[] arguments)
        {
            Log.WarnFormat(
                "{0}: {1}",
                row["title"],
                String.Format(message, arguments));
        }

        public static void LogContentError(this DataRow row, string message, params object[] arguments)
        {
            Log.ErrorFormat(
                "{0}: {1}",
                row["title"],
                String.Format(message, arguments));
        }

        public static void LogContentError(this DataRow row, string message, Exception ex, params object[] arguments)
        {
            Log.ErrorFormat(
                "{0}: {1}",
                ex,
                row["title"],
                String.Format(message, arguments));
        }

        #endregion
    }
}