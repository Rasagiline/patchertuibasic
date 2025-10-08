using Spectre.Console;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Loadout_Patcher_Enhanced
{
    public static class Processing
    {
        #region DllImports
        // Make sure to close this handle once done. The process won't be terminated.
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, out int numberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, out int numberOfBytesWritten);

        // Receive and be able to interpret errors that can occur when tying to open a process or read/write its memory
        // Returns an error code without message
        [DllImport("kernel32.dll")]
        private static extern int GetLastError();

        // Only GetErrorMessage(int? errorCode, char openReadWrite) should use this in order to get an error message in every case
        [DllImport("kernel32.dll")]
        private static extern int SetLastError(int lastError);

        // Gets the error message back from the system
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId,
            uint dwLanguageId, out StringBuilder msgOut, int nSize, IntPtr Arguments);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr CreateToolhelp32Snapshot([In] UInt32 dwFlags, [In] UInt32 th32ProcessID);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern bool Process32First([In] IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern bool Process32Next([In] IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle([In] IntPtr hObject);
        #endregion

        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_READ = 0x0010;
        const int PROCESS_VM_WRITE = 0x0020;

        // They are used for the output from FormatMessage
        const int ALLOCATE_BUFFER = 0x00000100;
        const int IGNORE_INSERTS = 0x00000200;
        const int FROM_SYSTEM = 0x00001000;

        // Changing the data type to int? is not allowed
        private static int numberOfBytesRead = 0;
        private static int numberOfBytesWritten = 0;

        // The variables grant more control of recovering error codes
        // GetLastError() is not perfectly reliable
        // 1. Errors that have absolutely nothing to do with ProcessMemory don't influence these variables
        // 2. Some functions automatically reset the last error to 0 on success
        // At least 1 method is responsible to clean them up as needed
        private static int? errorCodeOpening = null;
        private static int? errorCodeReading = null;
        private static int? errorCodeWriting = null;

        // uberentEndpoint, uesEndpoint and matchmakingEndpoint with their addresses
        public static readonly KeyValuePair<string, int>[] BasicEndpoints = new KeyValuePair<string, int>[3]
        {
            new KeyValuePair<string, int>( "uberent.com", 0x1015434 ),
            new KeyValuePair<string, int>( "ues.loadout.com", 0x0f438b8 ),
            new KeyValuePair<string, int>( "mm2.loadout.com", 0x1015540 )
        };

        public const int MapAddress = 0x0cc94d0;
        public const string DefaultMapReadMemory = "Shooting_Gallery_Solo";

        private static double uptimeInSec;
        // public static bool sseUser = false;

        private static List<string> webApiEndpoints;

        // Once we have a process, we don't dispose it because a closed process provides a lot of information
        private static Process? loadoutProcess;

        private static Process? loadoutParentProcess;

        // The process handle is important for reading and writing memory
        private static IntPtr? loadoutProcessHandle;

        // Loadout can't be started directly and the process that launches it can be different
        private static string? loadoutParentProcessName;

        // Path to SmartSteamEmu
        private static string? sSEPath;

        // Path to the Loadout game files
        private static string? loadoutPath;

        // Enum used only internally
        [Flags]
        private enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F,
            NoHeaps = 0x40000000
        }

        // Struct used only internally
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            const int MAX_PATH = 260;
            internal UInt32 dwSize;
            internal UInt32 cntUsage;
            internal UInt32 th32ProcessID;
            internal IntPtr th32DefaultHeapID;
            internal UInt32 th32ModuleID;
            internal UInt32 cntThreads;
            internal UInt32 th32ParentProcessID;
            internal Int32 pcPriClassBase;
            internal UInt32 dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string szExeFile;
        }

        public static Process? LoadoutProcess
        {
            get { return loadoutProcess; }
            set { loadoutProcess = value; }
        }
        public static Process? LoadoutParentProcess
        {
            get { return loadoutParentProcess; }
            set { loadoutParentProcess = value; }
        }

        public static IntPtr? LoadoutProcessHandle
        {
            get { return loadoutProcessHandle; }
            set { loadoutProcessHandle = value; }
        }

        public static double UptimeInSec
        {
            get { return uptimeInSec; }
            set { uptimeInSec = value; }
        }

        public static List<string> WebApiEndpoints
        {
            get { return webApiEndpoints; }
            set { webApiEndpoints = value; }
        }

        public static void FetchProcesses(string exeFileToLookFor, int processId = 0)
        {
            (Process?, Process?) standardAndParentProcess = (null, null);
            IntPtr handleToSnapshot = IntPtr.Zero;
            try
            {
                PROCESSENTRY32 procEntry = new();
                procEntry.dwSize = (UInt32)Marshal.SizeOf(typeof(PROCESSENTRY32));
                handleToSnapshot = CreateToolhelp32Snapshot((uint)SnapshotFlags.Process, 0);
                if (Process32First(handleToSnapshot, ref procEntry))
                {
                    while (Process32Next(handleToSnapshot, ref procEntry))
                    {
                        // Careful of Linux, probably must distinguish
                        // byte[] theProcess = UTF8Encoding.UTF8.GetBytes(procEntry.szExeFile);
                        // remove the comments if .CharSet.Auto does the job on Linux
                        //Console.WriteLine("Found: " + procEntry.szExeFile);
                        if (procEntry.szExeFile == exeFileToLookFor)
                        {
                            standardAndParentProcess = (Process.GetProcessById((int)procEntry.th32ProcessID), Process.GetProcessById((int)procEntry.th32ParentProcessID));
                            LoadoutProcess = standardAndParentProcess.Item1;
                            LoadoutParentProcess = standardAndParentProcess.Item2;
                            break;
                        }
                    }
                }
                else
                {
                    throw new ApplicationException(string.Format("[bold][white]Failed with win32 error code {0}[/][/]", Marshal.GetLastWin32Error()));
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("[bold][white]Can't get processes at all.[/][/]", ex);
            }
            finally
            {
                // Cleaning up the snapshot object!
                CloseHandle(handleToSnapshot);
            }
        }

        public static string OverwriteStringAtOffset(Process loadoutProcess, int offset, string stringToReplace, string replacementString, bool isMap = false)
        {
            // Reset the out parameter
            numberOfBytesWritten = 0;

            AnsiConsole.MarkupLine("[bold][white]> Beginning patching {0} ...[/][/]", stringToReplace);
            AnsiConsole.MarkupLine("[bold][white]> Patching {0} at {1}[/][/]", stringToReplace, offset);

            /* If we want to write the map into the memory, we want the replacementString to be exactly 29 characters long */
            /* If it's shorter, we must fill it with binary null characters */
            if (isMap)
            {
                int remainingLength = 29 - replacementString.Length;
                replacementString += new string('\u0000', remainingLength);
                replacementString = replacementString.Substring(0, 29);
            }

            /* We can then write our replacement */
            if (!WriteMemory(offset, replacementString.ToCharArray()))
            {
                var text = new Markup("[bold][white]------------------------------> Patching failed! <------------------------------[/][/]");
                // Center the text in the terminal window
                text.Justification = Justify.Center;
                // Render the text to the console
                AnsiConsole.Write(text); Console.WriteLine();
                return "";
            }
            AnsiConsole.MarkupLine("[bold][white]> Written value {0} at {1}[/][/]", replacementString, offset);

            /* We check if the value was correctly written. An error code as output is an integer provided by the system. In case of an error, the patcher will automatically restart */
            return CheckStringAtOffset(loadoutProcess, offset, replacementString, true);
        }

        public static bool WriteMemory(int offset, char[] value)
        {
            // Transforms a character array, basically a string, to an array of bytes
            // The buffer will contain the data used to overwrite
            byte[] buffer = Encoding.UTF8.GetBytes(value);

            if (!WriteProcessMemory((int)LoadoutProcessHandle!, offset, buffer, buffer.Length, out numberOfBytesWritten))
            {
                errorCodeWriting = GetLastError();
                return false;
            }
            return true;
        }

        public static string CheckStringAtOffset(Process loadoutProcess, int offset, string replacementString, bool checkForPerfectMatch = false)
        {
            // Reset the out parameter
            numberOfBytesRead = 0;

            // Checks if the process is running
            if (Process.GetProcessesByName(loadoutProcess.ProcessName).Length == 0 || loadoutProcess.HasExited)
            {
                AnsiConsole.MarkupLine("[bold][white]> Error: Couldn't find a running process called: {0}[/][/]", loadoutProcess.ProcessName);
                AnsiConsole.MarkupLine("[bold][white]> The process must have been closed.[/][/]");
                return "";
            }

            Processing.LoadoutProcessHandle = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, loadoutProcess.Id);
            errorCodeOpening = GetLastError();

            /* We obtain memory to check if it's empty. If it isn't, we convert it to a readable string */
            //byte[] memoryRead = ReadMemory(offset, replacementString.Length);
            //string memoryReadString = new (System.Text.Encoding.UTF8.GetString(memoryRead).Replace("\0", ""));

            byte[] memoryRead = ReadMemory(offset, replacementString.Length);
            /* For comparison and accurate displaying, we remove the binary null characters from the strings */
            string memoryReadString = new(System.Text.Encoding.UTF8.GetString(memoryRead).Replace("\0", ""));
            replacementString = replacementString.Replace("\0", "");

            /* We want an exact match if we want to read what we have just written. If we didn't write, we must skip this part */
            /* If we read a custom map string, it can be anything. Only reading and going in there can lead to infinite restarts of the patcher */
            if (checkForPerfectMatch)
            {
                // Consider adding null, CompareOptions.Ordinal to the comparison when working with UTF-16
                if (String.Compare(memoryReadString, replacementString) != 0)
                {
                    AnsiConsole.MarkupLine("[bold][white]> Error: Read string value {0} instead of {1}[/][/]", memoryReadString, replacementString);
                    AnsiConsole.MarkupLine("[bold][white]> Restarting the patcher ...[/][/]");
                    /**
                     * If the final check fails, we set our error code.
                     * This is needed for the GetErrorMessage() method and the patcherRestarted boolean.
                     * Error code 666 is not reserved, so it can be used.
                     * Consider removing the error code if a patcher restart becomes unnecessary.
                     * Alternatively, use SetErrorCode(uint errorCode) directly.
                     */
                    errorCodeReading = 666;
                }
            }

            //processHandle = null;
            //loadoutProcess.Dispose();


            // Read read memory buffer converted to a readable string (UTF-8)
            return memoryReadString;
        }

        public static byte[] ReadMemory(int offset, int size)
        {
            // To read unicode, multiply the byte size by 2 before calling this method
            // 'H e l l o   W o r l d ! '
            byte[] buffer = new byte[size];

            if (!ReadProcessMemory((int)LoadoutProcessHandle!, offset, buffer, size, out numberOfBytesRead))
            {
                errorCodeReading = GetLastError();
            }

            return buffer;
        }

        public static bool GetLastErrorOfProcessMemory()
        {
            if (errorCodeOpening != null && errorCodeOpening != 0 || errorCodeReading != null && errorCodeReading != 0 || errorCodeWriting != null && errorCodeWriting != 0)
            {
                // The system is only able to output 1 error code which gets overwritten frequently
                // With this approach, 3 separate error codes can be recovered
                if (errorCodeOpening != null && errorCodeOpening != 0 && errorCodeOpening != 666)
                {
                    GetErrorMessage((int)errorCodeOpening, 'o');
                }
                if (errorCodeReading != null && errorCodeReading != 0 && errorCodeReading != 666)
                {
                    GetErrorMessage((int)errorCodeReading, 'r');
                }
                if (errorCodeWriting != null && errorCodeWriting != 0 && errorCodeWriting != 666)
                {
                    GetErrorMessage((int)errorCodeWriting, 'w');
                }
                return true;
            }
            return false;
        }

        private static void GetErrorMessage(int errorCode, char openReadWrite)
        {
            // Questionable if externErrorCode is needed
            int externErrorCode;
            externErrorCode = SetLastError(errorCode);
            StringBuilder errorMessage = new(512);
            externErrorCode = FormatMessage(ALLOCATE_BUFFER | FROM_SYSTEM | IGNORE_INSERTS, IntPtr.Zero, errorCode, 0, out errorMessage, errorMessage.Capacity, IntPtr.Zero);
            AnsiConsole.MarkupLine("[bold][white]> Error code: {0}[/][/]", errorCode);
            if (openReadWrite == 'o')
            {
                AnsiConsole.MarkupLine("[bold][white]> The process could not be opened.[/][/]");
                AnsiConsole.MarkupLine("[bold][white]> System error message about this error: [/][/]");
                AnsiConsole.MarkupLine("[bold][white]> {0}[/][/]", errorMessage);
            }
            else if (openReadWrite == 'r')
            {
                AnsiConsole.MarkupLine("[bold][white]> The memory could not be read.[/][/]");
                AnsiConsole.MarkupLine("[bold][white]> System error message about this error: [/][/]");
                AnsiConsole.MarkupLine("[bold][white]> {0}[/][/]", errorMessage);
                if (errorCode == 299) { Console.WriteLine("[bold][white]> Known error: Loadout must have been closed![/][/]"); }
            }
            else if (openReadWrite == 'w')
            {
                AnsiConsole.MarkupLine("[bold][white]> The memory could not be overwritten.[/][/]");
                AnsiConsole.MarkupLine("[bold][white]> System error message about this error: [/][/]");
                AnsiConsole.MarkupLine("[bold][white]> {0}[/][/]", errorMessage);
                if (errorCode == 5) { Console.WriteLine("[bold][white]> Known error: Loadout must have been closed![/][/]"); }
            }
            AnsiConsole.MarkupLine("[bold][white]> Restarting the patcher ...[/][/]");
            if (errorMessage is not null)
            {
                errorMessage.Clear();
            }
        }
    }
}
