FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY BudgetApp.Api/BudgetApp.Api.csproj BudgetApp.Api/
RUN dotnet restore BudgetApp.Api/BudgetApp.Api.csproj
COPY BudgetApp.Api/ BudgetApp.Api/
RUN dotnet publish BudgetApp.Api/BudgetApp.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "BudgetApp.Api.dll"]
