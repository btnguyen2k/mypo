#!/bin/sh

BASENAME=MyPo
PROJNAME=Portfolio
TARGET_FRAMEWORK="net8.0"

# create projects
dotnet new classlib -o ${BASENAME}.${PROJNAME}.Shared -f ${TARGET_FRAMEWORK}
dotnet new webapi -o ${BASENAME}.${PROJNAME}.Api -f ${TARGET_FRAMEWORK}
cd ${BASENAME}.Blazor && dotnet new razorclasslib -o ${BASENAME}.Blazor.${PROJNAME}.App -f ${TARGET_FRAMEWORK} && cd ..

# Add projects to solution
dotnet sln add ${BASENAME}.${PROJNAME}.Shared/${BASENAME}.${PROJNAME}.Shared.csproj
dotnet sln add ${BASENAME}.${PROJNAME}.Api/${BASENAME}.${PROJNAME}.Api.csproj
dotnet sln add ${BASENAME}.Blazor/${BASENAME}.Blazor.${PROJNAME}.App/${BASENAME}.Blazor.${PROJNAME}.App.csproj

# Add references
dotnet add ${BASENAME}.${PROJNAME}.Shared/${BASENAME}.${PROJNAME}.Shared.csproj reference ${BASENAME}.Shared/${BASENAME}.Shared.csproj
dotnet add ${BASENAME}.${PROJNAME}.Shared/${BASENAME}.${PROJNAME}.Shared.csproj reference ${BASENAME}.Shared.EF/${BASENAME}.Shared.EF.csproj
dotnet add ${BASENAME}.${PROJNAME}.Api/${BASENAME}.${PROJNAME}.Api.csproj reference ${BASENAME}.${PROJNAME}.Shared/${BASENAME}.${PROJNAME}.Shared.csproj
dotnet add ${BASENAME}.${PROJNAME}.Api/${BASENAME}.${PROJNAME}.Api.csproj reference ${BASENAME}.Shared.Api/${BASENAME}.Shared.Api.csproj
dotnet add ${BASENAME}.Blazor/${BASENAME}.Blazor.${PROJNAME}.App/${BASENAME}.Blazor.${PROJNAME}.App.csproj reference ${BASENAME}.${PROJNAME}.Shared/${BASENAME}.${PROJNAME}.Shared.csproj

dotnet add ${BASENAME}.Api/${BASENAME}.Api.csproj reference ${BASENAME}.${PROJNAME}.Api/${BASENAME}.${PROJNAME}.Api.csproj
dotnet add ${BASENAME}.Blazor/${BASENAME}.Blazor/${BASENAME}.Blazor.csproj reference ${BASENAME}.${PROJNAME}.Api/${BASENAME}.${PROJNAME}.Api.csproj
dotnet add ${BASENAME}.Blazor/${BASENAME}.Blazor/${BASENAME}.Blazor.csproj reference ${BASENAME}.Blazor/${BASENAME}.Blazor.${PROJNAME}.App/${BASENAME}.Blazor.${PROJNAME}.App.csproj
dotnet add ${BASENAME}.Blazor/${BASENAME}.Blazor.Client/${BASENAME}.Blazor.Client.csproj reference ${BASENAME}.Blazor/${BASENAME}.Blazor.${PROJNAME}.App/${BASENAME}.Blazor.${PROJNAME}.App.csproj
