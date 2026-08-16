BEGIN;

-- Fix empty PlanType on installment plan
UPDATE plans SET "PlanType" = 'Installment', "UpdatedAtUtc" = NOW()
WHERE "Id" = '00e18cdc-2406-4a8d-874c-538f7906920f' AND COALESCE("PlanType", '') = '';

-- Demo partners for expense plan
INSERT INTO partners ("Id","PlanId","Name","Color","DefaultPct","SortOrder","CreatedAtUtc","IsDeleted","LinkedUserId","InviteEmail","Iban")
VALUES
  ('11111111-1111-4111-8111-111111111101', 'bd654c05-f007-4973-babc-01f16d966945', 'Eren', '#6366f1', 50, 0, NOW(), false, 'a3f6a26e-cff0-42b8-a5df-5046cbbac2e9', 'eren34akgoz@gmail.com', NULL),
  ('11111111-1111-4111-8111-111111111102', 'bd654c05-f007-4973-babc-01f16d966945', 'Ayşe', '#10b981', 50, 1, NOW(), false, NULL, NULL, NULL)
ON CONFLICT ("Id") DO NOTHING;

-- Link owner member to Eren partner
UPDATE plan_members
SET "PartnerId" = '11111111-1111-4111-8111-111111111101', "UpdatedAtUtc" = NOW()
WHERE "PlanId" = 'bd654c05-f007-4973-babc-01f16d966945'
  AND "UserId" = 'a3f6a26e-cff0-42b8-a5df-5046cbbac2e9'
  AND "IsDeleted" = false;

-- Rename plan for clarity
UPDATE plans
SET "Title" = 'Ev Ortak Giderleri',
    "Description" = 'Demo: Eren & Ayşe ortak fatura / market takibi',
    "UpdatedAtUtc" = NOW()
WHERE "Id" = 'bd654c05-f007-4973-babc-01f16d966945';

-- Clear previous demo rows if re-run
DELETE FROM expense_shares WHERE "ExpenseId" IN (
  SELECT "Id" FROM expenses WHERE "PlanId" = 'bd654c05-f007-4973-babc-01f16d966945'
);
DELETE FROM expenses WHERE "PlanId" = 'bd654c05-f007-4973-babc-01f16d966945';
DELETE FROM expense_recurrences WHERE "PlanId" = 'bd654c05-f007-4973-babc-01f16d966945';
DELETE FROM settlement_transfers WHERE "PlanId" = 'bd654c05-f007-4973-babc-01f16d966945';

-- Paid / planned expenses
INSERT INTO expenses ("Id","PlanId","CategoryId","RecurrenceId","Name","OccurredOn","TotalAmount","ShareType","Status","PaidByPartnerId","Note","PeriodKey","CreatedAtUtc","IsDeleted")
VALUES
  ('22222222-2222-4222-8222-222222222201', 'bd654c05-f007-4973-babc-01f16d966945', 'a1affd86-f6da-4e05-8544-271db1f2713f', NULL, 'Elektrik faturası', CURRENT_DATE - 12, 1200.00, 'Equal', 'Paid', '11111111-1111-4111-8111-111111111101', 'Eren ödedi — eşit paylaşım', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222202', 'bd654c05-f007-4973-babc-01f16d966945', '9d60e426-fb15-4b1e-a54b-5d2c787357bb', NULL, 'Haftalık market', CURRENT_DATE - 5, 860.00, 'Equal', 'Paid', '11111111-1111-4111-8111-111111111102', 'Ayşe ödedi', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222203', 'bd654c05-f007-4973-babc-01f16d966945', '5ecf10b3-f1ac-44d6-8c29-e60152750a0c', NULL, 'Yemek siparişi', CURRENT_DATE - 3, 420.00, 'Equal', 'Paid', '11111111-1111-4111-8111-111111111101', '', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222204', 'bd654c05-f007-4973-babc-01f16d966945', '9515c876-7a84-429a-9e58-fb376d0cc69e', NULL, 'Uber / taksi', CURRENT_DATE - 2, 180.00, 'Equal', 'Paid', '11111111-1111-4111-8111-111111111102', '', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222205', 'bd654c05-f007-4973-babc-01f16d966945', 'a1affd86-f6da-4e05-8544-271db1f2713f', NULL, 'İnternet (Eren kişisel)', CURRENT_DATE - 8, 450.00, 'Custom', 'Paid', '11111111-1111-4111-8111-111111111101', 'Sadece Eren payı — borç yok', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222206', 'bd654c05-f007-4973-babc-01f16d966945', 'a1affd86-f6da-4e05-8544-271db1f2713f', NULL, 'Su faturası', CURRENT_DATE + 5, 340.00, 'Equal', 'Planned', NULL, 'Gelecek dönem', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222207', 'bd654c05-f007-4973-babc-01f16d966945', '7977bc34-4f6b-4f22-bba3-dde68b234f76', NULL, 'Temizlik malzemesi', CURRENT_DATE - 1, 250.00, 'Default', 'Paid', '11111111-1111-4111-8111-111111111101', 'Varsayılan %50/%50', NULL, NOW(), false);

