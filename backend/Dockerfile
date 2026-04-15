# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Capitec.Dispute.API/Capitec.Dispute.API.csproj", "Capitec.Dispute.API/"]
COPY ["Capitec.Dispute.Application/Capitec.Dispute.Application.csproj", "Capitec.Dispute.Application/"]
COPY ["Capitec.Dispute.Domain/Capitec.Dispute.Domain.csproj", "Capitec.Dispute.Domain/"]
COPY ["Capitec.Dispute.Infrastructure/Capitec.Dispute.Infrastructure.csproj", "Capitec.Dispute.Infrastructure/"]

RUN dotnet restore "Capitec.Dispute.API/Capitec.Dispute.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Capitec.Dispute.API"
RUN dotnet build "Capitec.Dispute.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Capitec.Dispute.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "Capitec.Dispute.API.dll"]