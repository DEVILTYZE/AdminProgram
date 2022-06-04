FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["AdminService/AdminService.csproj", "AdminService/"]
RUN dotnet restore "AdminService/AdminService.csproj"
COPY . .
WORKDIR "/src/AdminService"
RUN dotnet build "AdminService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdminService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdminService.dll"]
