namespace AlwaysOnTop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using GlobalLowLevelHooks;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoSize = 0x0001;
        private const uint TopMostFlags = SwpNoMove | SwpNoSize;
        
        private static readonly IntPtr HWndNoTopMost = new IntPtr(-2);
        private static readonly IntPtr HWndTopMost = new IntPtr(-1);
        
        private readonly KeyboardHook hook = new KeyboardHook();
        private readonly Dictionary<IntPtr, IntPtr> managedWindows = new Dictionary<IntPtr, IntPtr>();

        public MainWindow()
        {
            this.InitializeComponent();
            this.hook.KeyDown += this.HookKeyDown;
            this.hook.Install();
            
            System.Windows.Forms.ContextMenu cm = new System.Windows.Forms.ContextMenu();

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon
            {
                Icon = Properties.Resources.AOT,
                Visible = true,
                Text = Properties.Resources.SystemTrayRunning,
                ContextMenu = cm
            };

            cm.MenuItems.Add(Properties.Resources.SystemTrayExit, (s, e) =>
            {
                Application.Current.Shutdown();
                ni.Visible = false;
            });
        }

        ~MainWindow()
        {
            this.hook.KeyDown -= this.HookKeyDown;
            this.hook.Uninstall();
        }

        private Process GetCurrentWindow()
        {
            int hWnd = Win32API.GetForegroundWindow();
            Process process = Process.GetProcessById(this.GetWindowProcessID(hWnd));
            return process;
        }

        private int GetWindowProcessID(int hWnd)
        {
            Win32API.GetWindowThreadProcessId(hWnd, out int pid);
            return pid;
        }

        private void HookKeyDown(KeyboardHook.VKeys key)
        {
            if (key == KeyboardHook.VKeys.F10)
            {
                Process process = this.GetCurrentWindow();
                IntPtr hwnd = process.MainWindowHandle;
                if (this.managedWindows.TryGetValue(hwnd, out IntPtr position))
                {
                    if (position == HWndNoTopMost)
                    {
                        position = HWndTopMost;
                    }
                    else
                    {
                        position = HWndNoTopMost;
                    }

                    this.managedWindows[hwnd] = position;
                }
                else
                {
                    this.managedWindows.Add(hwnd, position);
                    position = HWndTopMost;
                }
                
                this.SetWindowAlwaysOnTop(hwnd, position);
            }
        }

        private void SetWindowAlwaysOnTop(IntPtr hWnd, IntPtr position) =>
            Win32API.SetWindowPos(hWnd, position, 0, 0, 0, 0, TopMostFlags);
    }
}
