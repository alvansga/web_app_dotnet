## Requirements

using dotnet 9.0+ (currently dotnet 10.0)

```
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.0 
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
```

---

## 🔐 Security Architecture

### Token-Based Player Authentication
Setiap player mendapat **secret token** (random GUID) saat registrasi via `POST /api/players`. Token ini wajib dikirim di setiap request yang memodifikasi state (POST/PUT/DELETE). Backend memvalidasi token terhadap database — jika tidak cocok, request ditolak dengan `401 Unauthorized`.

**Flow:**
1. `POST /api/players` → response: `{ id, token }`
2. Frontend menyimpan `{ id, name, token }` di `sessionStorage`
3. Semua API call berikutnya menyertakan `token` di request body

### CORS (Cross-Origin Resource Sharing)
Hanya origin yang terdaftar di `GameCorsPolicy` yang diizinkan:
- `http://localhost:5000`
- `http://localhost:5001`
- `https://localhost:5001`
- `http://127.0.0.1:5000`

> **Deploy production:** tambahkan domain asli ke `Program.cs` → `builder.Services.AddCors(...)`.

### Rate Limiting
- **100 request/menit per IP** (fixed window)
- Lebih dari batas → `429 Too Many Requests`
- Queue: 10 request antrian (FIFO)

### CSRF Protection (AntiForgery)
- Middleware `UseAntiforgery()` memvalidasi semua request POST/PUT/DELETE
- Cookie `CSRF-TOKEN` (HttpOnly=false, SameSite=Strict) dikirim otomatis
- Frontend membaca cookie & mengirim balik via header `X-CSRF-TOKEN`

### Swagger — Development Only
Swagger UI hanya aktif di environment **Development** (`ASPNETCORE_ENVIRONMENT=Development`). Di production, endpoint Swagger tidak tersedia.

---

## create database

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## install swashbuckle for swagger

```
dotnet add package Swashbuckle.AspNetCore --version 6.6.2
dotnet restore
dotnet build
```

## test api with swagger / postman

- run app
```
dotnet run
```

## add api players

```
dotnet ef migrations add AddPlayerTable
dotnet ef database update
```

## add api rooms

```
dotnet ef migrations add AddGameRoom
dotnet ef database update
```

reset db
```
# 1. Hapus database
del codename.db

# 2. Hapus migration terakhir (optional tapi disarankan)
dotnet ef migrations remove

# 3. Buat ulang migration
dotnet ef migrations add InitAll

# 4. Apply
dotnet ef database update
```


## add api get room details GET /api/rooms/{code}



move to dotnet 10.0
run:
dotnet clean
dotnet restore
dotnet build

pastikan: dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
hash-r

dotnet ef database update
dotnet run