-- Adds a realistic installment dashboard fixture for a known local user.
-- It is idempotent and never alters existing plans or data.
DO $$
DECLARE
  target_email constant text := 'hasanerenakgz@gmail.com';
  target_user_id text;
  plan_id uuid := gen_random_uuid();
  owner_partner_id uuid := gen_random_uuid();
  co_partner_id uuid := gen_random_uuid();
  delivery_installment_id uuid := gen_random_uuid();
  existing_plan_id uuid;
BEGIN
  SELECT "Id" INTO target_user_id
  FROM "AspNetUsers"
  WHERE lower("Email") = lower(target_email);

  IF target_user_id IS NULL THEN
    RAISE EXCEPTION 'User % was not found.', target_email;
  END IF;

  SELECT "Id" INTO existing_plan_id
  FROM plans
  WHERE "OwnerUserId" = target_user_id
    AND "Title" = 'Demo · Ev Finansmanı'
    AND NOT "IsDeleted";

  IF existing_plan_id IS NOT NULL THEN
    RAISE NOTICE 'Demo plan already exists: %', existing_plan_id;
    RETURN;
  END IF;

  INSERT INTO plans (
    "Id", "OwnerUserId", "PlanType", "Title", "Description", "DeliveryInstallmentId",
    "CreatedAtUtc", "IsDeleted", "IbanMode", "RequireReceipt", "RemindersEnabled",
    "ReminderDaysBefore", "ReminderDaysAfter"
  ) VALUES (
    plan_id, target_user_id, 'Installment', 'Demo · Ev Finansmanı',
    'TDD ve arayüz kontrolleri için gerçekçi ödeme, vade ve mahsuplaşma verisi.', delivery_installment_id,
    now(), false, 'None', false, true, ARRAY[7, 3, 1], ARRAY[1, 3]
  );

  INSERT INTO partners (
    "Id", "PlanId", "Name", "Color", "DefaultPct", "SortOrder", "LinkedUserId", "CreatedAtUtc", "IsDeleted"
  ) VALUES
    (owner_partner_id, plan_id, 'Yusuf', '#38bdf8', 60.00, 0, target_user_id, now(), false),
    (co_partner_id, plan_id, 'Deniz', '#f59e0b', 40.00, 1, null, now(), false);

  INSERT INTO plan_members (
    "Id", "PlanId", "UserId", "Role", "PartnerId", "CreatedAtUtc", "IsDeleted"
  ) VALUES (
    gen_random_uuid(), plan_id, target_user_id, 'Owner', owner_partner_id, now(), false
  );

  INSERT INTO installments (
    "Id", "PlanId", "Name", "DueDate", "TotalAmount", "ShareType", "SortOrder", "CreatedAtUtc", "IsDeleted"
  ) VALUES
    (gen_random_uuid(), plan_id, 'Peşinat', current_date - 75, 15000.00, 'Default', 1, now(), false),
    (gen_random_uuid(), plan_id, '1. Aylık Taksit', current_date - 45, 9500.00, 'Default', 2, now(), false),
    (gen_random_uuid(), plan_id, '2. Aylık Taksit', current_date - 15, 9500.00, 'Default', 3, now(), false),
    (delivery_installment_id, plan_id, 'Teslimat Ödemesi', current_date + 5, 12000.00, 'Default', 4, now(), false),
    (gen_random_uuid(), plan_id, '3. Aylık Taksit', current_date + 35, 9500.00, 'Default', 5, now(), false),
    (gen_random_uuid(), plan_id, '4. Aylık Taksit', current_date + 65, 9500.00, 'Default', 6, now(), false);

  INSERT INTO payments (
    "Id", "InstallmentId", "PartnerId", "IsPaid", "PaidAt", "PaidByPartnerId", "Note",
    "CreatedAtUtc", "IsDeleted", "ReviewStatus", "ReviewedAtUtc", "ReviewedByUserId"
  )
  SELECT
    gen_random_uuid(), i."Id", partner.id,
    CASE
      WHEN i."SortOrder" <= 2 THEN true
      WHEN i."SortOrder" = 3 AND partner.id = owner_partner_id THEN true
      ELSE false
    END,
    CASE
      WHEN i."SortOrder" <= 2 THEN i."DueDate" - 2
      WHEN i."SortOrder" = 3 AND partner.id = owner_partner_id THEN i."DueDate" - 1
      ELSE null
    END,
    CASE
      WHEN i."SortOrder" <= 2 THEN partner.id
      WHEN i."SortOrder" = 3 AND partner.id = owner_partner_id THEN owner_partner_id
      ELSE null
    END,
    CASE
      WHEN i."SortOrder" <= 2 THEN 'Demo: tamamlandı'
      WHEN i."SortOrder" = 3 AND partner.id = owner_partner_id THEN 'Demo: Yusuf payını ödedi'
      ELSE 'Demo: ödeme bekleniyor'
    END,
    now(), false,
    CASE WHEN i."SortOrder" <= 2 THEN 'Approved' ELSE 'None' END,
    CASE WHEN i."SortOrder" <= 2 THEN now() ELSE null END,
    CASE WHEN i."SortOrder" <= 2 THEN target_user_id ELSE null END
  FROM installments i
  CROSS JOIN (VALUES (owner_partner_id), (co_partner_id)) AS partner(id)
  WHERE i."PlanId" = plan_id;
