# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["FinancialAccount.API/FinancialAccount.API.csproj", "FinancialAccount.API/"]
RUN dotnet restore "FinancialAccount.API/FinancialAccount.API.csproj"

# Copy full source and build
COPY . .
WORKDIR "/src/FinancialAccount.API"
RUN dotnet build "FinancialAccount.API.csproj" -c Release -o /app/build
RUN dotnet publish "FinancialAccount.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "FinancialAccount.API.dll"]
