using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace WatchedSynchronizer.Configuration
{

  /// <summary>
  /// The class WatchedSynchronizer.Configuration.Utils contains methods for serialization.
  /// This is used to store the plugin configuration.
  /// </summary>

  public static class Utils
  {

    #region Public methods

    public static string Serialize(WatchedSynchronizerConfiguration objConfiguration)
    {
      string strConfiguration = Convert.ToBase64String(ObjectToByteArray(objConfiguration));
      return strConfiguration;
    }

    public static WatchedSynchronizerConfiguration Deserialize(string strConfiguration)
    {
      WatchedSynchronizerConfiguration objConfiguration = ByteArrayToObject(Convert.FromBase64String(strConfiguration)) as WatchedSynchronizerConfiguration;
      return objConfiguration;
    }

    #endregion

    #region Private methods

    private static byte[] ObjectToByteArray(Object objObject)
    {
      if (objObject == null)
      {
        return null;
      }
      BinaryFormatter bfoBinaryFormatter = new BinaryFormatter();
      MemoryStream mstMemoryStream = new MemoryStream();
      bfoBinaryFormatter.Serialize(mstMemoryStream, objObject);
      return mstMemoryStream.ToArray();
    }

    private static Object ByteArrayToObject(byte[] bteBytes)
    {
      MemoryStream mstMemoryStream = new MemoryStream();
      BinaryFormatter bfoBinaryFormatter = new BinaryFormatter();
      mstMemoryStream.Write(bteBytes, 0, bteBytes.Length);
      mstMemoryStream.Seek(0, SeekOrigin.Begin);
      Object objObject = (Object)bfoBinaryFormatter.Deserialize(mstMemoryStream);
      return objObject;
    }

    #endregion

  }

  //The classes WatchedSynchronizer.Configuration.WatchedSynchronizerConfiguration and WatchedSynchronizer.Configuration.WatchedSynchronizerConfigurationEntry
  //are used for storing and serializing the plugin configuration.

  [Serializable()]
  public class WatchedSynchronizerConfiguration
  {
    public List<WatchedSynchronizerConfigurationEntry> mConfigurationEntries;
  }

  [Serializable()]
  public class WatchedSynchronizerConfigurationEntry
  {
    public bool mEnabled;
    public string mDatabaseType;
    public string mDatabasePath;
  }
}
