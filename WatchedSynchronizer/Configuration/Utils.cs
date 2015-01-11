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


  /// <summary>
  /// The classes WatchedSynchronizer.Configuration.WatchedSynchronizerConfiguration and WatchedSynchronizer.Configuration.WatchedSynchronizerConfigurationEntry
  /// are used for storing and serializing the plugin configuration.
  /// </summary>

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
