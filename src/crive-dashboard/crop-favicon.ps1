Add-Type -AssemblyName System.Drawing

$csharp = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class ImageCropper {
    public static Rectangle GetCropBounds(Bitmap bmp) {
        int w = bmp.Width;
        int h = bmp.Height;
        int minX = w, maxX = 0, minY = h, maxY = 0;
        
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int bytes = Math.Abs(data.Stride) * h;
        byte[] rgbValues = new byte[bytes];
        Marshal.Copy(data.Scan0, rgbValues, 0, bytes);
        bmp.UnlockBits(data);
        
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                int index = (y * data.Stride) + (x * 4);
                byte alpha = rgbValues[index + 3];
                if (alpha > 5) {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }
        
        if (minX > maxX || minY > maxY) return new Rectangle(0, 0, w, h);
        
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        int size = Math.Max(width, height);
        
        // Add 5% padding so it breathes a little
        int padding = (int)(size * 0.05);
        size += padding * 2;
        
        int sqX = minX - padding - (size - width - padding * 2) / 2;
        int sqY = minY - padding - (size - height - padding * 2) / 2;
        
        return new Rectangle(sqX, sqY, size, size);
    }
}
"@

Add-Type -TypeDefinition $csharp -ReferencedAssemblies System.Drawing

$srcPath = "C:\Users\Bryan\.gemini\antigravity\brain\b2363ed4-d068-4c24-9320-831a44bb497b\.user_uploaded\media_1787601024728.png"
$srcPath = "C:\Users\Bryan\.gemini\antigravity\brain\b2363ed4-d068-4c24-9320-831a44bb497b\.user_uploaded\media_1787600888080.png"
$pubPath = "c:\Users\Bryan\OneDrive\Documentos\Projetos Bryan - 01\Crive\src\crive-dashboard\public"

$srcImg = [System.Drawing.Bitmap]::FromFile($srcPath)
$bounds = [ImageCropper]::GetCropBounds($srcImg)

Write-Host "Cropping to: X=$($bounds.X), Y=$($bounds.Y), Size=$($bounds.Width)"

function Save-ResizedImage {
    param($img, $bounds, $size, $outPath)
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $graph = [System.Drawing.Graphics]::FromImage($bmp)
    $graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graph.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graph.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graph.Clear([System.Drawing.Color]::Transparent)
    
    $destRect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $graph.DrawImage($img, $destRect, $bounds.X, $bounds.Y, $bounds.Width, $bounds.Height, [System.Drawing.GraphicsUnit]::Pixel)
    
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graph.Dispose()
    $bmp.Dispose()
}

Save-ResizedImage $srcImg $bounds 16 "$pubPath\favicon-16x16.png"
Save-ResizedImage $srcImg $bounds 32 "$pubPath\favicon-32x32.png"
Save-ResizedImage $srcImg $bounds 48 "$pubPath\favicon-48x48.png"
Save-ResizedImage $srcImg $bounds 180 "$pubPath\apple-touch-icon.png"
Save-ResizedImage $srcImg $bounds 512 "$pubPath\favicon.png"

$srcImg.Dispose()
Write-Host "Icons auto-cropped and generated successfully."
