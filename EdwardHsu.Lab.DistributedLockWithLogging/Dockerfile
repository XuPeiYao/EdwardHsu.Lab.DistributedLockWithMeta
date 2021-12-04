#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["EdwardHsu.Lab.DistributedLockWithLogging/EdwardHsu.Lab.DistributedLockWithLogging.csproj", "EdwardHsu.Lab.DistributedLockWithLogging/"]
RUN dotnet restore "EdwardHsu.Lab.DistributedLockWithLogging/EdwardHsu.Lab.DistributedLockWithLogging.csproj"
COPY . .
WORKDIR "/src/EdwardHsu.Lab.DistributedLockWithLogging"
RUN dotnet build "EdwardHsu.Lab.DistributedLockWithLogging.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EdwardHsu.Lab.DistributedLockWithLogging.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EdwardHsu.Lab.DistributedLockWithLogging.dll"]