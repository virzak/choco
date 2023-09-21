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

namespace chocolatey.infrastructure.adapters
{
    using System;
    using System.IO;
    using app;
    using commandline;
    using platforms;
    using Windows.Win32.Storage.FileSystem;
    using Windows.Win32.System.Console;
    using static Windows.Win32.PInvoke;

    /// <summary>
    /// Adapter for System.Console
    /// </summary>
    public sealed class Console : IConsole
    {
        public string ReadLine()
        {
            if (!ApplicationParameters.AllowPrompts) return string.Empty;

            return System.Console.ReadLine();
        }

        public string ReadLine(int timeoutMilliseconds)
        {
            if (!ApplicationParameters.AllowPrompts) return string.Empty;

            return ReadLineTimeout.Read(timeoutMilliseconds);
        }

        public System.ConsoleKeyInfo ReadKey(bool intercept)
        {
            if (!ApplicationParameters.AllowPrompts) return new System.ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false);

            return System.Console.ReadKey(intercept);
        }

        public System.ConsoleKeyInfo ReadKey(int timeoutMilliseconds)
        {
            if (!ApplicationParameters.AllowPrompts) return new System.ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false);

            return ReadKeyTimeout.ReadKey(timeoutMilliseconds);
        }

        public TextWriter Error { get { return System.Console.Error; } }

        public TextWriter Out { get { return System.Console.Out; } }

        public void Write(object value)
        {
            System.Console.Write(value.ToStringSafe());
        }

        public void WriteLine()
        {
            System.Console.WriteLine();
        }

        public void WriteLine(object value)
        {
            System.Console.WriteLine(value);
        }

        public System.ConsoleColor BackgroundColor
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.BackgroundColor;

                return System.ConsoleColor.Black;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.BackgroundColor = value;
            }
        }

        public System.ConsoleColor ForegroundColor
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.ForegroundColor;

                return System.ConsoleColor.Gray;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.ForegroundColor = value;
            }
        }

        public int BufferWidth
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.BufferWidth;

                return GetConsoleBuffer().dwSize.X; //the current console window width
            }
            set
            {
                if (!IsOutputRedirected) System.Console.BufferWidth = value;
            }
        }

        public int BufferHeight
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.BufferHeight;

                return GetConsoleBuffer().dwSize.Y; //the current console window height
            }
            set
            {
                if (!IsOutputRedirected) System.Console.BufferHeight = value;
            }
        }

        public void SetBufferSize(int width, int height)
        {
            if (!IsOutputRedirected) System.Console.SetBufferSize(width, height);
        }

        public string Title
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.Title;

                return string.Empty;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.Title = value;
            }
        }

        public bool KeyAvailable
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.KeyAvailable;

                return false;
            }
        }

        public int CursorSize
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.CursorSize;

                return GetConsoleBuffer().dwCursorPosition.Y;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.CursorSize = value;
            }
        }

        public int LargestWindowWidth
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.LargestWindowWidth;

                return GetConsoleBuffer().dwMaximumWindowSize.X; //the max console window width
            }
        }

        public int LargestWindowHeight
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.LargestWindowHeight;

                return GetConsoleBuffer().dwMaximumWindowSize.Y; //the max console window height
            }
        }

        public int WindowWidth
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.WindowWidth;

                return GetConsoleBuffer().dwSize.X; //the current console window width
            }
            set
            {
                if (!IsOutputRedirected) System.Console.WindowWidth = value;
            }
        }

        public int WindowHeight
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.WindowHeight;

                return GetConsoleBuffer().dwSize.Y; //the current console window height
            }
            set
            {
                if (!IsOutputRedirected) System.Console.WindowHeight = value;
            }
        }

        public void SetWindowSize(int width, int height)
        {
            if (!IsOutputRedirected) System.Console.SetWindowSize(width, height);
        }

        public int WindowLeft
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.WindowLeft;

                return GetConsoleBuffer().srWindow.Left;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.WindowLeft = value;
            }
        }

        public int WindowTop
        {
            get
            {
                if (!IsOutputRedirected) return System.Console.WindowTop;

                return GetConsoleBuffer().srWindow.Top;
            }
            set
            {
                if (!IsOutputRedirected) System.Console.WindowTop = value;
            }
        }

        public void SetWindowPosition(int width, int height)
        {
            if (!IsOutputRedirected) System.Console.SetWindowPosition(width, height);
        }

        /// <remarks>
        /// Based on http://stackoverflow.com/a/20737289/18475 / http://stackoverflow.com/a/3453272/18475
        /// </remarks>
        public bool IsOutputRedirected
        {
            get
            {
                if (!IsWindows()) return false;

                return FILE_TYPE.FILE_TYPE_CHAR != GetFileType(GetStdHandle_SafeHandle(STD_HANDLE.STD_OUTPUT_HANDLE));
            }
        }

        /// <remarks>
        /// Based on http://stackoverflow.com/a/20737289/18475 / http://stackoverflow.com/a/3453272/18475
        /// </remarks>
        public bool IsErrorRedirected
        {
            get
            {
                if (!IsWindows()) return false;

                return FILE_TYPE.FILE_TYPE_CHAR != GetFileType(GetStdHandle_SafeHandle(STD_HANDLE.STD_ERROR_HANDLE));
            }
        }

        /// <remarks>
        /// Based on http://stackoverflow.com/a/20737289/18475 / http://stackoverflow.com/a/3453272/18475
        /// </remarks>
        public bool IsInputRedirected
        {
            get
            {
                if (!IsWindows()) return false;

                return FILE_TYPE.FILE_TYPE_CHAR != GetFileType(GetStdHandle_SafeHandle(STD_HANDLE.STD_INPUT_HANDLE));
            }
        }

        private bool IsWindows()
        {
            return Platform.GetPlatform() == PlatformType.Windows;
        }

        private CONSOLE_SCREEN_BUFFER_INFO GetConsoleBuffer()
        {
            var defaultConsoleBuffer = new CONSOLE_SCREEN_BUFFER_INFO
            {
                dwSize = new COORD(),
                dwCursorPosition = new COORD(),
                dwMaximumWindowSize = new COORD(),
                srWindow = new SMALL_RECT(),
                wAttributes = 0,
            };

            if (!IsWindows()) return defaultConsoleBuffer;

            CONSOLE_SCREEN_BUFFER_INFO csbi;
            if (GetConsoleScreenBufferInfo(GetStdHandle_SafeHandle(STD_HANDLE.STD_OUTPUT_HANDLE), out csbi))
            {
                // if the console buffer exists
                return csbi;
            }

            return defaultConsoleBuffer;
        }
    }
}
