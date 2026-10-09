-- Seed data for the pilot: roles, staff and their reporting lines.
--
-- This file is executed on every start-up after the EF Core migrations have been
-- applied, so every statement must be safe to run again. Ids are fixed and the
-- inserts are conflict-safe, which is what keeps a restart from duplicating rows.
--
-- The file contains no DDL and no DELETE: schema belongs to the migrations and
-- existing rows belong to whoever edited them.

-- Roles. Codes are the values that travel on the wire; names are the labels.
INSERT INTO roles (id, code, name) VALUES
    (1, 'Employee', 'Employee'),
    (2, 'Manager', 'Manager'),
    (3, 'FinanceOfficer', 'Finance Officer')
ON CONFLICT (id) DO NOTHING;

-- Top of the reporting line first, so the self-referencing foreign key resolves.
INSERT INTO employees (id, display_name, email, role_id, manager_id) VALUES
    (1, 'Ada Lovelace', 'ada.lovelace@example.com', 2, NULL)
ON CONFLICT (id) DO NOTHING;

-- Everybody else reports to an employee inserted above.
INSERT INTO employees (id, display_name, email, role_id, manager_id) VALUES
    (2, 'Grace Hopper', 'grace.hopper@example.com', 3, 1),
    (3, 'Alan Turing', 'alan.turing@example.com', 1, 1),
    (4, 'Katherine Johnson', 'katherine.johnson@example.com', 1, 1),
    (5, 'Margaret Hamilton', 'margaret.hamilton@example.com', 1, 2)
ON CONFLICT (id) DO NOTHING;

-- The explicit ids above bypass the identity sequences, so resynchronise them.
-- Without this a later generated insert would collide with a seeded row.
SELECT setval(
    pg_get_serial_sequence('roles', 'id'),
    GREATEST(COALESCE((SELECT MAX(id) FROM roles), 1), 1),
    true);

SELECT setval(
    pg_get_serial_sequence('employees', 'id'),
    GREATEST(COALESCE((SELECT MAX(id) FROM employees), 1), 1),
    true);
