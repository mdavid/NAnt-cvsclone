// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Tom Jordan (tdjordan@users.sourceforge.net)

using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Compiles Visual J# programs using vjc, Microsoft's J# compiler.
    /// </summary>
    /// <example>
    ///   <para>Compile <c>helloworld.jsl</c> to <c>helloworld.exe</c>.</para>
    ///   <code>
    ///     <![CDATA[
    ///<vjc target="exe" output="helloworld.exe" debug="true">
    ///   <sources>
    ///      <includes name="helloworld.jsl"/>
    ///   </sources>
    ///</vjc>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("vjc")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class VjcTask : CompilerBase {
        #region Private Instance Fields

        bool _secureScoping = false;
        string _x = null;
        string _libPath = null;
        string _jcpa = null;
        string _codepage = null;
        string _warningLevel = null;
        string _noWarn = null;

        #endregion Private Instance Fields
           
        #region Public Instance Properties

        /// <summary>
        /// Specifies whether package-scoped members are accessible outside of the assembly.
        /// In other words, package scope is treated as assembly scope when emitting metadata.
        /// <para>By default, secure scoping is off.</para>
        /// <para>Corresponds to the <c>/securescoping</c> flag.</para>
        /// </summary>
        /// <value>
        /// <c>true</c> if the option should be passed to the compiler; otherwise,
        /// <c>false</c>.
        /// </value>
        /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfsecurescopingmakepackage-scopedmembersinaccessibleoutsideassembly.htm">See the Visual J# Reference for details.</a></remarks>
        /// <example>
        /// <code><![CDATA[<vjc securescoping='true'/>]]></code>
        /// </example>
        [TaskAttribute("securescoping")]
        [BooleanValidator()]
        public bool SecureScoping {
            get { return _secureScoping; }
            set { _secureScoping = value; }
        }
       
        /// <summary>
        /// Specifies whether to disable language extensions.
        /// <para>Corresponds to the <c>/x</c> flag.</para>
        /// </summary>
        /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfxdisablelanguageextensions.htm">See the Visual J# Reference for details.</a></remarks>
        /// <value>
        /// <para>The value of this property must be either <c>all</c>, <c>net</c>, or an empty string.</para>
        /// <para>Note: <c>net</c> disables only .NET Framework extensions while <c>all</c> also disables VJ++ 6.0 extensions.</para>
        /// </value>
        /// <example>
        /// <para>To disable only the .NET Framework extensions:<c><![CDATA[
        /// <vjc x='net'/>
        /// ]]></c></para>
        /// <para>To disable the .NET Framework extensions and the VJ++ 6.0 extensions:<c><![CDATA[
        /// <vjc x='all'/>
        /// ]]></c></para>
        /// </example>
        [TaskAttribute("x")]
        public string X {
            get { return _x; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _x = value;
                } else {
                    _x = null;
                }
            }
        }
       
        /// <summary>
        /// Specifies the location of assemblies referenced by way of the <c>/reference</c> flag.
        /// <para>Corresponds to the <c>/libpath:dir[;dir2]</c> flag.</para>
        /// </summary>
        /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrflibpathspecifyassemblyreferencelocations.htm">See the Visual J# Reference for details.</a></remarks>
        /// <value>
        /// <para>The value of this property must exist or an empty string.</para>
        /// <para>If <c>false</c>, or an empty string, the switch is omitted.</para>
        /// </value>
        [TaskAttribute("libpath")]
        public string LibPath {
            get { return _libPath; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _libPath = value;
                } else {
                    _libPath = null;
                }
            }
        }
       
        /// <summary>
        /// Associate Java-language/COM package names.
        /// <para>Corresponds to the <c>/jcpa:package=namespace</c> and <c>/jcpa:@filename</c> flags.</para>
        /// </summary>
        /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfjcpaassociatejava-compackages.htm">See the Visual J# Reference for details.</a></remarks>
        /// <value>
        /// <para>The value of this propery. must be <c>package=namespace</c>, <c>@filename</c>, or an empty string.</para>
        /// </value>
        /// <example>
        /// <para>Map package 'x' to namespace 'y':<c><![CDATA[
        /// <vjc jcpa='x=y'/>
        /// ]]></c></para>
        /// </example>
        [TaskAttribute("jcpa")]
        public string Jcpa {
            get { return _jcpa; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _jcpa = value;
                } else {
                    _jcpa = null;
                }
            }
        }
       
        /// <summary>
        /// Specifies the code page to use for all source code files in the compilation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/codepage</c> flag.
        /// </para>
        /// <para>
        /// <a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjlrfcodepagespecifycodepageforsourcecodefiles.htm">See the Visual J# Reference for details.</a>
        /// </para>
        /// </remarks>
        [TaskAttribute("codepage")]
        public string Codepage {
            get { return _codepage; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _codepage = value;
                } else {
                    _codepage = null;
                }
            }
        }

        /// <summary>
        /// Specifies the warning level for the compiler to display. Valid values are 0-4. Default is 4.
        /// </summary>
        /// <value>The warning level for the compiler to display.</value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/warn</c> option.
        /// </para>
        /// </remarks>
        [TaskAttribute("warninglevel")]
        [Int32Validator(0, 4)]
        public string WarningLevel {
            get { return _warningLevel; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _warningLevel = value;
                } else {
                    _warningLevel = null;
                }
            }
        }

        /// <summary>
        /// Specifies a comma-separated list of warnings that should be suppressed 
        /// by the compiler.
        /// </summary>
        /// <value>Comma-separated list of warnings that should be suppressed by the compiler.</value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/nowarn</c> option.
        /// </para>
        /// </remarks>
        [TaskAttribute("nowarn")]
        public string NoWarn {
            get { return _noWarn; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _noWarn = value;
                } else {
                    _noWarn = null;
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of CompilerBase

        /// <summary>
        /// Writes the compiler options to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer"><see cref="TextWriter" /> to which the compiler options should be written.</param>
        protected override void WriteOptions(TextWriter writer) {
            // handle secure scoping.
            if (SecureScoping) {
                WriteOption(writer, "securescoping"); 
            }

            // handle the disable framework extensions option.
            if (X != null) {
                WriteOption(writer, "x", X);
            }

            // handle the libpath option.
            if (LibPath != null ) {
                WriteOption(writer, "libpath", LibPath);
            }

            // handle the jcpa option.
            if (Jcpa != null) {
                WriteOption(writer, "jcpa", Jcpa);
            }

            // handle the codepage option.
            if (Codepage != null) {
                WriteOption(writer, "codepage", Codepage);
            }

            // handle debug builds.
            if (Debug) {
                WriteOption(writer, "debug");
                WriteOption(writer, "define", "DEBUG");
                WriteOption(writer, "define", "TRACE");
            }

            if (WarningLevel != null) {
                WriteOption(writer, "warn", WarningLevel);
            }

            if (NoWarn != null) {
                WriteOption(writer, "nowarn", NoWarn);
            }
        }

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>For the J# compiler, the file extension is always <c>jsl</c>.</value>
        protected override string Extension {
            get { return "jsl"; }
        }

        #endregion Override implementation of CompilerBase
    }
}
