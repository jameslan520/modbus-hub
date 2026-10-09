using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScadaApp.Models;
using ScadaApp.Services;

namespace ScadaApp.Views;

public partial class ChannelConfigDialog : Window
{
    private readonly ChannelConfig _config;
    private readonly Snapshot _snapshot;

    public ChannelConfigDialog(ChannelConfig config, IEnumerable<string> ports, IEnumerable<int> baudRates)
    {
        _config = config;
        _snapshot = Snapshot.Capture(config);
        InitializeComponent();

        var portList = ports
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!string.IsNullOrWhiteSpace(config.PortName) &&
            !portList.Exists(p => string.Equals(p, config.PortName, StringComparison.OrdinalIgnoreCase)))
            portList.Insert(0, config.PortName.Trim());

        PortCombo.ItemsSource = portList;
        BaudCombo.ItemsSource = baudRates;

        DataBitsCombo.ItemsSource = SerialPortHelper.DataBitsOptions;

        // Windows 上 1.5 停止位仅在「数据位=5」时可用，而本项目只提供 7/8 数据位，
        // 暴露出来必然导致串口打开失败，因此不在界面上给出该选项。
        StopBitsCombo.ItemsSource = SerialPortHelper.StopBitsOptions
            .Where(s => s != StopBits.OnePointFive)
            .Select(s => new StopBitsOption(DescribeStopBits(s), s))
            .ToList();

        ParityCombo.ItemsSource = SerialPortHelper.ParityOptions
            .Select(p => new ParityOption(DescribeParity(p), p))
            .ToList();

        DataContext = config;
    }

    private static string DescribeStopBits(StopBits value) => value switch
    {
        StopBits.One => "1 位",
        StopBits.Two => "2 位",
        _ => value.ToString()
    };

    private static string DescribeParity(Parity value) => value switch
    {
        Parity.None => "无校验 (None)",
        Parity.Odd => "奇校验 (Odd)",
        Parity.Even => "偶校验 (Even)",
        Parity.Mark => "校验位恒为 1 (Mark)",
        Parity.Space => "校验位恒为 0 (Space)",
        _ => value.ToString()
    };

    private sealed record StopBitsOption(string Text, StopBits Value);

    private sealed record ParityOption(string Text, Parity Value);

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        PortCombo.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
        PortCombo.GetBindingExpression(ComboBox.SelectedItemProperty)?.UpdateSource();
        BaudCombo.GetBindingExpression(ComboBox.SelectedItemProperty)?.UpdateSource();
        DataBitsCombo.GetBindingExpression(ComboBox.SelectedValueProperty)?.UpdateSource();
        StopBitsCombo.GetBindingExpression(ComboBox.SelectedValueProperty)?.UpdateSource();
        ParityCombo.GetBindingExpression(ComboBox.SelectedValueProperty)?.UpdateSource();

        var port = (PortCombo.SelectedItem as string)?.Trim();
        if (string.IsNullOrWhiteSpace(port))
            port = PortCombo.Text?.Trim();

        _config.Name = _config.Name?.Trim() ?? string.Empty;
        _config.PortName = port ?? string.Empty;
        if (_config.SlaveId is < 1 or > 247)
            _config.SlaveId = 1;

        foreach (var tag in _config.Tags)
            tag.SlaveId = _config.SlaveId;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _snapshot.Restore(_config);
        DialogResult = false;
        Close();
    }

    public static bool Edit(ChannelConfig config, IEnumerable<string> ports, IEnumerable<int> baudRates)
    {
        var dialog = new ChannelConfigDialog(config, ports, baudRates)
        {
            Owner = Application.Current.MainWindow
        };
        return dialog.ShowDialog() == true;
    }

    private sealed record Snapshot(
        string Name,
        string PortName,
        int BaudRate,
        int DataBits,
        StopBits StopBits,
        Parity Parity,
        int PollingIntervalMs,
        byte SlaveId)
    {
        public static Snapshot Capture(ChannelConfig config) => new(
            config.Name,
            config.PortName,
            config.BaudRate,
            config.DataBits,
            config.StopBits,
            config.Parity,
            config.PollingIntervalMs,
            config.SlaveId);

        public void Restore(ChannelConfig config)
        {
            config.Name = Name;
            config.PortName = PortName;
            config.BaudRate = BaudRate;
            config.DataBits = DataBits;
            config.StopBits = StopBits;
            config.Parity = Parity;
            config.PollingIntervalMs = PollingIntervalMs;
            config.SlaveId = SlaveId;
        }
    }
}
