FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar os arquivos de projeto e restaurar dependências
COPY ["src/Crive.Api/Crive.Api.csproj", "Crive.Api/"]
COPY ["src/Crive.Shared/Crive.Shared.csproj", "Crive.Shared/"]
RUN dotnet restore "Crive.Api/Crive.Api.csproj"

# Copiar o restante do código e compilar
COPY src/ .
WORKDIR "/src/Crive.Api"
RUN dotnet publish "Crive.Api.csproj" -c Release -o /app/publish

# Imagem final de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Forçar a aplicação a ouvir na porta 5000 dentro do container
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Crive.Api.dll"]
