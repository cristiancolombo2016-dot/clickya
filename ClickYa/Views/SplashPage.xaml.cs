using Microsoft.Maui.Controls;

namespace ClickYa.Views
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await Task.Delay(200);

            // LOGO aparece + rebote
            await Logo.FadeTo(1, 700);
            await Logo.ScaleTo(1.15, 700, Easing.CubicOut);
            await Logo.ScaleTo(1.0, 400, Easing.CubicIn);

            // TEXTO aparece
            await Titulo.FadeTo(1, 600);

            await Task.Delay(800);

            // IR A HOMEPAGE (CORREGIDO)
            await Shell.Current.GoToAsync("//HomePage");



        }
    }
}
