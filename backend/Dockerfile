FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# Copy solution and project files first for layer caching
COPY BudgetApp.sln .
COPY BudgetApp.Api/BudgetApp.Api.csproj BudgetApp.Api/
COPY BudgetApp.Tests/BudgetApp.Tests.csproj BudgetApp.Tests/
RUN dotnet restore

# Copy everything else
COPY . .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "run", "--project", "BudgetApp.Api", "--no-launch-profile"]
