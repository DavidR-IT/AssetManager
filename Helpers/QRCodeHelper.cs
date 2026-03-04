using QRCoder;

namespace AssetManager.Helpers
{
    public static class QRCodeHelper
    {

        /// <summary>
        /// Generates a QR code as a PNG byte array
        /// </summary>
        /// <param name="data">The data to encode in the QR code</param>
        /// <param name="pixelsPerModule">Size of each module/pixel (default 20)</param>
        /// <returns>PNG image as byte array</returns>
        public static byte[] GenerateQRCodeBytes(string data, int pixelsPerModule = 20)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                return qrCode.GetGraphic(pixelsPerModule);
            }
        }
    }
}
