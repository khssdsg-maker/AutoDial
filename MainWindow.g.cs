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
            Height = 780;
            Width = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF5));
            FontFamily = new FontFamily("Microsoft YaHei");

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var mainStack = new StackPanel { Margin = new Thickness(20) };

            // Title bar with changelog button
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            titlePanel.Children.Add(new TextBlock { Text = "宽带自动拨号", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)), VerticalAlignment = VerticalAlignment.Center });
            var btnChangelog = new Button
            {
                Content = "更新日志 v2.4",
                FontSize = 11,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(10, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xED)),
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                BorderThickness = new Thickness(0),
                ToolTip = "查看软件更新历史"
            };
            btnChangelog.Click += BtnChangelog_Click;
            titlePanel.Children.Add(btnChangelog);
            mainStack.Children.Add(titlePanel);

            // Account List Card
            var card1 = CreateCard();
            var card1Stack = new StackPanel();
            card1Stack.Children.Add(new TextBlock { Text = "账号列表", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
            lstAccounts = new ListBox { Height = 80, FontSize = 13, BorderThickness = new Thickness(0) };
            lstAccounts.SelectionChanged += lstAccounts_SelectionChanged;
            card1Stack.Children.Add(lstAccounts);
            var btnPanel1 = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
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
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
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
            settingsStack.Children.Add(new TextBlock { Text = "设置", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
            var checksPanel = new StackPanel { Orientation = Orientation.Horizontal };
            chkAutoDial = new CheckBox { Content = "开机自动拨号", FontSize = 12, Margin = new Thickness(0, 0, 12, 0) };
            chkSilent = new CheckBox { Content = "静默拨号", FontSize = 12, Margin = new Thickness(0, 0, 12, 0) };
            chkAutoReconnect = new CheckBox { Content = "断线自动重连", FontSize = 12 };
            checksPanel.Children.Add(chkAutoDial);
            checksPanel.Children.Add(chkSilent);
            checksPanel.Children.Add(chkAutoReconnect);
            settingsStack.Children.Add(checksPanel);
            card3.Child = settingsStack;
            mainStack.Children.Add(card3);

            // Status
            var statusBorder = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 10) };
            var statusPanel = new StackPanel { Orientation = Orientation.Horizontal };
            statusDot = new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            statusPanel.Children.Add(statusDot);
            lblStatus = new TextBlock { Text = "未连接", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0) };
            statusPanel.Children.Add(lblStatus);
            lblOnlineTime = new TextBlock { Text = "--:--:--", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)), VerticalAlignment = VerticalAlignment.Center };
            statusPanel.Children.Add(lblOnlineTime);
            statusBorder.Child = statusPanel;
            mainStack.Children.Add(statusBorder);

            // Action Buttons
            var btnPanel2 = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            btnDial = MakeActionBtn("拨号连接", BtnDial_Click, "#2196F3");
            btnDisconnect = MakeActionBtn("断开连接", BtnDisconnect_Click, "#F44336");
            btnPanel2.Children.Add(btnDial);
            btnPanel2.Children.Add(new Border { Width = 10 });
            btnPanel2.Children.Add(btnDisconnect);
            mainStack.Children.Add(btnPanel2);

            // Save Button
            btnSave = MakeActionBtn("保存设置", BtnSave_Click, "#4CAF50");
            btnSave.HorizontalAlignment = HorizontalAlignment.Center;
            btnSave.Width = 180;
            btnSave.Height = 40;
            mainStack.Children.Add(btnSave);

            scrollViewer.Content = mainStack;
            Content = scrollViewer;
        }

        Border CreateCard()
        {
            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, Opacity = 0.06, ShadowDepth = 2 }
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
                Padding = new Thickness(10, 5, 10, 5),
                FontSize = 11,
                Margin = new Thickness(2, 0, 2, 0),
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
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(18, 8, 18, 8),
                Width = 130,
                Height = 40,
                Margin = new Thickness(4, 0, 4, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btn.Click += click;
            return btn;
        }

        void AddField(Grid grid, string label, int row, out TextBox textBox)
        {
            var lbl = new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(lbl, row);
            grid.Children.Add(lbl);

            textBox = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(8, 6, 8, 6),
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

        TextBox txtPassVisible;
        bool isPasswordVisible = false;

        void AddPasswordField(Grid grid, string label, int row, out PasswordBox textBox)
        {
            var lbl = new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(lbl, row);
            grid.Children.Add(lbl);

            var wrapper = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            wrapper.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            textBox = new PasswordBox
            {
                FontSize = 13,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33))
            };
            Grid.SetColumn(textBox, 0);
            wrapper.Children.Add(textBox);

            txtPassVisible = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(txtPassVisible, 0);
            wrapper.Children.Add(txtPassVisible);

            var toggleBtn = new Button
            {
                Content = "👁",
                FontSize = 14,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(4, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(0),
                ToolTip = "点击显示/隐藏密码"
            };
            toggleBtn.Click += TogglePassword_Click;
            Grid.SetColumn(toggleBtn, 1);
            wrapper.Children.Add(toggleBtn);

            Grid.SetRow(wrapper, row);
            Grid.SetColumn(wrapper, 1);
            grid.Children.Add(wrapper);
        }

        void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            if (isPasswordVisible)
            {
                txtPassVisible.Text = txtPass.Password;
                txtPass.Visibility = Visibility.Collapsed;
                txtPassVisible.Visibility = Visibility.Visible;
            }
            else
            {
                txtPass.Password = txtPassVisible.Text;
                txtPass.Visibility = Visibility.Visible;
                txtPassVisible.Visibility = Visibility.Collapsed;
            }
        }
    }
}