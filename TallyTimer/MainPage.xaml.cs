using System;
using System.Device.Location;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using TallyTimer.Shared;
using Microsoft.Phone.Shell;
using System.Diagnostics;

namespace TallyTimer
{
    public partial class MainPage : IDisposable
    {
        int _count = 0;
        readonly Setting<bool> _redVibrant = new Setting<bool>("RedVibrant", false);
        readonly Setting<bool> _orangevibrant = new Setting<bool>("OrangeVibrant", false);
        readonly Setting<int> _savedCount = new Setting<int>("SavedCount", 0);
        readonly Setting<int> _intervall = new Setting<int>("Intervall", 90);
        readonly Setting<TimeSpan> _totalTime = new Setting<TimeSpan>("TotalTime", TimeSpan.Zero);
        readonly Setting<bool> _wasRunning = new Setting<bool>("WasRunning", false);
        readonly Setting<bool> _flipped = new Setting<bool>("Flipped", false);
        readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(.1) };
        readonly Setting<DateTime> _previousTick = new Setting<DateTime>("PreviousTick", DateTime.MinValue);
        private ApplicationBarIconButton _decreaseButton;
        private ApplicationBarIconButton _resetButton;
        private ApplicationBarIconButton _pauseButton;
        private ApplicationBarIconButton _editButton;
        private ApplicationBarMenuItem _lockMenuItem;
        private ApplicationBarMenuItem _flipMenuItem;
        private ApplicationBarMenuItem _aboutMenuItem;
        private GeoCoordinateWatcher _gcw;
        readonly Setting<SupportedPageOrientation> _savedSupportedOrientations = new Setting<SupportedPageOrientation>("SavedSupportedOrientations", SupportedPageOrientation.PortraitOrLandscape);


        public MainPage()
        {
            InitializeComponent();
            _gcw = new GeoCoordinateWatcher();
            _gcw.PositionChanged += gcw_PositionChanged;
            _gcw.Start();
            InitAppBar();
            if (_totalTime.Value == TimeSpan.Zero)
                _totalTime.Value = _totalTime.Value.Add(new TimeSpan(0, 0, 0, _intervall.Value));
            _timer.Tick += Timer_Tick;
            TimeSpanPicker.Value = SecondsToTimespan(_intervall.Value);
            TimeSpanPicker.ValueChanged += Picker_ValueChanged;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _count++;
            CountTextBlock.Text = _count.ToString("N0");
            ResetTimer();
            Start();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _savedCount.Value = _count;
            _previousTick.Value = DateTime.Now;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _count = _savedCount.Value;
            CountTextBlock.Text = _count.ToString("N0");
            if (_previousTick.Value > DateTime.MinValue)
            {
                if (_wasRunning.Value)
                {
                    _totalTime.Value = _totalTime.Value.Subtract(DateTime.Now - _previousTick.Value);
                }
            }
            if (_wasRunning.Value)
            {
                Start();
            }
            else
            {
                _pauseButton.IconUri = new Uri("Images/appbar.transport.play.rest.png", UriKind.Relative);
            }
            ShowCurrentTime();
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            // change the layout, based on the current orientation
            if (e.Orientation == PageOrientation.PortraitUp)
            {
                RenderTransform = null;
                // move the controls to the star-sized column
                // and to the row above the list
                Grid.SetColumn(TallyPanel, 1);
                Grid.SetRow(TallyPanel, 0);
            }
            else
            {
                if(_flipped.Value)
                {
                    RenderTransform = new RotateTransform() { Angle = 180, CenterX = 300, CenterY = 240 };   
                }
                // for landscape mode, move the controls to the first column
                // and to the star-sized row
                Grid.SetColumn(TallyPanel, 0);
                Grid.SetRow(TallyPanel, 1);
            }
            base.OnOrientationChanged(e);
        }

