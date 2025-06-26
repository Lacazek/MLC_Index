using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

public class ProgressWindow : Window
{
    private ProgressBar progressBar;
    private TextBlock progressText;

    public ProgressWindow(int max)
    {
        Title = "Calcul des indices en attente...";
        Width = 400;
        Height = 100;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        var stackPanel = new StackPanel { Margin = new Thickness(10) };

        progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 20
        };

        progressText = new TextBlock
        {
            Text = "0%",
            Margin = new Thickness(0, 5, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        stackPanel.Children.Add(progressBar);
        stackPanel.Children.Add(progressText);

        Content = stackPanel;
    }

    public void UpdateProgress(int value, string text)
    {
        progressBar.Value = value;
        progressText.Text = text;

        Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate { }));


    }
}
