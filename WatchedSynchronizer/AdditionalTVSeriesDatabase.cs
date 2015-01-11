using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;

namespace WatchedSynchronizer
{

  /// <summary>
  /// The class WatchedSynchronizer.AdditionalTVSeriesDatabase provides all necessary properties/methods to interact with a tvseries database.
  /// </summary>
  
  class AdditionalTVSeriesDatabase
  {
    #region Declaration

    private string mDatabaseFile;
    private AdditionalTVSeriesDatabaseSQLite mDatabase;

    #endregion Declaration;

    #region Constructors

    public AdditionalTVSeriesDatabase(string strDatabaseFile)
    {
      mDatabaseFile = strDatabaseFile;
      mDatabase = AdditionalDatabaseFactory.GetTVSeriesDatabase(mDatabaseFile);
    }

    #endregion

    #region Properties

    public string DatabaseName
    {
      get
      {
        if (mDatabase != null)
        {
          return mDatabase.DatabaseName;
        }
        return string.Empty;
      }
    }

    #endregion

    #region Public methods

    public void ReOpen()
    {
      Dispose();
      mDatabase = AdditionalDatabaseFactory.GetTVSeriesDatabase(mDatabaseFile);
    }

    public void Dispose()
    {
      if (mDatabase != null)
      {
        mDatabase.Dispose();
        Log.Info("WatchedSynchronizer: Tv series database closed.");
      }
      mDatabase = null;
    }

    //Gets the CompositeId (unique key to identify an episode) based on the path and filename.

    public string GetCompositeId(string strFilenameAndPath)
    {
      return mDatabase.GetCompositeId(strFilenameAndPath);
    }

    //Gets the CompositeId2 based on the CompositeId if a media file contains two episodes. Seems currently unused.

    public void GetCompositeIdsForEpisode(string strCompositeId, ref ArrayList alsCompositeIds)
    {
      string strCompositeId2 = mDatabase.GetCompositeId2(strCompositeId);
      if (strCompositeId2 == string.Empty)
      {
        alsCompositeIds.Add(strCompositeId);
      }
      else
      {
        mDatabase.GetCompositeIds(strCompositeId2, ref alsCompositeIds);
      }
    }

    //Sets the StopTime of the episode if it wasn't watched to the end.

    public void SetEpisodeStopTime(string strCompositeId, int intStopTime)
    {
      mDatabase.SetEpisodeStopTime(strCompositeId, intStopTime);
    }

    //Gets the WatchedStatus of the episode.

    public void GetEpisodeWatchedStatus(string strCompositeId, out bool bolWatched, out string strWatchedDate, out int intStopTime)
    {
      bolWatched = mDatabase.GetEpisodeWatchedStatus(strCompositeId);
      strWatchedDate = mDatabase.GetEpisodeWatchedDate(strCompositeId);
      intStopTime = mDatabase.GetEpisodeStopTime(strCompositeId);
    }

    //Sets the WatchedStatus of the episode.

    public void SetEpisodeWatchedStatus(string strCompositeId, bool bolWatched, string strWatchedDate, int intStopTime, bool bolFirstWatched = false)
    {
      if (bolWatched == true && bolFirstWatched == true)
      {
        mDatabase.SetEpisodeFirstWatchedDate(strCompositeId, strWatchedDate);
        mDatabase.SetEpisodeLastWatchedDate(strCompositeId, strWatchedDate);
      }
      else if (bolWatched == true)
      {
        mDatabase.SetEpisodeLastWatchedDate(strCompositeId, strWatchedDate);
      }
      mDatabase.SetEpisodeWatchedStatus(strCompositeId, bolWatched);
      mDatabase.SetEpisodeWatchedDate(strCompositeId, strWatchedDate);
      mDatabase.SetEpisodeStopTime(strCompositeId, intStopTime);
    }

    //Gets WatchedStatus of the season.

    public void GetSeasonWatchedStatus(string strCompositeId, out string strSeasonId, out bool bolSeasonWatched)
    {
      strSeasonId = mDatabase.GetSeasonId(strCompositeId);
      bolSeasonWatched = mDatabase.GetSeasonWatchedStatus(strSeasonId);
    }

    //Gets the WatchedStatus of the series.

    public void GetSeriesWatchedStatus(string strCompositeId, out string strSeriesId, out bool bolSeasonWatched)
    {
      strSeriesId = mDatabase.GetSeriesId(strCompositeId);
      bolSeasonWatched = mDatabase.GetSeriesWatchedStatus(strSeriesId);
    }

    //Decreases the number of the unwatched episodes of a season.

    public void SeasonUnWatchedCountDecrease(string strSeasonId)
    {
      int intUnwatchedEpisodes = mDatabase.GetSeasonUnwatchedEpisodes(strSeasonId);
      if (intUnwatchedEpisodes == 1)
      {
        mDatabase.SetSeasonUnwatchedEpisodes(strSeasonId, 0);
        mDatabase.SetSeasonWatchedStatus(strSeasonId, true);
      }
      else
      {
        mDatabase.SetSeasonUnwatchedEpisodes(strSeasonId, intUnwatchedEpisodes - 1);
      }
    }

    //Decreases the number of the unwatched episodes of a series.

    public void SeriesUnWatchedCountDecrease(string strSeriesId)
    {
      int intUnwatchedEpisodes = mDatabase.GetSeriesUnwatchedEpisodes(strSeriesId);
      if (intUnwatchedEpisodes == 1)
      {
        mDatabase.SetSeriesUnwatchedEpisodes(strSeriesId, 0);
        mDatabase.SetSeriesWatchedStatus(strSeriesId, true);
      }
      else
      {
        mDatabase.SetSeriesUnwatchedEpisodes(strSeriesId, intUnwatchedEpisodes - 1);
      }
    }

    #endregion

  }
}
