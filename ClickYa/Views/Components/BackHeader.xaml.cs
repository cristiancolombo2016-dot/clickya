namespace ClickYa.Views.Components;

public partial class BackHeader : ContentView
{
    public BackHeader()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
