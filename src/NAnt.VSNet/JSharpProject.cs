// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Laurent Arnal (laurent@clae.net)

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class JSharpProject : ManagedProjectBase {
        #region Public Instance Constructors

        public JSharpProject(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) : base(solution, projectPath, xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir) {
        }

        #endregion Public Instance Constructors

        #region Override implementation of ProjectBase

        /// <summary>
        /// Gets the type of the project.
        /// </summary>
        /// <value>
        /// The type of the project.
        /// </value>
        public override ProjectType Type {
            get { return ProjectType.CSharp; }
        }

        /// <summary>
        /// Verifies whether the specified XML fragment represents a valid project
        /// that is supported by this <see cref="ProjectBase" />.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project file.</param>
        /// <exception cref="BuildException">
        ///   <para>The XML fragment is not supported by this <see cref="ProjectBase" />.</para>
        ///   <para>-or-</para>
        ///   <para>The XML fragment does not represent a valid project (for this <see cref="ProjectBase" />).</para>
        /// </exception>
        protected override void VerifyProjectXml(XmlElement docElement) {
            if (!IsSupported(docElement)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project '{0}' is not a valid J# project.", ProjectPath),
                    Location.UnknownLocation);
            }
        }

        /// <summary>
        /// Returns the Visual Studio product version of the specified project
        /// XML fragment.
        /// </summary>
        /// <param name="docElement">The document element of the project.</param>
        /// <returns>
        /// The Visual Studio product version of the specified project XML 
        /// fragment.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The product version could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The product version is not supported.</para>
        /// </exception>
        protected override ProductVersion DetermineProductVersion(XmlElement docElement) {
            return GetProductVersion(docElement.SelectSingleNode("./VISUALJSHARP"));
        }

        /// <summary>
        /// Prepares the project for being built.
        /// </summary>
        /// <param name="config">The configuration in which the project will be built.</param>
        /// <remarks>
        /// Ensures the configuration-level object directory exists and ensures
        /// that none of the output files are marked read-only.
        /// </remarks>
        protected override void Prepare(ConfigurationBase config) {
            // Visual J#.NET uses the <project dir>\obj\<configuration>
            // as working directory, so we should do the same to make
            // sure relative paths are resolved correctly
            // (eg. AssemblyKeyFile attribute)

            // ensure configuration-level object directory exists
            if (!config.ObjectDir.Exists) {
                config.ObjectDir.Create();
                config.ObjectDir.Refresh();
            }
        }

        /// <summary>
        /// Returns a <see cref="ProcessStartInfo" /> for launching the compiler
        /// for this project.
        /// </summary>
        /// <param name="config">The configuration to build.</param>
        /// <param name="responseFile">The response file for the compiler.</param>
        /// <returns>
        /// A <see cref="ProcessStartInfo" /> for launching the compiler for
        /// this project.
        /// </returns>
        protected override ProcessStartInfo GetProcessStartInfo(ConfigurationBase config, string responseFile) {
            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(SolutionTask.
                Project.TargetFramework.FrameworkDirectory.FullName, "vjc.exe"),
                "@\"" + responseFile + "\"");
            psi.WorkingDirectory = config.ObjectDir.FullName;
            return psi;
        }

        #endregion Override implementation of ProjectBase

        #region Public Static Methods

        /// <summary>
        /// Returns a value indicating whether the project represented by the
        /// specified XML fragment is supported by <see cref="JSharpProject" />.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project to check.</param>
        /// <returns>
        /// <see langword="true" /> if <see cref="CSharpProject" /> supports
        /// the specified project; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// <para>
        /// A project is identified as as J# project, if the XML fragment at
        /// least has the following information:
        /// </para>
        /// <code>
        ///   <![CDATA[
        /// <VisualStudioProject>
        ///     <JSHARP
        ///         ProductVersion="..."
        ///         ....
        ///     >
        ///         ...
        ///     </JSHARP>
        /// </VisualStudioProject>
        ///   ]]>
        /// </code>
        /// </remarks>
        public static bool IsSupported(XmlElement docElement) {
            if (docElement == null) {
                return false;
            }

            if (docElement.Name != "VisualStudioProject") {
                return false;
            }

            XmlNode projectNode = docElement.SelectSingleNode("./VISUALJSHARP");
            if (projectNode == null) {
                return false;
            }

            try {
                ProductVersion productVersion = GetProductVersion(projectNode);
                // no need to perform version check here as this is done in 
                // GetProductVersion
            } catch {
                // product version could not be determined or is not supported
                return false;
            }

            return true;
        }

        #endregion Public Static Methods
    }
}
