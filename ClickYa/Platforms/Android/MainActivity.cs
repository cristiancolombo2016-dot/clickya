using Android.App;
using Android.Content.PM;
using Android.OS;

namespace ClickYa;

[Activity(
    Theme = "@style/Maui.MainTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // 🔥 Ocultamos SOLO la ActionBar nativa de Android
        if (SupportActionBar != null)
            SupportActionBar.Hide();
    }
}
