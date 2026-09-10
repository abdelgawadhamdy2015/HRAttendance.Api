# Secure local setup

The API now requires SQL Server and does not store database credentials, JWT signing keys, or admin passwords in `appsettings.json`.

From the backend project directory:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=HRAttendance;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "generate-a-random-secret-at-least-32-characters-long"
dotnet user-secrets set "BootstrapAdmin:Username" "your-admin-username"
dotnet user-secrets set "BootstrapAdmin:Email" "your-admin-email"
dotnet user-secrets set "BootstrapAdmin:Password" "your-strong-admin-password"
```

Run the API in Development. On the first run, the configured bootstrap admin is created and receives the seeded application permissions. No demo employees, attendance records, missions, permission requests, or notifications are created by the seed code.

If the database already contains old demo rows from an earlier version, review and remove those rows once in the database; the application intentionally does not delete existing production data automatically.

After changing a user's permissions, the Flutter app refreshes `/api/auth/me`, and backend authorization checks the current database assignment on every protected request. A user can therefore be revoked without waiting for the JWT permission claim to expire.
