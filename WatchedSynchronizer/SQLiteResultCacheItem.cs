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
