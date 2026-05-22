using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace ClickYa.Views;

public partial class ImageViewerPage : ContentPage
{
    double currentScale = 1;
    double startScale = 1;

    public ImageViewerPage(List<string> images, int startIndex = 0)
    {
        InitializeComponent();

        images ??= new List<string>();
        Carousel.ItemsSource = images;
        Carousel.Position = Math.Max(0, Math.Min(startIndex, images.Count - 1));
    }

    void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (sender is not Image image)
            return;

        if (e.Status == GestureStatus.Started)
        {
            startScale = image.Scale;
        }
        else if (e.Status == GestureStatus.Running)
        {
            double newScale = startScale * e.Scale;
            image.Scale = Math.Max(1, Math.Min(newScale, 4)); // zoom 1x a 4x
        }
    }

    async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
