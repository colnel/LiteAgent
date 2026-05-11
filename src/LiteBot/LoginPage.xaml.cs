using System.Windows.Input;

namespace LiteBot;
public partial class LoginPage : ContentPage
{
    private string currentCaptcha = string.Empty;
    public LoginPage()
    {
        InitializeComponent();
        GenerateCaptcha();
    }

    // 生成随机验证码
    private void GenerateCaptcha()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Random random = new Random();
        char[] captcha = new char[4];
        for (int i = 0; i < 4; i++)
        {
            captcha[i] = chars[random.Next(chars.Length)];
        }
        currentCaptcha = new string(captcha);

        // 让验证码显示稍微带点样式
        CaptchaLabel.Text = currentCaptcha;

        // 随机设置字体颜色和旋转角度，模拟验证码干扰效果
        CaptchaLabel.TextColor = Color.FromRgb(
            random.Next(0, 150),
            random.Next(50, 180),
            random.Next(80, 200)
        );
        CaptchaLabel.Rotation = random.Next(-8, 8);
        CaptchaLabel.FontSize = random.Next(20, 24);
    }

    // 点击验证码区域刷新
    private void OnCaptchaTapped(object sender, EventArgs e)
    {
        GenerateCaptcha();
    }

    // 登录按钮点击
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string? username = UsernameEntry.Text?.Trim();
        string? password = PasswordEntry.Text;
        string? captchaInput = CaptchaEntry.Text?.Trim();

        if (string.IsNullOrEmpty(username))
        {
            await DisplayAlertAsync("提示", "请输入用户名", "确定");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            await DisplayAlertAsync ("提示", "请输入密码", "确定");
            return;
        }
        if (string.IsNullOrEmpty(captchaInput))
        {
            await DisplayAlertAsync("提示", "请输入验证码", "确定");
            return;
        }
        //为调试方便，所以暂时注释掉验证码验证部分，后期需恢复
        /*
        if (!captchaInput.Equals(currentCaptcha, StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync("提示", "验证码错误", "确定");
            GenerateCaptcha();       // 验证码错误后刷新
            CaptchaEntry.Text = string.Empty;
            return;
        }
        */
        // 模拟登录（此处可接入真实验证逻辑）
        await DisplayAlertAsync("登录成功", $"欢迎, {username}!", "确定");
        if ( Application.Current is not null)
        {
            // 登录成功，跳转到主页
            Application.Current.Windows[0].Width = 1440;
            Application.Current.Windows[0].Height = 810;
            // 使用 Application.Current.MainPage 进行导航
            Application.Current.ActivateWindow(Application.Current.Windows[0]);
            Application.Current.MainPage = new MainPage();
            // 可选：清除登录页，防止返回
            // 但简单示例保持堆栈即可
        }
    }   

    // 注册链接点击
    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await DisplayAlertAsync("注册", "跳转至注册页面（功能待实现）", "确定");
    }

    // 找回密码链接点击
    private async void OnForgotPwdTapped(object sender, EventArgs e)
    {
        await DisplayAlertAsync("找回密码", "跳转至找回密码页面（功能待实现）", "确定");
    }
}