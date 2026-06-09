# Alarm Notification EmailApp Client Control

EmailApp sekarang dibuat sebagai **AVEVA/HMI client control DLL** berbasis **WinForms .NET Framework 4.8**, mengikuti pola project FingerPrint.

## Output

Setelah build di Windows/Visual Studio, output yang dipakai untuk import HMI adalah:

```text
bin\Debug\EmailApp.dll
```

Control utama yang dicoba saat import:

```text
EmailApp.Controls.EmailAppControl
```

## Kenapa Bukan net8.0-windows?

Project FingerPrint memakai:

```xml
<OutputType>Library</OutputType>
<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
```

Karena dokumentasi AVEVA/HMI menyebut import `.DLL` client controls, project ini sekarang juga diset menjadi:

```xml
<OutputType>Library</OutputType>
<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
```

Ini lebih cocok untuk HMI/Industrial Graphic Editor dibanding output `.exe` atau `.NET 8` WinForms.

## Struktur Aktif

File yang dikompilasi oleh `EmailApp.csproj`:

```text
Controls/
├── EmailAppControl.cs       Control utama untuk HMI
├── LoginControl.cs          Control login sederhana
├── ManualEmailControl.cs    Kirim email manual
├── RecipientControl.cs      Kelola recipient dan group
└── SmtpControl.cs           Kelola SMTP

Properties/
└── AssemblyInfo.cs
```

## Build

Build dari Windows dengan Visual Studio atau MSBuild yang punya .NET Framework 4.8 Developer Pack:

```bat
msbuild EmailApp.csproj /p:Configuration=Debug
```

Atau buka project di Visual Studio lalu Build.

Di Linux, build akan gagal karena reference assemblies .NET Framework 4.8 tidak tersedia:

```text
The reference assemblies for .NETFramework,Version=v4.8 were not found
```

Itu normal. Build final harus dilakukan di Windows.

## Cara Import ke HMI

1. Build project di Windows.
2. Ambil file:

```text
bin\Debug\EmailApp.dll
```

3. Import DLL itu sebagai client control di Industrial Graphic Editor.
4. Pilih control:

```text
EmailApp.Controls.EmailAppControl
```

5. Set property yang dibutuhkan dari HMI/designer, terutama:

```text
ConnectionString
SmtpHost
SmtpPort
SmtpUser
SmtpPassword
FromEmail
```

## Catatan Database

Control memakai ADO.NET langsung ke SQL Server. Default connection string:

```text
Server=localhost;Database=EmailDB;Trusted_Connection=True;TrustServerCertificate=True
```

Tabel yang dipakai:

```text
Emails
EmailGroups
SetSmtp
```

Pastikan database dan tabel sudah ada sebelum fitur load/add/save dipakai.
