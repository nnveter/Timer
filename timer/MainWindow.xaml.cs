// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using timer.utils;
using Vanara.PInvoke;
using Windows.UI.Shell;
using WinRT;
using WinUIEx;
using static Vanara.PInvoke.Shell32;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace timer
{

    public sealed partial class MainWindow : WindowEx
    {

        WindowsSystemDispatcherQueueHelper m_wsdqHelper; // See below for implementation.
        MicaController m_backdropController;
        SystemBackdropConfiguration m_configurationSource;

        //IntPtr hwnd;

        public static DispatcherTimer dispatcherTimer = new DispatcherTimer();

        int isWork = 0;
        long thisTime = 0;
        long endTime;
        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            TrySetSystemBackdrop();

            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            //hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            //TaskbarProgress.SetState(hwnd, TaskbarProgress.TaskbarStates.Normal);
            //Console.Write("\r{0}%", "10");
            //TaskbarProgress.Reset(hwnd);
        }

        void Finish()
        {
            new ToastContentBuilder()
                .AddArgument("action", "viewConversation")
                .AddText("Время вышло!")
                .Show();
            isWork = 0;
            thisTime = 0;
            CheckButton();
        }

        
        void dispatcherTimer_Tick(object sender, object e)
        {
            thisTime++;
            ProgressBar.Value = thisTime;
            TimeSpan ts = TimeSpan.FromSeconds(endTime) - TimeSpan.FromSeconds(thisTime); 
            TimerBox.Text = ts.ToString();
            //TaskbarProgress.SetValue(hwnd, thisTime, endTime);
            if (thisTime >= endTime) {
                Finish();
                dispatcherTimer.Stop();
                return;
            }           
        }

        void TextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) {
                ProgressBar.Maximum = ConvertUtils.MainStringToMs(TimerBox.Text);
                endTime = ConvertUtils.MainStringToMs(TimerBox.Text);
                thisTime = 0;
                dispatcherTimer.Start();
                isWork = 1;
                CheckButton();
            }
        }

        void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            switch (button.Tag) 
            {
                case "Start":
                    ProgressBar.Maximum = ConvertUtils.MainStringToMs(TimerBox.Text);
                    endTime = ConvertUtils.MainStringToMs(TimerBox.Text);
                    thisTime = 0;
                    dispatcherTimer.Start();
                    isWork = 1;
                    break;
                case "Stop":
                    ProgressBar.Value = 0;
                    TimerBox.Text = "";
                    thisTime = 0;
                    dispatcherTimer.Stop();
                    isWork = 0;
                    break;
                case "Pause":
                    Pause_();
                    break;
            }
            CheckButton();
        }

        void Pause_() {
            if (isWork == 2)
            {
                dispatcherTimer.Start();
                isWork = 1;
            }
            else {
                dispatcherTimer.Stop();
                isWork = 2;
            }
        }

        void CheckButton() {
            if (isWork == 1)
            {
                Pause.Label = "Pause";
                Stop.IsEnabled = true;
                Pause.IsEnabled = true;
            }
            else if (isWork == 0)
            {
                Pause.Label = "Pause";
                Stop.IsEnabled = false;
                Pause.IsEnabled = false;
            }
            else
            {
                Pause.Label = "Resume";
                Pause.IsEnabled = true;
            }
        }


        //Thems
        bool TrySetSystemBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Create the policy object.
                m_configurationSource = new SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_backdropController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_backdropController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_backdropController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            // so it doesn't try to use this closed window.
            if (m_backdropController != null)
            {
                m_backdropController.Dispose();
                m_backdropController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }
    }


    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }

    }
}
