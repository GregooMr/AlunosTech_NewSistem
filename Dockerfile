FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AlunosTech.NewSistem/AlunosTech.NewSistem.csproj", "AlunosTech.NewSistem/"]
RUN dotnet restore "AlunosTech.NewSistem/AlunosTech.NewSistem.csproj"

COPY . .
WORKDIR "/src/AlunosTech.NewSistem"
RUN dotnet publish "AlunosTech.NewSistem.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AlunosTech.NewSistem.dll"]