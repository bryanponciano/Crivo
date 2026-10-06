-- Seed data for testing
-- Ensure to run EF migrations first: dotnet ef migrations add Initial && dotnet ef database update

-- You can manually insert initial tenant and user if needed:
-- INSERT INTO "Tenants" ("Id", "Name", "Slug", "IsActive", "CreatedAt", "UpdatedAt", "UninstallPasswordHash")
-- VALUES ('00000000-0000-0000-0000-000000000001', 'Test Tenant', 'test', true, NOW(), NOW(), '');
