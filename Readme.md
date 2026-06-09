# Alarm Notification Email App

Aplikasi desktop WinForms untuk memonitor alarm dari database HMI/SCADA AVEVA Wonderware dan mengirim notifikasi email ke daftar penerima yang dikelola di aplikasi.

## Ringkasan

EmailApp sekarang disiapkan sebagai WinForms control library, bukan web app/Blazor. Output yang dituju adalah DLL berisi `UserControl`, mengikuti pola project FingerPrint.

Database yang dipakai:

| Database | Fungsi |
| --- | --- |
| `EmailDB` | Menyimpan user, SMTP, penerima email, group email, dan tracking pengiriman |
| `WWALMDB` | Database alarm HMI/SCADA yang berisi `AlarmMaster` dan `AlarmDetail` |

Alur utama:

1. User login dari aplikasi desktop.
2. Admin/operator mengatur SMTP dan daftar penerima email.
3. Background monitor membaca alarm baru dari `WWALMDB` selama aplikasi berjalan.
4. Email dikirim ke recipient yang terdaftar.
5. Pengiriman dicatat di `AlarmEmailTracking` agar alarm yang sama tidak dikirim berulang.

## Fitur Desktop

| Area | Fungsi |
| --- | --- |
| EmailAppControl | Control utama untuk dipasang di HMI/designer, berisi tab recipient, SMTP, dan manual email |
| Login | Validasi user dari database aplikasi |
| Recipients | Tambah/hapus penerima email dan group |
| SMTP | Mengatur host, port, user, password, dan from email |
| Manual Email | Kirim email manual ke semua recipient |
| Alarm Monitor | Membaca alarm dari database AVEVA dan mengirim email otomatis saat aplikasi berjalan |

## Struktur Folder

```text
EmailApp/
├── Controls/                UserControl WinForms untuk login, SMTP, recipient, manual email
├── Forms/                   Form utama desktop
├── Desktop/                 Registrasi dependency injection desktop
├── Configuration/           Options class untuk appsettings
├── Data/                    Entity Framework DbContext
├── Migrations/              Migration database aplikasi
├── Models/                  Entity database
├── Services/                Business logic, email service, alarm monitor
├── appsettings.json         Konfigurasi aplikasi
└── EmailApp.csproj          Project desktop WinForms
```

Folder web lama seperti `Components`, `Controllers`, `Contracts`, `Extensions`, dan `wwwroot` sudah dihapus karena tidak dipakai lagi.

## File Penting

| File | Fungsi |
| --- | --- |
| `Controls/EmailAppControl.cs` | UserControl utama yang paling aman dipilih dari HMI/designer |
| `Forms/LoginForm.cs` | Window login |
| `Forms/DashboardForm.cs` | Window utama dengan tab recipient, SMTP, manual email |
| `Controls/LoginControl.cs` | UserControl login |
| `Controls/RecipientControl.cs` | UserControl penerima email dan group |
| `Controls/SmtpControl.cs` | UserControl konfigurasi SMTP |
| `Controls/ManualEmailControl.cs` | UserControl kirim email manual |
| `Desktop/DesktopServiceCollectionExtensions.cs` | Registrasi DbContext, services, options, dan alarm monitor |
| `Services/AlarmMonitoringService.cs` | Background monitor alarm AVEVA |
| `Services/EmailService.cs` | Pengiriman email SMTP |
| `Data/AppDbContext.cs` | Database aplikasi |
| `Data/AlarmDbContext.cs` | Database alarm AVEVA/Wonderware |

## Prasyarat Development

- Windows OS
- .NET 8 SDK dengan workload Windows Desktop
- SQL Server
- SMTP account
- `dotnet-ef` jika perlu migration

Project menggunakan target:

```xml
<OutputType>Library</OutputType>
<TargetFramework>net8.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
```

Karena memakai WinForms, build penuh perlu dijalankan di Windows atau environment yang punya `Microsoft.NET.Sdk.WindowsDesktop`.

## Setup

Restore dependency:

```bash
dotnet restore
```

Build di Windows:

```bash
dotnet build
```

Output debug yang diharapkan:

```text
bin\Debug\net8.0-windows\EmailApp.dll
bin\Debug\net8.0-windows\appsettings.json
```

Control utama untuk dicoba di HMI/designer:

```text
EmailApp.Controls.EmailAppControl
```

## Konfigurasi

Konfigurasi utama ada di `appsettings.json`.

Contoh:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
      "Microsoft.EntityFrameworkCore.Query": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EmailDB;Trusted_Connection=True;TrustServerCertificate=True",
    "AlarmDatabase": "Server=localhost;Database=WWALMDB;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "YOUR_EMAIL@gmail.com",
    "SmtpPass": "YOUR_APP_PASSWORD",
    "FromEmail": "YOUR_EMAIL@gmail.com"
  },
  "Monitoring": {
    "CheckIntervalSeconds": 1,
    "LookbackMinutes": 5,
    "ProcessingLookbackDays": 2,
    "AlarmStates": [
      "UNACK_ALM",
      "UNACK_RTN"
    ]
  }
}
```

Catatan:

- `DefaultConnection` dipakai untuk database aplikasi.
- `AlarmDatabase` dipakai untuk database alarm AVEVA/Wonderware.
- Untuk Gmail, gunakan App Password, bukan password login biasa.
- Simpan password SMTP dan connection string production dengan aman.

## Catatan Build Saat Ini

Di environment Linux, `dotnet restore` bisa berhasil, tetapi `dotnet build` akan gagal karena target WinForms membutuhkan Windows Desktop SDK:

```text
Microsoft.NET.Sdk.WindowsDesktop.targets was not found
```

Ini normal untuk project WinForms. Lakukan build di Windows/Visual Studio.

## Catatan AVEVA/HMI

Aplikasi ini sekarang dibuat mendekati output FingerPrint: sebuah WinForms DLL yang berisi `UserControl`.

Catatan penting:

- Project FingerPrint memakai `.NET Framework 4.8` dan menghasilkan `FingerPrint11.dll`.
- Project EmailApp saat ini memakai `.NET 8 Windows` dan menghasilkan `EmailApp.dll`.
- Jika HMI/AVEVA hanya bisa load `.NET Framework` control, maka `EmailApp.dll` net8.0-windows tetap tidak akan muncul. Dalam kasus itu perlu dibuat wrapper/control khusus `.NET Framework 4.8`.