        protected void Timer_Tick(object sender, EventArgs e)
        {
            var seconds = new TimeSpan(0, 0, 0, 0, 140);
            _totalTime.Value -= seconds;
            var vibrate = VibrateController.Default;

            if (_totalTime.Value < new TimeSpan(0, 0, 0, 0) && LayoutRoot.Background != new SolidColorBrush(Colors.Red))
            {
                if (!_redVibrant.Value)
                {
                    vibrate.Start(TimeSpan.FromMilliseconds(1000));
                    _redVibrant.Value = true;
                }
                LayoutRoot.Background = new SolidColorBrush(Colors.Red);
            }
            else if (_totalTime.Value < new TimeSpan(0, 0, 0, 10) && LayoutRoot.Background != new SolidColorBrush(Colors.Orange))
            {
                if (!_orangevibrant.Value)
                {
                    vibrate.Start(TimeSpan.FromMilliseconds(1000));
                    _orangevibrant.Value = true;
                }
                LayoutRoot.Background = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                ResetColorsVibrant();
            }
            ShowCurrentTime();
        }

        private void ResetColorsVibrant()
        {
            LayoutRoot.Background = new SolidColorBrush(Colors.Transparent);
            _redVibrant.Value = false;
            _orangevibrant.Value = false;
        }

        protected void PauseButton_Click(object sender, EventArgs e)
        {
            if (_wasRunning.Value)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        protected void ResetButton_Click(object sender, EventArgs e)
        {
            Reset();
            Stop();
        }

        protected void DecreaseButton_Click(object sender, EventArgs e)
        {
            _count--;
            CountTextBlock.Text = _count.ToString("N0");
        }

        private void InitAppBar()
        {
            var appBar = new ApplicationBar();

            _decreaseButton = new ApplicationBarIconButton(new Uri("Images/appbar.minus.rest.png", UriKind.Relative));
            _decreaseButton.Click += DecreaseButton_Click;
            _decreaseButton.Text = "Decrease";
            appBar.Buttons.Add(_decreaseButton);

            _resetButton = new ApplicationBarIconButton(new Uri("Images/appbar.refresh.rest.png", UriKind.Relative));
            _resetButton.Click += ResetButton_Click;
            _resetButton.Text = "Reset";
            appBar.Buttons.Add(_resetButton);

            _pauseButton = new ApplicationBarIconButton(new Uri("Images/appbar.transport.pause.rest.png", UriKind.Relative));
            _pauseButton.Click += PauseButton_Click;
            _pauseButton.Text = "Pause";
            appBar.Buttons.Add(_pauseButton);

            _editButton = new ApplicationBarIconButton(new Uri("Images/appbar.edit.rest.png", UriKind.Relative));
            _editButton.Click += EditButton_Click;
            _editButton.Text = "Edit";
            appBar.Buttons.Add(_editButton);

            _lockMenuItem = new ApplicationBarMenuItem { Text = "lock screen" };
            _lockMenuItem.Click += LockMenuItemClick;
            appBar.MenuItems.Add(_lockMenuItem);

            _flipMenuItem = new ApplicationBarMenuItem { Text = "Flip orientation" };
            _flipMenuItem.Click += FlipMenuItemOnClick;
            appBar.MenuItems.Add(_flipMenuItem);

            _aboutMenuItem = new ApplicationBarMenuItem { Text = "About" };
            _aboutMenuItem.Click += AboutMenuItemOnClick;
            appBar.MenuItems.Add(_aboutMenuItem);

            ApplicationBar = appBar;
        }

        private void AboutMenuItemOnClick(object sender, EventArgs eventArgs)
        {
            NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
        }

        private void FlipMenuItemOnClick(object sender, EventArgs eventArgs)
        {
            if(Orientation == PageOrientation.LandscapeLeft || Orientation == PageOrientation.LandscapeRight)
            {
                if(_flipped.Value)
                {
                    RenderTransform = null;
                    _flipped.Value = false;
                }
                else
                {
                    RenderTransform = new RotateTransform() { Angle = 180, CenterX = 300, CenterY = 240 };
                    _flipped.Value = true;
                }
            }
        }

        private void LockMenuItemClick(object sender, EventArgs eventArgs)
        {
            if (SupportedOrientations != SupportedPageOrientation.PortraitOrLandscape)
            {
                // We are locked, so unlock now
                SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;

                // Change the state of the application bar button to reflect this
                _lockMenuItem.Text = "lock screen";
            }
            else
            {
                // We are unlocked, so lock to the current orientation now
                SupportedOrientations = IsMatchingOrientation(PageOrientation.Portrait) ? SupportedPageOrientation.Portrait : SupportedPageOrientation.Landscape;

                // Change the state of the application bar button to reflect this
                _lockMenuItem.Text = "unlock";
            }

            // Remember the new setting after the page has been left
            _savedSupportedOrientations.Value = SupportedOrientations;
        }

        bool IsMatchingOrientation(PageOrientation orientation)
        {
            return ((Orientation & orientation) == orientation);
        }

        private TimeSpan SecondsToTimespan(int s)
        {
            var hours = (int)Math.Floor(s / 3600);
            var minutes = (int)Math.Floor(s / 60);
            var seconds = s % 60;
            return new TimeSpan(hours, minutes, seconds);
        }

        private void ResetTimer()
        {
            _totalTime.Value = _totalTime.Value = TimeSpan.Zero;
            _totalTime.Value = _totalTime.Value.Add(new TimeSpan(0, 0, 0, _intervall.Value));
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (_editButton.IconUri.ToString().Contains("Images/appbar.cancel.rest.png"))
            {
                TimeSpanPicker.Visibility = Visibility.Collapsed;
                TotalTimeDisplay.Visibility = Visibility.Visible;
                _editButton.IconUri = new Uri("Images/appbar.edit.rest.png", UriKind.Relative);
            }
            else
            {
                TimeSpanPicker.Visibility = Visibility.Visible;
                TotalTimeDisplay.Visibility = Visibility.Collapsed;
                _editButton.IconUri = new Uri("Images/appbar.cancel.rest.png", UriKind.Relative);
            }
        }

        private void Picker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            _intervall.Value = Convert.ToInt32(e.NewValue.TotalSeconds);
            TimeSpanPicker.Visibility = Visibility.Collapsed;
            TotalTimeDisplay.Visibility = Visibility.Visible;
            _editButton.IconUri = new Uri("Images/appbar.edit.rest.png", UriKind.Relative);
            ResetTimer();
        }

