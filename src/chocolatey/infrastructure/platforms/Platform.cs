﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.platforms
{
    using System;
    using System.ComponentModel;
    using adapters;
    using filesystem;
    using Environment = adapters.Environment;
    using static Windows.Win32.PInvoke;
    using Windows.Win32.System.SystemInformation;

    /// <summary>
    ///   OS Platform detection
    /// </summary>
    /// <remarks>
    ///   Based on http://stackoverflow.com/questions/10138040/how-to-detect-properly-windows-linux-mac-operating-systems
    /// </remarks>
    public static class Platform
    {
        private static Lazy<IEnvironment> _environmentInitializer = new Lazy<IEnvironment>(() => new Environment());
        private static Lazy<IFileSystem> _fileSystemInitializer = new Lazy<IFileSystem>(() => new DotNetFileSystem());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitializeWith(Lazy<IEnvironment> environment, Lazy<IFileSystem> file_system)
        {
            _environmentInitializer = environment;
            _fileSystemInitializer = file_system;
        }

        private static IFileSystem FileSystem
        {
            get { return _fileSystemInitializer.Value; }
        }

        private static IEnvironment Environment
        {
            get { return _environmentInitializer.Value; }
        }

        public static PlatformType GetPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return PlatformType.Mac;

                case PlatformID.Unix:
                    // Well, there are chances macOS is reported as Unix instead of macOS (MacOSX).
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (FileSystem.DirectoryExists("/Applications")
                        & FileSystem.DirectoryExists("/System")
                        & FileSystem.DirectoryExists("/Users")
                        & FileSystem.DirectoryExists("/Volumes"))
                        return PlatformType.Mac;
                    else
                        return PlatformType.Linux;
                default:
                    return PlatformType.Windows;
            }
        }

        public static Version GetVersion()
        {
            return Environment.OSVersion.Version;
        }

        public static string GetName()
        {
            switch (GetPlatform())
            {
                case PlatformType.Linux:
                    return "Linux";
                case PlatformType.Mac:
                    return "macOS";
                case PlatformType.Windows:
                    return GetWindowsVersionName(GetVersion());
                default:
                    return "";
            }
        }


        /// <summary>
        ///   Gets the name of the Windows version
        /// </summary>
        /// <param name="version">The version.</param>
        /// <remarks>Looked at http://www.csharp411.com/determine-windows-version-and-edition-with-c/</remarks>
        private unsafe static string GetWindowsVersionName(Version version)
        {
            var name = "Windows";
            var isServer = false;

            var osVersionInfo = new OSVERSIONINFOEXW
            {
                dwOSVersionInfoSize = (uint)sizeof(OSVERSIONINFOEXW)
            };
            var success = GetVersionEx((OSVERSIONINFOW*)&osVersionInfo);
            if (success)
            {
                isServer = osVersionInfo.wProductType == VER_NT_SERVER;
            }


            //https://msdn.microsoft.com/en-us/library/windows/desktop/ms724832.aspx
            //switch doesn't like a double, but a string is fine?!
            string majorMinor = version.Major + "." + version.Minor;
            switch (majorMinor)
            {
                case "10.0":
                    name = isServer ? "Windows Server 2016" : "Windows 10";
                    break;
                case "6.4":
                    name = isServer ? "Windows Server 2016" : "Windows 10";
                    break;
                case "6.3":
                    name = isServer ? "Windows Server 2012 R2" : "Windows 8.1";
                    break;
                case "6.2":
                    name = isServer ? "Windows Server 2012" : "Windows 8";
                    break;
                case "6.1":
                    name = isServer ? "Windows Server 2008 R2" : "Windows 7";
                    break;
            }

            return name;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEnvironment> environment, Lazy<IFileSystem> file_system)
            => InitializeWith(environment, file_system);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static PlatformType get_platform()
            => GetPlatform();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static Version get_version()
            => GetVersion();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_name()
            => GetName();
#pragma warning restore IDE1006
    }
}
