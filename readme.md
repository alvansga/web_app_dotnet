## Requirements

using dotnet 9.0

```
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.0 
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
```


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