        private void ShowCurrentTime()
        {
            TotalTimeDisplay.Time = _totalTime.Value;
        }

        void Stop()
        {
            _timer.Stop();
            _wasRunning.Value = false;
            _editButton.IsEnabled = true;
            _pauseButton.IconUri = new Uri("Images/appbar.transport.play.rest.png", UriKind.Relative);
        }

        private void Start()
        {
            _timer.Start();
            _wasRunning.Value = true;
            _editButton.IsEnabled = false;
            _pauseButton.IconUri = new Uri("Images/appbar.transport.pause.rest.png", UriKind.Relative);
        }

        private void Reset()
        {
            _count = 0;
            CountTextBlock.Text = _count.ToString("N0");
            ResetTimer();
            ResetColorsVibrant();
            ShowCurrentTime();
        }

        private void gcw_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            // Stop the GeoCoordinateWatcher now that we have the device location.
            _gcw.Stop();

            adControl1.Latitude = e.Position.Location.Latitude;
            adControl1.Longitude = e.Position.Location.Longitude;
        }

        private void adControl1_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            Debug.WriteLine("AdControl error: " + e.Error.Message);
        }

        private void adControl1_AdRefreshed(object sender, EventArgs e)
        {
            Debug.WriteLine("AdControl new ad received");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_gcw != null)
                {
                    _gcw.Dispose();
                    _gcw = null;
                }
            }
        }
    }
}