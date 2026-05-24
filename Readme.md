# Alarm Notification Email App

Aplikasi ini digunakan untuk memonitor alarm dari database HMI/SCADA AVEVA Wonderware dan mengirimkan notifikasi email ke daftar penerima yang dikelola dari web aplikasi.

Dokumentasi ini dibagi menjadi dua bagian:

- Panduan User: untuk operator/admin yang memakai aplikasi.
- Panduan Programmer: untuk developer yang melakukan setup, maintenance, dan pengembangan.

## Ringkasan Sistem

EmailApp adalah aplikasi ASP.NET Core Blazor Server dengan dua koneksi database:

| Database | Fungsi |
| --- | --- |
| `EmailDb` | Database aplikasi untuk user, penerima email, SMTP, group email, dan tracking pengiriman |
| `WWALMDB` | Database alarm dari HMI/SCADA yang berisi `AlarmMaster` dan `AlarmDetail` |

Alur utama:

1. User login ke aplikasi.
2. Admin mengatur SMTP dan daftar penerima email.
3. Background service membaca alarm baru dari `WWALMDB`.
4. Jika ada alarm dengan state yang dikonfigurasi, sistem mengirim email ke penerima aktif.
5. Status pengiriman dicatat di `AlarmEmailTracking` agar alarm yang sama tidak dikirim berulang.

## Panduan User

### Login

1. Buka aplikasi di browser.
2. Masuk ke halaman `/login`.
3. Isi username dan password.
4. Setelah berhasil login, user akan diarahkan ke halaman utama.

Jika halaman utama menampilkan login kembali setelah refresh, pastikan login berhasil dan cookie browser tidak diblokir.

### Home

Menu `Home` menampilkan data alarm dari database alarm. Halaman ini digunakan untuk melihat kondisi alarm dan ringkasan data yang sedang dipantau.

### User Management

Menu `Users` digunakan untuk mengelola akun aplikasi.

Fitur utama:

- Menambah user baru.
- Menghapus user.
- Mengatur role admin atau user.

Catatan:

- Menu ini hanya boleh digunakan oleh administrator.
- Gunakan password yang kuat.
- Jangan berbagi akun antar operator.

### Email Settings

Menu `Email Settings` digunakan untuk mengelola seluruh konfigurasi email dari satu tempat.

Fitur utama:

| Bagian | Fungsi |
| --- | --- |
| SMTP Server | Mengatur server SMTP pengirim email |
| Manual Email | Mengirim email manual ke semua penerima |
| Recipients | Mengelola alamat email penerima alarm |
| Groups | Mengelompokkan penerima email |

### Mengatur SMTP

Isi data SMTP sesuai email pengirim yang digunakan.

Contoh Gmail:

| Field | Contoh |
| --- | --- |
| Host | `smtp.gmail.com` |
| Port | `587` |
| User | `nama-email@gmail.com` |
| Password | App Password Gmail |
| From Email | `nama-email@gmail.com` |

Untuk Gmail, password yang digunakan biasanya bukan password login Gmail, tetapi App Password.

### Menambahkan Penerima Email

1. Buka `Email Settings`.
2. Isi alamat email penerima.
3. Pilih group jika diperlukan.
4. Klik `Add Email`.

Email yang sudah terdaftar akan menerima notifikasi ketika alarm baru terdeteksi.

### Mengirim Email Manual

1. Buka `Email Settings`.
2. Isi subject dan message pada bagian `Manual Email`.
3. Klik `Send to all`.

Fitur ini berguna untuk memastikan konfigurasi SMTP dan daftar penerima sudah benar.

### Logout

Klik tombol `Log out` di sidebar untuk keluar dari aplikasi.

## Panduan Programmer

### Tech Stack

| Komponen | Teknologi |
| --- | --- |
| Runtime | .NET 8 |
| UI | ASP.NET Core Blazor Server |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core |
| Email | MailKit dan MimeKit |
| Authentication | Cookie Authentication dan JWT |
| Password Hashing | BCrypt.Net |
| Background Worker | `BackgroundService` |

### Struktur Folder

