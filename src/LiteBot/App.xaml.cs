namespace LiteBot;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new LoginPage()) { 
            TitleBar = new TitleBar() {
                Background = new SolidColorBrush(Colors.Transparent),
            },
            Title = "LiteBot" ,
            Width=400,
            Height=560,
        };
    }
}
