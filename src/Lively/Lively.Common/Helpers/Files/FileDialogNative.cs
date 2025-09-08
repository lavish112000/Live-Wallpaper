using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Lively.Common.Helpers.Files
{
    public static class FileDialogNative
    {
        public static string PickSingleFile(string filter)
        {
            var files = ShowOpenFileDialog(filter);
            return files.Any() ? files[0] : null;
        }

        public static IReadOnlyList<string> PickMultipleFiles(string filter)
        {
            return ShowOpenFileDialog(filter, true);
        }

        public static string PickSaveFile(string filter, string suggestedFileName, string defaultExtension)
        {
            const int MAX_PATH = 260;
            // Allocate unmanaged memory for the file path (Unicode, 2 bytes per char)
            IntPtr fileBuffer = Marshal.AllocHGlobal(MAX_PATH * 2);
            // Zero out memory
            RtlZeroMemory(fileBuffer, (uint)(MAX_PATH * 2));

            var ofn = new OpenFileName
            {
                structSize = Marshal.SizeOf(typeof(OpenFileName)),
                filter = filter,
                file = fileBuffer,
                maxFile = MAX_PATH,
                fileTitle = new string(new char[64]),
                maxFileTitle = 0,
                defExt = defaultExtension,
                flags = (int)(OpenFileNameFlags.OFN_EXPLORER
                                  | OpenFileNameFlags.OFN_PATHMUSTEXIST
                                  | OpenFileNameFlags.OFN_NOCHANGEDIR
                                  | OpenFileNameFlags.OFN_OVERWRITEPROMPT)
            };

            // Pre-fill suggested file name (Unicode.)
            if (!string.IsNullOrWhiteSpace(suggestedFileName))
            {
                var bytes = Encoding.Unicode.GetBytes(suggestedFileName);
                Marshal.Copy(bytes, 0, fileBuffer, Math.Min(bytes.Length, MAX_PATH * 2 - 2));
            }

            string resultPath = null;

            try
            {
                if (GetSaveFileName(ofn))
                    resultPath = Marshal.PtrToStringUni(ofn.file);
            }
            finally
            {
                Marshal.FreeHGlobal(fileBuffer);
            }

            return resultPath;
        }

        private static IReadOnlyList<string> ShowOpenFileDialog(string filter, bool multiSelect = false)
        {
            const int MAX_FILE_LENGTH = 2048;
            var ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.filter = filter;
            ofn.fileTitle = new string(new char[MAX_FILE_LENGTH]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.flags = (int)OpenFileNameFlags.OFN_HIDEREADONLY | (int)OpenFileNameFlags.OFN_EXPLORER | (int)OpenFileNameFlags.OFN_FILEMUSTEXIST | (int)OpenFileNameFlags.OFN_PATHMUSTEXIST;

            // Create buffer for file names
            ofn.file = Marshal.AllocHGlobal(MAX_FILE_LENGTH * Marshal.SystemDefaultCharSize);
            ofn.maxFile = MAX_FILE_LENGTH;

            // Zero out memory
            RtlZeroMemory(ofn.file, (uint)(MAX_FILE_LENGTH * Marshal.SystemDefaultCharSize));

            if (multiSelect)
            {
                //If the user selects more than one file, the lpstrFile buffer returns the path to the current directory followed by the file names of the selected files.
                //The nFileOffset member is the offset, in bytes or characters, to the first file name, and the nFileExtension member is not used.
                //For Explorer-style dialog boxes, the directory and file name strings are NULL separated, with an extra NULL character after the last file name.
                //This format enables the Explorer-style dialog boxes to return long file names that include spaces.
                ofn.flags |= (int)OpenFileNameFlags.OFN_ALLOWMULTISELECT;
            }

            var result = new List<string>();
            var success = GetOpenFileName(ofn);
            if (success)
            {
                IntPtr filePointer = ofn.file;
                long pointer = (long)filePointer;
                string file = Marshal.PtrToStringAuto(filePointer);
                var strList = new List<string>();

                // Retrieve file names
                while (file.Length > 0)
                {
                    strList.Add(file);

                    pointer += file.Length * Marshal.SystemDefaultCharSize + Marshal.SystemDefaultCharSize;
                    filePointer = checked((IntPtr)pointer);
                    file = Marshal.PtrToStringAuto(filePointer);
                }

                if (strList.Count > 1)
                {
                    for (int i = 1; i < strList.Count; i++)
                    {
                        result.Add(Path.Combine(strList[0], strList[i]));
                    }
                }
                else
                {
                    result.AddRange(strList);
                }
            }
            Marshal.FreeHGlobal(ofn.file);

            return result;
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        private static extern void RtlZeroMemory(IntPtr dest, uint size);

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        private static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter;
            public string customFilter;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public IntPtr file;
            public int maxFile = 0;
            public string fileTitle;
            public int maxFileTitle = 0;
            public string initialDir;
            public string title;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        private enum OpenFileNameFlags
        {
            OFN_OVERWRITEPROMPT = 0x2,
            OFN_HIDEREADONLY = 0x4,
            OFN_NOCHANGEDIR = 0x8,
            OFN_FORCESHOWHIDDEN = 0x10000000,
            OFN_ALLOWMULTISELECT = 0x200,
            OFN_EXPLORER = 0x80000,
            OFN_FILEMUSTEXIST = 0x1000,
            OFN_PATHMUSTEXIST = 0x800,
        }
    }
}
