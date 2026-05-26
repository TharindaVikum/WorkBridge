# WorkBridge

## Project Overview
WorkBridge is a professional service management platform built with ASP.NET Core and a static web frontend. It enables users to submit assignment and web development requests, allows admins to manage requests, supports file uploads/downloads, and includes JWT authentication with email notifications.

## Features
- User registration, login, and profile management
- Admin dashboard for request review, approval, and fulfillment
- Secure JWT authentication for API access
- File upload and download support for service requests
- Request lifecycle tracking with status updates
- Email notifications for request events using SendGrid
- Inline API documentation via Swagger
- Responsive frontend with modern UX patterns

## Technologies Used
- .NET 10 / ASP.NET Core
- Entity Framework Core with SQL Server LocalDB
- ASP.NET Identity
- JWT authentication
- SendGrid email integration
- Swagger / OpenAPI
- HTML, CSS, JavaScript frontend
- Visual Studio-compatible project structure

## Modules
- `Controllers/` — API controllers for auth, profiles, requests, files, payments
- `Data/` — `AppDbContext` and EF Core data layer
- `DTOs/` — request and response shapes for API endpoints
- `Models/` — domain models including users, requests, files, chat messages
- `Services/` — reusable services such as email notifications
- `wwwroot/` — frontend HTML/CSS/JS assets and static uploads
- `Program.cs` — application startup, authentication, middleware, and seed logic
- `appsettings.json` — application configuration placeholders for secrets and storage paths

## How to Run
1. Install the .NET 10 SDK.
2. Open the project folder in a code editor or terminal.
3. Restore dependencies:
   ```bash
dotnet restore
```
4. Update configuration values in `appsettings.json` or use `appsettings.Development.json`:
   - `ConnectionStrings:DefaultConnection`
   - `JwtSettings:SecretKey`
   - `SendGrid:ApiKey`
   - `SendGrid:FromEmail`
   - `SeedAdmin:Email`
   - `SeedAdmin:Password`
5. Build and run the project:
   ```bash
dotnet build
dotnet run
```
6. Open the browser and navigate to the local application endpoint or open `wwwroot/WorkBridge.html`.

## Developer
- Developed for software engineering portfolio presentation
- Project prepared for secure public GitHub upload
- Repository name suggestion: `workbridge-aspnet-service-platform`

## Notes for Public Upload
- Sensitive values have been replaced with placeholders in `appsettings.json`.
- Local secrets and service keys should be stored outside source control.
- Do not upload generated folders such as `.vs`, `bin`, or `obj`.
