using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace OC.OfficeLiteServer;

public static class Y200
{
    public const string PATH = @"C:\KRC\Y200Interface\Y200Clnt.dll";
    
    public enum Result
    {
        Ok = 0,
        InvalidHandle = -2147220991,
        NoSharedMem = -2147220990,
        InvalidParam = -2147220989,
        InvalidReadParam = -2147220988,
        InvalidWriteParam = -2147220987,
        TreadFail = -2147220986,
        TreadSyncFail = -2147220985,
        InvalidIpcName = -2147220984,
        InvalidImageSize = -2147024713
    }
        
    [DllImport(PATH)]
    public static extern int WMY200CreateIPCRange(
        [In] string pszIpcName, uint uReadOffs, uint uReadSize, uint uWriteOffs, uint uWriteSize, ref int pHandle);

    [DllImport(PATH)]
    public static extern int WMY200DestroyIPCRange(int hRange);

    [DllImport(PATH)]
    public static extern int WMY200SetEventWindow(int hRange, int hWnd, uint nMsg);

    [DllImport(PATH)]
    public static extern int WMY200SetNotifyEnable(int hRange, int bNotify);

    [DllImport(PATH)]
    public static extern int WMY200IsNotifyEnable(int hRange, ref int pbNotify);

    [DllImport(PATH)]
    public static extern int WMY200SetWinMODEventNotify(int hRange, int bNotify);

    [DllImport(PATH)]
    public static extern int WMY200IsWinMODEventNotify(int hRange, ref int pbNotify);

    [DllImport(PATH)]
    public static extern int WMY200SetEventTimeout(int hRange, uint dwTimeout);

    [DllImport(PATH)]
    public static extern int WMY200GetEventTimeout(int hRange, ref uint pdwTimeout);

    [DllImport(PATH)]
    public static extern int WMY200SetMutexTimeout(int hRange, uint dwTimeout);

    [DllImport(PATH)]
    public static extern int WMY200GetMutexTimeout(int hRange, ref uint pdwTimeout);

    [DllImport(PATH)]
    public static extern int WMY200GetTickCount(int hRange, ref uint pdwCount);

    [DllImport(PATH)]
    public static extern int WMY200GetCycleCount(int hRange, ref uint pdwCount);

    [DllImport(PATH)]
    public static extern int WMY200GetIPCName(int hRange, StringBuilder lpBuffer, ref uint lpnSize);

    [DllImport(PATH)]
    public static extern int WMY200GetIPCMapSize(int hRange, ref uint pnMapSize);

    [DllImport(PATH)]
    public static extern int WMY200GetIPCProcImgSize(int hRange, ref uint pnProcImgSize);

    [DllImport(PATH)]
    public static extern int WMY200GetIPCEvent(int hRange, ref SafeWaitHandle phEvent);

    [DllImport(PATH)]
    public static extern int WMY200ReadBlock8(int hRange, 
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In][Out] byte[] pDest, uint uElemOffs, uint uElemCount);

    [DllImport(PATH)]
    public static extern int WMY200WriteBlock8(int hRange, 
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In] byte[] pSrc, uint uElemOffs, uint uElemCount);

    [DllImport(PATH)]
    public static extern int WMY200ReadBlock16(int hRange, 
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In][Out] short[] pDest, uint uElemOffs, uint uElemCount);

    [DllImport(PATH)]
    public static extern int WMY200WriteBlock16(int hRange, 
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In] short[] pSrc, uint uElemOffs, uint uElemCount);

    [DllImport(PATH)]
    public static extern int WMY200ReadBlock32(int hRange, 
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In][Out] int[] pDest, uint uElemOffs, uint uElemCount);

    [DllImport(PATH)]
    public static extern int WMY200WriteBlock32(int hRange, 
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In] int[] pSrc, uint uElemOffs, uint uElemCount);

    [DllImport(PATH)]
    public static extern int WMY200ReadBlockFloat(int hRange, 
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In][Out] float[] pDest, uint uElemOffs, uint uElemCount);

    [DllImport(PATH)]
    public static extern int WMY200WriteBlockFloat(int hRange, 
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In] float[] pSrc, uint uElemOffs, uint uElemCount);

    [DllImport(PATH)]
    public static extern int WMY200Read8(int hRange, ref byte pDest, uint uElemOffs);

    [DllImport(PATH)]
    public static extern int WMY200Write8(int hRange, byte byValue, uint uElemOffs);

    [DllImport(PATH)]
    public static extern int WMY200Read16(int hRange, ref short pDest, uint uElemOffs);

    [DllImport(PATH)]
    public static extern int WMY200Write16(int hRange, short wValue, uint uElemOffs);

    [DllImport(PATH)]
    public static extern int WMY200Read32(int hRange, ref int pDest, uint uElemOffs);

    [DllImport(PATH)]
    public static extern int WMY200Write32(int hRange, int dwValue, uint uElemOffs);

    [DllImport(PATH)]
    public static extern int WMY200ReadFloat(int hRange, ref float pDest, uint uElemOffs);

    [DllImport(PATH)]
    public static extern int WMY200WriteFloat(int hRange, float fltValue, uint uElemOffs);
}