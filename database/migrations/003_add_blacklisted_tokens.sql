-- Migration: Add BlacklistedToken entity for JWT token blacklisting
-- Created: 2025-12-24

-- Create BlacklistedTokens table
CREATE TABLE IF NOT EXISTS "BlacklistedTokens" (
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "TokenId" TEXT NOT NULL,
    "BlacklistedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Create index on TokenId for faster lookups
CREATE INDEX IF NOT EXISTS "IX_BlacklistedTokens_TokenId" ON "BlacklistedTokens" ("TokenId");

-- Create index on ExpiresAt for cleanup operations
CREATE INDEX IF NOT EXISTS "IX_BlacklistedTokens_ExpiresAt" ON "BlacklistedTokens" ("ExpiresAt");

-- Add comments
COMMENT ON TABLE "BlacklistedTokens" IS 'Stores blacklisted JWT tokens for enhanced security';
COMMENT ON COLUMN "BlacklistedTokens"."Id" IS 'Unique identifier for the blacklisted token record';
COMMENT ON COLUMN "BlacklistedTokens"."TokenId" IS 'The JWT ID (jti claim) of the blacklisted token';
COMMENT ON COLUMN "BlacklistedTokens"."BlacklistedAt" IS 'When the token was blacklisted';
COMMENT ON COLUMN "BlacklistedTokens"."ExpiresAt" IS 'When the token naturally expires (for cleanup purposes)';
