FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ForeignCitizenRules.Domain/ForeignCitizenRules.Domain.csproj src/ForeignCitizenRules.Domain/
COPY src/ForeignCitizenRules.Persistence/ForeignCitizenRules.Persistence.csproj src/ForeignCitizenRules.Persistence/
COPY src/ForeignCitizenRules.Application/ForeignCitizenRules.Application.csproj src/ForeignCitizenRules.Application/
COPY src/ForeignCitizenRules.Api/ForeignCitizenRules.Api.csproj src/ForeignCitizenRules.Api/
RUN dotnet restore src/ForeignCitizenRules.Api/ForeignCitizenRules.Api.csproj

COPY . ./
RUN dotnet publish src/ForeignCitizenRules.Api/ForeignCitizenRules.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "ForeignCitizenRules.Api.dll"]
