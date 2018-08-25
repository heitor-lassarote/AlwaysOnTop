namespace AlwaysOnTop
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
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
        }

        ~MainWindow()
        {
            this.hook.KeyDown -= this.HookKeyDown;
            this.hook.Uninstall();
        }

        private void ButtonRemoveClick(object sender, RoutedEventArgs e)
        {
            Process[] selectedItems = new Process[this.ListBoxWindows.SelectedItems.Count];
            this.ListBoxWindows.SelectedItems.CopyTo(selectedItems, 0);
            foreach (Process window in selectedItems)
            {
                this.SetWindowAlwaysOnTop(window.MainWindowHandle, HWndNoTopMost);
                this.ListBoxWindows.Items.Remove(window);
                this.managedWindows.Remove(window.MainWindowHandle);
            }
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
                        this.ListBoxWindows.Items.Remove(process);
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
