#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["AsyncProcessor.Azure.EventHub.Example.Producer/AsyncProcessor.Azure.EventHub.Example.Producer.csproj", "AsyncProcessor.Azure.EventHub.Example.Producer/"]
COPY ["AsyncProcessor.Azure.EventHub/AsyncProcessor.Azure.EventHub.csproj", "AsyncProcessor.Azure.EventHub/"]
COPY ["AsyncProcessor/AsyncProcessor.csproj", "AsyncProcessor/"]
COPY ["AsyncProcessor.Example.Models/AsyncProcessor.Example.Models.csproj", "AsyncProcessor.Example.Models/"]
RUN dotnet restore "AsyncProcessor.Azure.EventHub.Example.Producer/AsyncProcessor.Azure.EventHub.Example.Producer.csproj"
COPY . .
WORKDIR "/src/AsyncProcessor.Azure.EventHub.Example.Producer"
RUN dotnet build "AsyncProcessor.Azure.EventHub.Example.Producer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AsyncProcessor.Azure.EventHub.Example.Producer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV DOTNET_ENVIRONMENT="Local"
ENTRYPOINT ["dotnet", "AsyncProcessor.Azure.EventHub.Example.Producer.dll"]