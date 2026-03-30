```markdown
# Alarm Notification System

Sistem monitoring alarm otomatis dari database AVEVA (Wonderware) yang mengirimkan notifikasi email ke penerima terdaftar.

## 📋 Daftar Isi

- [Fitur](#-fitur)
- [Arsitektur](#-arsitektur)
- [Teknologi](#-teknologi)
- [Struktur Database](#-struktur-database)
- [Instalasi](#-instalasi)
- [Konfigurasi](#-konfigurasi)
- [Cara Penggunaan](#-cara-penggunaan)
- [API Endpoints](#-api-endpoints)
- [Monitoring](#-monitoring)
- [Troubleshooting](#-troubleshooting)
- [Deployment](#-deployment)

---

## 🚀 Fitur

| Fitur | Deskripsi |
|-------|-----------|
| 🔔 **Monitoring Alarm Real-time** | Service background mengecek alarm baru setiap 10 detik |
| 📧 **Notifikasi Email Otomatis** | Kirim email ke semua penerima terdaftar saat alarm baru |
| 📊 **Dashboard Statistik** | Visualisasi data alarm (total, unack, priority, groups) |
| 👥 **User Management** | Kelola user dengan role admin/user |
| 📬 **Email Management** | Kelola daftar penerima email |
| 🔐 **Authentication** | Login dengan JWT token dan role-based access |
| 📝 **Tracking Pengiriman** | Catat semua email yang sudah dikirim |
| 🧪 **Testing Page** | Insert alarm manual untuk testing |

---

## 🏗️ Arsitektur

```
┌─────────────────────────────────────────────────────────────────┐
│                    ALARM NOTIFICATION SYSTEM                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐    ┌──────────────────┐                   │
│  │   Alarm Database │    │   Email Database │                   │
│  │  (AVEVA/Wonder)  │    │   (Aplikasi)     │                   │
│  │                  │    │                  │                   │
│  │ • AlarmDetail    │    │ • Users          │                   │
│  │ • AlarmMaster    │    │ • Emails         │                   │
│  │ • Cause          │    │ • Groups         │                   │
│  │ • Comment        │    │ • UserGroups     │                   │
│  │ • OperatorDetails│    │ • AlarmEmailTracking│                │
│  └────────┬─────────┘    └────────┬─────────┘                   │
│           │                       │                             │
│           ▼                       ▼                             │
│  ┌──────────────────────────────────────────────┐              │
│  │           Background Monitoring Service       │              │
│  │         (AlarmMonitoringService)              │              │
│  │     Cek alarm baru setiap 10 detik            │              │
│  │     Status: UNACK_ALM                         │              │
│  └──────────────────┬───────────────────────────┘              │
│                     │                                            │
│                     ▼                                            │
│  ┌──────────────────────────────────────────────┐              │
│  │              Email Service                    │              │
│  │         (SMTP / MailKit)                      │              │
│  └──────────────────┬───────────────────────────┘              │
│                     │                                            │
│                     ▼                                            │
│              ┌──────────────┐                                   │
│              │  Email Server │                                   │
│              └──────────────┘                                   │
│                                                                  │
│  ┌──────────────────────────────────────────────┐              │
│  │              Web Interface (Blazor)           │              │
│  │  • Dashboard  • Users  • Emails              │              │
│  │  • Send Email • Testing                      │              │
│  └──────────────────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Teknologi

| Komponen | Teknologi |
|----------|-----------|
| Framework | .NET 8 (ASP.NET Core Blazor) |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core |
| Email Service | MailKit & MimeKit |
| Authentication | JWT (JSON Web Token) |
| Password Hashing | BCrypt.Net |
| Background Service | .NET BackgroundService |

---

## 📁 Struktur Database

### Database Alarm (alarmNotification)

| Tabel | Kolom Utama | Keterangan |
|-------|-------------|------------|
| AlarmMaster | AlarmId, TagName, GroupName, Priority | Master data alarm |
| AlarmDetail | AlarmDetailId, AlarmId, AlarmState, EventStamp | Detail transisi alarm |
| Cause | CauseId, CauseDescription | Penyebab alarm |
| Comment | CommentId, Comment | Komentar alarm |
| OperatorDetails | OperatorID, UserFullName | Data operator |

### Database Aplikasi (EmailDb)

| Tabel | Kolom Utama | Keterangan |
|-------|-------------|------------|
| Users | Id, Username, Password, IsAdmin | User aplikasi |
| Emails | Id, Address | Daftar email penerima |
| Groups | Id, Name, Description | Group user |
| UserGroups | UserId, GroupId | Mapping user ke group |
| AlarmEmailTracking | Id, AlarmDetailId, EmailSent, CreatedAt | Tracking pengiriman |

---

## 📦 Instalasi

### Prasyarat

- .NET 8 SDK
- SQL Server (LocalDB / SQL Server)
- SMTP Server (Gmail / SMTP Perusahaan)

### Langkah Instalasi

```bash
# 1. Clone repository
git clone [repository-url]
cd EmailApp

# 2. Restore packages
dotnet restore

# 3. Update connection string di appsettings.json
# Sesuaikan dengan database Anda

# 4. Buat migration dan update database
dotnet ef migrations add InitialCreate
dotnet ef database update

# 5. Buat tabel tracking
# Jalankan script SQL di bawah

# 6. Jalankan aplikasi
dotnet run
```

