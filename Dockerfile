FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app
# EXPOSE 8010
# ENV ASPNETCORE_URLS=http://+:8010
#
# copy csproj and restore as distinct layers
COPY *.sln .
COPY Json/Burst/*.csproj                ./Json/Burst/
COPY Json/Fliox/*.csproj                ./Json/Fliox/
COPY Json/Fliox.Annotation/*.csproj     ./Json/Fliox.Annotation/
COPY Json/Fliox.Hub/*.csproj            ./Json/Fliox.Hub/
COPY Json/Fliox.Hub.AspNetCore/*.csproj ./Json/Fliox.Hub.AspNetCore/
COPY Json/Fliox.Hub.Explorer/*.csproj   ./Json/Fliox.Hub.Explorer/
COPY Json/Fliox.Hub.GraphQL/*.csproj    ./Json/Fliox.Hub.GraphQL/
COPY Demos~/Demo/Client/*.csproj        ./Demos~/Demo/Client/
COPY Demos~/Demo/Hub/*.csproj           ./Demos~/Demo/Hub/
#
RUN dotnet restore Demos~/Demo/Hub/DemoHub.csproj
#
# copy everything else and build app
COPY Json/Burst/.                       ./Json/Burst/
COPY Json/Fliox/.                       ./Json/Fliox/
COPY Json/Fliox.Annotation/.            ./Json/Fliox.Annotation/
COPY Json/Fliox.Hub/.                   ./Json/Fliox.Hub/
COPY Json/Fliox.Hub.AspNetCore/.        ./Json/Fliox.Hub.AspNetCore/
COPY Json/Fliox.Hub.Explorer/.          ./Json/Fliox.Hub.Explorer/
COPY Json/Fliox.Hub.GraphQL/.           ./Json/Fliox.Hub.GraphQL/
COPY Demos~/Demo/Client/.               ./Demos~/Demo/Client/
COPY Demos~/Demo/Hub/.                  ./Demos~/Demo/Hub/
#
WORKDIR /app/Demos~/Demo/Hub/
RUN dotnet publish DemoHub.csproj -c Release -o out
#
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime 

WORKDIR /app 
#
COPY --from=build /app/Demos~/Demo/Hub/out ./
ENTRYPOINT ["dotnet", "DemoHub.dll"]

# --- usage
# docker build -t demo-hub:latest .
# docker run -it --rm -p 80:8010 demo-hub:latest


 
 