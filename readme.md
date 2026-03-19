# 🎭 Coup Card Game - Real-time Multiplayer

Aplikasi permainan kartu **Coup** versi digital yang dibangun menggunakan **ASP.NET Core MVC** dan **SignalR** untuk sinkronisasi real-time antar pemain.

## 🚀 Fitur Utama
- **Real-time Sync**: Menggunakan SignalR (WebSockets), semua pemain akan melihat update status game secara instan tanpa perlu refresh halaman.
- **Sistem Sesi**: Setiap tab browser dapat menjadi pemain yang berbeda menggunakan `HttpContext.Session`.
- **Mekanisme Lengkap**: Implementasi penuh Challenge (Bluffing) dan Blocking (Counter-actions).
- **Responsive UI**: Tampilan yang dioptimalkan untuk Desktop maupun Smartphone/Tablet.

---

## 🃏 Peran & Aksi (Roles & Actions)

### 1. Peran Khusus
| Peran | Kemampuan (Aksi) | Bisa Di-Block Oleh |
| :--- | :--- | :--- |
| **Duke** | **Tax**: Mengambil 3 koin. | - |
| **Assassin** | **Assassinate**: Bayar 3 koin untuk membunuh 1 pengaruh musuh. | Contessa |
| **Captain** | **Steal**: Mengambil 2 koin dari musuh. | Captain / Ambassador |
| **Ambassador** | **Exchange**: Menukar kartu dengan deck. | - |
| **Contessa** | - | Blokir **Assassinate**. |

### 2. Aksi Umum
- **Income**: Mengambil 1 koin (Tidak bisa di-challenge/block).
- **Foreign Aid**: Mengambil 2 koin (Bisa di-block oleh **Duke**).
- **Coup**: Bayar 7 koin untuk membunuh 1 pengaruh musuh (Tidak bisa di-challenge/block).

---

## 🕹️ Alur Permainan (Game Flow)

Permainan berjalan secara otomatis melalui state machine berikut:

### 1. Fase Lobby
- Pemain memasukkan nama dan bergabung ke permainan.
- Tombol **Start Game** akan muncul jika pemain minimal ada 2 orang.

### 2. Fase Aksi (Action Phase)
- Pemain yang sedang gilirannya memilih salah satu aksi (Income, Tax, Steal, dll).

### 3. Fase Challenge (Challenge Phase) 📢
- Jika aksi bisa di-challenge (seperti Tax, Steal, Assassinate), semua pemain lain diberikan pilihan: **CHALLENGE** atau **PASS**.
- **Challenge Berhasil (Pemain Bohong)**: Pemain kehilangan 1 kartu, aksi dibatalkan.
- **Challenge Gagal (Pemain Jujur)**: Penantang kehilangan 1 kartu, aksi tetap lanjut.

### 4. Fase Blokir (Block Phase) 🛡️
- Beberapa aksi (Steal, Assassinate, Foreign Aid) dapat di-block oleh peran tertentu.
- Target diberikan kesempatan untuk melakukan **BLOCK** atau **ALLOW**.
- Jika Block dilakukan, game masuk ke fase **Challenge untuk Block**.

### 5. Fase Eliminasi
- Jika pemain kehilangan pengaruh (Influences) sampai 0, pemain dinyatakan **Mati (Dead)**.
- Pemenang adalah satu-satunya pemain yang bertahan hidup.

---

## 🛠️ Stack Teknologi
- **Backend**: C# / .NET 9.0 (ASP.NET Core MVC)
- **Real-time**: SignalR
- **Frontend**: HTML5, CSS3 (Vanilla CSS), JavaScript
- **State Management**: Memory-stored Game Object (Thread-safe logic di Controller)

---

## 📝 Cara Menjalankan
1. Clone repository atau buka di VS Code.
2. Jalankan perintah di terminal:
   ```powershell
   dotnet run --urls="http://0.0.0.0:5253"
   ```
3. Akses `http://localhost:5253` di browser.
4. Buka tab baru atau Incognito untuk mensimulasikan pemain kedua.
