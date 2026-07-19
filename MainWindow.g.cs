using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AutoDial
{
    public partial class MainWindow : Window
    {
        internal ListBox lstAccounts;
        internal TextBox txtName;
        internal TextBox txtUser;
        internal PasswordBox txtPass;
        internal CheckBox chkAutoDial;
        internal CheckBox chkSilent;
        internal CheckBox chkAutoReconnect;
        internal TextBlock lblStatus;
        internal Ellipse statusDot;
        internal TextBlock lblOnlineTime;
        internal Button btnDial;
        internal Button btnDisconnect;
        internal Button btnSave;

        private void InitializeComponent()
        {
            Title = "宽带自动拨号";
            Height = 680;
            Width = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF5));
            FontFamily = new FontFamily("Microsoft YaHei");

            var mainStack = new StackPanel { Margin = new Thickness(20) };

            // Title
            mainStack.Children.Add(new TextBlock { Text = "宽带自动拨号", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)), Margin = new Thickness(0, 0, 0, 16) });

            // Account List Card
            var card1 = CreateCard();
            var card1Stack = new StackPanel();
            card1Stack.Children.Add(new TextBlock { Text = "账号列表", FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
            lstAccounts = new ListBox { Height = 90, FontSize = 14, BorderThickness = new Thickness(0) };
            lstAccounts.SelectionChanged += lstAccounts_SelectionChanged;
            card1Stack.Children.Add(lstAccounts);
            var btnPanel1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            btnPanel1.Children.Add(MakeBtn("新增", BtnAdd_Click, "#E0E0E0", "#555"));
            btnPanel1.Children.Add(MakeBtn("删除", BtnDelete_Click, "#E0E0E0", "#555"));
            btnPanel1.Children.Add(MakeBtn("导入", BtnImport_Click, "#E0E0E0", "#555"));
            btnPanel1.Children.Add(MakeBtn("导出", BtnExport_Click, "#E0E0E0", "#555"));
            btnPanel1.Children.Add(MakeBtn("创建系统连接", BtnCreateConnection_Click, "#FF9800", "#FFF"));
            btnPanel1.Children.Add(MakeBtn("删除系统连接", BtnDeleteConnection_Click, "#F44336", "#FFF"));
            card1Stack.Children.Add(btnPanel1);
            card1.Child = card1Stack;
            mainStack.Children.Add(card1);

            // Fields Card
            var card2 = CreateCard();
            var fieldsGrid = new Grid();
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 6; i++) fieldsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddField(fieldsGrid, "连接名称", 0, out txtName);
            AddField(fieldsGrid, "用户名", 2, out txtUser);
            AddPasswordField(fieldsGrid, "密码", 4, out txtPass);

            card2.Child = fieldsGrid;
            mainStack.Children.Add(card2);

            // Settings Card
            var card3 = CreateCard();
            var settingsStack = new StackPanel();
            settingsStack.Children.Add(new TextBlock { Text = "设置", FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
            var checksPanel = new StackPanel { Orientation = Orientation.Horizontal };
            chkAutoDial = new CheckBox { Content = "开机自动拨号", FontSize = 13, Margin = new Thickness(0, 0, 15, 0) };
            chkSilent = new CheckBox { Content = "静默拨号", FontSize = 13, Margin = new Thickness(0, 0, 15, 0) };
            chkAutoReconnect = new CheckBox { Content = "断线自动重连", FontSize = 13 };
            checksPanel.Children.Add(chkAutoDial);
            checksPanel.Children.Add(chkSilent);
            checksPanel.Children.Add(chkAutoReconnect);
            settingsStack.Children.Add(checksPanel);
            card3.Child = settingsStack;
            mainStack.Children.Add(card3);

            // Status
            var statusBorder = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12, 16, 12) };
            var statusPanel = new StackPanel { Orientation = Orientation.Horizontal };
            statusDot = new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)), Margin = new Thickness(0, 0, 10, 0) };
            statusPanel.Children.Add(statusDot);
            lblStatus = new TextBlock { Text = "未连接", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 20, 0) };
            statusPanel.Children.Add(lblStatus);
            lblOnlineTime = new TextBlock { Text = "--:--:--", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)), VerticalAlignment = VerticalAlignment.Center };
            statusPanel.Children.Add(lblOnlineTime);
            statusBorder.Child = statusPanel;
            mainStack.Children.Add(statusBorder);
            mainStack.Children.Add(new Border { Height = 10 });

            // Action Buttons
            var btnPanel2 = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            btnDial = MakeActionBtn("拨号连接", BtnDial_Click, "#2196F3");
            btnDisconnect = MakeActionBtn("断开连接", BtnDisconnect_Click, "#F44336");
            btnPanel2.Children.Add(btnDial);
            btnPanel2.Children.Add(new Border { Width = 10 });
            btnPanel2.Children.Add(btnDisconnect);
            mainStack.Children.Add(btnPanel2);
            mainStack.Children.Add(new Border { Height = 10 });

            btnSave = MakeActionBtn("保存设置", BtnSave_Click, "#4CAF50");
            btnSave.HorizontalAlignment = HorizontalAlignment.Center;
            btnSave.Width = 200;
            btnSave.Height = 42;
            mainStack.Children.Add(btnSave);

            Content = mainStack;
        }

        Border CreateCard()
        {
            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 12),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 15, Opacity = 0.08, ShadowDepth = 2 }
            };
        }

        Button MakeBtn(string text, RoutedEventHandler click, string bg, string fg)
        {
            var btn = new Button
            {
                Content = text,
                Background = (Brush)new BrushConverter().ConvertFromString(bg),
                Foreground = (Brush)new BrushConverter().ConvertFromString(fg),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 12,
                Margin = new Thickness(3, 0, 3, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btn.Click += click;
            return btn;
        }

        Button MakeActionBtn(string text, RoutedEventHandler click, string bg)
        {
            var btn = new Button
            {
                Content = text,
                Background = (Brush)new BrushConverter().ConvertFromString(bg),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(20, 10, 20, 10),
                Width = 140,
                Height = 45,
                Margin = new Thickness(5, 0, 5, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btn.Click += click;
            return btn;
        }

        void AddField(Grid grid, string label, int row, out TextBox textBox)
        {
            var lbl = new TextBlock { Text = label, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(lbl, row);
            grid.Children.Add(lbl);

            textBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(10, 8, 10, 8),
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(textBox, row);
            Grid.SetColumn(textBox, 1);
            grid.Children.Add(textBox);
        }

        void AddPasswordField(Grid grid, string label, int row, out PasswordBox textBox)
        {
            var lbl = new TextBlock { Text = label, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(lbl, row);
            grid.Children.Add(lbl);

            textBox = new PasswordBox
            {
                FontSize = 14,
                Padding = new Thickness(10, 8, 10, 8),
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(textBox, row);
            Grid.SetColumn(textBox, 1);
            grid.Children.Add(textBox);
        }
    }
}