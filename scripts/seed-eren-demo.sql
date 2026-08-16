-- Seed demo data for eren34akgoz@gmail.com (ids injected by runner)

BEGIN;

-- ========== EXPENSE PLAN ==========
UPDATE plans
SET "Title" = 'Ev Ortak Giderleri',
    "Description" = 'Demo: Eren & Ayşe ortak fatura / market takibi',
    "UpdatedAtUtc" = NOW()
WHERE "Id" = :'ep';

DELETE FROM expense_shares WHERE "ExpenseId" IN (SELECT "Id" FROM expenses WHERE "PlanId" = :'ep');
DELETE FROM expenses WHERE "PlanId" = :'ep';
DELETE FROM expense_share_templates WHERE "RecurrenceId" IN (SELECT "Id" FROM expense_recurrences WHERE "PlanId" = :'ep');
DELETE FROM expense_recurrences WHERE "PlanId" = :'ep';
DELETE FROM settlement_transfers WHERE "PlanId" = :'ep';
UPDATE plan_members SET "PartnerId" = NULL WHERE "PlanId" = :'ep';
DELETE FROM partners WHERE "PlanId" = :'ep';

INSERT INTO partners ("Id","PlanId","Name","Color","DefaultPct","SortOrder","CreatedAtUtc","IsDeleted","LinkedUserId","InviteEmail","Iban")
VALUES
  ('11111111-1111-4111-8111-111111111101', :'ep', 'Eren', '#6366f1', 50, 0, NOW(), false, :'uid', 'eren34akgoz@gmail.com', NULL),
  ('11111111-1111-4111-8111-111111111102', :'ep', 'Ayşe', '#10b981', 50, 1, NOW(), false, NULL, NULL, NULL);

UPDATE plan_members
SET "PartnerId" = '11111111-1111-4111-8111-111111111101', "UpdatedAtUtc" = NOW()
WHERE "PlanId" = :'ep' AND "UserId" = :'uid' AND "IsDeleted" = false;

