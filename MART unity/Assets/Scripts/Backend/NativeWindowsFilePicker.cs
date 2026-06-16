using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static class NativeWindowsFilePicker
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct OpenFileName
    {
        public int structSize;
        public System.IntPtr dlgOwner;
        public System.IntPtr instance;
        public string filter;
        public string customFilter;
        public int maxCustFilter;
        public int filterIndex;
        public StringBuilder file;
        public int maxFile;
        public StringBuilder fileTitle;
        public int maxFileTitle;
        public string initialDir;
        public string title;
        public int flags;
        public short fileOffset;
        public short fileExtension;
        public string defExt;
        public System.IntPtr custData;
        public System.IntPtr hook;
        public string templateName;
        public System.IntPtr reservedPtr;
        public int reservedInt;
        public int flagsEx;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
#endif

    public static bool IsSupported =>
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        true;
#else
        false;
#endif

    public static string OpenImageFile()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        OpenFileName dialog = new OpenFileName
        {
            structSize = Marshal.SizeOf(typeof(OpenFileName)),
            filter = "Image Files\0*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp\0All Files\0*.*\0",
            file = new StringBuilder(1024),
            maxFile = 1024,
            fileTitle = new StringBuilder(256),
            maxFileTitle = 256,
            title = "Select profile image",
            defExt = "png",
            flags = 0x00000008 | 0x00001000 | 0x00080000,
        };

        return GetOpenFileName(dialog) ? dialog.file.ToString() : string.Empty;
#else
        Debug.LogWarning("Native file picker is not supported on this platform.");
        return string.Empty;
#endif
    }
}
