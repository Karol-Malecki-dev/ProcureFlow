import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import path from 'node:path';

export const purchaseRequestFixture = {
  organizationId: '00000000-0000-0000-0000-000000000501',
  branchId: '00000000-0000-0000-0000-000000000502',
  unitId: '00000000-0000-0000-0000-000000000503',
  productId: '00000000-0000-0000-0000-000000000504',
};

interface PurchaseRequestFixtureUsers {
  employeeEmail: string;
  managerEmail: string;
  procurementEmail: string;
}

function sqlLiteral(value: string): string {
  return `'${value.replaceAll("'", "''")}'`;
}

export function seedPurchaseRequestFixture(users: PurchaseRequestFixtureUsers) {
  const now = new Date().toISOString();
  const year = new Date().getUTCFullYear();
  const month = new Date().getUTCMonth() + 1;
  const budgetId = randomUUID();
  const employeeMembershipId = randomUUID();
  const managerMembershipId = randomUUID();
  const procurementMembershipId = randomUUID();
  const organizationId = sqlLiteral(purchaseRequestFixture.organizationId);
  const branchId = sqlLiteral(purchaseRequestFixture.branchId);
  const unitId = sqlLiteral(purchaseRequestFixture.unitId);
  const productId = sqlLiteral(purchaseRequestFixture.productId);
  const employeeEmail = sqlLiteral(users.employeeEmail);
  const managerEmail = sqlLiteral(users.managerEmail);
  const procurementEmail = sqlLiteral(users.procurementEmail);

  const sql = `
BEGIN;

DO $$
BEGIN
  IF (SELECT COUNT(*) FROM "Users" WHERE "Email" IN (${employeeEmail}, ${managerEmail}, ${procurementEmail})) <> 3 THEN
    RAISE EXCEPTION 'Expected all purchase-request E2E users to exist before fixture setup';
  END IF;
END $$;

INSERT INTO "Organizations" (
  "Id", "Name", "Address_Street", "Address_BuildingNumber", "Address_ApartmentNumber",
  "Address_City", "Address_PostalCode", "Address_Country", "Code", "Bio", "IsArchived")
VALUES (
  ${organizationId}, 'ProcureFlow E2E Organization', 'E2E Street', '1', NULL,
  'Warsaw', '00-001', 'Poland', 'PF-E2E-ORG', 'Deterministic browser fixture', false)
ON CONFLICT ("Id") DO UPDATE SET
  "Name" = EXCLUDED."Name",
  "Code" = EXCLUDED."Code",
  "IsArchived" = false;

INSERT INTO "Branches" (
  "Id", "Name", "Address_Street", "Address_BuildingNumber", "Address_ApartmentNumber",
  "Address_City", "Address_PostalCode", "Address_Country", "Code", "IsArchived", "OrganizationId")
VALUES (
  ${branchId}, 'E2E Branch', 'E2E Street', '1', NULL,
  'Warsaw', '00-001', 'Poland', 'PF-E2E-BRANCH', false, ${organizationId})
ON CONFLICT ("Id") DO UPDATE SET
  "Name" = EXCLUDED."Name",
  "Code" = EXCLUDED."Code",
  "IsArchived" = false,
  "OrganizationId" = EXCLUDED."OrganizationId";

INSERT INTO "UnitOfMeasures" (
  "Id", "OrganizationId", "Name", "Symbol", "IsActive", "CreatedAt", "UpdatedAt",
  "CreatedByUserId", "UpdatedByUserId")
SELECT
  ${unitId}, ${organizationId}, 'Piece', 'pc', true, ${sqlLiteral(now)}, ${sqlLiteral(now)},
  "Id", "Id"
FROM "Users"
WHERE "Email" = ${procurementEmail}
ON CONFLICT ("Id") DO UPDATE SET
  "OrganizationId" = EXCLUDED."OrganizationId",
  "Name" = EXCLUDED."Name",
  "Symbol" = EXCLUDED."Symbol",
  "IsActive" = true,
  "UpdatedAt" = EXCLUDED."UpdatedAt",
  "UpdatedByUserId" = EXCLUDED."UpdatedByUserId";

INSERT INTO "Products" (
  "Id", "OrganizationId", "Name", "Code", "UnitOfMeasureId", "UnitPrice",
  "IsAvailable", "IsActive", "CreatedAt", "UpdatedAt", "CreatedByUserId", "UpdatedByUserId")
SELECT
  ${productId}, ${organizationId}, 'E2E Monitor', 'PF-E2E-MONITOR', ${unitId}, 10.50,
  true, true, ${sqlLiteral(now)}, ${sqlLiteral(now)}, "Id", "Id"
FROM "Users"
WHERE "Email" = ${procurementEmail}
ON CONFLICT ("Id") DO UPDATE SET
  "OrganizationId" = EXCLUDED."OrganizationId",
  "Name" = EXCLUDED."Name",
  "Code" = EXCLUDED."Code",
  "UnitOfMeasureId" = EXCLUDED."UnitOfMeasureId",
  "UnitPrice" = EXCLUDED."UnitPrice",
  "IsAvailable" = true,
  "IsActive" = true,
  "UpdatedAt" = EXCLUDED."UpdatedAt",
  "UpdatedByUserId" = EXCLUDED."UpdatedByUserId";

INSERT INTO "BranchMonthlyBudgets" (
  "Id", "BranchId", "Year", "Month", "LimitAmount", "UsedAmount", "ConcurrencyStamp")
VALUES (${sqlLiteral(budgetId)}, ${branchId}, ${year}, ${month}, 100000.00, 0.00, ${sqlLiteral(randomUUID())})
ON CONFLICT ("BranchId", "Year", "Month") DO UPDATE SET
  "LimitAmount" = EXCLUDED."LimitAmount",
  "UsedAmount" = 0.00,
  "ConcurrencyStamp" = EXCLUDED."ConcurrencyStamp";

DELETE FROM "Memberships"
WHERE "UserId" IN (
  SELECT "Id" FROM "Users" WHERE "Email" IN (${employeeEmail}, ${managerEmail}, ${procurementEmail})
);

INSERT INTO "Memberships" ("Id", "OrganizationId", "UserId", "BranchId", "Role", "IsActive")
SELECT ${sqlLiteral(employeeMembershipId)}, ${organizationId}, "Id", ${branchId}, 1, true
FROM "Users" WHERE "Email" = ${employeeEmail};

INSERT INTO "Memberships" ("Id", "OrganizationId", "UserId", "BranchId", "Role", "IsActive")
SELECT ${sqlLiteral(managerMembershipId)}, ${organizationId}, "Id", ${branchId}, 2, true
FROM "Users" WHERE "Email" = ${managerEmail};

INSERT INTO "Memberships" ("Id", "OrganizationId", "UserId", "BranchId", "Role", "IsActive")
SELECT ${sqlLiteral(procurementMembershipId)}, ${organizationId}, "Id", NULL, 3, true
FROM "Users" WHERE "Email" = ${procurementEmail};

COMMIT;
`;

  execFileSync(
    'docker',
    [
      'compose',
      'exec',
      '-T',
      'db',
      'psql',
      '-U',
      process.env.POSTGRES_USER ?? 'postgres',
      '-d',
      process.env.POSTGRES_DB ?? 'dotnetreact',
      '-v',
      'ON_ERROR_STOP=1',
      '-c',
      sql,
    ],
    {
      cwd: process.env.E2E_REPOSITORY_ROOT ?? path.resolve(process.cwd(), '..'),
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
    },
  );

  return purchaseRequestFixture;
}