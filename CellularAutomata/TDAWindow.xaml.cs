using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CellularAutomata
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TwoDimAutomata _Automata;
        int _Width, _Height;
        int _RawStride;
        readonly int _BytePerPixel;
        byte[] _RawImage;
        readonly byte[] _RawPixel;
        PixelFormat _PixelFormat;
        DpiScale _DpiScale;
        WriteableBitmap _WriteableBitmap;

        bool _Iterating = false;

        public MainWindow()
        {
            InitializeComponent();

            _PixelFormat = PixelFormats.Rgb128Float;

            _Width = 200;
            _Height = 200;
            _RawStride = (_Width * _PixelFormat.BitsPerPixel + 7) / 8;
            _RawImage = new byte[_RawStride * _Height];

            _BytePerPixel = _PixelFormat.BitsPerPixel / 8;
            _RawPixel = new byte[_BytePerPixel];

            _DpiScale = VisualTreeHelper.GetDpi(this);

            _WriteableBitmap = new(_Width, _Height, _DpiScale.PixelsPerInchX, _DpiScale.PixelsPerInchY, _PixelFormat, null);
            _Image.Source = _WriteableBitmap;

            _Automata = new([3], [2, 3], (0, 0, _Width, _Height));
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_Iterating) return;

            var p = e.GetPosition(_Image);
            _Automata[(int)p.X, (int)p.Y] = true;
            SetImage((int)p.X, (int)p.Y, 1.0f);
        }

        private void Image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_Iterating) return;

            var p = e.GetPosition(_Image);
            _Automata[(int)p.X, (int)p.Y] = false;
            SetImage((int)p.X, (int)p.Y, 0.0f);
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (_Iterating) return;

            var p = e.GetPosition(_Image);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _Automata[(int)p.X, (int)p.Y] = true;
                SetImage((int)p.X, (int)p.Y, 1.0f);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                _Automata[(int)p.X, (int)p.Y] = false;
                SetImage((int)p.X, (int)p.Y, 0.0f);
            }
        }

        private void SetImage(int x, int y, float red)
        {
            if (x < 0 || x >= _Width) return;
            _RawPixel.AsSpan().Clear();
            ref var color = ref MemoryMarshal.AsRef<Color>(_RawPixel);
            color.r = red;

            _WriteableBitmap.WritePixels(new(x, y, 1, 1), _RawPixel, stride: _RawStride, 0);
        }


        private void ButtonRandomGenerated_Click(object sender, RoutedEventArgs e)
        {
            if (_Iterating) return;

            for (int i = 0; i < 100; i++)
            {
                var x = Random.Shared.Next(_Width);
                var y = Random.Shared.Next(_Height);
                _Automata[x, y] = true;
            }

            RefreshImage();
        }

        private void ButtonSingleStep_Click(object sender, RoutedEventArgs e)
        {
            if (_Iterating) return;

            _Automata.Iterate();
            RefreshImage();
        }

        DispatcherTimer? IterateTimer;
        private void ButtonContinuousIteration_Click(object sender, RoutedEventArgs e)
        {
            if (_Iterating) return;
            _Iterating = true;

            if (IterateTimer == null)
            {
                IterateTimer = new(DispatcherPriority.Render);
                IterateTimer.Tick += IterateTimer_Tick;
                IterateTimer.Interval = TimeSpan.FromSeconds(1.0 / 10);
            }

            _TickCount = 0;
            IterateTimer.Start();
            UpdateUIByContinuousIteration();
        }

        Task? TaskIterateTimerTick;
        int _TickCount;
        private void IterateTimer_Tick(object? sender, EventArgs e)
        {
            if (TaskIterateTimerTick != null && !TaskIterateTimerTick.IsCompleted)
                return;

            TaskIterateTimerTick = Task.Run(() =>
            {
                _Automata.Iterate();

                Dispatcher.Invoke(() =>
                {
                    _TickCount++;
                    Title = $"二维元胞自动机 {_TickCount}Ticks";
                    RefreshImage();
                });
            });
        }

        private void ButtonStopIteration_Click(object sender, RoutedEventArgs e)
        {
            if (_Iterating)
                _Iterating = false;
            IterateTimer?.Stop();
            Title = $"二维元胞自动机";
            UpdateUIByStopIteration();
        }

        private void UpdateUIByContinuousIteration()
        {
            ButtonContinuousIteration.IsEnabled = false;
            ButtonRandomGenerated.IsEnabled = false;
            ButtonResetAutomata.IsEnabled = false;
            ButtonResetSpace.IsEnabled = false;
            ButtonSingleStep.IsEnabled = false;
            ButtonStopIteration.IsEnabled = true;
            ButtonClear.IsEnabled = false;

            TextBoxAutomataBirthCondition.IsEnabled = false;
            TextBoxAutomataSurviveCondition.IsEnabled = false;
            TextBoxSpaceHeight.IsEnabled = false;
            TextBoxSpaceWidth.IsEnabled = false;
        }

        private void UpdateUIByStopIteration()
        {
            ButtonContinuousIteration.IsEnabled = true;
            ButtonRandomGenerated.IsEnabled = true;
            ButtonResetAutomata.IsEnabled = true;
            ButtonResetSpace.IsEnabled = true;
            ButtonSingleStep.IsEnabled = true;
            ButtonStopIteration.IsEnabled = false;
            ButtonClear.IsEnabled = true;

            TextBoxAutomataBirthCondition.IsEnabled = true;
            TextBoxAutomataSurviveCondition.IsEnabled = true;
            TextBoxSpaceHeight.IsEnabled = true;
            TextBoxSpaceWidth.IsEnabled = true;
        }

        private void ButtonResetAutomata_Click(object sender, RoutedEventArgs e)
        {
            if (_Iterating) return;

            HashSet<int> intsBirthCondition = [];
            {
                var textBirthCondition = TextBoxAutomataBirthCondition.Text;
                var stringsBirthCondition = textBirthCondition.Split(',', ' ', ';', '.');
                foreach (string stringBirthCondition in stringsBirthCondition)
                {
                    if (int.TryParse(stringBirthCondition, out var intBirthConditionValue))
                    {
                        if (intBirthConditionValue >= 1 && intBirthConditionValue <= 8)
                        {
                            intsBirthCondition.Add(intBirthConditionValue);
                        }
                    }
                }
                TextBoxAutomataBirthCondition.Text = string.Join(",", intsBirthCondition.Order());
            }

            HashSet<int> intsSurviveCondition = [];
            {
                var textSurviveCondition = TextBoxAutomataSurviveCondition.Text;
                var stringsSurviveCondition = textSurviveCondition.Split(',', ' ', ';', '.');
                foreach (string stringSurviveCondition in stringsSurviveCondition)
                {
                    if (int.TryParse(stringSurviveCondition, out var intSurviveConditionValue))
                    {
                        if (intSurviveConditionValue >= 0 && intSurviveConditionValue <= 8)
                        {
                            intsSurviveCondition.Add(intSurviveConditionValue);
                        }
                    }
                }
                TextBoxAutomataSurviveCondition.Text = string.Join(",", intsSurviveCondition.Order());
            }

            var newAutomata = new TwoDimAutomata(intsBirthCondition, intsSurviveCondition, (0, 0, _Width, _Height));
            foreach(var index in _Automata.Data)
            {
                newAutomata[index.x, index.y] = true;
            }
            _Automata = newAutomata;

            RefreshImage();

            TextBlockFactor.Text = $"参数 B{string.Join("", intsBirthCondition)} S{string.Join("", intsSurviveCondition)}";
        }

        private void ButtonResetSpace_Click(object sender, RoutedEventArgs e)
        {
            if (_Iterating) return;

            int imageWidth = _Width;
            {
                var textSpaceWidth = TextBoxSpaceWidth.Text;
                if (int.TryParse(textSpaceWidth, out var intWidth))
                {
                    imageWidth = int.Clamp(intWidth, 200, (int)GridImage.ActualWidth * 1);
                }
                TextBoxSpaceWidth.Text = imageWidth.ToString();
            }

            int imageHeight = _Height;
            {
                var textSpaceHeight = TextBoxSpaceHeight.Text;
                if (int.TryParse(textSpaceHeight, out var intHeight))
                {
                    imageHeight = int.Clamp(intHeight, 200, (int)GridImage.ActualHeight * 1);
                }
                TextBoxSpaceHeight.Text = imageHeight.ToString();
            }

            _Width = imageWidth;
            _Height = imageHeight;
            _RawStride = (_Width * _PixelFormat.BitsPerPixel + 7) / 8;
            _RawImage = new byte[_RawStride * _Height];
            if (_RawImage.Length != _RawStride * _Height)
            {
                _RawImage = new byte[_RawStride * _Height];
            }

            if (_WriteableBitmap.Width != _Width || _WriteableBitmap.Height != _Height)
            {
                _WriteableBitmap = new(_Width, _Height, _DpiScale.PixelsPerInchX, _DpiScale.PixelsPerInchY, _PixelFormat, null);
                _Image.Source = _WriteableBitmap;
                _Automata = new(_Automata.RuleNumber, (0, 0, _Width, _Height));

                RefreshImage();
            }

            TextBlockSpace.Text = $"区域 {_Width}x{_Height}";
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            if (_Iterating) return;

            _Automata.Clear();

            RefreshImage();
        }

        private void RefreshImage()
        {
            var spanRawImage = _RawImage.AsSpan();
            spanRawImage.Clear();

            Parallel.ForEach(_Automata.Data, index =>
            {
                var (x, y) = index;
                if (x < 0 || x >= _Width) return;
                var offset = x + _Width * y;
                ref var color = ref MemoryMarshal.AsRef<Color>(_RawImage.AsSpan(_BytePerPixel * offset));
                color.r = 1f;
            });

            _WriteableBitmap.WritePixels(new(0, 0, _Width, _Height), _RawImage, stride: _RawStride, 0);
        }
    }

    struct Color
    {
        public float r, g, b, a;
    }
}