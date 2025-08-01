﻿using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class WinDialog
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int MessageBox(
            IntPtr hWnd,
            string lpText,
            string lpCaption,
            uint uType);


        // Constantes para MessageBox
        public const int MB_OK = 0x00000000;
        public const int MB_ICONERROR = 0x00000010;
        public const int MB_ICONWARNING = 0x00000030;
        public const int MB_ICONINFORMATION = 0x00000040;
        public const int MB_TOPMOST = 0x00040000;
        public const int MB_SYSTEMMODAL = 0x00001000; 

        public static void ShowInfoDialog(string title, string message)
        {
            WinDialog.MessageBox(
                IntPtr.Zero,
                message,
                title,
                WinDialog.MB_OK | WinDialog.MB_ICONINFORMATION);
        }

        public static void ShowErrorDialog(string title, string message)
        {
            WinDialog.MessageBox(
                IntPtr.Zero,
                message,
                title,
                WinDialog.MB_OK | WinDialog.MB_ICONERROR);
        }

        public static void ShowFatalError(string title, string message)
        {
            WinDialog.MessageBox(
                     IntPtr.Zero,
                     $"ERROR CRÍTICO:\n{message}",
                     title,
                     WinDialog.MB_OK | WinDialog.MB_ICONERROR | WinDialog.MB_TOPMOST);
        }
    }
}
