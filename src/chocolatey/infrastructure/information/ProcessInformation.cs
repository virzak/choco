// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.information
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using Microsoft.Win32.SafeHandles;
    using platforms;
    using Windows.Win32.Foundation;
    using Windows.Win32.Security;
    using static Windows.Win32.PInvoke;

    public sealed class ProcessInformation
    {
        public static unsafe bool UserIsAdministrator()
        {
            if (Platform.GetPlatform() != PlatformType.Windows) return false;

            var isAdmin = false;

            using (var identity = WindowsIdentity.GetCurrent())
            {
                if (identity != null)
                {
                    var principal = new WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                    // Any version of Windows less than 6 does not have UAC
                    // so bail with the answer from the above check
                    if (Platform.GetVersion().Major < 6) return isAdmin;

                    if (!isAdmin)
                    {
                        // Processes subject to UAC actually have the Administrators group
                        // stripped out from the process, and will return false for any
                        // check about being an administrator, including a check against
                        // the native `CheckTokenMembership` or `UserIsAdmin`. Instead we
                        // need to perform a not 100% answer on whether they are an admin
                        // based on if we have a split token.
                        // Crediting http://www.davidmoore.info/blog/2011/06/20/how-to-check-if-the-current-user-is-an-administrator-even-if-uac-is-on/
                        // and http://blogs.msdn.com/b/cjacks/archive/2006/10/09/how-to-determine-if-a-user-is-a-member-of-the-administrators-group-with-uac-enabled-on-windows-vista.aspx
                        // NOTE: from the latter (the original) -
                        //    Note that this technique detects if the token is split or not.
                        //    In the vast majority of situations, this will determine whether
                        //    the user is running as an administrator. However, there are
                        //    other user types with advanced permissions which may generate a
                        //    split token during an interactive login (for example, the
                        //    Network Configuration Operators group). If you are using one of
                        //    these advanced permission groups, this technique will determine
                        //    the elevation type, and not the presence (or absence) of the
                        //    administrator credentials.
                        "chocolatey".Log().Debug(@"User may be subject to UAC, checking for a split token (not 100%
 effective).");

                        TOKEN_ELEVATION_TYPE elevationType;

                        using (var token = new SafeFileHandle(identity.Token, false))
                        {

                            var successfulCall = GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenElevationType, &elevationType, sizeof(TOKEN_ELEVATION_TYPE), out var tokenInfLength);

                            if (!successfulCall)
                            {
                                "chocolatey".Log().Warn("Error during native GetTokenInformation call - {0}".FormatWith(Marshal.GetLastWin32Error()));
                            }


                            switch (elevationType)
                            {
                                // TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.
                                case TOKEN_ELEVATION_TYPE.TokenElevationTypeFull:
                                // TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.
                                case TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited:
                                    isAdmin = true;
                                    break;
                            }
                        }
                    }
                }
            }

            return isAdmin;
        }

        public static bool IsElevated()
        {
            if (Platform.GetPlatform() != PlatformType.Windows) return false;

            using (var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.Duplicate))
            {
                if (identity != null)
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }

            return false;
        }

        public static bool UserIsTerminalServices()
        {
            return Environment.GetEnvironmentVariable("SESSIONNAME").ToStringSafe().ContainsSafe("rdp-");
        }

        public static bool UserIsRemote()
        {
            return UserIsTerminalServices() || Environment.GetEnvironmentVariable("SESSIONNAME").ToStringSafe() == string.Empty;
        }

        public static bool UserIsSystem()
        {
            if (Platform.GetPlatform() != PlatformType.Windows) return false;

            var isSystem = false;

            using (var identity = WindowsIdentity.GetCurrent())
            {
                isSystem = identity.IsSystem;
            }

            return isSystem;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool user_is_administrator()
            => UserIsAdministrator();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool process_is_elevated()
            => IsElevated();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool user_is_terminal_services()
            => UserIsTerminalServices();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool user_is_remote()
            => UserIsRemote();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool user_is_system()
            => UserIsSystem();
#pragma warning restore IDE1006
    }
}
