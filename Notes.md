## Notes

Creating a new library:

```
dotnet new classlib -o library
dotnet sln add library/library.csproj
```

Creating a new console app:

```
dotnet new console -o app
dotnet sln add app/app.csproj
```

Referencing a package:

```
dotnet add app/app.csproj reference library/library.csproj
```