### Script SQL untuk Tabel Tracking

```sql
USE EmailDb;
GO

CREATE TABLE AlarmEmailTracking (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AlarmDetailId INT NOT NULL,
    AlarmId INT NOT NULL,
    EmailSent BIT DEFAULT 0,
    EmailSentAt DATETIME NULL,
    EmailRecipients NVARCHAR(MAX) NULL,
    ErrorMessage NVARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
```

---

## ⚙️ Konfigurasi

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EmailDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True",
    "AlarmDatabase": "Server=localhost;Database=alarmNotification;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "your-app-password",
    "FromEmail": "your-email@gmail.com"
  },
  "Jwt": {
    "Key": "your-secret-key-minimum-32-characters",
    "Issuer": "EmailApp",
    "Audience": "EmailAppUsers"
  },
  "Monitoring": {
    "CheckIntervalSeconds": 10,
    "LookbackMinutes": 5
  }
}
```

### User Awal

```sql
-- Tambah user admin (password di-hash dengan BCrypt)
INSERT INTO Users (Id, Username, Password, IsAdmin)
VALUES (NEWID(), 'admin', '$2a$11$...', 1);
```

---

## 🖥️ Cara Penggunaan

### 1. Login

- Buka `http://localhost:5146/login`
- Masukkan username dan password

### 2. Dashboard (`/`)

Menampilkan statistik alarm:
- Total alarm
- Jumlah UNACK
- Priority 500
- Jumlah group unik
- Tabel alarm log

### 3. User Management (`/users`)

- Tambah user baru
- Delete user
- Set role admin

### 4. Email Management (`/emails`)

- Tambah email penerima
- Edit email
- Delete email

### 5. Manual Send Email (`/send-email`)

- Tulis pesan manual
- Kirim ke semua penerima

### 6. Testing Page (`/testing`)

- Insert alarm manual
- Quick action buttons
- Lihat 10 alarm terbaru

---

## 🔌 API Endpoints

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| POST | `/api/auth/login` | Login user |
| POST | `/api/auth/logout` | Logout user |
| GET | `/api/alarm` | Get data alarm |

### Login Request

```json
{
  "username": "admin",
  "password": "password123"
}
```

### Login Response

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "guid",
    "username": "admin",
    "isAdmin": true
  }
}
```

---

## 📊 Monitoring

### Log Output

```
info: EmailApp.Services.AlarmMonitoringService[0]
      Alarm Monitoring Service started
info: EmailApp.Services.AlarmMonitoringService[0]
      Found 1 new alarms
info: EmailApp.Services.AlarmMonitoringService[0]
      Email sent for alarm 85: CIP0_CIP_PUMP5807.FaultAlarm
```

### Cek Tracking

```sql
SELECT * FROM AlarmEmailTracking ORDER BY Id DESC;
```

### Cek Alarm Terbaru

```sql
SELECT TOP 10 
    ad.EventStamp, 
    ad.AlarmState, 
    am.TagName,
    am.Priority
FROM AlarmDetail ad
INNER JOIN AlarmMaster am ON ad.AlarmId = am.AlarmId
ORDER BY ad.EventStamp DESC;
```

---

## 🔧 Troubleshooting

### Email Tidak Terkirim

| Masalah | Solusi |
|---------|--------|
| SMTP configuration error | Cek `appsettings.json` section Email |
| Gmail password salah | Gunakan App Password, bukan password biasa |
| Koneksi internet | Pastikan koneksi internet stabil |

### Alarm Tidak Terdeteksi

| Masalah | Solusi |
|---------|--------|
| Status alarm bukan UNACK_ALM | Alarm harus dengan status UNACK_ALM |
| EventStamp terlalu lama | Service hanya mengambil alarm baru |
| Alarm sudah dikirim | Cek tabel AlarmEmailTracking |

### Database Connection Error

| Masalah | Solusi |
|---------|--------|
| SQL Server tidak jalan | Jalankan SQL Server service |
| Connection string salah | Cek koneksi di appsettings.json |
| Firewall block | Buka port 1433 |

---

## 🚢 Deployment

### Windows Service

```bash
# Publish
dotnet publish -c Release -o ./publish

# Install service
sc create AlarmEmailService binPath="C:\path\to\publish\EmailApp.exe"
sc start AlarmEmailService
```

### Linux Daemon (systemd)

Buat file `/etc/systemd/system/alarm-email.service`:

```ini
[Unit]
Description=Alarm Email Service
After=network.target

[Service]
WorkingDirectory=/opt/alarm-email
ExecStart=/usr/bin/dotnet /opt/alarm-email/EmailApp.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable alarm-email
sudo systemctl start alarm-email
sudo systemctl status alarm-email
```

---

## 📝 Format Email

```
Subject: [ALARM] {TagName} - Priority {Priority}

Body:
ALARM NOTIFICATION
==================
Tag: {TagName}
Group: {GroupName}
Priority: {Priority}
Time: {EventStamp}
State: {AlarmState}

Please check immediately.
```

---