```text
EmailApp/
├── Components/
│   ├── Layout/              UI layout, sidebar, empty layout
│   └── Pages/               Halaman Blazor
├── Configuration/           Options class untuk appsettings
├── Contracts/               Request DTO untuk API
├── Controllers/             API controller
├── Data/                    Entity Framework DbContext
├── Extensions/              Registrasi dependency injection dan authentication
├── Migrations/              Migration untuk database aplikasi
├── Models/                  Entity database
├── Services/                Business logic dan background service
├── wwwroot/                 Static assets, CSS, JavaScript, logo
├── appsettings.json         Konfigurasi aplikasi
├── Program.cs               HTTP pipeline dan startup aplikasi
└── EmailApp.csproj          Project file
```

### File Penting

| File | Fungsi |
| --- | --- |
| `Program.cs` | Middleware, authentication, authorization, route Blazor, controller API |
| `Extensions/ServiceCollectionExtensions.cs` | Registrasi service, DbContext, auth, HTTP client |
| `Data/AppDbContext.cs` | Database aplikasi `EmailDb` |
| `Data/AlarmDbContext.cs` | Mapping database alarm `WWALMDB` |
| `Services/AlarmMonitoringService.cs` | Background service pembaca alarm dan pengirim notifikasi |
| `Services/EmailService.cs` | Pengiriman email SMTP dan format body email |
| `Services/SmtpSettingsService.cs` | Pengelolaan konfigurasi SMTP dari database |
| `Components/Pages/Emails.razor` | UI Email Settings |
| `Components/Pages/Login.razor` | UI login |
| `Controllers/AuthController.cs` | API login/logout |
| `Controllers/AlarmController.cs` | API insert alarm manual |

### Prasyarat Development

Pastikan tools berikut tersedia:

- .NET 8 SDK
- SQL Server
- `dotnet-ef`
- SMTP account

Cek versi .NET:

```bash
dotnet --version
```

Install `dotnet-ef` jika belum tersedia:

```bash
dotnet tool install --global dotnet-ef
```

Jika memakai fish shell:

```bash
fish_add_path ~/.dotnet/tools
```

### Setup Project

Restore dependency:

```bash
dotnet restore
```

Build project:

```bash
dotnet build
```

Jalankan aplikasi:

```bash
dotnet run
```

Jika ingin menjalankan di port yang spesifik:

```bash
dotnet run --urls http://localhost:5146
```

### Konfigurasi

Konfigurasi utama berada di `appsettings.json`.

