FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "Directory.Build.props", "./"]
COPY ["src/Cinema.Api/Cinema.Api.csproj", "src/Cinema.Api/"]
COPY ["src/Cinema.Application/Cinema.Application.csproj", "src/Cinema.Application/"]
COPY ["src/Cinema.Domain/Cinema.Domain.csproj", "src/Cinema.Domain/"]
COPY ["src/Cinema.Contracts/Cinema.Contracts.csproj", "src/Cinema.Contracts/"]
COPY ["src/Cinema.Infrastructure/Cinema.Infrastructure.csproj", "src/Cinema.Infrastructure/"]
RUN dotnet restore "src/Cinema.Api/Cinema.Api.csproj"
COPY . .
WORKDIR /src/src/Cinema.Api
RUN dotnet build "Cinema.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV ASPNETCORE_HTTP_PORTS=5001
EXPOSE 5001
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cinema.Api.dll"]
