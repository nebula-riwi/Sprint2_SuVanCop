using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Drawing;
using Amazon.S3;
using System.Drawing.Printing;
using System.Net;
using ZXing;
using ZXing.Common;
using System.Net.Http;
using Amazon.S3.Transfer;
using SuVanCop.Models;
using SuVanCop.Data;

namespace SuVanCop.Controllers;

public class UserController : Controller
{
    private readonly PostgresDbContext _context;
    private readonly IConfiguration _config;

    public UserController(PostgresDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }





    private Bitmap GenerarCodigoDeBarras(string nuip)
    {
        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = ZXing.BarcodeFormat.CODE_128,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = 160,
                Height = 60,
                Margin = 2
            }
        };

        var pixelData = writer.Write(nuip);

        var bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
            System.Drawing.Imaging.ImageLockMode.WriteOnly,
            bitmap.PixelFormat);
        try
        {
            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

  private void ImprimirTicketCarnet(User user)
{
    PrintDocument pd = new PrintDocument();
    pd.PrinterSettings.PrinterName = "XP-58";

    pd.PrintPage += (sender, e) =>
    {
        Graphics g = e.Graphics;
        int ancho = 220;
        int alto = 500; 

        
        g.FillRectangle(Brushes.White, 0, 0, ancho, alto);

        
        using var httpLogo = new HttpClient();
        var logoBytes = httpLogo.GetByteArrayAsync("https://i.ibb.co/Qv3zm29F/image-1759875201459-removebg-preview.png").Result;
        using var logoStream = new MemoryStream(logoBytes);
        var logo = Image.FromStream(logoStream);

            int logoWidth = 100;
            int logoHeight = 40;
            int logoX = (ancho - logoWidth) / 2;
            int logoY = 10;

            g.DrawImage(logo, logoX, logoY, logoWidth, logoHeight);

        
        g.DrawLine(new Pen(Color.DarkGray, 10), 15, 40, ancho - 15, 40);

        
        try
        {
            using var httpFoto = new HttpClient();
            var fotoBytes = httpFoto.GetByteArrayAsync(user.PictureUrl).Result;

            using var fotoStream = new MemoryStream(fotoBytes);
            var foto = Image.FromStream(fotoStream);

            int imgSize = 120;
            int imgX = (ancho - imgSize) / 2;
            int imgY = 50;

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(imgX, imgY, imgSize, imgSize);
            g.SetClip(path);
            g.DrawImage(foto, imgX, imgY, imgSize, imgSize);
            g.ResetClip();
        }
        catch
        {
            g.DrawString("[Foto no disponible]", new Font("Arial", 8, FontStyle.Italic), Brushes.Gray, (ancho - 100) / 2, 90);
        }

        
        g.DrawLine(Pens.LightGray, 20, 160, ancho - 20, 160);

        
        var datosFont = new Font("Consolas", 10, FontStyle.Regular);
        string nombre = $"Nombre: {user.Names} {user.LastNames}";
        string nuip = $"NUIP: {user.Nuip}";
        string rh = $"RH: {user.Rh}";

        g.DrawString(nombre, datosFont, Brushes.Black, (ancho - g.MeasureString(nombre, datosFont).Width) / 2, 170);
        g.DrawString(nuip, datosFont, Brushes.Black, (ancho - g.MeasureString(nuip, datosFont).Width) / 2, 195);
        g.DrawString(rh, datosFont, Brushes.Black, (ancho - g.MeasureString(nuip, datosFont).Width) / 2, 225);

        // Línea decorativa
        g.DrawLine(Pens.LightGray, 20, 220, ancho - 20, 220);

        var barcodeImage = GenerarCodigoDeBarras(user.Nuip);
        g.DrawImage(barcodeImage, (ancho - 160) / 2, 250, 160, 60);

       
    };

    pd.Print();
}


    public IActionResult Index()
    {
        var users = _context.users.ToList();
        return View(users);
    }
    [HttpPost]
    public IActionResult Create([Bind("Names,LastNames,Nuip,PictureUrl,Rh")] User user)
    {
        if (ModelState.IsValid)
        {
            
            if (!string.IsNullOrEmpty(user.PictureUrl) && user.PictureUrl.StartsWith("data:image"))
            {
                var base64Data = user.PictureUrl.Split(',')[1];
                var bytes = Convert.FromBase64String(base64Data);
                var fileName = $"foto_{DateTime.Now.Ticks}.png";

                
                var bucketName = _config["AWS:BucketName"];
                var accessKey = _config["AWS:AccessKey"];
                var secretKey = _config["AWS:SecretKey"];
                var region = Amazon.RegionEndpoint.GetBySystemName(_config["AWS:Region"]);

                using var s3Client = new AmazonS3Client(accessKey, secretKey, region);
                using var stream = new MemoryStream(bytes);
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = fileName,
                    BucketName = bucketName,
                    ContentType = "image/png"
                };

                var transferUtility = new TransferUtility(s3Client);
                transferUtility.Upload(uploadRequest); // 👈 Sincrónico

                
                user.PictureUrl = $"https://{bucketName}.s3.amazonaws.com/{fileName}";
            }

            _context.users.Add(user);
            _context.SaveChanges(); // 👈 Sincrónico

            ImprimirTicketCarnet(user); // 👈 Llamada directa

            TempData["message"] = "Usuario creado exitosamente!";
            return RedirectToAction(nameof(Index));
        }

        return View(user);
    }

    
    public IActionResult Destroy(int id)
    {
        var user = _context.users.Find(id);
        if (user == null)
        {
            return NotFound();
        }
        _context.users.Remove(user);
        _context.SaveChanges();
        TempData["message"] = "Usuario eliminado exitosamente!";
        
        return RedirectToAction(nameof(Index));
        
    }
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var user = _context.users.Find(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }
    public IActionResult Edit(int id, [Bind("Names,LastNames,Nuip")]User UpdateUser)
    {
        var user = _context.users.Find(id);
        if (user == null)
            {
            return NotFound();
            }
        user.Names = UpdateUser.Names;
        user.LastNames = UpdateUser.LastNames;
        user.Nuip = UpdateUser.Nuip;
        _context.SaveChanges();
        TempData["message"] = "Usuario editado exitosamente!";
        return RedirectToAction(nameof(Index));
        
    }
    public IActionResult Details(int id)
    {
        var user = _context.users.Find(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }
    public IActionResult DetailsByNuip(string nuip)
    {
        var user = _context.users.FirstOrDefault(u => u.Nuip == nuip);
        if (user == null)
        {
            return NotFound();
        }
        return View("Details", user);
    }


}