Contoh aman untuk development:

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
      "Microsoft.EntityFrameworkCore.Query": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EmailDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True",
    "AlarmDatabase": "Server=localhost;Database=WWALMDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "YOUR_EMAIL@gmail.com",
    "SmtpPass": "YOUR_APP_PASSWORD",
    "FromEmail": "YOUR_EMAIL@gmail.com"
  },
  "Jwt": {
    "Key": "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY",
    "Issuer": "EmailApp",
    "Audience": "EmailAppUsers"
  },
  "ApiBaseUrl": "http://localhost:5146",
  "Monitoring": {
    "CheckIntervalSeconds": 10,
    "LookbackMinutes": 5,
    "AlarmStates": [
      "UNACK_ALM",
      "UNACK_RTN"
    ]
  }
}
```

Catatan security:

- Jangan commit password database, SMTP password, atau JWT key production.
- Untuk production, gunakan environment variable, secret manager, atau konfigurasi server.
- Minimal panjang `Jwt:Key` harus cukup kuat untuk HMAC SHA256.

Override konfigurasi dengan environment variable:

```bash
ConnectionStrings__DefaultConnection="Server=localhost;Database=EmailDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
ConnectionStrings__AlarmDatabase="Server=localhost;Database=WWALMDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
Email__SmtpPass="YOUR_APP_PASSWORD"
Jwt__Key="YOUR_LONG_RANDOM_SECRET"
```

### Database

#### Database Aplikasi

Database aplikasi menggunakan `AppDbContext` dan migration EF Core.

Tabel utama:

| Tabel | Fungsi |
| --- | --- |
| `Users` | Akun aplikasi |
| `Emails` | Daftar penerima email |
| `Groups` | Group user lama |
| `UserGroups` | Relasi user dan group lama |
| `EmailGroups` | Group penerima email |
| `SetSmtp` | Konfigurasi SMTP aktif |
| `AlarmEmailTracking` | Tracking alarm yang sudah diproses email |

Karena project memiliki lebih dari satu `DbContext`, selalu sebutkan context saat menjalankan EF command.

Update database aplikasi:

```bash
dotnet ef database update --context AppDbContext
```

Membuat migration baru:

```bash
dotnet ef migrations add NamaMigration --context AppDbContext
```

#### Database Alarm

Database alarm menggunakan `AlarmDbContext`.

Tabel yang dibaca:

| Tabel | Fungsi |
| --- | --- |
| `AlarmMaster` | Master alarm, tag, group, priority |
| `AlarmDetail` | Event/transisi alarm |

`AlarmDbContext` digunakan sebagai mapping ke database HMI/SCADA. Jangan membuat migration untuk database alarm kecuali memang database tersebut dimiliki oleh aplikasi ini.

### Authentication

Aplikasi menggunakan dua mekanisme:

| Mekanisme | Fungsi |
| --- | --- |
| Cookie Authentication | Session browser untuk halaman Blazor |
| JWT | Response API login dan kebutuhan API client |

Login diproses oleh `POST /api/auth/login`.

Setelah login berhasil:

- Server membuat authentication cookie.
- API juga mengembalikan JWT token.
- Blazor menggunakan cookie untuk menjaga session saat reload halaman.

### Background Monitoring

Service utama: `Services/AlarmMonitoringService.cs`.

Alur kerja:

1. Service berjalan otomatis saat aplikasi start.
2. Service membaca alarm dari `WWALMDB.dbo.AlarmDetail`.
3. Alarm yang diproses mengikuti konfigurasi `Monitoring:AlarmStates`.
4. Service hanya membaca alarm dalam rentang waktu terbaru berdasarkan `LookbackMinutes` dan `_lastCheckTime`.
5. Service mengecek `AlarmEmailTracking` agar alarm tidak dikirim dua kali.
6. Email dikirim ke semua address di tabel `Emails`.
7. Hasil pengiriman dicatat ke `AlarmEmailTracking`.

Konfigurasi interval:

```json
{
  "Monitoring": {
    "CheckIntervalSeconds": 10,
    "LookbackMinutes": 5,
    "AlarmStates": [
      "UNACK_ALM",
      "UNACK_RTN"
    ]
  }
}
```

### Email Flow

Service utama: `Services/EmailService.cs`.

Sumber konfigurasi SMTP:

1. Data `SetSmtp` dari database aplikasi.
2. Jika data SMTP di database kosong, fallback ke section `Email` di `appsettings.json`.

Format email:

- Sistem mengirim HTML email agar tampilan lebih rapi.
- Plain text fallback tetap tersedia untuk email client yang tidak mendukung HTML.
- Body email di-encode sebelum masuk HTML untuk menghindari injection.

### API Endpoint

#### Login

```http
POST /api/auth/login
Content-Type: application/json
```

Request:

```json
{
  "username": "admin",
  "password": "admin123"
}
```

Response sukses:

```json
{
  "token": "JWT_TOKEN",
  "user": {
    "id": "USER_ID",
    "username": "admin",
    "isAdmin": true
  }
}
```

#### Logout

```http
POST /api/auth/logout
```

#### Insert Alarm Manual

```http
POST /api/alarm
Content-Type: application/json
```

Request:

```json
{
  "alarmId": 1,
  "alarmState": "UNACK_ALM",
  "eventStamp": "2026-05-25T10:30:00",
  "priority": 500,
  "operatorName": "Operator"
}
```

Catatan:

- `alarmId` harus ada di tabel `AlarmMaster`.
- Jika penerima email tersedia, sistem akan mengirim email setelah alarm dibuat.

### Routing Halaman

| Route | Akses | Fungsi |
| --- | --- | --- |
| `/login` | Public | Login |
| `/` | Authenticated | Home/dashboard |
| `/users` | Admin | User management |
| `/email-settings` | Admin | SMTP, recipients, groups, manual email |
| `/emails` | Admin | Alias halaman Email Settings |
| `/set-smtp` | Admin | Redirect ke Email Settings |
| `/send-email` | Admin | Redirect ke Email Settings |

### Cara Verifikasi Setelah Setup

1. Build aplikasi.

```bash
dotnet build
```

2. Jalankan migration aplikasi.

```bash
dotnet ef database update --context AppDbContext
```

3. Pastikan database aplikasi berisi tabel berikut.

```sql
USE EmailDb;

SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;
```

Minimal tabel aplikasi:

- `__EFMigrationsHistory`
- `Users`
- `Emails`
- `EmailGroups`
- `SetSmtp`
- `AlarmEmailTracking`

4. Pastikan database alarm berisi tabel Wonderware.

```sql
USE WWALMDB;

SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;
```

Tabel penting:

- `AlarmMaster`
- `AlarmDetail`

5. Jalankan aplikasi dan login.

```bash
dotnet run --urls http://localhost:5146
```

6. Buka:

```text
http://localhost:5146/login
```

7. Atur SMTP dan tambah minimal satu recipient di `Email Settings`.

8. Kirim manual email untuk test.

### Windows Service

Aplikasi sudah disiapkan untuk berjalan sebagai Windows Service dengan nama:

```text
CIP Station Alarm Notification
```

Publish aplikasi untuk Windows:

```powershell
.\scripts\windows\publish-service.ps1
```

Install dan jalankan service dari PowerShell sebagai administrator:

```powershell
.\scripts\windows\install-service.ps1
```

Hapus service:

```powershell
.\scripts\windows\uninstall-service.ps1
```

Jika ingin menjalankan manual tanpa script:

```bash
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

```powershell
sc.exe create "CIP Station Alarm Notification" binPath= "C:\Path\To\EmailApp\publish\EmailApp.exe" start= auto
```

Jalankan service:

```powershell
sc.exe start "CIP Station Alarm Notification"
```

Stop service:

```powershell
sc.exe stop "CIP Station Alarm Notification"
```

Hapus service:

```powershell
sc.exe delete "CIP Station Alarm Notification"
```

Catatan deployment:

- Pastikan server Windows punya akses jaringan ke SQL Server `EmailDb` dan `WWALMDB`.
- Pastikan service account punya permission baca file konfigurasi dan akses jaringan yang diperlukan.
- Simpan `appsettings.json` production di folder publish atau gunakan environment variable.
- Test manual email dan alarm notification sebelum service dipakai production.

### Troubleshooting

#### `dotnet ef` tidak ditemukan

Install tool:

```bash
dotnet tool install --global dotnet-ef
```

Untuk fish shell:

```bash
fish_add_path ~/.dotnet/tools
```

#### `More than one DbContext was found`

Gunakan parameter `--context`.

```bash
dotnet ef database update --context AppDbContext
```

#### Login gagal dengan `Invalid salt version`

Artinya password di database bukan hash BCrypt yang valid. Buat ulang password user memakai BCrypt hash, atau reset user dari aplikasi jika masih ada akun admin lain.

#### Browser menampilkan HTTP 401 bukan halaman login

Pastikan middleware authentication dan status code redirect aktif di `Program.cs`, lalu restart aplikasi. Untuk halaman browser, request unauthorized harus diarahkan ke `/login?returnUrl=...`.

#### Email tidak terkirim

Cek hal berikut:

- SMTP host dan port benar.
- Password SMTP benar.
- Untuk Gmail, gunakan App Password.
- Penerima email sudah terdaftar.
- Server bisa mengakses SMTP provider.
- Log aplikasi tidak menampilkan error authentication SMTP.

#### Alarm tidak mengirim email

Cek hal berikut:

- Database alarm menggunakan connection string `AlarmDatabase`.
- Tabel `AlarmDetail` memiliki data baru dengan `AlarmState` yang masuk di `Monitoring:AlarmStates`, misalnya `UNACK_ALM` atau `UNACK_RTN`.
- `EventStamp` masuk dalam rentang monitoring.
- Data belum ada di `AlarmEmailTracking`.
- Minimal satu recipient tersedia di tabel `Emails`.

#### Query EF muncul terus di console

Monitoring memang berjalan periodik sesuai `Monitoring:CheckIntervalSeconds`. Jika log terlalu ramai, atur level logging EF menjadi `Warning` di `appsettings.json`.

#### Warning vulnerability MailKit saat build

Jika muncul warning `NU1902` untuk MailKit, cek versi terbaru package dan update jika sudah aman untuk project.

```bash
dotnet list package --vulnerable
dotnet add package MailKit
dotnet add package MimeKit
```

### Catatan Maintenance

- Jangan mengubah schema database `WWALMDB` tanpa koordinasi dengan pemilik sistem HMI/SCADA.
- Gunakan migration hanya untuk `AppDbContext`.
- Simpan secret production di luar repository.
- Setelah mengubah model aplikasi, buat migration baru dan review hasil migration sebelum deploy.
- Setelah mengubah authentication atau email flow, test login, reload halaman, logout, SMTP save, manual email, dan alarm notification.
