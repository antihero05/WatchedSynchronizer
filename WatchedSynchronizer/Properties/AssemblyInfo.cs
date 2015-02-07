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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MediaPortal.Common.Utils;

[assembly: AssemblyTitle("WatchedSynchronizer")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("WatchedSynchronizer")]
[assembly: AssemblyCopyright("Copyright ©  2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: CompatibleVersion("1.7.0.0","1.6.0.0")]
[assembly: UsesSubsystem("MP",true)]
[assembly: UsesSubsystem("MP.Config", true)]
[assembly: UsesSubsystem("MP.DB.Videos",true)]
[assembly: UsesSubsystem("MP.Externals.SQLite",true)]
[assembly: UsesSubsystem("MP.Players",true)]
[assembly: UsesSubsystem("MP.Plugins",true)]

[assembly: ComVisible(false)]

[assembly: Guid("b298ce34-f556-44cc-a131-83ec57fd6a50")]

[assembly: AssemblyVersion("0.5.4.4")]
[assembly: AssemblyFileVersion("0.5.4.4")]
