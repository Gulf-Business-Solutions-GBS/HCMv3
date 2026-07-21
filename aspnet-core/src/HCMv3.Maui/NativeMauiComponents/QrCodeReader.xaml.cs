using Abp;
using ZXing.Net.Maui;

namespace HCMv3.Maui.NativeMauiComponents;

public partial class QrCodeReader : HCMv3NativeMauiComponentBase
{
    public event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;

    public QrCodeReader()
    {

        InitializeComponent();
    }

    private async void scanner_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        try
        {
            if (e.Results == null)
            {
                throw new AbpException("Scan results are empty!");
            }

            OnBarcodesDetected(e);

            Scanner.IsDetecting = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "An error occurred: " + ex.Message, "OK");
        }
    }

    protected virtual void OnBarcodesDetected(BarcodeDetectionEventArgs e)
    {
        BarcodesDetected?.Invoke(this, e);
    }
}