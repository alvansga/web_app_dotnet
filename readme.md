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