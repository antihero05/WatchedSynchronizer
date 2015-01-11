#region GPL License

//Mediaportal (http://www.team-mediaportal.com/) Plugin - WatchedSynchronizer
//Copyright (C) 2015 Max Wimmelbacher
//
//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; either version 2
//of the License, or (at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Video.Database;

namespace WatchedSynchronizer
{
  /// <summary>
  /// The class WatchedSynchronizer.AdditionalDatabaseFactory is providing static methods to get database objects.
  /// These returned objects are mapped to the database residing on the path provided in the input parameter strDatabaseFile.
  /// </summary>
  class AdditionalDatabaseFactory
  {

    #region Public Methods

    public static IVideoDatabase GetVideoDatabase(string strDatabaseFile)
    {
      var objAdditionalDatabase = new AdditionalVideoDatabaseSQLite(strDatabaseFile);
      return objAdditionalDatabase.Instance;
    }

    public static AdditionalTVSeriesDatabaseSQLite GetTVSeriesDatabase(string strDataBaseFile)
    {
      var objAdditionalDatabase = new AdditionalTVSeriesDatabaseSQLite(strDataBaseFile);
      return objAdditionalDatabase;
    }

    #endregion

  }
}