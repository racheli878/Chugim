# שרת Chugim — בנייה בשני שלבים: SDK לבנייה, ו-runtime קטן להרצה.
# Render בונה את הקובץ הזה בעצמו; במחשב המקומי לא צריך Docker.

# ---------- שלב 1: בנייה ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# קודם רק קבצי הפרויקטים — כך שכבת ה-restore נשמרת ב-cache כל עוד התלויות לא השתנו.
COPY Directory.Build.props ./
COPY src/Chugim.Domain/Chugim.Domain.csproj src/Chugim.Domain/
COPY src/Chugim.Application/Chugim.Application.csproj src/Chugim.Application/
COPY src/Chugim.Infrastructure/Chugim.Infrastructure.csproj src/Chugim.Infrastructure/
COPY src/Chugim.API/Chugim.API.csproj src/Chugim.API/
RUN dotnet restore src/Chugim.API/Chugim.API.csproj

COPY src/ src/
RUN dotnet publish src/Chugim.API/Chugim.API.csproj -c Release -o /app --no-restore

# ---------- שלב 2: הרצה ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# תיקיית הלוגים של NLog צריכה הרשאת כתיבה למשתמש הלא-מנהל שהשרת רץ בו.
RUN mkdir -p /app/logs && chown "$APP_UID" /app/logs
USER $APP_UID

# Render מעביר את התנועה (HTTPS אצלו) לפורט הזה.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Chugim.API.dll"]
