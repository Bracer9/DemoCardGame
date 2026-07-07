FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TinyPixelFights.csproj ./
RUN dotnet restore TinyPixelFights.csproj

COPY . ./
RUN dotnet publish TinyPixelFights.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080
CMD ["sh", "-c", "dotnet TinyPixelFights.dll --urls http://0.0.0.0:${PORT:-8080}"]
