-- UmiHealth Initial Database Migration
-- Generated for UmiHealthDbContext

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create Tenants table
CREATE TABLE IF NOT EXISTS "Tenants" (
    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
    "Name" character varying(255) NOT NULL,
    "Description" text NOT NULL,
    "Subdomain" character varying(100) NOT NULL,
    "DatabaseName" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL,
    "SubscriptionExpiresAt" timestamp with time zone NULL,
    "SubscriptionPlan" character varying(50) NOT NULL,
    "ContactEmail" character varying(255) NOT NULL,
    "ContactPhone" text NOT NULL,
    "Address" text NOT NULL,
    "City" text NOT NULL,
    "Country" text NOT NULL,
    "PostalCode" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" text NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "DeletedBy" text NULL,
    CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id")
);

-- Create Branches table
CREATE TABLE IF NOT EXISTS "Branches" (
    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
    "TenantId" uuid NOT NULL,
    "BranchId" uuid NULL,
    "Name" character varying(255) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Phone" character varying(50) NULL,
    "Email" character varying(255) NULL,
    "Address" text NULL,
    "City" text NULL,
    "Country" text NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" text NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "DeletedBy" text NULL,
    CONSTRAINT "PK_Branches" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Branches_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
);

-- Create Users table
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
    "TenantId" uuid NOT NULL,
    "BranchId" uuid NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "UserName" character varying(100) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "PhoneNumber" character varying(50) NOT NULL,
    "PasswordHash" text NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "EmailConfirmed" boolean NOT NULL DEFAULT false,
    "PhoneNumberConfirmed" boolean NOT NULL DEFAULT false,
    "TwoFactorEnabled" boolean NOT NULL DEFAULT false,
    "FailedLoginAttempts" integer NOT NULL DEFAULT 0,
    "LastLoginAt" timestamp with time zone NULL,
    "RefreshToken" text NULL,
    "RefreshTokenExpiry" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" text NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "DeletedBy" text NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Users_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tenants_Subdomain" ON "Tenants" ("Subdomain");
CREATE INDEX IF NOT EXISTS "IX_Tenants_IsActive" ON "Tenants" ("IsActive");
CREATE INDEX IF NOT EXISTS "IX_Branches_TenantId" ON "Branches" ("TenantId");
CREATE INDEX IF NOT EXISTS "IX_Branches_IsActive" ON "Branches" ("IsActive");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Branches_Code" ON "Branches" ("Code");
CREATE INDEX IF NOT EXISTS "IX_Users_TenantId" ON "Users" ("TenantId");
CREATE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_UserName" ON "Users" ("UserName");
CREATE INDEX IF NOT EXISTS "IX_Users_IsActive" ON "Users" ("IsActive");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_TenantId_Email" ON "Users" ("TenantId", "Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_TenantId_UserName" ON "Users" ("TenantId", "UserName");

-- Create trigger to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_tenants_updated_at BEFORE UPDATE ON "Tenants" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_branches_updated_at BEFORE UPDATE ON "Branches" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON "Users" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
