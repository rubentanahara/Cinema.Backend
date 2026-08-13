FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "Directory.Build.props", "./"]
COPY ["src/CleanTemplate.Api/CleanTemplate.Api.csproj", "src/CleanTemplate.Api/"]
COPY ["src/CleanTemplate.Application/CleanTemplate.Application.csproj", "src/CleanTemplate.Application/"]
COPY ["src/CleanTemplate.Domain/CleanTemplate.Domain.csproj", "src/CleanTemplate.Domain/"]
COPY ["src/CleanTemplate.Contracts/CleanTemplate.Contracts.csproj", "src/CleanTemplate.Contracts/"]
COPY ["src/CleanTemplate.Infrastructure/CleanTemplate.Infrastructure.csproj", "src/CleanTemplate.Infrastructure/"]
RUN dotnet restore "src/CleanTemplate.Api/CleanTemplate.Api.csproj"
COPY . .
WORKDIR /src/src/CleanTemplate.Api
RUN dotnet build "CleanTemplate.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV ASPNETCORE_HTTP_PORTS=5001
EXPOSE 5001
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CleanTemplate.Api.dll"]
