using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using MediaPortal.Video.Database;

namespace WatchedSynchronizer
{

  /// <summary>
  /// The class WatchedSynchronizer.AdditionalVideoDatabase provides all necessary properties/methods to interact with a video database.
  /// </summary>

  public class AdditionalVideoDatabase
  {

    #region Declaration

    private string mDatabaseFile;
    private IVideoDatabase mDatabase;

    #endregion Declaration;

    #region Constructors

    public AdditionalVideoDatabase(string strDatabaseFile)
    {
      mDatabaseFile = strDatabaseFile;
      mDatabase = AdditionalDatabaseFactory.GetVideoDatabase(mDatabaseFile);
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

    #region Public Methods

    public void ReOpen()
    {
      Dispose();
      mDatabase = AdditionalDatabaseFactory.GetVideoDatabase(mDatabaseFile);
    }

    public void Dispose()
    {
      if (mDatabase != null)
      {
        mDatabase.Dispose();
        Log.Info("WatchedSynchronizer: Video database closed.");
      }
      mDatabase = null;
    }

    //Gets the FileId (unique key to identify an file) based on the path and filename.

    public int GetFileId(string strFilenameAndPath)
    {
      return mDatabase.GetFileId(strFilenameAndPath);
    }

    //Gets the MovieId (unique key to identify an movie) based on the path and filename.

    public int GetMovieId(string strFilenameAndPath)
    {
      return mDatabase.GetMovieId(strFilenameAndPath);
    }

    //Gets the FileIds based on the MovieId if a movie consists of several media files.

    public void GetFilesForMovie(int intMovieId, ref ArrayList alsFiles)
    {
      mDatabase.GetFilesForMovie(intMovieId, ref alsFiles);
    }

    //Gets the IMDBMovie object (containing e.g. Watched status).

    public void GetMovieInfoById(int intMovieId, ref IMDBMovie objIMDBMovie)
    {
      mDatabase.GetMovieInfoById(intMovieId, ref objIMDBMovie);
    }

    //Sets the IMDBMovie object.

    public void SetMovieInfoById(int intMovieId, ref IMDBMovie objIMDBMovie)
    {
      mDatabase.SetMovieInfoById(intMovieId, ref objIMDBMovie);
    }

    //Gets the VideoDuration for a media file.

    public int GetVideoDuration(int intFileId)
    {
      return mDatabase.GetVideoDuration(intFileId);
    }

    //Sets the VideoDuration for a media file. Not used in this plugin.

    public void SetVideoDuration(int intFileId, int intDuration)
    {
      mDatabase.SetVideoDuration(intFileId, intDuration);
    }

    //Gets MovieStopTime and bteResumeData for a media file.

    public int GetMovieStopTimeAndResumeData(int intFileId, out byte[] bteResumeData, int intBDTitle)
    {
      return mDatabase.GetMovieStopTimeAndResumeData(intFileId, out bteResumeData, intBDTitle);
    }

    //Sets MovieStopTime and bteResumeData for a media file.

    public void SetMovieStopTimeAndResumeData(int intFileId, int intStopTime, byte[] bteResumeData, int intBDTitle)
    {
      mDatabase.SetMovieStopTimeAndResumeData(intFileId, intStopTime, bteResumeData, intBDTitle);
    }

    //Clears the MovieStopTime for a media file.

    public void DeleteMovieStopTime(int intFileId)
    {
      mDatabase.DeleteMovieStopTime(intFileId);
    }

    //Gets the MovieWatchedStatus.

    public bool GetMovieWatchedStatus(int intMovieId, out int intPercent, out int intWatchedCount)
    {
      return mDatabase.GetMovieWatchedStatus(intMovieId, out intPercent, out intWatchedCount);
    }

    //Sets the MovieWatchedStatus

    public void SetMovieWatchedStatus(int intMovieId, bool bolWatched, int intPercent)
    {
      mDatabase.SetMovieWatchedStatus(intMovieId, bolWatched, intPercent);
    }

    //Sets the Watched status.

    public void SetWatched(IMDBMovie objIMDBMovie)
    {
      mDatabase.SetWatched(objIMDBMovie);
    }

    //Gets the Watched status.

    public void SetDateWatched(IMDBMovie objIMDBMovie)
    {
      mDatabase.SetDateWatched(objIMDBMovie);
    }

    //Increase the number the movie was watched.

    public void MovieWatchedCountIncrease(int intMovieId)
    {
      mDatabase.MovieWatchedCountIncrease(intMovieId);
    }

    #endregion
  }
}