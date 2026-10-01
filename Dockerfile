FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Backend/Backend.slnx Backend/
COPY Backend/Project.Api/ Backend/Project.Api/
COPY Backend/Project.Application/ Backend/Project.Application/
COPY Backend/Project.Infrastructure/ Backend/Project.Infrastructure/
RUN dotnet publish Backend/Project.Api/Project.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads/listings /app/dpkeys
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Project.Api.dll"]
