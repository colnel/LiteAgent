namespace LiteBot;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Clear(); // 清空本地存储
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