END $$;

-- Companion expense plan so the expense-tracking screens also have meaningful states.
DO $$
DECLARE
  target_email constant text := 'hasanerenakgz@gmail.com';
  target_user_id text;
  plan_id uuid := gen_random_uuid();
  owner_partner_id uuid := gen_random_uuid();
  co_partner_id uuid := gen_random_uuid();
  existing_plan_id uuid;
BEGIN
  SELECT "Id" INTO target_user_id
  FROM "AspNetUsers"
  WHERE lower("Email") = lower(target_email);

  IF target_user_id IS NULL THEN
    RAISE EXCEPTION 'User % was not found.', target_email;
  END IF;

  SELECT "Id" INTO existing_plan_id
  FROM plans
  WHERE "OwnerUserId" = target_user_id
    AND "Title" = 'Demo · Ortak Yaşam Giderleri'
    AND NOT "IsDeleted";

  IF existing_plan_id IS NOT NULL THEN
    RAISE NOTICE 'Demo expense plan already exists: %', existing_plan_id;
    RETURN;
  END IF;

  INSERT INTO plans (
    "Id", "OwnerUserId", "PlanType", "Title", "Description", "CreatedAtUtc", "IsDeleted",
    "IbanMode", "RequireReceipt", "RemindersEnabled", "ReminderDaysBefore", "ReminderDaysAfter"
  ) VALUES (
    plan_id, target_user_id, 'Expense', 'Demo · Ortak Yaşam Giderleri',
    'Gider tablosu, kategori dağılımı ve mahsuplaşma için örnek veri.', now(), false,
    'None', false, false, ARRAY[]::integer[], ARRAY[]::integer[]
  );

  INSERT INTO partners (
    "Id", "PlanId", "Name", "Color", "DefaultPct", "SortOrder", "LinkedUserId", "CreatedAtUtc", "IsDeleted"
  ) VALUES
    (owner_partner_id, plan_id, 'Yusuf', '#38bdf8', 60.00, 0, target_user_id, now(), false),
    (co_partner_id, plan_id, 'Deniz', '#f59e0b', 40.00, 1, null, now(), false);

  INSERT INTO plan_members (
    "Id", "PlanId", "UserId", "Role", "PartnerId", "CreatedAtUtc", "IsDeleted"
  ) VALUES (
    gen_random_uuid(), plan_id, target_user_id, 'Owner', owner_partner_id, now(), false
  );

  INSERT INTO expense_categories ("Id", "PlanId", "Name", "Color", "SortOrder", "CreatedAtUtc", "IsDeleted") VALUES
    (gen_random_uuid(), plan_id, 'Ev', '#818cf8', 0, now(), false),
    (gen_random_uuid(), plan_id, 'Market', '#34d399', 1, now(), false),
    (gen_random_uuid(), plan_id, 'Fatura', '#f59e0b', 2, now(), false),
    (gen_random_uuid(), plan_id, 'Sosyal', '#f472b6', 3, now(), false);

  INSERT INTO expenses (
    "Id", "PlanId", "CreatedByUserId", "CategoryId", "Name", "OccurredOn", "TotalAmount",
    "ShareType", "Status", "PaidByPartnerId", "Note", "CreatedAtUtc", "IsDeleted"
  )
  SELECT
    gen_random_uuid(), plan_id, target_user_id, category."Id", item.name, item.occurred_on, item.total_amount,
    'Default', item.status, CASE WHEN item.status = 'Paid' THEN owner_partner_id ELSE null END,
    item.note, now(), false
  FROM (
    VALUES
      ('Ev', 'Ağustos kirası', current_date - 30, 18500.00, 'Paid', 'Demo: Yusuf ödedi'),
      ('Fatura', 'Elektrik ve su', current_date - 18, 1450.00, 'Paid', 'Demo: ortak fatura'),
      ('Market', 'Haftalık market', current_date - 11, 2375.50, 'Paid', 'Demo: mutfak alışverişi'),
      ('Sosyal', 'Hafta sonu etkinliği', current_date - 7, 1280.00, 'Paid', 'Demo: ortak aktivite'),
      ('Market', 'Aylık market bütçesi', current_date - 2, 3200.00, 'Planned', 'Demo: planlanan alışveriş'),
      ('Fatura', 'İnternet faturası', current_date + 3, 650.00, 'Planned', 'Demo: yaklaşan fatura'),
      ('Ev', 'Apartman aidatı', current_date + 8, 1750.00, 'Planned', 'Demo: yaklaşan gider')
  ) AS item(category_name, name, occurred_on, total_amount, status, note)
  JOIN expense_categories category ON category."PlanId" = plan_id AND category."Name" = item.category_name;

  INSERT INTO expense_payments ("Id", "ExpenseId", "PartnerId", "Amount")
  SELECT gen_random_uuid(), expense."Id", owner_partner_id, expense."TotalAmount"
  FROM expenses expense
  WHERE expense."PlanId" = plan_id AND expense."Status" = 'Paid';
END $$;
