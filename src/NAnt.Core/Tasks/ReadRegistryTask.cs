// NAnt - A .NET build tool
// Copyright (C) 2002 Scott Hernandez
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Globalization;
using System.Security.Permissions;

using Microsoft.Win32;

using SourceForge.NAnt.Attributes;

[assembly: RegistryPermissionAttribute(SecurityAction.RequestMinimum , Unrestricted=true)]

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// A task that reads a value or set of values from the Windows Registry into one or 
    /// more NAnt properties.
    /// </summary>
    /// <remarks>
    ///     <p>
    ///         Do not use a leading slash on the key value.
    ///     </p>
    ///     <p>
    ///         Hive values can be one of the following values from the RegistryHive enum<see cref="Microsoft.Win32.RegistryHive"/>
    ///         <table>
    ///             <tr><td>LocalMachine</td><td></td></tr>
    ///             <tr><td>CurrentUser</td><td></td></tr>
    ///             <tr><td>Users</td><td></td></tr>
    ///             <tr><td>ClassesRoot</td><td></td></tr>
    ///         </table>
    ///     </p>
    /// </remarks>
    /// <example>
    ///     <para>Reads a single value from the registry</para>
    ///     <code><![CDATA[<readregistry property="sdkRoot" key="SOFTWARE\Microsoft\.NETFramework\sdkInstallRoot" hive="LocalMachine" />]]></code>
    ///     <para>Reads all the registry values in a key</para>
    ///     <code><![CDATA[<readregistry prefix="dotNetFX" key="SOFTWARE\Microsoft\.NETFramework\sdkInstallRoot" hive="LocalMachine" />]]></code>
    /// </example>
    [TaskName("readregistry")]
    public class ReadRegistryTask : Task {
        #region Private Instance Fields

        private string _propName = null;
        private string _propPrefix = null;
        private string _regKey = null;
        private string _regKeyValueName = null;
        private RegistryHive[] _regHive = {RegistryHive.LocalMachine};
        private string _regHiveString = RegistryHive.LocalMachine.ToString(CultureInfo.InvariantCulture);

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The property to set to the specified registry key value.
        /// </summary>
        [TaskAttribute("property")]
        public virtual string PropertyName {
            get { return _propName; }
            set { _propName = value; }
        }

        /// <summary>
        /// The prefix to use for the specified registry key values.
        /// </summary>
        [TaskAttribute("prefix")]
        public virtual string PropertyPrefix{
            get { return _propPrefix; }
            set { _propPrefix = value; }
        }

        /// <summary>
        /// The registry key to read.
        /// </summary>
        [TaskAttribute("key", Required=true)]
        public virtual string RegistryKey {
            get { return this._regKey; }
            set {
                string[] pathParts = value.Split("\\".ToCharArray(0,1)[0]);
                _regKeyValueName = pathParts[pathParts.Length - 1];
                _regKey = value.Substring(0, (value.Length - _regKeyValueName.Length));
            }
        }

        /// <summary>
        /// The registry hive to use.
        /// </summary>
        /// <remarks>
        /// <seealso cref="Microsoft.Win32.RegistryHive" />
        /// </remarks>
        /// <value>
        /// The enum of type <see cref="Microsoft.Win32.RegistryHive"/> values including LocalMachine, Users, CurrentUser and ClassesRoot.
        /// </value>
        [TaskAttribute("hive")]
        public virtual string RegistryHiveName {
            get { return _regHiveString; }
            set {
                _regHiveString = value;
                string[] tempRegHive = _regHiveString.Split(" ".ToCharArray()[0]);
                _regHive = (RegistryHive[]) Array.CreateInstance(typeof(RegistryHive), tempRegHive.Length);
                for (int x=0; x<tempRegHive.Length; x++) {
                    _regHive[x] = (RegistryHive) Enum.Parse(typeof(RegistryHive), tempRegHive[x], true);
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            object regKeyValue = null;

            if (_regKey == null) {
                throw new BuildException("Missing registry key!");
            }

            RegistryKey mykey = null;
            if (_propName != null) {
                mykey = LookupRegKey(_regKey, _regHive);
                regKeyValue = mykey.GetValue(_regKeyValueName);
                if (regKeyValue != null) {
                    string val = regKeyValue.ToString();
                    Properties[_propName] = val;
                } else {
                    throw new BuildException(String.Format(CultureInfo.InvariantCulture, "Registry Value Not Found! - key='{0}';hive='{1}';", _regKey + "\\" + _regKeyValueName, _regHiveString));
                }
            } else if (_propName == null && _propPrefix != null) {
                mykey = LookupRegKey(_regKey, _regHive);
                foreach (string name in mykey.GetValueNames()) {
                    Properties[_propPrefix + "." + name] = mykey.GetValue(name).ToString();
                }
            } else {
                throw new BuildException("Missing both a property name and property prefix; atleast one if required!");
            }
        }

        #endregion Override implementation of Task

        #region Protected Static Methods

        protected RegistryKey LookupRegKey(string key, RegistryHive[] registries) {
            foreach (RegistryHive hive in registries) {
                Log(Level.Verbose, "Opening {0}:{1}.", hive.ToString(CultureInfo.InvariantCulture), key);
                RegistryKey returnkey = GetHiveKey(hive).OpenSubKey(key, false);
                if (returnkey != null) {
                    return returnkey;
                }
            }
            throw new BuildException(String.Format(CultureInfo.InvariantCulture, "Registry Path Not Found! - key='{0}';hive='{1}';", key, registries.ToString()));
        }

        protected RegistryKey GetHiveKey(RegistryHive hive) {
            switch(hive) {
                case RegistryHive.LocalMachine:
                    return Registry.LocalMachine;
                case RegistryHive.Users:
                    return Registry.Users;
                case RegistryHive.CurrentUser:
                    return Registry.CurrentUser;
                case RegistryHive.ClassesRoot:
                    return Registry.ClassesRoot;
                default:
                    Log(Level.Verbose, "Registry not found for {0}.", hive.ToString(CultureInfo.InvariantCulture));
                    return null;
            }
        }

        #endregion Protected Static Methods
    }
}
