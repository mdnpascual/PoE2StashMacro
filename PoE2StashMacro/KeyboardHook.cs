using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PoE2StashMacro;

public class KeyboardHook
{
    private const int WH_KEYBOARD_LL = 13;
    private LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    public event Action<Keys> KeyUp;
    public event Action<Keys> KeyDown;

    private MouseAutomation mouseAutomation;
    private HashSet<Keys> _keysToSuppress = new HashSet<Keys>();

    public KeyboardHook(MouseAutomation mouseAutomation)
    {
        this.mouseAutomation = mouseAutomation;
        _proc = HookCallback;
    }

    public void HookKeyboard()
    {
        _hookID = SetHook(_proc);
    }

    public void UnhookKeyboard()
    {
        UnhookWindowsHookEx(_hookID);
    }

    public void AddKeyToSuppress(Keys key)
    {
        _keysToSuppress.Add(key);
    }
    public void RemoveKeyToSuppress(Keys key)
    {
        _keysToSuppress.Remove(key);
    }
    public void ClearKeyToSuppress()
    {
        _keysToSuppress.Clear();
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                KeyDown?.Invoke(key);
                // Suppress the key if it's in the suppression list
                if (_keysToSuppress.Contains(key) && !mouseAutomation.IsProgrammaticKeyPress())
                {
                    return 1; // Suppress the key event
                }
            }
            else if (wParam == (IntPtr)WM_KEYUP)
            {
                KeyUp?.Invoke(key);
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
}