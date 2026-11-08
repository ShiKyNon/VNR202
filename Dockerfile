FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json HanhTrinh.sln ./
COPY src/HanhTrinh.Web/HanhTrinh.Web.csproj src/HanhTrinh.Web/
RUN dotnet restore src/HanhTrinh.Web/HanhTrinh.Web.csproj
COPY src/ src/
RUN dotnet publish src/HanhTrinh.Web/HanhTrinh.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render truyền cổng qua biến PORT
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet HanhTrinh.Web.dll"]