INSERT INTO expense_shares ("Id","ExpenseId","PartnerId","Amount") VALUES
  ('33333333-3333-4333-8333-333333333301', '22222222-2222-4222-8222-222222222205', '11111111-1111-4111-8111-111111111101', 450.00),
  ('33333333-3333-4333-8333-333333333302', '22222222-2222-4222-8222-222222222205', '11111111-1111-4111-8111-111111111102', 0.00);

INSERT INTO expense_recurrences (
  "Id","PlanId","CategoryId","Name","TotalAmount","ShareType","DefaultPaidByPartnerId",
  "Frequency","AnchorDay","StartDate","EndDate","NextOccurrence","IsActive","Note","CreatedAtUtc","IsDeleted"
) VALUES (
  '44444444-4444-4444-8444-444444444401',
  'bd654c05-f007-4973-babc-01f16d966945',
  'a1affd86-f6da-4e05-8544-271db1f2713f',
  'Aylık elektrik',
  1100.00,
  'Equal',
  '11111111-1111-4111-8111-111111111101',
  'Monthly',
  15,
  DATE_TRUNC('month', CURRENT_DATE)::date,
  NULL,
  DATE_TRUNC('month', CURRENT_DATE)::date + 14,
  true,
  'Her ay 15inde otomatik oluşur',
  NOW(),
  false
);

INSERT INTO settlement_transfers (
  "Id","PlanId","FromPartnerId","ToPartnerId","Amount","TransferredOn","Note","CreatedAtUtc","IsDeleted"
) VALUES (
  '55555555-5555-4555-8555-555555555501',
  'bd654c05-f007-4973-babc-01f16d966945',
  '11111111-1111-4111-8111-111111111102',
  '11111111-1111-4111-8111-111111111101',
  400.00,
  CURRENT_DATE - 1,
  'Demo mahsup — Ayşe → Eren',
  NOW(),
  false
);

COMMIT;

SELECT 'partners' AS kind, count(*)::text AS n FROM partners WHERE "PlanId"='bd654c05-f007-4973-babc-01f16d966945' AND "IsDeleted"=false
UNION ALL SELECT 'expenses', count(*)::text FROM expenses WHERE "PlanId"='bd654c05-f007-4973-babc-01f16d966945' AND "IsDeleted"=false
UNION ALL SELECT 'recurrences', count(*)::text FROM expense_recurrences WHERE "PlanId"='bd654c05-f007-4973-babc-01f16d966945' AND "IsDeleted"=false
UNION ALL SELECT 'transfers', count(*)::text FROM settlement_transfers WHERE "PlanId"='bd654c05-f007-4973-babc-01f16d966945' AND "IsDeleted"=false;
