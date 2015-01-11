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
using SQLite.NET;

namespace WatchedSynchronizer
{

  /// <summary>
  /// The class WatchedSynchronizer.SQLiteResultCacheItem is implemented to prevent unnecessary database queries by using a simple caching mechanism.
  /// </summary>

  class SQLiteResultCacheItem
  {

    #region Declaration

    private string mKey;
    private SQLiteResultSet mSQLiteResultSet;
    private bool mIsDirty;

    #endregion

    #region Constructors

    public SQLiteResultCacheItem(string strKey, SQLiteResultSet objResultSet)
    {
      mKey = strKey;
      mSQLiteResultSet = new SQLiteResultSet();
      mSQLiteResultSet = objResultSet;
      mIsDirty = false;
    }

    #endregion

    #region Properties

    public string Key
    {
      get
      {
        return mKey;
      }
      set
      {
        mKey = value;
      }
    }

    public SQLiteResultSet ResultSet
    {
      get
      {
        return mSQLiteResultSet;
      }
      set
      {
        mSQLiteResultSet = value;
      }
    }

    public bool IsDirty
    {
      get
      {
        return mIsDirty;
      }
      set
      {
        mIsDirty = value;
      }
    }

    #endregion

  }
}