INSERT INTO expenses ("Id","PlanId","CategoryId","RecurrenceId","Name","OccurredOn","TotalAmount","ShareType","Status","PaidByPartnerId","Note","PeriodKey","CreatedAtUtc","IsDeleted")
VALUES
  ('22222222-2222-4222-8222-222222222201', :'ep', :'cat_fatura', NULL, 'Elektrik faturası', CURRENT_DATE - 12, 1200.00, 'Equal', 'Paid', '11111111-1111-4111-8111-111111111101', 'Eren ödedi — eşit paylaşım', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222202', :'ep', :'cat_market', NULL, 'Haftalık market', CURRENT_DATE - 5, 860.00, 'Equal', 'Paid', '11111111-1111-4111-8111-111111111102', 'Ayşe ödedi', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222203', :'ep', :'cat_mutfak', NULL, 'Yemek siparişi', CURRENT_DATE - 3, 420.00, 'Equal', 'Paid', '11111111-1111-4111-8111-111111111101', '', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222204', :'ep', :'cat_ulasim', NULL, 'Uber / taksi', CURRENT_DATE - 2, 180.00, 'Equal', 'Paid', '11111111-1111-4111-8111-111111111102', '', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222205', :'ep', :'cat_fatura', NULL, 'İnternet (Eren kişisel)', CURRENT_DATE - 8, 450.00, 'Custom', 'Paid', '11111111-1111-4111-8111-111111111101', 'Sadece Eren payı — borç yok', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222206', :'ep', :'cat_fatura', NULL, 'Su faturası', CURRENT_DATE + 5, 340.00, 'Equal', 'Planned', NULL, 'Gelecek dönem', NULL, NOW(), false),
  ('22222222-2222-4222-8222-222222222207', :'ep', :'cat_diger', NULL, 'Temizlik malzemesi', CURRENT_DATE - 1, 250.00, 'Default', 'Paid', '11111111-1111-4111-8111-111111111101', 'Varsayılan %50/%50', NULL, NOW(), false);

INSERT INTO expense_shares ("Id","ExpenseId","PartnerId","Amount") VALUES
  ('33333333-3333-4333-8333-333333333301', '22222222-2222-4222-8222-222222222205', '11111111-1111-4111-8111-111111111101', 450.00),
  ('33333333-3333-4333-8333-333333333302', '22222222-2222-4222-8222-222222222205', '11111111-1111-4111-8111-111111111102', 0.00);

INSERT INTO expense_recurrences (
  "Id","PlanId","CategoryId","Name","TotalAmount","ShareType","DefaultPaidByPartnerId",
  "Frequency","AnchorDay","StartDate","EndDate","NextOccurrence","IsActive","Note","CreatedAtUtc","IsDeleted"
) VALUES (
  '44444444-4444-4444-8444-444444444401', :'ep', :'cat_fatura', 'Aylık elektrik', 1100.00, 'Equal',
  '11111111-1111-4111-8111-111111111101', 'Monthly', 15,
  DATE_TRUNC('month', CURRENT_DATE)::date, NULL,
  DATE_TRUNC('month', CURRENT_DATE)::date + 14,
  true, 'Her ay 15inde otomatik oluşur', NOW(), false
);

INSERT INTO settlement_transfers (
  "Id","PlanId","FromPartnerId","ToPartnerId","Amount","TransferredOn","Note","CreatedAtUtc","IsDeleted"
) VALUES (
  '55555555-5555-4555-8555-555555555501', :'ep',
  '11111111-1111-4111-8111-111111111102',
  '11111111-1111-4111-8111-111111111101',
  400.00, CURRENT_DATE - 1, 'Demo mahsup — Ayşe → Eren', NOW(), false
);

-- ========== INSTALLMENT PLAN ==========
UPDATE plans
SET "Title" = 'Fuzul Ev Konut Projesi',
    "Description" = 'Demo taksit planı — Eren & Yusuf',
    "PlanType" = 'Installment',
    "UpdatedAtUtc" = NOW()
WHERE "Id" = :'ip';

UPDATE plan_members SET "PartnerId" = NULL WHERE "PlanId" = :'ip';
DELETE FROM payments WHERE "InstallmentId" IN (SELECT "Id" FROM installments WHERE "PlanId" = :'ip');
DELETE FROM installment_shares WHERE "InstallmentId" IN (SELECT "Id" FROM installments WHERE "PlanId" = :'ip');
UPDATE plans SET "DeliveryInstallmentId" = NULL WHERE "Id" = :'ip';
DELETE FROM installments WHERE "PlanId" = :'ip';
DELETE FROM partners WHERE "PlanId" = :'ip';

INSERT INTO partners ("Id","PlanId","Name","Color","DefaultPct","SortOrder","CreatedAtUtc","IsDeleted","LinkedUserId","InviteEmail","Iban")
VALUES
  ('66666666-6666-4666-8666-666666666601', :'ip', 'Eren', '#6366f1', 50, 0, NOW(), false, :'uid', 'eren34akgoz@gmail.com', NULL),
  ('66666666-6666-4666-8666-666666666602', :'ip', 'Yusuf', '#f59e0b', 50, 1, NOW(), false, NULL, NULL, NULL);

UPDATE plan_members
SET "PartnerId" = '66666666-6666-4666-8666-666666666601', "UpdatedAtUtc" = NOW()
WHERE "PlanId" = :'ip' AND "UserId" = :'uid' AND "IsDeleted" = false;

-- 6 demo installments
INSERT INTO installments ("Id","PlanId","Name","DueDate","TotalAmount","ShareType","SortOrder","CreatedAtUtc","IsDeleted")
VALUES
  ('77777777-7777-4777-8777-777777777701', :'ip', '1. Tasarruf Taksiti', CURRENT_DATE - 90, 15000.00, 'Default', 0, NOW(), false),
  ('77777777-7777-4777-8777-777777777702', :'ip', '2. Tasarruf Taksiti', CURRENT_DATE - 60, 15000.00, 'Default', 1, NOW(), false),
  ('77777777-7777-4777-8777-777777777703', :'ip', '3. Tasarruf Taksiti', CURRENT_DATE - 30, 15000.00, 'Default', 2, NOW(), false),
  ('77777777-7777-4777-8777-777777777704', :'ip', '4. Tasarruf Taksiti', CURRENT_DATE, 15000.00, 'Default', 3, NOW(), false),
  ('77777777-7777-4777-8777-777777777705', :'ip', '5. Tasarruf Taksiti', CURRENT_DATE + 30, 15000.00, 'Default', 4, NOW(), false),
  ('77777777-7777-4777-8777-777777777706', :'ip', '6. Tasarruf Taksiti', CURRENT_DATE + 60, 15000.00, 'Equal', 5, NOW(), false);

UPDATE plans SET "DeliveryInstallmentId" = '77777777-7777-4777-8777-777777777704' WHERE "Id" = :'ip';

-- Paid first 3 installments for both partners (self-paid)
INSERT INTO payments ("Id","InstallmentId","PartnerId","IsPaid","PaidAt","PaidByPartnerId","Note","CreatedAtUtc","IsDeleted","ReviewStatus")
SELECT gen_random_uuid(), i."Id", p."Id", true, i."DueDate", p."Id", 'Demo ödeme', NOW(), false, 'None'
FROM installments i
CROSS JOIN partners p
WHERE i."PlanId" = :'ip' AND p."PlanId" = :'ip'
  AND i."SortOrder" < 3;

-- Partial: 4th installment — only Eren paid
INSERT INTO payments ("Id","InstallmentId","PartnerId","IsPaid","PaidAt","PaidByPartnerId","Note","CreatedAtUtc","IsDeleted","ReviewStatus")
VALUES
  (gen_random_uuid(), '77777777-7777-4777-8777-777777777704', '66666666-6666-4666-8666-666666666601', true, CURRENT_DATE, '66666666-6666-4666-8666-666666666601', 'Eren ödedi', NOW(), false, 'None'),
  (gen_random_uuid(), '77777777-7777-4777-8777-777777777704', '66666666-6666-4666-8666-666666666602', false, NULL, NULL, '', NOW(), false, 'None');

COMMIT;

SELECT 'expense_partners' AS k, count(*)::text FROM partners WHERE "PlanId" = :'ep' AND "IsDeleted"=false
UNION ALL SELECT 'expenses', count(*)::text FROM expenses WHERE "PlanId" = :'ep' AND "IsDeleted"=false
UNION ALL SELECT 'recurrences', count(*)::text FROM expense_recurrences WHERE "PlanId" = :'ep' AND "IsDeleted"=false
UNION ALL SELECT 'transfers', count(*)::text FROM settlement_transfers WHERE "PlanId" = :'ep' AND "IsDeleted"=false
UNION ALL SELECT 'inst_partners', count(*)::text FROM partners WHERE "PlanId" = :'ip' AND "IsDeleted"=false
UNION ALL SELECT 'installments', count(*)::text FROM installments WHERE "PlanId" = :'ip' AND "IsDeleted"=false
UNION ALL SELECT 'payments', count(*)::text FROM payments p JOIN installments i ON i."Id"=p."InstallmentId" WHERE i."PlanId" = :'ip';
