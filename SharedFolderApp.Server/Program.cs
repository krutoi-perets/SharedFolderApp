using SharedFolderApp.Server.Services;
using System.Net.NetworkInformation;

var builder = WebApplication.CreateBuilder(args);

// Создание серверной корневой папки
var serverFolder = Path.Combine(
    AppContext.BaseDirectory,
    "ServerFolder"
);
Directory.CreateDirectory(serverFolder);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(new FileService(serverFolder));
builder.Services.AddSingleton<ArchiveService>();

builder.WebHost.UseUrls("http://0.0.0.0:5212");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

//Console.WriteLine("Сервер запущен");
//Console.WriteLine("Доступные IPv4 адреса:");

//foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
//{
//    if (networkInterface.OperationalStatus != OperationalStatus.Up)
//        continue;

//    foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
//    {
//        if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
//        {
//            Console.WriteLine($"    {address.Address}:5212");
//        }
//    }
//}

app.Run();
